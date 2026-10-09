using System.Collections.Generic;
using TinyWorlds;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;

/// <summary>Tank-style movement of the existing non-FBX parent. Never moves the XR Origin or camera.</summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(-2000)]
public sealed class LadybirdPlayerController : MonoBehaviour
{
    [Header("Scene references")]
    public Transform groundContact;
    public Transform facingReference;
    [Tooltip("Stationary marker outside this hierarchy. Also defines the exploration boundary centre.")]
    public Transform startingPoint;
    public Collider[] groundSurfaces = new Collider[0];
    public XROrigin xrOrigin;
    public InputActionReference leftThumbstick;
    public LadybirdAutonomousExplorer autonomousExplorer;

    [Header("Movement")]
    [Min(0f)] public float movementSpeed = 0.15f;
    [Min(0f)] public float turningSpeed = 60f;
    [Min(0.001f)] public float acceleration = 0.3f;
    [Min(0.001f)] public float deceleration = 0.5f;
    [Range(0f, 0.95f)] public float inputDeadzone = 0.15f;
    public bool editorKeyboardEnabled = true;
    public LadybirdMovementSafety safety = new LadybirdMovementSafety();

    [Header("Body exploration and interaction priority")]
    public bool enterAfterBodyExploration = true;
    [Tooltip("Prototype2b opt-in: transition UI owns activation and completion tracking.")]
    public bool externalTransitionControl;
    public bool EditorTestActive => editorTest;
    [Min(0f)] public float interactionPauseTime = 0.5f;
    public AntennaInteraction antennaInteraction;
    public WingInteraction wingInteraction;
    public ElytraInteraction elytraInteraction;
    public NarrationManager narrationManager;

    [Header("Runtime diagnostics")]
    private bool controlActive;
    private float currentSpeed;
    [SerializeField] private string movementStatus = "Inspect body parts first";
    [SerializeField] private Vector2 lastMovementInput;
    [SerializeField] private string recoveryStatus = "Input available; R reset in Editor test mode";
    public Vector2 LastMovementInput => lastMovementInput;
    public string RecoveryStatus => recoveryStatus;
    private void OnDestroy() { safety?.Dispose(); }
    public bool ControlActive => controlActive;
    public string MovementStatus => movementStatus;

    private InputAction driveAction;
    private bool actionHasDeadzone;
    private readonly Dictionary<Behaviour, bool> suspended = new Dictionary<Behaviour, bool>();
    private XRBaseInteractable[] interactables = new XRBaseInteractable[0];
    private bool antennaExplored, shellExplored, wingsExplored;
    private bool initialized, ownsMovement, editorTest;
    private Vector3 initialParentPosition;
    private Quaternion initialParentRotation;
    private Vector3 boundaryCenter;
    private float interactionUntil, readySince = -1f, lastDiagnosticTime;

    private void Awake() { initialParentPosition = transform.position; initialParentRotation = transform.rotation; }

    private void OnEnable()
    {
        controlActive = false;
        antennaExplored = shellExplored = wingsExplored = false;
        readySince = -1f;
        if (autonomousExplorer == null) autonomousExplorer = GetComponent<LadybirdAutonomousExplorer>();
        if (antennaInteraction == null) antennaInteraction = GetComponentInChildren<AntennaInteraction>(true);
        if (wingInteraction == null) wingInteraction = GetComponentInChildren<WingInteraction>(true);
        if (elytraInteraction == null) elytraInteraction = GetComponentInChildren<ElytraInteraction>(true);
        interactables = GetComponentsInChildren<XRBaseInteractable>(true);
        foreach (XRBaseInteractable item in interactables) item.selectEntered.AddListener(OnPartSelected);
    }

    private void OnDisable()
    {
        EndPlayerControl();
        foreach (XRBaseInteractable item in interactables)
            if (item != null) item.selectEntered.RemoveListener(OnPartSelected);
        interactables = new XRBaseInteractable[0];
    }

