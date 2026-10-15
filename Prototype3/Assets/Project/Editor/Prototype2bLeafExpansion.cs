using System;
using System.Collections.Generic;
using System.Linq;
using TinyWorlds;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Explicit native-Editor measurement and tuning of Prototype2b only. No automatic scene edits.</summary>
public static class Prototype2bLeafExpansion
{
    const string Path="Assets/Project/Scenes/Prototype2b.unity";
    public static void ConfigureBatch() { Process(true); }
    public static void VerifyBatch() { Process(false); }
    static void Process(bool save)
    {
        var scene=EditorSceneManager.OpenScene(Path);
        var all=scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<Transform>(true)).ToArray();
        var player=all.Single(x=>x.name=="LadybirdExploreModel_OLD").GetComponent<LadybirdPlayerController>();
        var leaf=all.Single(x=>x.name=="GiantLeaf").GetComponent<MeshCollider>();
        if(leaf==null || leaf.convex || leaf.isTrigger || !leaf.enabled || leaf.sharedMesh==null || !player.groundSurfaces.Contains(leaf))
            throw new Exception("Existing non-convex walkable leaf configuration is invalid.");
        var roots=scene.GetRootGameObjects();var original=all.ToDictionary(x=>x.gameObject,x=>x.gameObject.activeSelf);
        try
        {
            foreach(var name in new[]{"OnboardingZone","SelectionZone"})roots.Single(x=>x.name==name).SetActive(false);
            for(var t=leaf.transform;t!=null;t=t.parent)t.gameObject.SetActive(true);
            for(var t=player.transform;t!=null;t=t.parent)t.gameObject.SetActive(true);
            Physics.SyncTransforms();
            var b=leaf.bounds;var center=player.startingPoint.position;var safety=player.safety;
            Debug.Log($"LEAF transform position={leaf.transform.position:F4}, scale={leaf.transform.localScale:F4}, mesh={leaf.sharedMesh.name}, convex={leaf.convex}; world bounds min={b.min:F4} max={b.max:F4}; old half extents={safety.boundaryHalfExtents:F4}, full-footprint radius={safety.bodyRadius:F4}, start marker={center:F4}");
            // Enclose both sides of the actual collider around the unchanged start. This is only a broad limit;
            // 33 footprint raycasts, step/drop/slope checks and obstacle sweeps decide the curved usable area.
            var extents=new Vector2(Mathf.Max(center.x-b.min.x,b.max.x-center.x),Mathf.Max(center.z-b.min.z,b.max.z-center.z));
            safety.boundaryShape=LadybirdMovementSafety.BoundaryShape.WorldRectangle;safety.boundaryHalfExtents=extents;
            player.movementSpeed=.50f;player.acceleration=1f;player.deceleration=2f;
            var walking=player.GetComponent<LadybirdWalkingAnimator>();
            // Bound cadence at the new speed and modestly extend stride; gait still reads actual displacement.
            walking.referenceSpeed=.50f;walking.gaitFrequency=2.5f;walking.strideAmplitude=.08f;
            // Ensure the full standing leg mesh bounds fit the unchanged conservative movement footprint.
            float measured=0f;
            foreach(var leg in walking.legs)foreach(var mesh in leg.segments)
                for(int corner=0;corner<8;corner++)
                {
                    var bounds=mesh.bounds;var p=bounds.center+Vector3.Scale(bounds.extents,new Vector3((corner&1)==0?-1:1,(corner&2)==0?-1:1,(corner&4)==0?-1:1));
                    var delta=p-player.groundContact.position;delta.y=0;measured=Mathf.Max(measured,delta.magnitude);
                }
            if(measured>safety.bodyRadius+.001f)throw new Exception("Leg footprint exceeds safety radius: "+measured);
            if(!safety.TryFindStart(player.transform,player.groundSurfaces,center,center,out var start,out var reason))throw new Exception("Start rejected: "+reason);
            Debug.Log($"LEAF new coarse half extents={extents:F4}, supported start={start:F4}, measured standing legs radius={measured:F4}");
            ValidateConnected(player,start,center,out var reachable,out int count,out float furthest);
            if(furthest<8f || count<100)throw new Exception("Insufficient verified distant connected area.");
            Debug.Log($"LEAF reachable 0.5m grid: {count} samples, approximate sampled area={count*.25f:F2} m²; bounds min={reachable.min:F3} max={reachable.max:F3}; farthest verified route endpoint distance={furthest:F2}m. Every grid edge sampled at <= {safety.maximumMovementStep}m in both directions with footprint and obstacle checks. Curved edges remain runtime raycast-limited.");
            foreach(var pair in original)pair.Key.SetActive(pair.Value);
            if(save)
            {
                EditorUtility.SetDirty(player);EditorUtility.SetDirty(walking);EditorSceneManager.MarkSceneDirty(scene);
                if(!EditorSceneManager.SaveScene(scene))throw new Exception("Save failed.");
            }
        }
        finally { foreach(var pair in original)if(pair.Key!=null)pair.Key.SetActive(pair.Value); }
    }
    static bool Segment(LadybirdPlayerController c,Vector3 from,Vector3 target,Vector3 center,out Vector3 end)
    {
        end=from;Vector3 delta=target-from;delta.y=0;int steps=Mathf.Max(1,Mathf.CeilToInt(delta.magnitude/c.safety.maximumMovementStep));
        for(int i=1;i<=steps;i++)
        {
            Vector3 next=Vector3.Lerp(from,target,(float)i/steps);next.y=end.y;
            if(!c.safety.TryMovementPosition(c.groundSurfaces,end,next,center,out var ground,out _) ||
                !c.safety.TryObstacleMotion(c.transform,end,ground-end,c.groundSurfaces,out var actual,out _) ||
                (actual-(ground-end)).sqrMagnitude>1e-8f)return false;
            end=ground;
        }
        return true;
    }
    static void ValidateConnected(LadybirdPlayerController c,Vector3 start,Vector3 center,out Bounds bounds,out int count,out float furthest)
    {
        var visited=new Dictionary<Vector2Int,Vector3>{{Vector2Int.zero,start}};var queue=new Queue<Vector2Int>();queue.Enqueue(Vector2Int.zero);
        bounds=new Bounds(start,Vector3.zero);furthest=0;
        var directions=new[]{Vector2Int.right,Vector2Int.left,Vector2Int.up,Vector2Int.down};
        while(queue.Count>0)
        {
            var cell=queue.Dequeue();var from=visited[cell];bounds.Encapsulate(from);furthest=Mathf.Max(furthest,Vector3.Distance(start,from));
            foreach(var d in directions)
            {
                var key=cell+d;if(visited.ContainsKey(key))continue;
                var next=start+new Vector3(key.x*.5f,0,key.y*.5f);next.y=from.y;
                if(!Segment(c,from,next,center,out var end) || !Segment(c,end,from,center,out _))continue;
                visited.Add(key,end);queue.Enqueue(key);
            }
        }
        count=visited.Count;
    }
}
