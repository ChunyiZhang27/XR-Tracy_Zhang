using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;

/// <summary>Scene-local handover to the existing XRI move provider, with a collision-constrained horizontal habitat area.</summary>
[DefaultExecutionOrder(-1500)]
public sealed class FoodSearchPlayerLocomotion : MonoBehaviour, IConstrainedXRBodyManipulator
{
    public FoodSearchActivity activity;
    public ContinuousMoveProvider moveProvider;
    public XRBodyTransformer transformer;
    public InputActionReference leftMove;
    [Min(.05f)] public float moveSpeed = .6f;
    [Min(.5f)] public float maximumTravel = 6f;
    [Min(.1f)] public float habitatPadding = 1f;
    [Min(.01f)] public float supportTolerance = .08f;
    public bool Active { get; private set; }
    [SerializeField] string movementStatus = "Waiting for Food Search";
    public string MovementStatus => movementStatus;
    public XRMovableBody linkedBody { get; private set; }
    public CollisionFlags lastCollisionFlags { get; private set; }
    public bool isGrounded => true;
    CharacterController capsule;
    IConstrainedXRBodyManipulator previousManipulator;
    XRInputValueReader<Vector2> previousLeft, previousRight;
    bool previousFly, previousStrafe;
    float previousSpeed, previousStep, floorY;
    Vector3 entry;
    Bounds habitatBounds;
    readonly RaycastHit[] hits = new RaycastHit[64];