    private void OnPartSelected(SelectEnterEventArgs args)
    {
        interactionUntil = Time.time + interactionPauseTime;
        XRBaseInteractable item = args.interactableObject as XRBaseInteractable;
        if (item == null) return;
        if (externalTransitionControl) return;
        // Observe existing progress events; do not call or change the original progress tracker.
        for (int i = 0; i < item.selectEntered.GetPersistentEventCount(); i++)
        {
            string method = item.selectEntered.GetPersistentMethodName(i);
            if (method == "MarkAntennaExplored") antennaExplored = true;
            if (method == "MarkElytraExplored") shellExplored = true;
            if (method == "MarkWingExplored" && wingInteraction != null && wingInteraction.IsAnimating)
                wingsExplored = true;
        }
    }

    private bool BodyIsBusy(out string reason)
    {
        foreach (XRBaseInteractable item in interactables)
            if (item != null && item.isActiveAndEnabled && item.isSelected)
            { reason = "Body part selected"; return true; }
        if (antennaInteraction != null && antennaInteraction.IsAnimating)
        { reason = "Antennae animating"; return true; }
        if (wingInteraction != null && wingInteraction.IsAnimating)
        { reason = "Wings animating"; return true; }
        if (elytraInteraction != null && elytraInteraction.IsOpen)
        { reason = "Close the elytra before driving"; return true; }
        if (Time.time < interactionUntil) { reason = "Interaction settling"; return true; }
        reason = "Ready";
        return false;
    }

    private void Update()
    {
        if (!controlActive)
        {
            if (externalTransitionControl || !enterAfterBodyExploration || editorTest || !antennaExplored || !shellExplored || !wingsExplored) return;
            if (BodyIsBusy(out string busy) || (narrationManager != null && narrationManager.IsNarrationPlaying))
            { readySince = -1f; SetStatus(busy == "Ready" ? "Narration playing" : busy); return; }
            if (readySince < 0f) readySince = Time.time;
            // Allow the existing completion coroutine to start its narration before taking control.
            if (Time.time - readySince >= 0.8f)
            {
                if (BeginPlayerControl()) readySince = -1f;
                else readySince = Time.time; // Do not retry the full safe-start search every frame.
            }
            return;
        }
        EnforceExclusiveMovement();
        if (BodyIsBusy(out string blocked)) { currentSpeed = 0f; SetStatus(blocked); return; }

        Vector2 input = ReadDriveInput(out bool stop, out bool reset);
        lastMovementInput = input;
        if (reset) { ResetToStartingPoint(); return; }
        if (stop) { currentSpeed = 0f; SetStatus("Stopped (Space)"); return; }
        SimulateMovement(input, Mathf.Min(Time.deltaTime, 0.1f));
    }

    public bool BeginPlayerControl()
    {
        if (!Application.isPlaying || !isActiveAndEnabled) return false;
        if (controlActive) return true;
        if (!externalTransitionControl && !editorTest && enterAfterBodyExploration && (!antennaExplored || !shellExplored || !wingsExplored))
        { SetStatus("Inspect antennae, elytra and flight wings before driving"); return false; }
        if (groundContact == null || !groundContact.IsChildOf(transform) ||
            facingReference == null || !facingReference.IsChildOf(transform) ||
            startingPoint == null || startingPoint.IsChildOf(transform) ||
            xrOrigin == null || xrOrigin.transform.IsChildOf(transform) || transform.IsChildOf(xrOrigin.transform))
        { SetStatus("Invalid references: contact/facing must be children; start and XR Origin must be separate"); return false; }

        Physics.SyncTransforms();
        boundaryCenter = startingPoint.position;
        if (!safety.TryFindStart(transform, groundSurfaces, startingPoint.position, boundaryCenter,
            out Vector3 start, out string reason))
        { SetStatus("Cannot enter control: " + reason); return false; }
        if (!initialized || (externalTransitionControl && !editorTest))
        {
            transform.position += start - groundContact.position;
            initialParentPosition = transform.position;
            initialParentRotation = transform.rotation;
            initialized = true;
        }
        else if (!safety.TryPosition(groundSurfaces, groundContact.position, boundaryCenter, groundContact.position.y,
            out _, out reason)) { SetStatus(reason); return false; }

        SuspendMovement();
        CreateDriveAction();
        currentSpeed = 0f;
        controlActive = true;
        SetStatus(editorTest ? "Editor movement test: WASD / Space / R" : "Player control: left thumbstick");
        return true;
    }

