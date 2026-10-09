using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Moves a dedicated parent of the Ladybird, leaving every model-local transform alone.
/// Requires solid ground colliders; never falls or crosses an unsupported footprint.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(100)]
public sealed class LadybirdAutonomousExplorer : MonoBehaviour
{
    public enum ExplorationState { WaitingForGround, Inspecting, Walking, Interacting }

    [Header("References")]
    [Tooltip("A child marker at ground level beneath the Ladybird. Only its parent is moved. " +
             "If empty, this object's origin is the contact point.")]
    [SerializeField] private Transform groundContact;
    [Tooltip("The model root whose positive Z points toward its head. Preserves its existing rotation; " +
             "only the movement parent rotates. If empty, uses this parent's forward direction.")]
    [SerializeField] private Transform facingReference;
    [Tooltip("Optional stationary area marker outside the moving hierarchy. Otherwise uses the initial contact position.")]
    [SerializeField] private Transform explorationCenter;
    [Tooltip("Optional allowed ground colliders. Assign the leaf collider to avoid accepting scenery or the floor below it.")]
    [SerializeField] private Collider[] groundSurfaces = new Collider[0];

    [Header("Exploration (world metres and seconds)")]
    [Min(0f)] [SerializeField] private float movementSpeed = 0.06f;
    [Min(0f)] [SerializeField] private float turningSpeed = 35f;
    [Min(0.01f)] [SerializeField] private float explorationRadius = 1.25f;
    [Min(0.01f)] [SerializeField] private float arrivalDistance = 0.04f;
    [Min(0f)] [SerializeField] private float minimumPauseTime = 1.5f;
    [Min(0f)] [SerializeField] private float maximumPauseTime = 4f;
    [Min(0.1f)] [SerializeField] private float minimumWalkingTime = 3f;
    [Min(0.1f)] [SerializeField] private float maximumWalkingTime = 8f;
    [Range(0f, 90f)] [SerializeField] private float movementHeadingTolerance = 20f;

