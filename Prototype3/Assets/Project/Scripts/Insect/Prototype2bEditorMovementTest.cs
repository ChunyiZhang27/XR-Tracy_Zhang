using UnityEngine;

/// <summary>Editor-only, Play-mode bootstrap. In a player build this component has no behaviour.</summary>
[DefaultExecutionOrder(-10000)]
public sealed class Prototype2bEditorMovementTest : MonoBehaviour
{
#if UNITY_EDITOR
    [Tooltip("Temporary Play-mode bypass. Has no effect in Quest/player builds.")]
    public bool runMovementTestOnPlay;
    public LadybirdPlayerController controller;
    public ExperienceManager experienceManager;
    public GameObject onboardingZone;
    public GameObject selectionZone;
    public GameObject insectExploreZone;

    private bool running;
    private bool managerWasEnabled, onboardingWasActive, selectionWasActive, exploreWasActive;

    private void Awake()
    {
        if (!runMovementTestOnPlay || gameObject.scene.path != "Assets/Project/Scenes/Prototype2b.unity") return;
        if (controller == null || experienceManager == null || onboardingZone == null ||
            selectionZone == null || insectExploreZone == null)
        { Debug.LogError("Prototype2b movement test is missing references. Run the configuration menu first.", this); return; }
        managerWasEnabled = experienceManager.enabled;
        onboardingWasActive = onboardingZone.activeSelf;
        selectionWasActive = selectionZone.activeSelf;
        exploreWasActive = insectExploreZone.activeSelf;
        // Prevent the original manager's Start from starting onboarding. Never call its progress/reset methods.
        experienceManager.enabled = false;
        running = true;
    }

    private void Start()
    {
        if (!running) return;
        onboardingZone.SetActive(false);
        selectionZone.SetActive(false);
        insectExploreZone.SetActive(true);
        controller.SetEditorTestMode(true);
        if (!controller.BeginPlayerControl())
            Debug.LogError("Movement test could not start: " + controller.MovementStatus, controller);
        else
            Debug.Log("Prototype2b movement test active. Focus Game view: W/S drive, A/D turn, Space stop, R reset. " +
                "Select LadybirdExploreModel_OLD to inspect Movement Status. XR viewpoint is not repositioned.", this);
    }

    private void OnDisable()
    {
        if (!running) return;
        if (controller != null) { controller.EndPlayerControl(); controller.SetEditorTestMode(false); }
        if (onboardingZone != null) onboardingZone.SetActive(onboardingWasActive);
        if (selectionZone != null) selectionZone.SetActive(selectionWasActive);
        if (insectExploreZone != null) insectExploreZone.SetActive(exploreWasActive);
        if (experienceManager != null) experienceManager.enabled = managerWasEnabled;
        running = false;
    }
#endif
}
