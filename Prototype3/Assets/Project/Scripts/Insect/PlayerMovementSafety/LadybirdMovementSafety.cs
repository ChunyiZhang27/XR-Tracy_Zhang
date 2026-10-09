using System;
using UnityEngine;

namespace TinyWorlds
{
    /// <summary>Shared, fail-closed surface checks derived from the autonomous explorer.</summary>
    [Serializable]
    public sealed class LadybirdMovementSafety : IDisposable
    {
        [Min(0.01f)] public float bodyRadius = 0.35f;
        [Min(0.01f)] public float explorationRadius = 1.25f;
        public enum BoundaryShape { Circle, WorldRectangle }
        public BoundaryShape boundaryShape = BoundaryShape.Circle;
        [Tooltip("World X/Z outer half extents around the start marker. The body radius is subtracted; ground probes still determine actual support.")]
        public Vector2 boundaryHalfExtents = new Vector2(1.25f, 1.25f);
        [Min(0.01f)] public float probeHeight = 0.6f;
        [Min(0.01f)] public float probeDepth = 0.6f;
        [Min(0f)] public float maximumStepHeight = 0.06f;
        [Min(0f)] public float maximumDrop = 0.06f;
        [Range(0f, 60f)] public float maximumSlope = 30f;
        [Min(0.01f)] public float obstacleClearance = 0.03f;
        [Min(0.001f)] public float maximumMovementStep = 0.02f;
        public LayerMask obstacleMask = ~0;
        [Tooltip("Explicit decorative colliders that should not act as solid obstacles. Ground checks remain independent.")]
        public Collider[] ignoredObstacles = new Collider[0];

        private readonly RaycastHit[] hits = new RaycastHit[256];
        private readonly Collider[] overlaps = new Collider[256];

        public static Vector2 ApplyDeadzone(Vector2 input, float deadzone)
        {
            deadzone = Mathf.Clamp(deadzone, 0f, 0.95f);
            float length = input.magnitude;
            if (length <= deadzone) return Vector2.zero;
            return input.normalized * Mathf.Clamp01((length - deadzone) / (1f - deadzone));
        }

        public bool InsideBoundary(Vector3 point, Vector3 center)
        {
            return BoundaryExcess(point, center) <= 0f;
        }

        private float BoundaryExcess(Vector3 point, Vector3 center)
        {
            Vector3 delta = point - center; delta.y = 0f;
            if (boundaryShape == BoundaryShape.WorldRectangle)
            {
                Vector2 allowed = boundaryHalfExtents - Vector2.one * bodyRadius;
                if (allowed.x < 0f || allowed.y < 0f) return float.PositiveInfinity;
                return Mathf.Max(Mathf.Abs(delta.x) - allowed.x, Mathf.Abs(delta.z) - allowed.y);
            }
            float radius = explorationRadius - bodyRadius;
            return radius < 0f ? float.PositiveInfinity : delta.magnitude - radius;
        }

        public bool TryPosition(Collider[] surfaces, Vector3 point, Vector3 center, float referenceHeight,
            out Vector3 grounded, out string reason)
        {
            grounded = point;
            if (!InsideBoundary(point, center)) { reason = "Exploration boundary"; return false; }
            return TrySupportedPosition(surfaces, point, referenceHeight, out grounded, out reason);
        }

        // If a runtime boundary adjustment puts a supported Ladybird outside, allow only steps toward it.
        // Ordinary movement must remain inside. Recovery never bypasses ground or obstacle checks.
        public bool TryMovementPosition(Collider[] surfaces, Vector3 from, Vector3 point, Vector3 center,
            out Vector3 grounded, out string reason)
        {
            grounded = point;
            float oldExcess = BoundaryExcess(from, center), newExcess = BoundaryExcess(point, center);
            if (newExcess > 0f && !(oldExcess > 0f && newExcess < oldExcess - 0.000001f))
            { reason = "Exploration boundary"; return false; }
            return TrySupportedPosition(surfaces, point, from.y, out grounded, out reason);
        }

        public bool TrySupportedPosition(Collider[] surfaces, Vector3 point, float referenceHeight,
            out Vector3 grounded, out string reason)
        {
            grounded = point;
            if (!TryGround(surfaces, point, referenceHeight, out RaycastHit ground, out reason)) return false;
            // Centre, inner ring and outer ring protect the body rather than only the root pivot.
            for (int ring = 1; ring <= 2; ring++)
            {
                float radius = bodyRadius * ring * 0.5f;
                for (int i = 0; i < 16; i++)
                {
                    float angle = i * Mathf.PI * 2f / 16f;
                    Vector3 sample = point + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
                    if (!TryGround(surfaces, sample, ground.point.y, out _, out string sampleReason))
                    { reason = "Unsafe footprint: " + sampleReason; return false; }
                }
            }
            grounded.y = ground.point.y;
            reason = "Ready";
            return true;
        }

