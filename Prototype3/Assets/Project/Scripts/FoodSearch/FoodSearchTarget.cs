using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRSimpleInteractable), typeof(SphereCollider))]
public sealed class FoodSearchTarget : MonoBehaviour
{
    public enum IdentificationCategory { Aphid, Ant }
    public IdentificationCategory category;
    public FoodSearchActivity activity;
    public Renderer[] visuals;
    public bool Found { get; private set; }
    private XRSimpleInteractable interactable;
    private bool xrHover, mouseHover;
    private MaterialPropertyBlock block;
    private void OnEnable()
    {
        interactable = GetComponent<XRSimpleInteractable>();
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
    private void Selected(SelectEnterEventArgs _) { SelectTarget(); }
    private void HoverEntered(HoverEnterEventArgs _) { xrHover = true; Refresh(); }
    private void HoverExited(HoverExitEventArgs _) { xrHover = false; Refresh(); }
    public void SelectTarget() { if (activity != null) activity.Identify(this); }
    public void SetMouseHover(bool value) { if (mouseHover == value) return; mouseHover = value; Refresh(); }
    public void SetFound(bool value) { Found = value; Refresh(); }
    private void Refresh()
    {
        if (visuals == null) return;
        if (block == null) block = new MaterialPropertyBlock();
        foreach (var visual in visuals)
        {
            if (visual == null || visual.sharedMaterial == null) continue;
            var color = visual.sharedMaterial.GetColor("_BaseColor");
            if (Found) color = Color.Lerp(color, new Color(.65f, .85f, .35f), .6f);
            else if (xrHover || mouseHover) color = Color.Lerp(color, Color.white, .28f);
            visual.GetPropertyBlock(block); block.SetColor("_BaseColor", color); visual.SetPropertyBlock(block);
        }
    }
}
