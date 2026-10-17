using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
#if UNITY_EDITOR
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
#endif

/// <summary>Prototype2b E1 identification only. Never drives or reparents the Ladybird or camera.</summary>
public sealed class FoodSearchActivity : MonoBehaviour
{
    public LadybirdHabitatObjective objective;
    public GameObject insectArea;
    public FoodSearchTarget[] targets;
    public TMP_Text progressText, feedbackText;
    public GameObject tryAgainButton;
    [Min(1)] public int requiredAphids = 2;
    public bool Running { get; private set; }
    public bool Completed { get; private set; }
    public int FoundCount => found.Count;
    public const string Instruction = "Find and select two aphids hiding among the insects!";
    public const string CorrectFeedback = "Correct! You've found an aphid. Many ladybirds feed on aphids.";
    public const string IncorrectFeedback = "Not quite! Look for small, soft-bodied insects, often found together on leaves and stems.";
    public const string CompletionFeedback = "Well done! You've discovered what ladybirds eat and how they help plants.";
    private readonly HashSet<FoodSearchTarget> found = new HashSet<FoodSearchTarget>();
    private bool Eligible => objective == null || objective.FoodSearchActive;
    private void Awake() { EndSearch(); }
    public void BeginSearch()
    {
        if (!Eligible) return;
        found.Clear(); Completed = false; Running = true;
        if (targets != null) foreach (var target in targets) if (target != null) target.SetFound(false);
        if (feedbackText != null) feedbackText.text = "";
        if (tryAgainButton != null) tryAgainButton.SetActive(false);
        if (insectArea != null) insectArea.SetActive(true);
        RefreshProgress();
    }
    public void Identify(FoodSearchTarget target)
    {
        if (!Running || Completed || !Eligible || target == null || targets == null ||
            System.Array.IndexOf(targets, target) < 0 || found.Contains(target)) return;
        if (target.category != FoodSearchTarget.IdentificationCategory.Aphid)
        {
            if (feedbackText != null) feedbackText.text = IncorrectFeedback;
            return;
        }
        found.Add(target); target.SetFound(true); RefreshProgress();
        Completed = found.Count >= requiredAphids;
        if (feedbackText != null) feedbackText.text = Completed ? CompletionFeedback : CorrectFeedback;
        if (tryAgainButton != null) tryAgainButton.SetActive(Completed);
    }
    public void TryAgain() { if (Running && Eligible) BeginSearch(); }
    public void EndSearch()
    {
        Running = Completed = false; found.Clear();
        if (targets != null) foreach (var target in targets) if (target != null) target.SetFound(false);
        if (insectArea != null) insectArea.SetActive(false);
        if (feedbackText != null) feedbackText.text = "";
        if (tryAgainButton != null) tryAgainButton.SetActive(false);
        RefreshProgress();
#if UNITY_EDITOR
        SetMouseTarget(null);
#endif
    }
    private void RefreshProgress() { if (progressText != null) progressText.text = $"Aphids found: {found.Count} / {requiredAphids}"; }
    private void OnDisable() { EndSearch(); }
#if UNITY_EDITOR
    [Tooltip("Editor only: also enable Prototype2bEditorMovementTest.Run Movement Test On Play. Waits for its safe initialization and wing closure, then starts E1 without onboarding.")]
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