    bool Wanted => activity != null && activity.Running && !activity.Completed && !activity.Finished &&
        activity.objective != null && activity.objective.FoodSearchActive &&
        activity.objective.controller.ControlActive && activity.objective.controller.HabitatStopped;
    void Update()
    {
        if (Wanted && !Active) Begin();
        else if (!Wanted && Active) End();
        if (Active) { moveProvider.moveSpeed = moveSpeed; activity.objective.controller.SetFoodLocomotion(moveProvider, moveProvider.mediator, true); }
    }
    void Begin()
    {
        if (moveProvider == null || transformer == null || leftMove == null || leftMove.action == null)
        { movementStatus = "Missing existing locomotion/input references"; return; }
        var origin = activity.objective.controller.xrOrigin;
        capsule = origin.Origin.GetComponent<CharacterController>();
        if (capsule == null || !capsule.enabled || !transformer.enabled)
        { movementStatus = "Player CharacterController or body transformer unavailable"; return; }
        entry = origin.Camera.transform.position; entry.y = origin.Origin.transform.position.y;
        floorY = entry.y;
        habitatBounds = new Bounds(entry, Vector3.zero);
        habitatBounds.Encapsulate(activity.objective.habitatTarget.position);
        foreach (var target in activity.targets) if (target != null) habitatBounds.Encapsulate(target.transform.position);
        habitatBounds.Expand(new Vector3(habitatPadding * 2, 0, habitatPadding * 2));
        previousManipulator = transformer.constrainedBodyManipulator;
        previousLeft = moveProvider.leftHandMoveInput; previousRight = moveProvider.rightHandMoveInput;
        previousSpeed = moveProvider.moveSpeed; previousFly = moveProvider.enableFly; previousStrafe = moveProvider.enableStrafe;
        previousStep = capsule.stepOffset; capsule.stepOffset = 0f;
        moveProvider.leftHandMoveInput = new XRInputValueReader<Vector2>("Food Search left move") { inputActionReference = leftMove };
        moveProvider.rightHandMoveInput = new XRInputValueReader<Vector2>("Food Search no right move");
        moveProvider.enableFly = false; moveProvider.enableStrafe = true; moveProvider.moveSpeed = moveSpeed;
        // Existing provider and mediator stay responsible for input and queued movement. Only its constraint is wrapped.
        transformer.constrainedBodyManipulator = this;
        Active = true;
        movementStatus = "Food Search: left stick moves player; Ladybird stopped";
        activity.objective.controller.SetFoodLocomotion(moveProvider, moveProvider.mediator, true);
    }
    void End()
    {
        Active = false;
        movementStatus = "Food Search locomotion stopped";
        if (activity != null && activity.objective != null && activity.objective.controller != null)
            activity.objective.controller.SetFoodLocomotion(moveProvider, moveProvider != null ? moveProvider.mediator : null, false);
        if (transformer != null) transformer.constrainedBodyManipulator = previousManipulator;
        if (moveProvider != null)
        {
            moveProvider.leftHandMoveInput = previousLeft; moveProvider.rightHandMoveInput = previousRight;
            moveProvider.moveSpeed = previousSpeed; moveProvider.enableFly = previousFly; moveProvider.enableStrafe = previousStrafe;
        }
        if (capsule != null) capsule.stepOffset = previousStep;
    }
    public void OnLinkedToBody(XRMovableBody body) { linkedBody = body; }
    public void OnUnlinkedFromBody() { linkedBody = null; }
    public bool InsideArea(Vector3 position)
    {
        var delta = position - entry; delta.y = 0;
        float margin = capsule != null ? capsule.radius * capsule.transform.lossyScale.x + capsule.skinWidth : 0f;
        float radius = Mathf.Max(0f, maximumTravel - margin);
        return delta.sqrMagnitude <= radius * radius &&
            position.x >= habitatBounds.min.x + margin && position.x <= habitatBounds.max.x - margin &&
            position.z >= habitatBounds.min.z + margin && position.z <= habitatBounds.max.z - margin;
    }
    bool Supported(Vector3 point)
    {
        int count = Physics.RaycastNonAlloc(new Vector3(point.x, floorY + supportTolerance, point.z), Vector3.down,
            hits, supportTolerance * 2, ~0, QueryTriggerInteraction.Ignore);
        if (count == hits.Length) return false;
        for (int i = 0; i < count; i++)
        {
            var hit = hits[i];
            if (hit.collider == capsule || hit.collider.transform.IsChildOf(capsule.transform) ||
                hit.collider.GetComponentInParent<FoodSearchTarget>() != null) continue;
            if (Vector3.Angle(hit.normal, Vector3.up) <= capsule.slopeLimit) return true;
        }
        return false;
    }
    public CollisionFlags MoveBody(Vector3 motion)
    {
        // A queued transformation arriving just after completion must not move the player.
        if (!Active || !Wanted || linkedBody == null || capsule == null || !capsule.enabled) return CollisionFlags.None;
        motion.y = 0f;
        motion = Vector3.ClampMagnitude(motion, moveSpeed * Mathf.Min(Time.deltaTime, .1f));
        var local = linkedBody.GetBodyGroundLocalPosition();
        float height = Mathf.Max(capsule.radius * 2, linkedBody.xrOrigin.CameraInOriginSpaceHeight - local.y);
        capsule.height = height;
        capsule.center = new Vector3(local.x, local.y + height * .5f + capsule.skinWidth, local.z);
        var destination = linkedBody.GetBodyGroundWorldPosition() + motion;
        if (!InsideArea(destination)) { movementStatus = "Safe player area boundary"; return CollisionFlags.Sides; }
        float radius = capsule.radius * capsule.transform.lossyScale.x + capsule.skinWidth;
        if (!Supported(destination)) { movementStatus = "No level support at player destination"; return CollisionFlags.Below; }
        for (int i = 0; i < 8; i++)
        {
            float angle = i * Mathf.PI / 4;
            if (!Supported(destination + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius))
            { movementStatus = "Player footprint would cross unsupported ground"; return CollisionFlags.Below; }
        }
        lastCollisionFlags = capsule.Move(motion);
        movementStatus = lastCollisionFlags == CollisionFlags.None ? "Food Search player movement" : "Player capsule constrained by environment";
        return lastCollisionFlags;
    }
    void OnDisable() { if (Active) End(); }
    void OnDrawGizmosSelected()
    {
        if (!Active) return;
        Gizmos.color = Color.green;
        var center = habitatBounds.center; center.y = floorY;
        var size = habitatBounds.size; size.y = .02f;
        Gizmos.DrawWireCube(center, size);
        for (int i = 0; i < 64; i++)
        {
            float a = i * Mathf.PI / 32, b = (i + 1) * Mathf.PI / 32;
            Gizmos.DrawLine(entry + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * maximumTravel,
                entry + new Vector3(Mathf.Cos(b), 0, Mathf.Sin(b)) * maximumTravel);
        }
    }
}
