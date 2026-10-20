using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine;
#if UNITY_EDITOR
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
#endif

/// <summary>Prototype2b grab-only food mission. Never drives or reparents the Ladybird or camera.</summary>
public sealed class FoodSearchActivity : MonoBehaviour
{
    public Prototype2bVoiceGuidance voiceGuidance;
    public LadybirdHabitatObjective objective;
    public GameObject insectArea;
    public FoodSearchTarget[] targets;
    public TMP_Text progressText, feedbackText;
    public GameObject tryAgainButton;
    public GameObject searchPanel, completionPanel;
    public TMP_Text completionTitle;
    public UnityEngine.UI.Button finishExplorationButton;
    public GameObject backToInsectsPanel;
    public UnityEngine.UI.Button backToInsectsButton, existingBackToInsectsAction;
    [Min(0f)] public float incorrectFeedbackCooldown = 2f;
    float nextIncorrectFeedbackTime = float.NegativeInfinity;
    bool returningToInsects;
    public bool Finished { get; private set; }
    public bool CompletionVisible { get; private set; }
    Coroutine completionRoutine;
    [Min(1)] public int requiredAphids = 5;
    public bool Running { get; private set; }
    public bool Completed { get; private set; }
    public int FoundCount => found.Count;
    public const string Instruction = "Find and catch five aphids hiding among the insects!";
    public const string CorrectFeedback = "Correct! You've caught an aphid. Many ladybirds feed on aphids.";
    public const string IncorrectFeedback = "Not quite! That's an ant. Look for small, soft-bodied aphids with pear-shaped bodies.";
    public const string CompletionFeedback = "You've caught five aphids! Ladybirds help protect plants by feeding on aphids.";
    private readonly HashSet<FoodSearchTarget> found = new HashSet<FoodSearchTarget>();
    private bool Eligible => objective == null || objective.FoodSearchActive;
    private void Awake() { EndSearch(); }
    public void BeginSearch()
    {
        if (!Eligible || AnySelected() || returningToInsects) return;
        CancelCompletion();
        found.Clear(); Completed = CompletionVisible = Finished = false; Running = true;
        nextIncorrectFeedbackTime = float.NegativeInfinity;
        RefreshCompletionState();
        if (completionPanel != null) completionPanel.SetActive(false);
        if (searchPanel != null) searchPanel.SetActive(true);
        if (targets != null) foreach (var target in targets) if (target != null)
        {
            target.SetFound(false);
            var lifecycle = target.GetComponent<FoodSearchGrabReturn>();
            if (lifecycle != null) lifecycle.ResetForSearch();
            target.gameObject.SetActive(true);
        }
        if (feedbackText != null) feedbackText.text = "";
        if (tryAgainButton != null) tryAgainButton.SetActive(false);
        if (insectArea != null) insectArea.SetActive(true);
        RefreshProgress();
        if (voiceGuidance != null) voiceGuidance.StartFoodSearch();
    }
    // Kept for old scene/editor tooling: selection alone must never award a catch.
    public void Identify(FoodSearchTarget target) { }
    public void ConfirmGrab(FoodSearchTarget target, SelectEnterEventArgs args)
    {
        if (!Running || Completed || !Eligible || target == null || target.activity != this || targets == null ||
            System.Array.IndexOf(targets, target) < 0 || found.Contains(target) || args == null) return;
        var grab = target.GetComponent<XRGrabInteractable>();
        if (grab == null || !grab.isActiveAndEnabled || !grab.isSelected || args.interactableObject != grab ||
            args.interactorObject == null || !grab.interactorsSelecting.Contains(args.interactorObject)) return;
        if (target.category != FoodSearchTarget.IdentificationCategory.Aphid)
        {
            if (voiceGuidance != null) voiceGuidance.IncorrectGrab();
            if (Time.unscaledTime >= nextIncorrectFeedbackTime)
            {
                if (feedbackText != null) feedbackText.text = IncorrectFeedback;
                nextIncorrectFeedbackTime = Time.unscaledTime + incorrectFeedbackCooldown;
            }
            return;
        }
        found.Add(target); target.SetFound(true); RefreshProgress();
        Completed = found.Count >= requiredAphids;
        if (feedbackText != null) feedbackText.text = Completed ? "Five aphids caught! Release held insects to continue." : CorrectFeedback;
        if (Completed)
        {
            if (voiceGuidance != null) voiceGuidance.MissionComplete();
            completionRoutine = StartCoroutine(CompleteAfterRelease());
        }
    }
    bool AnySelected()
    {
        if (targets != null) foreach (var target in targets)
        {
            if (target == null) continue;
            var grab = target.GetComponent<XRGrabInteractable>();
            if (grab != null && grab.isSelected) return true;
        }
        return false;
    }
    IEnumerator CompleteAfterRelease()
    {
        // Never hide a target or show replay from inside selectEntered/selectExited callbacks.
        do { yield return null; } while (AnySelected());
        yield return new WaitForFixedUpdate();
        while (AnySelected()) yield return null;
        completionRoutine = null;
        if (!Running || !Completed || !Eligible) yield break;
        CompletionVisible = true;
        if (insectArea != null) insectArea.SetActive(false);
        if (searchPanel != null) searchPanel.SetActive(false);
        if (completionPanel != null) completionPanel.SetActive(true);
        if (tryAgainButton != null) tryAgainButton.SetActive(true);
    }
    void CancelCompletion() { if (completionRoutine != null) StopCoroutine(completionRoutine); completionRoutine = null; }
    public void TryAgain() { if (!returningToInsects && (Running || Finished) && Completed && CompletionVisible && Eligible) BeginSearch(); }
    public void FinishExploration()
    {
        if (!Running || Finished || !Completed || !CompletionVisible || !Eligible) return;
        // Remain at the final Habitat card. Explicit Try Again restarts only this mission.
        if (voiceGuidance != null) voiceGuidance.Silence();
        Finished = true;
        Running = false;
        if (searchPanel != null) searchPanel.SetActive(false);
        if (insectArea != null) insectArea.SetActive(false);
        if (objective != null && objective.controller != null) objective.controller.SetHabitatStopped(true);
        RefreshCompletionState();
    }
    void RefreshCompletionState()
    {
        if (backToInsectsPanel != null) backToInsectsPanel.SetActive(Finished && !returningToInsects);
        if (backToInsectsButton != null) backToInsectsButton.interactable = !returningToInsects;
        if (completionTitle != null) completionTitle.text = Finished ? "Exploration Complete!" : "Well Done!";
        if (finishExplorationButton != null)
        {
            finishExplorationButton.interactable = !Finished;
            var label = finishExplorationButton.GetComponentInChildren<TMP_Text>(true);
            if (label != null) label.text = Finished ? "Finished" : "Finish Exploration";
        }
    }
    public void BackToInsects()
    {
        if (!Finished || !CompletionVisible || returningToInsects || existingBackToInsectsAction == null) return;
        if (voiceGuidance != null) voiceGuidance.Silence();
        returningToInsects = true;
        if (backToInsectsButton != null) backToInsectsButton.interactable = false;
        StartCoroutine(ReturnToInsectsRoutine());
    }
    IEnumerator ReturnToInsectsRoutine()
    {
        yield return null; // Leave the UI/XR event stack before cancelling any remaining selections.
        if (targets != null) foreach (var target in targets)
        {
            if (target == null) continue;
            var grab = target.GetComponent<XRGrabInteractable>();
            if (grab != null && grab.isSelected && grab.interactionManager != null)
                grab.interactionManager.CancelInteractableSelection((IXRSelectInteractable)grab);
        }
        yield return null;
        EndSearch();
        if (searchPanel != null) searchPanel.SetActive(false);
        if (objective != null)
        {
            if (objective.arrivalCard != null) objective.arrivalCard.SetActive(false);
            if (objective.foodLearningCard1 != null) objective.foodLearningCard1.SetActive(false);
            if (objective.foodLearningCard2 != null) objective.foodLearningCard2.SetActive(false);
            if (objective.controller != null)
            {
                objective.controller.EndPlayerControl();
#if UNITY_EDITOR
                objective.controller.SetEditorTestMode(false);
#endif
            }
        }
        // Invoke the saved original navigation action, including its existing transition cleanup
        // and scene-reload/session-progress rules. No alternative selection system is introduced.
        existingBackToInsectsAction.onClick.Invoke();
    }
    public void EndSearch()
    {
        if ((Running || Finished) && voiceGuidance != null) voiceGuidance.Silence();
        CancelCompletion();
        Running = Completed = CompletionVisible = Finished = false; found.Clear();
        nextIncorrectFeedbackTime = float.NegativeInfinity;
        RefreshCompletionState();
        if (completionPanel != null) completionPanel.SetActive(false);
        if (targets != null) foreach (var target in targets) if (target != null) target.SetFound(false);
        if (insectArea != null) insectArea.SetActive(false);
        if (feedbackText != null) feedbackText.text = "";
        if (tryAgainButton != null) tryAgainButton.SetActive(false);
        RefreshProgress();
#if UNITY_EDITOR
        SetMouseTarget(null);
#endif
    }
    private void RefreshProgress() { if (progressText != null) progressText.text = $"Aphids caught: {found.Count} / {requiredAphids}"; }
    private void OnDisable() { EndSearch(); }
#if UNITY_EDITOR
    [Tooltip("Editor only: also enable Prototype2bEditorMovementTest.Run Movement Test On Play. Waits for its safe initialization and wing closure, then starts Food Search without onboarding.")]
    public bool startFoodSearchInEditorTest;
    private FoodSearchTarget mouseTarget;
    private IEnumerator Start()
    {
        if (!startFoodSearchInEditorTest || gameObject.scene.path != "Assets/Project/Scenes/Prototype2b.unity" || objective == null) yield break;
        while (!objective.controller.ControlActive) yield return null;
        objective.StartFoodSearchEditorTest();
    }
    private void Update()
    {
        if (!Running || Completed || !Eligible || Mouse.current == null || objective == null || objective.controller.xrOrigin == null)
        { SetMouseTarget(null); return; }
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) { SetMouseTarget(null); return; }
        var camera = objective.controller.xrOrigin.Camera;
        var ray = camera.ScreenPointToRay(Mouse.current.position.ReadValue());
        var hits = Physics.RaycastAll(ray, 12f, ~0, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a,b) => a.distance.CompareTo(b.distance));
        FoodSearchTarget hovered = null;
        foreach (var hit in hits)
        {
            if (hit.transform.IsChildOf(objective.controller.transform)) continue;
            hovered = hit.collider.GetComponentInParent<FoodSearchTarget>();
            if (hovered != null && hovered.activity != this) hovered = null;
            break;
        }
        SetMouseTarget(hovered);
        if (mouseTarget != null && Mouse.current.leftButton.wasPressedThisFrame) mouseTarget.SelectTarget();
    }
    private void SetMouseTarget(FoodSearchTarget target)
    {
        if (mouseTarget == target) return;
        if (mouseTarget != null) mouseTarget.SetMouseHover(false);
        mouseTarget = target;
        if (mouseTarget != null) mouseTarget.SetMouseHover(true);
    }
#endif
}
