using TMPro;
using UnityEngine;

/// <summary>Position-based objective: only the controlled Ladybird can arrive; no player trigger callbacks.</summary>
[DefaultExecutionOrder(500)]
public sealed class LadybirdHabitatObjective : MonoBehaviour
{
    public LadybirdPlayerController controller;
    public LadybirdEnvironmentTransition transition;
    public Transform habitatTarget;
    [Min(.05f)] public float destinationRadius = .45f;
    [Min(.01f)] public float arrivalHeightTolerance = .15f;
    [Tooltip("Editor-only navigation objective during the independent movement bypass. Inert in player builds.")]
    public bool enableInEditorMovementTest;
    public GameObject objectivePanel, continueButton, destinationMarker;
    public TMP_Text objectiveText;
    public bool Arrived { get; private set; }
    private bool attemptActive;
    private int resetVersion;
    const string Instruction = "Your ladybird is ready to explore! Guide it towards the leafy habitat to discover where it finds food.";
    const string Found = "Habitat Found! You've reached a place where ladybirds can search for food.";
    private bool Eligible()
    {
        if(controller == null || !controller.ControlActive) return false;
#if UNITY_EDITOR
        if(controller.EditorTestActive) return enableInEditorMovementTest;
#endif
        return transition != null && transition.CurrentMode == LadybirdEnvironmentTransition.Mode.Environment;
    }
    private void LateUpdate() { Evaluate(); }
    public void Evaluate()
    {
        if(!Eligible()) { EndAttempt(); return; }
        if(habitatTarget == null || controller.groundContact == null)
        { if(objectiveText != null) objectiveText.text = "Habitat references are missing."; return; }
        if(!attemptActive || resetVersion != controller.ResetVersion) BeginAttempt();
        if(Arrived || !InsideArrivalRegion(controller.groundContact.position)) return;
        // Recheck live surfaces and obstacles before accepting arrival.
        if(!controller.safety.TryPosition(controller.groundSurfaces,controller.groundContact.position,
                controller.startingPoint.position,controller.groundContact.position.y,out var grounded,out _) ||
            !controller.safety.PathIsClear(controller.transform,grounded,Vector3.forward,0f,out _,controller.groundSurfaces)) return;
        Arrived = true; controller.SetObjectiveStopped(true);
        if(objectiveText != null) objectiveText.text = Found;
        if(continueButton != null) continueButton.SetActive(true);
        if(destinationMarker != null) destinationMarker.SetActive(false);
    }
    public bool InsideArrivalRegion(Vector3 contact)
    {
        if(habitatTarget == null) return false;
        Vector3 delta=contact-habitatTarget.position;
        return Mathf.Abs(delta.y)<=arrivalHeightTolerance && new Vector2(delta.x,delta.z).sqrMagnitude<=destinationRadius*destinationRadius;
    }
    private void BeginAttempt()
    {
        attemptActive=true; Arrived=false; resetVersion=controller.ResetVersion;
        controller.SetObjectiveStopped(false);
        if(objectivePanel != null) objectivePanel.SetActive(true);
        if(continueButton != null) continueButton.SetActive(false);
        if(destinationMarker != null) destinationMarker.SetActive(true);
        if(objectiveText != null) objectiveText.text=Instruction;
    }
    public void ContinueToFoodDiscovery()
    { if(attemptActive && Arrived && Eligible() && objectiveText != null) objectiveText.text="Food discovery is coming next."; }
    private void EndAttempt()
    {
        if(attemptActive && controller != null) controller.SetObjectiveStopped(false);
        attemptActive=false; Arrived=false;
        if(objectivePanel != null) objectivePanel.SetActive(false);
        if(continueButton != null) continueButton.SetActive(false);
        if(destinationMarker != null) destinationMarker.SetActive(false);
    }
    private void OnDisable() { EndAttempt(); }
    private void OnDrawGizmosSelected()
    {
        if(habitatTarget == null) return;
        Gizmos.color=Color.green;Gizmos.DrawWireSphere(habitatTarget.position,destinationRadius);
    }
}