    public void EndPlayerControl()
    {
        controlActive = false;
        currentSpeed = 0f;
        driveAction?.Disable(); driveAction?.Dispose(); driveAction = null;
        // Restore exact enabled states; never enable a provider that was originally disabled.
        foreach (KeyValuePair<Behaviour, bool> item in suspended)
            if (item.Key != null) item.Key.enabled = item.Value;
        suspended.Clear(); ownsMovement = false;
    }

    private void SuspendMovement()
    {
        RememberAndDisable(autonomousExplorer);
        foreach (LocomotionProvider provider in xrOrigin.GetComponentsInChildren<LocomotionProvider>(true)) RememberAndDisable(provider);
        // Providers are suspended before their Update. Keep the transformer active to avoid retaining queued motion.
        // Disabling the mediator also prevents external providers from queuing fresh locomotion.
        foreach (LocomotionMediator mediator in xrOrigin.GetComponentsInChildren<LocomotionMediator>(true)) RememberAndDisable(mediator);
#if UNITY_EDITOR
        foreach (XRInteractionSimulator simulator in FindObjectsByType<XRInteractionSimulator>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (simulator.gameObject.scene == gameObject.scene) RememberAndDisable(simulator);
#endif
        ownsMovement = true;
    }

    private void RememberAndDisable(Behaviour item)
    {
        if (item == null || suspended.ContainsKey(item)) return;
        suspended.Add(item, item.enabled); item.enabled = false;
    }

    private void EnforceExclusiveMovement()
    {
        if (!ownsMovement) return;
        foreach (Behaviour item in suspended.Keys) if (item != null && item.enabled) item.enabled = false;
    }

    private void CreateDriveAction()
    {
        actionHasDeadzone = false;
        if (leftThumbstick != null && leftThumbstick.action != null)
        {
            driveAction = leftThumbstick.action.Clone();
            foreach (InputBinding binding in driveAction.bindings)
                if (!string.IsNullOrEmpty(binding.processors) && binding.processors.ToLowerInvariant().Contains("stickdeadzone"))
                    actionHasDeadzone = true;
            if (actionHasDeadzone)
            {
                driveAction.ApplyParameterOverride("stickDeadzone:min", inputDeadzone);
                driveAction.ApplyParameterOverride("stickDeadzone:max", 1f);
            }
        }
        else
        {
            actionHasDeadzone = false;
            driveAction = new InputAction("Ladybird Left Thumbstick", InputActionType.Value,
                "<XRController>{LeftHand}/primary2DAxis", expectedControlType: "Vector2");
        }
        // The private clone remains readable when controller action managers disable locomotion actions.
        driveAction.Enable();
    }

    private Vector2 ReadDriveInput(out bool stop, out bool reset)
    {
        stop = reset = false;
        Vector2 input = driveAction != null ? driveAction.ReadValue<Vector2>() : Vector2.zero;
        if (!actionHasDeadzone) input = LadybirdMovementSafety.ApplyDeadzone(input, inputDeadzone);
#if UNITY_EDITOR
        Keyboard keyboard = Keyboard.current;
        bool gameViewFocused = UnityEditor.EditorWindow.focusedWindow != null &&
            UnityEditor.EditorWindow.focusedWindow.GetType().Name == "GameView";
        if (editorKeyboardEnabled && keyboard != null && gameViewFocused)
        {
            Vector2 keys = new Vector2((keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f),
                (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f));
            if (keys != Vector2.zero) input = keys;
            stop = keyboard.spaceKey.isPressed;
            reset = editorTest && keyboard.rKey.wasPressedThisFrame;
        }
#endif
        return new Vector2(Mathf.Clamp(input.x, -1f, 1f), Mathf.Clamp(input.y, -1f, 1f));
    }

    // Public for deterministic movement checks; ordinary gameplay calls this only from Update.
    public void SimulateMovement(Vector2 input, float deltaTime)
    {
        if (!controlActive || !isActiveAndEnabled || deltaTime <= 0f) return;
        lastMovementInput = input;
        recoveryStatus = "Turning available on supported ground; reverse/slide checked per direction; R reset in Editor test mode";
        EnforceExclusiveMovement();
        if (BodyIsBusy(out string busy)) { currentSpeed = 0f; SetStatus(busy); return; }
        Physics.SyncTransforms();
        Vector3 contact = groundContact.position;
        if (!safety.TrySupportedPosition(groundSurfaces, contact, contact.y, out Vector3 ground, out string reason))
        { currentSpeed = 0f; recoveryStatus = "Ground unsafe: movement and turning halted; R reset in Editor test mode"; SetStatus(reason); return; }
        transform.position += ground - contact;
        contact = groundContact.position;
        float targetSpeed = Mathf.Clamp(input.y, -1f, 1f) * movementSpeed;
        bool slowing = Mathf.Abs(targetSpeed) < Mathf.Abs(currentSpeed) || currentSpeed * targetSpeed < 0f;
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, (slowing ? deceleration : acceleration) * deltaTime);
        // The footprint is a conservative circle, so yaw cannot expand it beyond a checked boundary.
        transform.RotateAround(contact, Vector3.up, Mathf.Clamp(input.x, -1f, 1f) * turningSpeed * deltaTime);
        Vector3 heading = facingReference.forward;
        heading.y = 0f;
        if (heading.sqrMagnitude < 0.0001f) { currentSpeed = 0f; SetStatus("Facing reference is vertical"); return; }
        Vector3 direction = heading.normalized * Mathf.Sign(currentSpeed);
        float distance = Mathf.Abs(currentSpeed) * deltaTime;
        // Substeps prevent gaps being skipped at high speeds or after a slow frame.
        float stepLimit = Mathf.Min(safety.maximumMovementStep, safety.bodyRadius * 0.25f);
        if (stepLimit <= 0f || distance > stepLimit * 128f)
        { currentSpeed = 0f; SetStatus("Movement sampling limit exceeded"); return; }
        string motionStatus = "Moving";
        while (distance > 0.000001f)
        {
            float step = Mathf.Min(distance, stepLimit);
            contact = groundContact.position;
            if (!safety.TryObstacleMotion(transform, contact, direction * step, groundSurfaces, out Vector3 motion, out reason))
            { ReportObstacleBlock(reason); return; }
            if (reason.StartsWith("Sliding")) motionStatus = reason;
            Vector3 target = contact + motion;
            if (!safety.TryMovementPosition(groundSurfaces, contact, target, boundaryCenter, out Vector3 next, out reason) ||
                !safety.TryObstacleMotion(transform, contact, next-contact, groundSurfaces, out Vector3 verified, out reason) ||
                (verified-(next-contact)).sqrMagnitude > 0.00000001f)
            { currentSpeed = 0f; SetStatus(reason == "Ready" ? "Obstacle at grounded destination" : reason); return; }
            transform.position += next - contact;
            distance -= step;
        }
        SetStatus(Mathf.Abs(currentSpeed) > 0.0001f ? motionStatus : "Stopped (turning available)");
    }

