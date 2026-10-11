using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>Opt-in reversible visual posture for Prototype2b movement. Never changes progress or GameObject activation.</summary>
public sealed class LadybirdMovementWingVisibility : MonoBehaviour
{
    public WingInteraction wings;
    public ElytraInteraction elytra;
    public LadybirdWalkingAnimator walking;
    public MeshRenderer[] flightWingRenderers;
    public XRBaseInteractable[] wingShellInteractables;
    private readonly Dictionary<MeshRenderer,bool> renderStates = new Dictionary<MeshRenderer,bool>();
    private readonly Dictionary<XRBaseInteractable,bool> interactionStates = new Dictionary<XRBaseInteractable,bool>();
    private bool captured, wingLock, shellLock;

    public IEnumerator Prepare()
    {
        if (captured) yield break;
        if (wings == null || elytra == null || flightWingRenderers == null || flightWingRenderers.Length != 2 ||
            flightWingRenderers[0] == null || flightWingRenderers[1] == null)
            throw new System.InvalidOperationException("Movement wing visibility requires both flight wing renderers and interaction references.");
        captured = true; wingLock = wings.MovementInteractionLocked; shellLock = elytra.MovementInteractionLocked;
        if (wingShellInteractables != null) foreach (var item in wingShellInteractables)
            if (item != null && !interactionStates.ContainsKey(item)) { interactionStates.Add(item,item.enabled); item.enabled = false; }
        // Existing animations finish before closing. XR requests are already suspended.
        while (wings.IsAnimating || elytra.IsAnimating) yield return null;
        if (elytra.IsOpen) elytra.ToggleElytra();
        while (wings.IsAnimating || elytra.IsAnimating) yield return null;
        if (wings.IsOpen) wings.CloseWings();
        while (wings.IsAnimating) yield return null;
        wings.MovementInteractionLocked = elytra.MovementInteractionLocked = true;
        foreach (var renderer in flightWingRenderers)
        {
            bool previous = walking != null ? walking.RendererEnabledBeforeOverride(renderer) : renderer.enabled;
            renderStates.Add(renderer,previous);
            if (walking != null) walking.SuppressRenderer(renderer,true,previous);
            else renderer.enabled = false;
        }
    }
    public void Restore()
    {
        if (!captured) return;
        foreach (var entry in renderStates) if (entry.Key != null)
        {
            if (walking != null) walking.SuppressRenderer(entry.Key,false,entry.Value);
            else entry.Key.enabled = entry.Value;
        }
        if (wings != null) wings.MovementInteractionLocked = wingLock;
        if (elytra != null) elytra.MovementInteractionLocked = shellLock;
        foreach (var entry in interactionStates) if (entry.Key != null) entry.Key.enabled = entry.Value;
        renderStates.Clear(); interactionStates.Clear(); captured = false;
    }
    private void OnDisable() { StopAllCoroutines(); Restore(); }
}
