using System;
using System.Collections.Generic;
using System.Linq;
using TinyWorlds;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Explicit test-scene-only tuning; boundary measurements use native collider world geometry.</summary>
public static class Prototype2bMovementTuning
{
    const string ScenePath = "Assets/Project/Scenes/Prototype2b.unity";
    public static void TuneBatch()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("Batch Editor required.");
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single); Tune();
    }
    public static void InspectObstaclesBatch()
    {
        if(!Application.isBatchMode)throw new InvalidOperationException("Batch only.");
        var scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
        var explore=scene.GetRootGameObjects().Single(x=>x.name=="InsectExploreZone");
        explore.SetActive(true);Physics.SyncTransforms();
        var player=explore.GetComponentInChildren<LadybirdPlayerController>(true);
        Debug.Log($"Collision inventory: mask={player.safety.obstacleMask.value}, radius={player.safety.bodyRadius}, clearance={player.safety.obstacleClearance}, configured grounds={player.groundSurfaces.Length}, ignored decoration colliders={player.safety.ignoredObstacles.Length}");
        foreach(var collider in explore.GetComponentsInChildren<Collider>(true))
        {
            bool own=collider.transform.IsChildOf(player.transform);
            bool ground=player.groundSurfaces.Contains(collider);
            bool decorative=player.safety.ignoredObstacles.Contains(collider);
            Debug.Log($"Collider: {collider.name}, {collider.GetType().Name}, layer={collider.gameObject.layer}, enabled={collider.enabled}, trigger={collider.isTrigger}, own={own}, ground={ground}, decoration={decorative}, world size={collider.bounds.size:F3}");
        }
        if(!player.safety.TryFindStart(player.transform,player.groundSurfaces,player.startingPoint.position,player.startingPoint.position,out _,out string reason))
            throw new InvalidOperationException("Safe start failed: "+reason);
        Debug.Log("Obstacle inventory and strict reset/start clearance verified; no scene save performed.");
    }
    [MenuItem("Tools/Tiny Worlds/Tune Prototype2b Movement Area")]
    public static void Tune()
    {
        var scene = SceneManager.GetActiveScene();
        if (Application.isPlaying || scene.path != ScenePath || scene.isDirty || SceneManager.sceneCount != 1)
            throw new InvalidOperationException("Open only saved Prototype2b outside Play mode.");
        var roots = scene.GetRootGameObjects();
        var explore = roots.Single(x=>x.name=="InsectExploreZone");
        var selection = roots.Single(x=>x.name=="SelectionZone");
        var onboarding = roots.Single(x=>x.name=="OnboardingZone");
        var player = explore.transform.Find("LadybirdExploreModel_OLD").GetComponent<LadybirdPlayerController>();
        if (player == null || player.startingPoint == null || player.groundSurfaces.Length != 1)
            throw new InvalidOperationException("Missing existing player setup.");
        var leaf = player.groundSurfaces[0] as MeshCollider;
        if (leaf == null || leaf.name != "GiantLeaf" || leaf.sharedMesh == null || leaf.isTrigger || !leaf.enabled)
            throw new InvalidOperationException("Expected the existing nontrigger GiantLeaf mesh collider.");
        bool ea=explore.activeSelf, sa=selection.activeSelf, oa=onboarding.activeSelf;
        string original=EditorJsonUtility.ToJson(player);
        try
        {
            explore.SetActive(true); selection.SetActive(false); onboarding.SetActive(false); Physics.SyncTransforms();
            var bounds=leaf.bounds; var center=player.startingPoint.position;
            // Inscribe world-aligned bounds around the existing start, with an additional 10cm edge margin.
            // AABB is only a coarse limit: complete footprint rays reject curved edges and holes at runtime.
            Vector2 extents=new Vector2(Mathf.Min(center.x-bounds.min.x,bounds.max.x-center.x),
                Mathf.Min(center.z-bounds.min.z,bounds.max.z-center.z))-Vector2.one*.10f;
            if (extents.x <= player.safety.bodyRadius || extents.y <= player.safety.bodyRadius)
                throw new InvalidOperationException("The measured walking surface cannot fit the existing footprint.");
            Undo.RecordObject(player,"Tune Prototype2b movement");
            player.movementSpeed=.35f; player.acceleration=.7f; player.deceleration=1.4f;
            player.safety.boundaryShape=LadybirdMovementSafety.BoundaryShape.WorldRectangle;
            player.safety.boundaryHalfExtents=extents;
            if (!player.safety.TryPosition(player.groundSurfaces,center,center,center.y,out var safeStart,out string reason) ||
                !player.safety.PathIsClear(player.transform,safeStart,Vector3.forward,0f,out reason))
                throw new InvalidOperationException("Existing reset start is unsafe: "+reason);
            // Flood-fill connected, fully supported and obstacle-clear contact positions, including swept edges.
            const float step=.1f;
            var visited=new HashSet<Vector2Int>(); var queue=new Queue<Vector2Int>();
            visited.Add(Vector2Int.zero); queue.Enqueue(Vector2Int.zero);
            var reachable=new Bounds(safeStart,Vector3.zero); int count=0;
            Vector2Int[] directions={Vector2Int.right,Vector2Int.left,Vector2Int.up,Vector2Int.down};
            while(queue.Count>0)
            {
                var cell=queue.Dequeue(); var from=center+new Vector3(cell.x*step,0,cell.y*step);
                player.safety.TryPosition(player.groundSurfaces,from,center,center.y,out from,out _);
                reachable.Encapsulate(from); count++;
                foreach(var direction in directions)
                {
                    var nextCell=cell+direction;
                    if(visited.Contains(nextCell)) continue;
                    var next=center+new Vector3(nextCell.x*step,0,nextCell.y*step);
                    if(!player.safety.TryPosition(player.groundSurfaces,next,center,from.y,out var ground,out _) ||
                        !player.safety.PathIsClear(player.transform,from,(ground-from).normalized,Vector3.Distance(from,ground),out _)) continue;
                    visited.Add(nextCell); queue.Enqueue(nextCell);
                }
            }
            if(count<10 || reachable.size.x<1f) throw new InvalidOperationException("Ground/obstacles do not provide a meaningfully larger connected area.");
            Debug.Log($"Prototype2b tuning: leaf world bounds min={bounds.min:F4}, max={bounds.max:F4}; start={center:F4}; " +
                $"body radius={player.safety.bodyRadius:F4}; outer X/Z half extents={extents:F4}; " +
                $"contact half extents={(extents-Vector2.one*player.safety.bodyRadius):F4}; " +
                $"connected 0.1m grid samples={count}, contact range min={reachable.min:F4}, max={reachable.max:F4}; " +
                "speed=0.35m/s, acceleration=0.7m/s², deceleration=1.4m/s². Runtime ground/obstacle checks remain authoritative.");
            explore.SetActive(ea); selection.SetActive(sa); onboarding.SetActive(oa);
            EditorUtility.SetDirty(player); EditorSceneManager.MarkSceneDirty(scene);
            if(!EditorSceneManager.SaveScene(scene,ScenePath)) throw new InvalidOperationException("Scene save failed.");
        }
        catch { EditorJsonUtility.FromJsonOverwrite(original,player); throw; }
        finally { explore.SetActive(ea); selection.SetActive(sa); onboarding.SetActive(oa); }
    }
}