    private void ReportObstacleBlock(string reason)
    {
        currentSpeed = 0f;
        Vector3 backward = -facingReference.forward; backward.y = 0f;
        bool reverse = backward.sqrMagnitude > 0.0001f &&
            safety.TryObstacleMotion(transform, groundContact.position, backward.normalized * safety.maximumMovementStep,
                groundSurfaces, out Vector3 escape, out _) &&
            safety.TryMovementPosition(groundSurfaces, groundContact.position, groundContact.position + escape,
                boundaryCenter, out _, out _);
        recoveryStatus = "Turning available; reverse " + (reverse ? "available" : "blocked in this heading") + "; R reset in Editor test mode";
        SetStatus(reason);
    }

    public bool ResetToStartingPoint()
    {
#if UNITY_EDITOR
        if (!editorTest || !controlActive || !initialized) return false;
        Physics.SyncTransforms();
        Vector3 resetContact = initialParentPosition + initialParentRotation *
            Quaternion.Inverse(transform.rotation) * (groundContact.position - transform.position);
        if (!safety.TryPosition(groundSurfaces, resetContact, boundaryCenter, resetContact.y, out Vector3 ground, out string reason) ||
            !safety.PathIsClear(transform, ground, Vector3.forward, 0f, out reason, groundSurfaces))
        { SetStatus("Reset blocked: " + reason); return false; }
        transform.SetPositionAndRotation(initialParentPosition + ground - resetContact, initialParentRotation);
        currentSpeed = 0f; SetStatus("Reset to verified start"); return true;
#else
        return false;
#endif
    }

