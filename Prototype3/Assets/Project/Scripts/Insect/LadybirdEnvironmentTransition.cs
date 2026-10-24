using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>Prototype2b-only coordinator. Progress remains owned by LadybirdExplorationProgress.</summary>
public sealed class LadybirdEnvironmentTransition : MonoBehaviour
{
    public Prototype2bVoiceGuidance voiceGuidance;
    public LadybirdMovementWingVisibility movementWings;
    public LadybirdExplorationProgress progress;
    public LadybirdPlayerController controller;
    public AntennaInteraction antenna;
    public WingInteraction wings;
    public ElytraInteraction elytra;
    public NarrationManager narration;
    public InsectInfoPanel infoPanel;
    public GameObject promptPanel, movementPanel, statusBackground;
    public TMP_Text statusText;
    public XRBaseInteractable[] bodyInteractables;
    public enum Mode { Body, Entering, Environment, Returning }
    public Mode CurrentMode { get; private set; }
    private readonly Dictionary<Behaviour, bool> disabled = new Dictionary<Behaviour, bool>();
    private Coroutine transition;
    private Vector3 bodyPosition;
    private Quaternion bodyRotation;
    private bool shellWasOpen, poseSaved, leavingSelection;

    // ===== 1. 身体与环境模式 =====
    // 监听进度组件的就绪事件来显示入口；禁用时解除监听并清理过渡。
    private void OnEnable()
    {
        CurrentMode = Mode.Body; leavingSelection = false;
        if (progress != null) progress.environmentReady.AddListener(ShowPrompt);
        RefreshUI();
    }
    private void OnDisable()
    {
        if (progress != null) progress.environmentReady.RemoveListener(ShowPrompt);
        ExitImmediately();
    }
    public void ShowPrompt() { if (CurrentMode == Mode.Body) RefreshUI(); }
    private bool DebugBypass => controller != null && controller.EditorTestActive;
    private void RefreshUI()
    {
        if (promptPanel != null) promptPanel.SetActive(!leavingSelection && !DebugBypass && CurrentMode == Mode.Body && progress != null && progress.EnvironmentReady);
        if (movementPanel != null) movementPanel.SetActive(!leavingSelection && !DebugBypass && CurrentMode == Mode.Environment);
    }
    private void Status(string message)
    {
        if (statusText != null) statusText.text = message;
        if (statusBackground != null) statusBackground.SetActive(!string.IsNullOrEmpty(message));
    }
    // 身体动画或 XR 选择仍在进行时，先等待，避免切换中途修改相同枢轴。
    private bool Busy()
    {
        if ((antenna != null && antenna.IsAnimating) || (wings != null && wings.IsAnimating) || (elytra != null && elytra.IsAnimating)) return true;
        if (bodyInteractables != null) foreach (var item in bodyInteractables)
            if (item != null && item.isActiveAndEnabled && item.isSelected) return true;
        return false;
    }
    // Explore 按钮入口；只有身体学习就绪且处于身体模式才开始过渡，阻止重复点击。
    public void ExploreEnvironment()
    {
        if (leavingSelection || DebugBypass || CurrentMode != Mode.Body || progress == null || !progress.EnvironmentReady) return;
        CurrentMode = Mode.Entering;
        RefreshUI();
        transition = StartCoroutine(EnterRoutine());
    }
    // ===== 2. 进入环境探索 =====
    // 保存原姿态、暂停身体选择、收翅关壳，再交给玩家控制器验证安全起点。
    private IEnumerator EnterRoutine()
    {
        Status("Preparing Ladybird… Release any selected body part.");
        while (Busy() || (narration != null && narration.IsNarrationPlaying)) yield return null;
        if (controller == null || elytra == null || wings == null)
        { Fail("Transition references are missing."); yield break; }
        bodyPosition = controller.transform.position; bodyRotation = controller.transform.rotation;
        shellWasOpen = elytra.IsOpen; poseSaved = true;
        // Disable XR selection, rather than animation scripts or colliders, so pivots close normally.
        if (bodyInteractables != null) foreach (var item in bodyInteractables)
            if (item != null && !disabled.ContainsKey(item)) { disabled.Add(item, item.enabled); item.enabled = false; }
        if (infoPanel != null) infoPanel.HideInfo();
        if (elytra.IsOpen) elytra.ToggleElytra();
        while (Busy()) yield return null;
        if (wings.IsOpen) { wings.CloseWings(); while (wings.IsAnimating) yield return null; }
        if (movementWings != null) yield return movementWings.Prepare();
        if (!controller.BeginPlayerControl())
        {
            Status("Cannot explore safely: " + controller.MovementStatus);
            CurrentMode = Mode.Returning;
            yield return RestoreBody();
            CurrentMode = Mode.Body; RefreshUI(); transition = null; yield break;
        }
        CurrentMode = Mode.Environment; Status(""); RefreshUI(); transition = null;
    }
    private void Fail(string reason) { CurrentMode = Mode.Body; Status(reason); RefreshUI(); transition = null; }
    public void ReturnToBodyExploration()
    {
        if (CurrentMode != Mode.Environment || DebugBypass) return;
        if (voiceGuidance != null) voiceGuidance.Silence();
        CurrentMode = Mode.Returning; RefreshUI();
        transition = StartCoroutine(ReturnRoutine());
    }
    private IEnumerator ReturnRoutine()
    {
        Status("Returning to body exploration…");
        yield return RestoreBody();
        CurrentMode = Mode.Body; Status(""); RefreshUI(); transition = null;
    }
    // ===== 3. 返回与恢复 =====
    // 结束驾驶并恢复原位置、翼显示和交互状态；不在这里重置已完成的身体学习。
    private IEnumerator RestoreBody()
    {
        if (controller != null) controller.EndPlayerControl();
        if (movementWings != null) movementWings.Restore();
        RestoreParent();
        if (shellWasOpen && elytra != null && !elytra.IsOpen)
        { elytra.ToggleElytra(); while (elytra.IsAnimating || (wings != null && wings.IsAnimating)) yield return null; }
        RestoreInteractions(); poseSaved = false;
    }
    private void RestoreParent()
    {
        if (poseSaved && controller != null) controller.transform.SetPositionAndRotation(bodyPosition, bodyRotation);
    }
    private void RestoreInteractions()
    {
        foreach (var item in disabled) if (item.Key != null) item.Key.enabled = item.Value;
        disabled.Clear();
    }
    // Called before the existing Back narration; also guarantees cleanup if the zone is disabled.
    public void ExitForSelection()
    {
        ExitImmediately();
        leavingSelection = true;
        Status(""); RefreshUI();
    }
    // 返回选择或对象禁用时取消过渡，停止旧旁白并恢复记录的组件状态。
    public void ExitImmediately()
    {
        if (voiceGuidance != null) voiceGuidance.Silence();
        if (transition != null) StopCoroutine(transition);
        transition = null;
        if (!DebugBypass && controller != null) controller.EndPlayerControl();
        if (movementWings != null && !DebugBypass) movementWings.Restore();
        RestoreParent(); RestoreInteractions(); poseSaved = false;
        CurrentMode = Mode.Body; RefreshUI();
    }
}
