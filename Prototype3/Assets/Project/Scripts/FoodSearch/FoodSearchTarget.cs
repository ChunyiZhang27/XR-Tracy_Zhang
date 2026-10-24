using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(SphereCollider))]
public sealed class FoodSearchTarget : MonoBehaviour
{
    // ===== 1. 目标身份与分类 =====
    // 用 Inspector 枚举判断 Aphid 或 Ant，不依赖物体名称；activity 指向本轮任务管理器。
    public enum IdentificationCategory { Aphid, Ant }
    public IdentificationCategory category;
    public FoodSearchActivity activity;
    public Renderer[] visuals;
    public bool Found { get; private set; }
    private XRBaseInteractable interactable;
    public bool RequiresGrab => GetComponent<XRGrabInteractable>() != null;
    private bool xrHover, mouseHover;
    private MaterialPropertyBlock block;
    // 启用时订阅 XR 选择和悬停事件；禁用时配对移除，避免重玩后重复回调。
    private void OnEnable()
    {
        interactable = GetComponent<XRGrabInteractable>();
        if (interactable == null) interactable = GetComponent<XRSimpleInteractable>();
        if (interactable == null) { Refresh(); return; }
        interactable.selectEntered.AddListener(Selected);
        interactable.firstHoverEntered.AddListener(HoverEntered);
        interactable.lastHoverExited.AddListener(HoverExited);
        Refresh();
    }
    private void OnDisable()
    {
        if (interactable != null)
        {
            interactable.selectEntered.RemoveListener(Selected);
            interactable.firstHoverEntered.RemoveListener(HoverEntered);
            interactable.lastHoverExited.RemoveListener(HoverExited);
        }
        xrHover = mouseHover = false;
    }
    // ===== 2. 真实抓取入口 =====
    // 只把 XRGrabInteractable 的 Select 事件转交任务；普通鼠标点击不计作捕获。
    private void Selected(SelectEnterEventArgs args)
    {
        var grab = interactable as XRGrabInteractable;
        if (grab != null && activity != null) activity.ConfirmGrab(this, args);
    }
    private void HoverEntered(HoverEnterEventArgs _) { xrHover = true; Refresh(); }
    private void HoverExited(HoverExitEventArgs _) { xrHover = false; Refresh(); }
    public void SelectTarget() { /* Mouse/ray clicks are not catches. */ }
    public void SetMouseHover(bool value) { if (mouseHover == value) return; mouseHover = value; Refresh(); }
    public void SetFound(bool value) { Found = value; Refresh(); }
    // ===== 3. 视觉反馈 =====
    // 悬停变亮、计分后偏绿；使用属性块覆盖颜色，保留导入模型的共享材质。
    private void Refresh()
    {
        if (visuals == null) return;
        if (block == null) block = new MaterialPropertyBlock();
        foreach (var visual in visuals)
        {
            if (visual == null) continue;
            var materials = visual.sharedMaterials;
            for (int slot = 0; slot < materials.Length; slot++)
            {
                var material = materials[slot];
                if (material == null) continue;
                // glTFast uses baseColorFactor; URP/Lit and legacy materials use different names.
                var property = material.HasProperty("_BaseColor") ? "_BaseColor" :
                    material.HasProperty("baseColorFactor") ? "baseColorFactor" :
                    material.HasProperty("_Color") ? "_Color" : null;
                if (property == null) continue;
                var color = material.GetColor(property);
                if (Found) color = Color.Lerp(color, new Color(.65f, .85f, .35f), .6f);
                else if (xrHover || mouseHover) color = Color.Lerp(color, Color.white, .28f);
                visual.GetPropertyBlock(block, slot);
                block.SetColor(property, color);
                visual.SetPropertyBlock(block, slot);
            }
        }
    }
}
