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
    public LadybirdHabitatObjective objective;
    public GameObject insectArea;
    public FoodSearchTarget[] targets;
    public TMP_Text progressText, feedbackText;
    public GameObject tryAgainButton;
    public GameObject searchPanel, completionPanel;
    public bool CompletionVisible { get; private set; }
    Coroutine completionRoutine;
    [Min(1)] public int requiredAphids = 5;
    public bool Running { get; private set; }
    public bool Completed { get; private set; }
    public int FoundCount => found.Count;
    public const string Instruction = "Find and catch five aphids hiding among the insects!";
    public const string CorrectFeedback = "Correct! You've caught an aphid. Many ladybirds feed on aphids.";
    public const string IncorrectFeedback = "Not quite! Look for tiny, soft-bodied insects with pear-shaped bodies and antennae.";
    public const string CompletionFeedback = "You found five aphids! Ladybirds help protect plants by feeding on aphids.";
    private readonly HashSet<FoodSearchTarget> found = new HashSet<FoodSearchTarget>();
    private bool Eligible => objective == null || objective.FoodSearchActive;
    private void Awake() { EndSearch(); }
    public void BeginSearch()
    {
        if (!Eligible || AnySelected()) return;
        CancelCompletion();
        found.Clear(); Completed = CompletionVisible = false; Running = true;
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
            if (feedbackText != null) feedbackText.text = IncorrectFeedback;
            return;
        }
        found.Add(target); target.SetFound(true); RefreshProgress();
        Completed = found.Count >= requiredAphids;
        if (feedbackText != null) feedbackText.text = Completed ? "Five aphids caught! Release held insects to continue." : CorrectFeedback;
        if (Completed) completionRoutine = StartCoroutine(CompleteAfterRelease());
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
    public void TryAgain() { if (Running && Completed && CompletionVisible && Eligible) BeginSearch(); }
    public void FinishExploration()
    {
        if (!Running || !Completed || !CompletionVisible || !Eligible) return;
        EndSearch();
        if (searchPanel != null) searchPanel.SetActive(false);
        if (objective == null || objective.transition == null) return;
#if UNITY_EDITOR
        if (objective.controller.EditorTestActive)
        {
            objective.controller.EndPlayerControl(); objective.controller.SetEditorTestMode(false);
            objective.transition.ExitImmediately(); objective.Evaluate(); return;
        }
#endif
        objective.transition.ReturnToBodyExploration();
    }
    public void EndSearch()
    {
        CancelCompletion();
        Running = Completed = CompletionVisible = false; found.Clear();
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
