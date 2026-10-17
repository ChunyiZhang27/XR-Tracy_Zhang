using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(SphereCollider))]
public sealed class FoodSearchTarget : MonoBehaviour
{
    public enum IdentificationCategory { Aphid, Ant }
    public IdentificationCategory category;
    public FoodSearchActivity activity;
    public Renderer[] visuals;
    public bool Found { get; private set; }
    private XRBaseInteractable interactable;
    public bool RequiresGrab => GetComponent<XRGrabInteractable>() != null;
    private bool xrHover, mouseHover;
    private MaterialPropertyBlock block;
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
    private void Selected(SelectEnterEventArgs args)
    {
        if (RequiresGrab)
        {
            var grab = interactable as XRGrabInteractable;
            if (grab == null || !grab.isSelected || args.interactorObject == null) return;
            Debug.Log("[Food Search Step A] XR grab confirmed: " + name, this);
            if (activity != null) activity.Identify(this);
        }
        else SelectTarget();
    }
    private void HoverEntered(HoverEnterEventArgs _) { xrHover = true; Refresh(); }
    private void HoverExited(HoverExitEventArgs _) { xrHover = false; Refresh(); }
    public void SelectTarget() { if (!RequiresGrab && activity != null) activity.Identify(this); }
    public void SetMouseHover(bool value) { if (mouseHover == value) return; mouseHover = value; Refresh(); }
    public void SetFound(bool value) { Found = value; Refresh(); }
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