        private bool TryGround(Collider[] surfaces, Vector3 point, float referenceHeight,
            out RaycastHit ground, out string reason)
        {
            ground = default;
            float nearest = float.PositiveInfinity;
            Ray ray = new Ray(new Vector3(point.x, referenceHeight + probeHeight, point.z), Vector3.down);
            if (surfaces != null)
            {
                foreach (Collider surface in surfaces)
                {
                    if (surface == null || !surface.enabled || surface.isTrigger || !surface.gameObject.activeInHierarchy)
                        continue;
                    if (surface.Raycast(ray, out RaycastHit hit, probeHeight + probeDepth) && hit.distance < nearest)
                    { nearest = hit.distance; ground = hit; }
                }
            }
            if (float.IsPositiveInfinity(nearest)) { reason = "No configured ground under probe"; return false; }
            float change = ground.point.y - referenceHeight;
            if (change > maximumStepHeight) { reason = "Step too high"; return false; }
            if (change < -maximumDrop) { reason = "Ground drop exceeds limit"; return false; }
            if (Vector3.Angle(ground.normal, Vector3.up) > maximumSlope)
            { reason = "Ground slope exceeds limit"; return false; }
            reason = "Ready";
            return true;
        }

        public bool PathIsClear(Transform owner, Vector3 contact, Vector3 direction, float distance, out string reason, Collider[] groundSurfaces = null)
        {
            Vector3 origin = contact + Vector3.up * (bodyRadius + obstacleClearance);
            int count = Physics.OverlapSphereNonAlloc(origin, bodyRadius, overlaps, obstacleMask, QueryTriggerInteraction.Ignore);
            if (count == overlaps.Length) { reason = "Obstacle query buffer full"; return false; }
            for (int i = 0; i < count; i++)
                if (!IgnoreObstacle(owner, overlaps[i], groundSurfaces)) { reason = "Obstacle overlap: " + overlaps[i].name; return false; }
            if (distance > 0f)
            {
                count = Physics.SphereCastNonAlloc(origin, bodyRadius, direction, hits, distance, obstacleMask, QueryTriggerInteraction.Ignore);
                if (count == hits.Length) { reason = "Obstacle query buffer full"; return false; }
                for (int i = 0; i < count; i++)
                    if (!IgnoreObstacle(owner, hits[i].collider, groundSurfaces)) { reason = "Obstacle ahead: " + hits[i].collider.name; return false; }
            }
            reason = "Ready";
            return true;
        }