    [Header("Ground and obstacle safety")]
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField] private LayerMask obstacleMask = ~0;
    [Tooltip("Conservative horizontal radius of the CLOSED Ladybird, including its feet.")]
    [Min(0.01f)] [SerializeField] private float bodyRadius = 0.35f;
    [Min(0.01f)] [SerializeField] private float groundProbeHeight = 0.6f;
    [Min(0.01f)] [SerializeField] private float groundProbeDepth = 0.6f;
    [Min(0f)] [SerializeField] private float maximumStepHeight = 0.06f;
    [Min(0f)] [SerializeField] private float maximumGroundDrop = 0.06f;
    [Range(0f, 60f)] [SerializeField] private float maximumGroundSlope = 30f;
    [Min(0f)] [SerializeField] private float edgeLookAhead = 0.12f;
    [Min(0.01f)] [SerializeField] private float obstacleClearance = 0.03f;
    [Tooltip("Caps movement sampling after a slow frame to avoid jumping across gaps.")]
    [Min(0.001f)] [SerializeField] private float maximumMovementStep = 0.02f;
    [Min(0.1f)] [SerializeField] private float retryDelay = 1f;

    [Header("Existing interaction priority")]
    [SerializeField] private bool pauseWhileHovered = true;
    [Min(0f)] [SerializeField] private float interactionResumeDelay = 2f;
    [Tooltip("Optional; automatically found in children when empty.")]
    [SerializeField] private AntennaInteraction antennaInteraction;
    [SerializeField] private WingInteraction wingInteraction;
    [SerializeField] private ElytraInteraction elytraInteraction;

    private readonly RaycastHit[] groundHits = new RaycastHit[64];
    private readonly RaycastHit[] obstacleHits = new RaycastHit[64];
    private readonly Collider[] overlappingObstacles = new Collider[64];
    private static readonly Vector3[] FootprintDirections =
    {
        Vector3.forward, Vector3.back, Vector3.left, Vector3.right,
        new Vector3(0.7071068f, 0f, 0.7071068f),
        new Vector3(-0.7071068f, 0f, 0.7071068f),
        new Vector3(0.7071068f, 0f, -0.7071068f),
        new Vector3(-0.7071068f, 0f, -0.7071068f)
    };

    private XRBaseInteractable[] interactables = new XRBaseInteractable[0];
    private Vector3 initialCenter;
    private Vector3 destination;
    private float pauseUntil;
    private float walkingUntil;
    private float interactionUntil;
    private bool centerCaptured;
    private bool manualPause;
    private bool initialized;
    private bool configurationValid;

    public ExplorationState State { get; private set; } = ExplorationState.WaitingForGround;
    public Vector3 Destination => destination;
    private Vector3 ContactPosition => groundContact != null ? groundContact.position : transform.position;
    private Vector3 Center => explorationCenter != null ? explorationCenter.position : initialCenter;

    private void OnEnable()
    {
        configurationValid = (groundContact == null || groundContact.IsChildOf(transform)) &&
                             (facingReference == null || facingReference.IsChildOf(transform)) &&
                             (explorationCenter == null || !explorationCenter.IsChildOf(transform));
        if (!configurationValid)
        {
            Debug.LogWarning("Ladybird explorer needs contact/facing references in its hierarchy and an area center outside it.", this);
            return;
        }

        if (antennaInteraction == null) antennaInteraction = GetComponentInChildren<AntennaInteraction>(true);
        if (wingInteraction == null) wingInteraction = GetComponentInChildren<WingInteraction>(true);
        if (elytraInteraction == null) elytraInteraction = GetComponentInChildren<ElytraInteraction>(true);

        // Runtime listeners supplement existing events without changing their serialized wiring.
        interactables = GetComponentsInChildren<XRBaseInteractable>(true);
        foreach (XRBaseInteractable interactable in interactables)
        {
            interactable.selectEntered.AddListener(OnSelected);
            interactable.activated.AddListener(OnActivated);
        }

        initialized = false;
        State = ExplorationState.WaitingForGround;
        pauseUntil = Time.time;
        interactionUntil = Time.time;
    }

    private void OnDisable()
    {
        foreach (XRBaseInteractable interactable in interactables)
        {
            if (interactable == null) continue;
            interactable.selectEntered.RemoveListener(OnSelected);
            interactable.activated.RemoveListener(OnActivated);
        }
        interactables = new XRBaseInteractable[0];
        initialized = false;
        State = ExplorationState.WaitingForGround;
    }

    /// <summary>Can also be called by an existing event; does not replace that event's listeners.</summary>
    public void PauseForInteraction() => interactionUntil = Time.time + interactionResumeDelay;

    /// <summary>Optional explicit pause for application-controlled inspection.</summary>
    public void SetPaused(bool paused)
    {
        manualPause = paused;
        if (!paused) PauseForInteraction();
    }

    private void OnSelected(SelectEnterEventArgs args) => PauseForInteraction();
    private void OnActivated(ActivateEventArgs args) => PauseForInteraction();

    private bool InteractionHasPriority()
    {
        if (manualPause) return true;
        foreach (XRBaseInteractable interactable in interactables)
        {
            if (interactable != null && interactable.isActiveAndEnabled &&
                (interactable.isSelected || (pauseWhileHovered && interactable.isHovered)))
                return true;
        }
        return (antennaInteraction != null && antennaInteraction.IsAnimating) ||
               (wingInteraction != null && wingInteraction.IsAnimating) ||
               (elytraInteraction != null && elytraInteraction.IsOpen);
    }

    private void Update()
    {
        if (!configurationValid) return;
        if (InteractionHasPriority())
        {
            PauseForInteraction();
            State = ExplorationState.Interacting;
            return;
        }
        if (Time.time < interactionUntil) return;
        if (State == ExplorationState.Interacting && initialized)
            BeginPause();

        Vector3 contact = ContactPosition;
        if (!centerCaptured)
        {
            initialCenter = contact;
            centerCaptured = true;
        }
        if (!initialized)
        {
            if (Time.time < pauseUntil) return;
            if (!TrySupportedPosition(contact, contact.y, out Vector3 grounded))
            {
                State = ExplorationState.WaitingForGround;
                pauseUntil = Time.time + retryDelay;
                return;
            }
            // The explicit contact marker lets the imported model retain all local poses and scale.
            transform.position += grounded - contact;
            initialized = true;
            BeginPause();
            return;
        }

        if (State == ExplorationState.Inspecting)
        {
            if (Time.time >= pauseUntil && movementSpeed > 0f && turningSpeed > 0f)
                ChooseDestination();
            return;
        }

        if (!TrySupportedPosition(contact, contact.y, out Vector3 currentGround))
        {
            initialized = false;
            State = ExplorationState.WaitingForGround;
            pauseUntil = Time.time + retryDelay;
            return;
        }
        transform.position += currentGround - contact;
        contact = ContactPosition;
        Vector3 delta = destination - contact;
        delta.y = 0f;
        if (delta.magnitude <= arrivalDistance || Time.time >= walkingUntil)
        {
            BeginPause();
            return;
        }

        Vector3 direction = delta.normalized;
        // Rotate the dedicated parent around the contact point, even when the model has an offset.
        Vector3 facing = facingReference != null ? facingReference.forward : transform.forward;
        facing.y = 0f;
        if (facing.sqrMagnitude < 0.0001f) return;
        float angle = Vector3.SignedAngle(facing, direction, Vector3.up);
        float turn = Mathf.Clamp(angle, -turningSpeed * Time.deltaTime, turningSpeed * Time.deltaTime);
        transform.RotateAround(contact, Vector3.up, turn);
        if (Mathf.Abs(angle - turn) > movementHeadingTolerance) return;

        float step = Mathf.Min(movementSpeed * Time.deltaTime, maximumMovementStep, delta.magnitude);
        Vector3 proposed = contact + direction * step;
        Vector3 ahead = contact + direction * Mathf.Max(step, edgeLookAhead);
        if (!TrySupportedPosition(proposed, currentGround.y, out Vector3 nextGround) ||
            !TrySupportedPosition(ahead, currentGround.y, out _) ||
            !PathIsClear(contact, direction, Mathf.Max(step, edgeLookAhead)))
        {
            BeginPause();
            return;
        }
        transform.position += nextGround - contact;
    }

    private void BeginPause()
    {
        State = ExplorationState.Inspecting;
        pauseUntil = Time.time + Random.Range(minimumPauseTime, maximumPauseTime);
    }

    private void ChooseDestination()
    {
        Vector3 contact = ContactPosition;
        float usableRadius = explorationRadius - bodyRadius;
        if (usableRadius > arrivalDistance)
        {
            for (int attempt = 0; attempt < 12; attempt++)
            {
                Vector2 random = Random.insideUnitCircle * usableRadius;
                Vector3 candidate = new Vector3(Center.x + random.x, contact.y, Center.z + random.y);
                Vector3 delta = candidate - contact;
                delta.y = 0f;
                if (delta.magnitude <= arrivalDistance * 2f) continue;
                if (!TrySupportedPosition(candidate, contact.y, out Vector3 ground)) continue;
                // Verify a short initial corridor; every actual movement step is checked again.
                if (!PathIsClear(contact, delta.normalized, Mathf.Min(delta.magnitude, edgeLookAhead))) continue;
                destination = ground;
                walkingUntil = Time.time + Random.Range(minimumWalkingTime, maximumWalkingTime);
                State = ExplorationState.Walking;
                return;
            }
        }
        State = ExplorationState.Inspecting;
        pauseUntil = Time.time + Mathf.Max(retryDelay, minimumPauseTime);
    }

    private bool InsideBoundary(Vector3 point)
    {
        Vector3 offset = point - Center;
        offset.y = 0f;
        float allowedRadius = explorationRadius - bodyRadius;
        return allowedRadius >= 0f && offset.sqrMagnitude <= allowedRadius * allowedRadius;
    }

    private bool TrySupportedPosition(Vector3 point, float referenceHeight, out Vector3 grounded)
    {
        grounded = point;
        if (!InsideBoundary(point) || !TryGround(point, referenceHeight, out RaycastHit centerHit)) return false;
        foreach (Vector3 direction in FootprintDirections)
        {
            if (!TryGround(point + direction * bodyRadius, centerHit.point.y, out _)) return false;
        }
        grounded.y = centerHit.point.y;
        return true;
    }

    private bool TryGround(Vector3 point, float referenceHeight, out RaycastHit ground)
    {
        ground = default;
        Vector3 origin = new Vector3(point.x, referenceHeight + groundProbeHeight, point.z);
        int count = Physics.RaycastNonAlloc(origin, Vector3.down, groundHits,
            groundProbeHeight + groundProbeDepth, groundMask, QueryTriggerInteraction.Ignore);
        // A full buffer might omit a nearer unsafe surface: fail conservatively.
        if (count == groundHits.Length) return false;
        float nearest = float.PositiveInfinity;
        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = groundHits[i];
            if (IsOwnCollider(hit.collider) || !IsAllowedGround(hit.collider) || hit.distance >= nearest) continue;
            nearest = hit.distance;
            ground = hit;
        }
        if (float.IsPositiveInfinity(nearest)) return false;
        float heightChange = ground.point.y - referenceHeight;
        return heightChange <= maximumStepHeight && heightChange >= -maximumGroundDrop &&
               Vector3.Angle(ground.normal, Vector3.up) <= maximumGroundSlope;
    }

    private bool IsAllowedGround(Collider candidate)
    {
        if (groundSurfaces == null || groundSurfaces.Length == 0) return true;
        foreach (Collider surface in groundSurfaces)
            if (surface != null && candidate == surface) return true;
        return false;
    }

    private bool IsOwnCollider(Collider candidate) =>
        candidate != null && candidate.transform.IsChildOf(transform);

    private bool PathIsClear(Vector3 contact, Vector3 direction, float distance)
    {
        Vector3 origin = contact + Vector3.up * (bodyRadius + obstacleClearance);
        int overlaps = Physics.OverlapSphereNonAlloc(origin, bodyRadius, overlappingObstacles,
            obstacleMask, QueryTriggerInteraction.Ignore);
        if (overlaps == overlappingObstacles.Length) return false;
        for (int i = 0; i < overlaps; i++)
            if (!IsOwnCollider(overlappingObstacles[i])) return false;

        int hits = Physics.SphereCastNonAlloc(origin, bodyRadius, direction, obstacleHits,
            distance, obstacleMask, QueryTriggerInteraction.Ignore);
        if (hits == obstacleHits.Length) return false;
        for (int i = 0; i < hits; i++)
            if (!IsOwnCollider(obstacleHits[i].collider)) return false;
        return true;
    }

    private void OnValidate()
    {
        movementSpeed = Mathf.Max(0f, movementSpeed);
        turningSpeed = Mathf.Max(0f, turningSpeed);
        bodyRadius = Mathf.Max(0.01f, bodyRadius);
        explorationRadius = Mathf.Max(bodyRadius + 0.01f, explorationRadius);
        arrivalDistance = Mathf.Max(0.01f, arrivalDistance);
        minimumPauseTime = Mathf.Max(0f, minimumPauseTime);
        maximumPauseTime = Mathf.Max(minimumPauseTime, maximumPauseTime);
        minimumWalkingTime = Mathf.Max(0.1f, minimumWalkingTime);
        maximumWalkingTime = Mathf.Max(minimumWalkingTime, maximumWalkingTime);
        maximumMovementStep = Mathf.Max(0.001f, maximumMovementStep);
        maximumStepHeight = Mathf.Max(0f, maximumStepHeight);
        maximumGroundDrop = Mathf.Max(0f, maximumGroundDrop);
        groundProbeHeight = Mathf.Max(maximumStepHeight + 0.01f, groundProbeHeight);
        groundProbeDepth = Mathf.Max(maximumGroundDrop + 0.01f, groundProbeDepth);
        obstacleClearance = Mathf.Max(0.01f, obstacleClearance);
        edgeLookAhead = Mathf.Max(0f, edgeLookAhead);
        retryDelay = Mathf.Max(0.1f, retryDelay);
        interactionResumeDelay = Mathf.Max(0f, interactionResumeDelay);
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 center = explorationCenter != null ? explorationCenter.position :
            (Application.isPlaying && centerCaptured ? initialCenter : ContactPosition);
        Gizmos.color = Color.cyan;
        const int segments = 64;
        for (int i = 0; i < segments; i++)
        {
            float a = i * Mathf.PI * 2f / segments;
            float b = (i + 1) * Mathf.PI * 2f / segments;
            Gizmos.DrawLine(center + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * explorationRadius,
                center + new Vector3(Mathf.Cos(b), 0f, Mathf.Sin(b)) * explorationRadius);
        }
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(ContactPosition + Vector3.up * (bodyRadius + obstacleClearance), bodyRadius);
        if (Application.isPlaying && State == ExplorationState.Walking)
            Gizmos.DrawLine(ContactPosition, destination);
    }
}
