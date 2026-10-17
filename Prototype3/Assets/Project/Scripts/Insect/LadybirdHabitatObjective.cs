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
    [Header("Optional Food Discovery learning cards")]
    public GameObject foodLearningCard1, foodLearningCard2, foodSearchReadyPanel;
    public FoodSearchActivity foodSearch;
    public bool FoodSearchActive => FoodNavigationAllowed && CurrentFoodStage == FoodStage.FoodSearchReady;
    public enum FoodStage { Inactive, Card1, Card2, FoodSearchReady }
    public FoodStage CurrentFoodStage { get; private set; }
    public bool FoodDiscoveryStarted { get; private set; }
    public bool Arrived { get; private set; }
    public const string FoodDiscoveryMessage = "Let's discover what ladybirds eat!";
    [Tooltip("Temporary click diagnostics; disable after verifying mouse and XR interaction.")]
    public bool logArrivalInteraction = true;
    private void Awake()
    {
        HideFoodLearning();
        if (arrivalCard == null) return;
        // TrackedDeviceGraphicRaycaster accepts XR event data only. Mouse pointers need this too.
        var canvas = arrivalCard.GetComponentInParent<Canvas>(true);
        if (canvas != null && canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
            canvas.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        if (continueButton != null && logArrivalInteraction)
        {
            var trigger = continueButton.GetComponent<UnityEngine.EventSystems.EventTrigger>();
            if (trigger == null) trigger = continueButton.AddComponent<UnityEngine.EventSystems.EventTrigger>();
            AddPointerDiagnostic(trigger, UnityEngine.EventSystems.EventTriggerType.PointerDown);
            AddPointerDiagnostic(trigger, UnityEngine.EventSystems.EventTriggerType.PointerClick);
        }
        arrivalCard.SetActive(false);
    }
    private void AddPointerDiagnostic(UnityEngine.EventSystems.EventTrigger trigger, UnityEngine.EventSystems.EventTriggerType type)
    {
        var entry = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = type };
        entry.callback.AddListener(_ => LogArrival("Button received " + type + " input."));
        trigger.triggers.Add(entry);
    }
    private void LogArrival(string message)
    {
        if (logArrivalInteraction) Debug.Log("[Habitat Arrival] " + message, this);
    }
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
        HideFoodLearning();
        if (objectivePanel != null) objectivePanel.SetActive(true);
        if (continueButton != null) continueButton.SetActive(false);
        if (destinationMarker != null) destinationMarker.SetActive(true);
        if (objectiveText != null) objectiveText.text = NavigationPrompt;
    }
    public void ContinueToFoodDiscovery()
    {
        LogArrival("Button OnClick callback invoked.");
        if (!attemptActive || !Arrived || !Eligible() || FoodDiscoveryStarted)
        {
            LogArrival($"Food Discovery rejected: attempt={attemptActive}, arrived={Arrived}, eligible={Eligible()}, alreadyStarted={FoodDiscoveryStarted}.");
            return;
        }
        LogArrival("Food Discovery accepted.");
        FoodDiscoveryStarted = true;
        controller.SetHabitatStopped(true);
        if (arrivalCard != null) arrivalCard.SetActive(false);
        if (continueButton != null) continueButton.SetActive(false);
        if (objectivePanel != null) objectivePanel.SetActive(true);
        if (objectiveText != null) objectiveText.text = FoodDiscoveryMessage;
        if (foodLearningCard1 != null) ShowFoodStage(FoodStage.Card1);
        else LogArrival("Placeholder UI shown: " + FoodDiscoveryMessage);
        foodDiscoveryStarted.Invoke();
    }
    private bool FoodNavigationAllowed => attemptActive && Arrived && FoodDiscoveryStarted && Eligible();
    public void NextFoodLearningCard()
    {
        if (FoodNavigationAllowed && CurrentFoodStage == FoodStage.Card1)
            ShowFoodStage(FoodStage.Card2);
    }
    public void BackToFirstFoodLearningCard()
    {
        if (FoodNavigationAllowed && CurrentFoodStage == FoodStage.Card2)
            ShowFoodStage(FoodStage.Card1);
    }
    public void StartFoodSearch()
    {
        if (FoodNavigationAllowed && CurrentFoodStage == FoodStage.Card2)
            ShowFoodStage(FoodStage.FoodSearchReady);
    }
    // Explicit restart hook for replay UI or a future lesson coordinator; does not reset body progress.
    public void RestartFoodDiscovery()
    {
        if (FoodNavigationAllowed && foodLearningCard1 != null) ShowFoodStage(FoodStage.Card1);
    }
    private void ShowFoodStage(FoodStage stage)
    {
        controller.SetHabitatStopped(true);
        CurrentFoodStage = stage;
        if (arrivalCard != null) arrivalCard.SetActive(false);
        if (objectivePanel != null) objectivePanel.SetActive(false);
        if (foodLearningCard1 != null) foodLearningCard1.SetActive(stage == FoodStage.Card1);
        if (foodLearningCard2 != null) foodLearningCard2.SetActive(stage == FoodStage.Card2);
        if (foodSearchReadyPanel != null) foodSearchReadyPanel.SetActive(stage == FoodStage.FoodSearchReady);
        if (foodSearch != null)
        {
            if (stage == FoodStage.FoodSearchReady) foodSearch.BeginSearch();
            else foodSearch.EndSearch();
        }
    }
    private void HideFoodLearning()
    {
        CurrentFoodStage = FoodStage.Inactive;
        if (foodSearch != null) foodSearch.EndSearch();
        if (foodLearningCard1 != null) foodLearningCard1.SetActive(false);
        if (foodLearningCard2 != null) foodLearningCard2.SetActive(false);
        if (foodSearchReadyPanel != null) foodSearchReadyPanel.SetActive(false);
    }
#if UNITY_EDITOR
    // Test-only shortcut after the existing movement bootstrap has safely closed wings and initialized control.
    public void StartFoodSearchEditorTest()
    {
        if (gameObject.scene.path != "Assets/Project/Scenes/Prototype2b.unity" || controller == null ||
            !controller.EditorTestActive || !controller.ControlActive || !enableInEditorMovementTest) return;
        attemptActive = Arrived = FoodDiscoveryStarted = true; resetVersion = controller.ResetVersion;
        ShowFoodStage(FoodStage.FoodSearchReady);
    }
#endif
    private void EndAttempt()
    {
        if (attemptActive && controller != null) controller.SetHabitatStopped(false);
        attemptActive = false; Arrived = false; FoodDiscoveryStarted = false;
        HideFoodLearning();
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