        private SphereCollider penetrationProbe;
        private SphereCollider Probe
        {
            get
            {
                if (penetrationProbe == null)
                {
                    var go = new GameObject("Ladybird collision query (not a scene asset)");
                    go.hideFlags = HideFlags.HideAndDontSave;
                    penetrationProbe = go.AddComponent<SphereCollider>();
                    penetrationProbe.enabled = false;
                }
                penetrationProbe.radius = bodyRadius;
                return penetrationProbe;
            }
        }
        public void Dispose()
        {
            if (penetrationProbe == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(penetrationProbe.gameObject);
            else UnityEngine.Object.DestroyImmediate(penetrationProbe.gameObject);
            penetrationProbe = null;
        }
        private bool Penetration(Collider other, Vector3 origin, out Vector3 normal, out float depth) =>
            Physics.ComputePenetration(Probe, origin, Quaternion.identity, other, other.transform.position,
                other.transform.rotation, out normal, out depth);

        public bool TryObstacleMotion(Transform owner, Vector3 contact, Vector3 desired, Collider[] surfaces,
            out Vector3 movement, out string reason)
        {
            movement = desired;
            if (MotionClear(owner, contact, desired, surfaces, out Vector3 normal, out reason)) return true;
            string blocked = reason;
            normal.y = 0f;
            if (normal.sqrMagnitude > 0.0001f)
            {
                normal.Normalize();
                Vector3 slide = Vector3.ProjectOnPlane(desired, normal); slide.y = 0f;
                // Never create motion from zero input, increase speed, or slide straight through a corner.
                if (slide.sqrMagnitude > 0.00000001f && Vector3.Dot(slide, desired) > 0f &&
                    MotionClear(owner, contact, slide, surfaces, out _, out _))
                { movement = slide; reason = "Sliding along " + blocked; return true; }
            }
            movement = Vector3.zero; reason = blocked; return false;
        }
        private bool MotionClear(Transform owner, Vector3 contact, Vector3 movement, Collider[] surfaces,
            out Vector3 blockingNormal, out string reason)
        {
            blockingNormal = Vector3.zero; reason = "Ready";
            Vector3 origin = contact + Vector3.up * (bodyRadius + obstacleClearance);
            int count = Physics.OverlapSphereNonAlloc(origin, bodyRadius, overlaps, obstacleMask, QueryTriggerInteraction.Ignore);
            if (count == overlaps.Length) { reason = "Obstacle query buffer full"; return false; }
            for (int i = 0; i < count; i++)
            {
                Collider item = overlaps[i]; if (IgnoreObstacle(owner,item,surfaces)) continue;
                if (!Penetration(item,origin,out Vector3 normal,out float oldDepth)) continue;
                bool remains = Penetration(item,origin+movement,out _,out float newDepth);
                if (remains && (newDepth > oldDepth + 0.000001f || Vector3.Dot(movement,normal) < -0.000001f))
                { blockingNormal=normal; reason="Obstacle overlap: "+item.name; return false; }
                // The initial overlap is recoverable; continue checking all other colliders.
            }
            if (movement.sqrMagnitude > 0.000000001f)
            {
                count=Physics.SphereCastNonAlloc(origin,bodyRadius,movement.normalized,hits,movement.magnitude,obstacleMask,QueryTriggerInteraction.Ignore);
                if(count==hits.Length){reason="Obstacle query buffer full";return false;}
                float nearest=float.PositiveInfinity; Collider blocked=null;
                for(int i=0;i<count;i++)
                {
                    Collider item=hits[i].collider;if(IgnoreObstacle(owner,item,surfaces))continue;
                    if(Penetration(item,origin,out _,out _))continue; // Already verified non-deepening recovery above.
                    Vector3 normal=hits[i].normal;
                    // A contact tangent to/behind the direction must not stop moving away.
                    if(Vector3.Dot(movement,normal)>=-0.000001f)continue;
                    if(hits[i].distance<nearest){nearest=hits[i].distance;blocked=item;blockingNormal=normal;}
                }
                if(blocked!=null){reason="Obstacle ahead: "+blocked.name;return false;}
            }
            // Validate the endpoint against all obstacles, including new overlaps missed by a sweep at contact.
            count=Physics.OverlapSphereNonAlloc(origin+movement,bodyRadius,overlaps,obstacleMask,QueryTriggerInteraction.Ignore);
            if(count==overlaps.Length){reason="Obstacle query buffer full";return false;}
            for(int i=0;i<count;i++)
            {
                var item=overlaps[i];if(IgnoreObstacle(owner,item,surfaces))continue;
                if(!Penetration(item,origin+movement,out Vector3 normal,out float depth))continue;
                bool wasOverlapping=Penetration(item,origin,out _,out float oldDepth);
                if(!wasOverlapping || depth>oldDepth+0.000001f)
                {blockingNormal=normal;reason="Obstacle overlap: "+item.name;return false;}
            }
            return true;
        }

        private static bool IsOwn(Transform owner, Collider collider) =>
            owner != null && collider != null && collider.transform.IsChildOf(owner);

        private bool IgnoreObstacle(Transform owner, Collider collider, Collider[] groundSurfaces = null)
        {
            if (IsOwn(owner, collider)) return true;
            if (groundSurfaces != null)
                foreach (Collider ground in groundSurfaces) if (ground != null && ground == collider) return true;
            if (ignoredObstacles != null)
                foreach (Collider ignored in ignoredObstacles) if (ignored != null && ignored == collider) return true;
            return false;
        }

        public bool TryFindStart(Transform owner, Collider[] surfaces, Vector3 preferred, Vector3 center,
            out Vector3 start, out string reason)
        {
            start = preferred;
            reason = "No safe start on the configured surface";
            string firstRejection = null;
            // Search only a bounded area, and validate the complete footprint and obstacle clearance.
            for (int ring = 0; ring <= 8; ring++)
            {
                int samples = ring == 0 ? 1 : 16;
                float radius = Mathf.Max(0f, explorationRadius - bodyRadius) * ring / 8f;
                for (int i = 0; i < samples; i++)
                {
                    float angle = i * Mathf.PI * 2f / samples;
                    Vector2 reach = boundaryShape == BoundaryShape.WorldRectangle ?
                        Vector2.Max(Vector2.zero, boundaryHalfExtents - Vector2.one * bodyRadius) * ring / 8f : Vector2.one * radius;
                    Vector3 point = ring == 0 ? preferred : center + new Vector3(Mathf.Cos(angle) * reach.x, 0f, Mathf.Sin(angle) * reach.y);
                    if (surfaces == null) continue;
                    foreach (Collider surface in surfaces)
                    {
                        if (surface == null || !surface.enabled || surface.isTrigger || !surface.gameObject.activeInHierarchy) continue;
                        Ray ray = new Ray(new Vector3(point.x, surface.bounds.max.y + probeHeight, point.z), Vector3.down);
                        if (!surface.Raycast(ray, out RaycastHit hit, surface.bounds.size.y + probeHeight + probeDepth)) continue;
                        point.y = hit.point.y;
                        if (TryPosition(surfaces, point, center, point.y, out Vector3 candidate, out reason) &&
                            PathIsClear(owner, candidate, Vector3.forward, 0f, out reason, surfaces))
                        { start = candidate; reason = "Ready"; return true; }
                        if (firstRejection == null) firstRejection = reason;
                    }
                }
            }
            reason = firstRejection ?? reason;
            return false;
        }
    }
}
