using TMPro;
using UnityEngine;

/// <summary>Opt-in positional objective for the controlled Ladybird. No hand/controller trigger callbacks or progress writes.</summary>
[DefaultExecutionOrder(500)]
public sealed class LadybirdHabitatObjective : MonoBehaviour
{
    public LadybirdPlayerController controller;
    public LadybirdEnvironmentTransition transition;
    public Transform habitatTarget;
    [Min(.05f)] public float destinationRadius = .45f;
    [Min(.01f)] public float arrivalHeightTolerance = .15f;
    [Tooltip("Run this objective during the existing independent Editor movement bypass. Has no effect in builds.")]
    public bool enableInEditorMovementTest;
    public GameObject objectivePanel, continueButton, destinationMarker;
    public TMP_Text objectiveText;
    [Header("Optional Prototype2b arrival card")]
    public GameObject arrivalCard;
    public UnityEngine.Events.UnityEvent foodDiscoveryStarted = new UnityEngine.Events.UnityEvent();
    public bool FoodDiscoveryStarted { get; private set; }
    public bool Arrived { get; private set; }
    public const string FoodDiscoveryMessage = "Let's discover what ladybirds eat!";
    private void Awake() { if (arrivalCard != null) arrivalCard.SetActive(false); }
    public bool AttemptActive => attemptActive;
    private bool attemptActive;
    private int resetVersion;
    public const string NavigationPrompt = "Your ladybird is ready to explore! Guide it towards the leafy habitat to discover where it finds food.";
    public const string ArrivalMessage = "Habitat Found! You've reached a place where ladybirds can search for food.";

    private bool Eligible()
    {
        if (controller == null || !controller.ControlActive) return false;
#if UNITY_EDITOR
        if (controller.EditorTestActive) return enableInEditorMovementTest;
#endif
        return transition != null && transition.CurrentMode == LadybirdEnvironmentTransition.Mode.Environment;
    }
    private void LateUpdate() { Evaluate(); }
    public void Evaluate()
    {
        if (!Eligible()) { EndAttempt(); return; }
        if (habitatTarget == null || controller.groundContact == null || controller.startingPoint == null)
        {
            if (objectivePanel != null) objectivePanel.SetActive(true);
            if (objectiveText != null) objectiveText.text = "Habitat setup is incomplete. Return to Body Exploration or reset the Editor test.";
            return;
        }
        if (!attemptActive || resetVersion != controller.ResetVersion) BeginAttempt();
        if (Arrived || !InsideArrivalRegion(controller.groundContact.position)) return;
        // Live safety still has authority: arriving at an unsupported or obstructed contact never succeeds.
        Physics.SyncTransforms();
        if (!controller.safety.TryPosition(controller.groundSurfaces, controller.groundContact.position,
                controller.startingPoint.position, controller.groundContact.position.y, out var ground, out _) ||
            !controller.safety.PathIsClear(controller.transform, ground, Vector3.forward, 0f, out _, controller.groundSurfaces)) return;
        Arrived = true;
        controller.SetHabitatStopped(true);
        if (arrivalCard != null)
        {
            if (objectivePanel != null) objectivePanel.SetActive(false);
            arrivalCard.SetActive(true);
        }
        if (objectiveText != null) objectiveText.text = ArrivalMessage;
        if (continueButton != null) continueButton.SetActive(true);
        if (destinationMarker != null) destinationMarker.SetActive(false);
    }
    public bool InsideArrivalRegion(Vector3 contact)
    {
        if (habitatTarget == null) return false;
        var delta = contact - habitatTarget.position;
        return Mathf.Abs(delta.y) <= arrivalHeightTolerance && new Vector2(delta.x, delta.z).sqrMagnitude <= destinationRadius * destinationRadius;
    }
    private void BeginAttempt()
    {
        attemptActive = true; Arrived = false; FoodDiscoveryStarted = false; resetVersion = controller.ResetVersion;
        if (arrivalCard != null) arrivalCard.SetActive(false);
        controller.SetHabitatStopped(false);
        if (objectivePanel != null) objectivePanel.SetActive(true);
        if (continueButton != null) continueButton.SetActive(false);
        if (destinationMarker != null) destinationMarker.SetActive(true);
        if (objectiveText != null) objectiveText.text = NavigationPrompt;
    }
    public void ContinueToFoodDiscovery()
    {
        if (!attemptActive || !Arrived || !Eligible() || FoodDiscoveryStarted) return;
        FoodDiscoveryStarted = true;
        if (arrivalCard != null) arrivalCard.SetActive(false);
        if (continueButton != null) continueButton.SetActive(false);
        if (objectivePanel != null) objectivePanel.SetActive(true);
        if (objectiveText != null) objectiveText.text = FoodDiscoveryMessage;
        foodDiscoveryStarted.Invoke();
    }
    private void EndAttempt()
    {
        if (attemptActive && controller != null) controller.SetHabitatStopped(false);
        attemptActive = false; Arrived = false; FoodDiscoveryStarted = false;
        if (arrivalCard != null) arrivalCard.SetActive(false);
        if (objectivePanel != null) objectivePanel.SetActive(false);
        if (continueButton != null) continueButton.SetActive(false);
        if (destinationMarker != null) destinationMarker.SetActive(false);
    }
    private void OnDisable() { EndAttempt(); }
    private void OnDrawGizmosSelected()
    {
        if (habitatTarget == null) return;
        Gizmos.color = new Color(.4f, 1f, .5f); Gizmos.DrawWireSphere(habitatTarget.position, destinationRadius);
    }
}