    public void SetEditorTestMode(bool active)
    {
#if UNITY_EDITOR
        editorTest = active;
#endif
    }

    private void SetStatus(string value)
    {
        if (movementStatus == value) return;
        movementStatus = value;
        if (editorTest && Time.unscaledTime - lastDiagnosticTime >= 0.5f)
        { Debug.Log("Ladybird: " + value, this); lastDiagnosticTime = Time.unscaledTime; }
    }

    private void OnValidate()
    {
        movementSpeed = Mathf.Max(0f, movementSpeed);
        turningSpeed = Mathf.Max(0f, turningSpeed);
        acceleration = Mathf.Max(0.001f, acceleration);
        deceleration = Mathf.Max(0.001f, deceleration);
        inputDeadzone = Mathf.Clamp(inputDeadzone, 0f, 0.95f);
        interactionPauseTime = Mathf.Max(0f, interactionPauseTime);
        if (safety == null) safety = new LadybirdMovementSafety();
        safety.bodyRadius = Mathf.Max(0.01f, safety.bodyRadius);
        safety.explorationRadius = Mathf.Max(safety.bodyRadius + 0.01f, safety.explorationRadius);
        safety.boundaryHalfExtents = Vector2.Max(Vector2.one * 0.01f, safety.boundaryHalfExtents);
        safety.maximumStepHeight = Mathf.Max(0f, safety.maximumStepHeight);
        safety.maximumDrop = Mathf.Max(0f, safety.maximumDrop);
        safety.probeHeight = Mathf.Max(safety.maximumStepHeight + 0.01f, safety.probeHeight);
        safety.probeDepth = Mathf.Max(safety.maximumDrop + 0.01f, safety.probeDepth);
        safety.maximumSlope = Mathf.Clamp(safety.maximumSlope, 0f, 60f);
        safety.obstacleClearance = Mathf.Max(0.01f, safety.obstacleClearance);
        safety.maximumMovementStep = Mathf.Max(0.001f, safety.maximumMovementStep);
    }

    private void OnDrawGizmosSelected()
    {
        if (groundContact == null || safety == null) return;
        Vector3 center = startingPoint != null ? startingPoint.position : groundContact.position;
        Gizmos.color = Color.cyan;
        if (safety.boundaryShape == LadybirdMovementSafety.BoundaryShape.WorldRectangle)
        {
            Vector3 outer = new Vector3(safety.boundaryHalfExtents.x * 2f, 0f, safety.boundaryHalfExtents.y * 2f);
            Gizmos.DrawWireCube(center, outer);
            Vector2 reach = Vector2.Max(Vector2.zero, safety.boundaryHalfExtents - Vector2.one * safety.bodyRadius);
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(center, new Vector3(reach.x * 2f, 0f, reach.y * 2f));
        }
        else for (int i = 0; i < 64; i++)
        {
            float a = i * Mathf.PI * 2f / 64f, b = (i + 1) * Mathf.PI * 2f / 64f;
            Gizmos.DrawLine(center + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * safety.explorationRadius,
                center + new Vector3(Mathf.Cos(b), 0f, Mathf.Sin(b)) * safety.explorationRadius);
        }
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundContact.position + Vector3.up * (safety.bodyRadius + safety.obstacleClearance), safety.bodyRadius);
        if (facingReference != null) Gizmos.DrawRay(groundContact.position, facingReference.forward * safety.bodyRadius);
    }
}
