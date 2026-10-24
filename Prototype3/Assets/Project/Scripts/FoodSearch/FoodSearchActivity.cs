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
    [Header("Correct catch sound (optional)")]
    public AudioClip correctCatchClip;
    public AudioSource correctCatchSource;
    [Range(0f, 1f)] public float correctCatchVolume = .6f;
    public LadybirdHabitatObjective objective;
    public GameObject insectArea;
    // ===== 1. 任务注册与状态 =====
    // 数组保存全部正确和错误目标；完成数量由 requiredAphids 决定，不等于可用 aphid 总数。
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
    Coroutine replayRoutine;
    [Min(1)] public int requiredAphids = 10;
    public bool Running { get; private set; }
    public bool Completed { get; private set; }
    public int FoundCount => found.Count;
    public const string Instruction = "Find any 10 aphids to feed your hungry ladybird!\nHold Grip to grab. Release to let go.";
    public const string CorrectFeedback = "Correct! You've caught an aphid. Many ladybirds feed on aphids.";
    public const string IncorrectFeedback = "Not quite! That's an ant. Look for small, soft-bodied aphids with pear-shaped bodies.";
    public const string CompletionFeedback = "You've caught ten aphids! Ladybirds help protect plants by feeding on aphids.";
    // 用目标组件引用记录唯一捕获，不靠名字；同一个目标在本轮只可贡献一次。
    private readonly HashSet<FoodSearchTarget> found = new HashSet<FoodSearchTarget>();
    private bool Eligible => objective == null || objective.FoodSearchActive;
    private void Awake() { EndSearch(); }
    // ===== 2. 开始与重玩共用初始化 =====
    // 清空计分和反馈，恢复所有目标姿态/可见性；只重启任务说明，不重播教学视频。
    public void BeginSearch()
    {
        if (!Eligible || AnySelected() || returningToInsects) return;
        CancelCompletion();
        if (correctCatchSource != null) correctCatchSource.Stop();
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
    // ===== 3. 抓取验证与计分 =====
    // 必须是已注册、未计分且正被事件中 Interactor 选择的 Grab 对象；Hover 和点击不计分。
    public void ConfirmGrab(FoodSearchTarget target, SelectEnterEventArgs args)
    {
        if (!Running || Completed || !Eligible || target == null || target.activity != this || targets == null ||
            System.Array.IndexOf(targets, target) < 0 || found.Contains(target) || args == null) return;
        var grab = target.GetComponent<XRGrabInteractable>();
        if (grab == null || !grab.isActiveAndEnabled || !grab.isSelected || args.interactableObject != grab ||
            args.interactorObject == null || !grab.interactorsSelecting.Contains(args.interactorObject)) return;
        // Ant 在加入计分集合之前返回；文字提示和语音分别使用自己的冷却。
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
        // 抓住时立即登记、更新进度和音效；真正隐藏由释放生命周期脚本在松手后处理。
        found.Add(target); target.SetFound(true); RefreshProgress();
        if (correctCatchClip != null && correctCatchSource != null && correctCatchSource.isActiveAndEnabled)
            correctCatchSource.PlayOneShot(correctCatchClip, correctCatchVolume);
        Completed = found.Count >= requiredAphids;
        if (feedbackText != null) feedbackText.text = Completed ? "Ten aphids caught! Release held insects to continue." : CorrectFeedback;
        if (Completed)
        {
            if (voiceGuidance == null || voiceGuidance.logCompletionNarration)
                Debug.Log($"[Food Search] Mission complete at {found.Count}/{requiredAphids} unique aphids; requesting completion narration" +
                    (voiceGuidance == null ? " (voice manager missing)." : "."), this);
            if (voiceGuidance != null) voiceGuidance.MissionComplete();
            completionRoutine = StartCoroutine(ShowCompletionAfterGrab());
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
    // ===== 4. 完成 UI =====
    // 第十个唯一正确捕获后，下一帧显示完成卡；不要求捕获剩余目标，也不等待所有目标松手。
    IEnumerator ShowCompletionAfterGrab()
    {
        // Count/voice complete in ConfirmGrab; show UI just outside the XR callback stack.
        // Targets remain active, so holding an optional insect cannot delay completion.
        yield return null;
        completionRoutine = null;
        if (!Running || !Completed || !Eligible) yield break;
        CompletionVisible = true;
        // Uncaught insects remain optional. ConfirmGrab rejects all scoring after Completed.
        // Finish Exploration still hides the area; Try Again restores every target.
        if (searchPanel != null) searchPanel.SetActive(false);
        if (completionPanel != null) completionPanel.SetActive(true);
        if (tryAgainButton != null) tryAgainButton.SetActive(true);
    }
    void CancelCompletion() { if (completionRoutine != null) StopCoroutine(completionRoutine); completionRoutine = null; }
    // ===== 5. 重玩与返回 =====
    // 若还有目标被持有，先请求 XR Manager 取消选择，清理结束后再重新初始化。
    public void TryAgain()
    {
        if (returningToInsects || replayRoutine != null || !(Running || Finished) || !Completed || !CompletionVisible || !Eligible) return;
        if (!AnySelected()) BeginSearch();
        else replayRoutine = StartCoroutine(ReplayAfterSelectionCleanup());
    }
    // 离开 UI/XR 回调栈后取消选择，等待 XRI 更新完毕，避免重置仍被手持的刚体。
    IEnumerator ReplayAfterSelectionCleanup()
    {
        yield return null; // Leave the UI/XR callback stack before cancelling optional grabs.
        if (targets != null) foreach (var target in targets)
        {
            if (target == null) continue;
            var grab = target.GetComponent<XRGrabInteractable>();
            if (grab != null && grab.isSelected && grab.interactionManager != null)
                grab.interactionManager.CancelInteractableSelection((IXRSelectInteractable)grab);
        }
        yield return null;
        while (AnySelected()) yield return null;
        replayRoutine = null;
        if (!returningToInsects && (Running || Finished) && Completed && CompletionVisible && Eligible) BeginSearch();
    }
    // 结束抓取并隐藏昆虫区域，保持瓢虫停止；显示最终摘要，等待玩家明确选择返回。
    public void FinishExploration()
    {
        if (replayRoutine != null || !Running || Finished || !Completed || !CompletionVisible || !Eligible) return;
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
        if (replayRoutine != null || !Finished || !CompletionVisible || returningToInsects || existingBackToInsectsAction == null) return;
        if (voiceGuidance != null) voiceGuidance.Silence();
        returningToInsects = true;
        if (backToInsectsButton != null) backToInsectsButton.interactable = false;
        StartCoroutine(ReturnToInsectsRoutine());
    }
    // 清理剩余选择和食物 UI，再调用原 Back 按钮事件，复用既有返回旁白与场景重载。
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
    // 离开任务时清除计分、完成状态、音效和提示；目标姿态在下次 BeginSearch 中恢复。
    public void EndSearch()
    {
        if (replayRoutine != null) StopCoroutine(replayRoutine);
        replayRoutine = null;
        if (correctCatchSource != null) correctCatchSource.Stop();
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
    // 进度分母读取任务阈值，所以有 15 个正确目标时仍可只要求任意 10 个。
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
    // Editor 鼠标路径只提供悬停反馈；SelectTarget 是空操作，不能替代真实 XR 抓取。
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
