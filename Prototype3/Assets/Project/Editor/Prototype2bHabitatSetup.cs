using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class Prototype2bHabitatSetup
{
    public static void InspectBatch()
    {
        var scene=EditorSceneManager.OpenScene("Assets/Project/Scenes/Prototype2b.unity");
        var all=scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<Transform>(true)).ToArray();
        var controller=all.Select(x=>x.GetComponent<LadybirdPlayerController>()).Single(x=>x!=null);
        // Activate only in this unsaved inspection copy: physics must see environment colliders.
        for(var t=controller.transform;t!=null;t=t.parent)t.gameObject.SetActive(true);
        foreach(var ground in controller.groundSurfaces) for(var t=ground.transform;t!=null;t=t.parent)t.gameObject.SetActive(true);
        Physics.SyncTransforms();
        Debug.Log("HABITAT bounds="+controller.safety.boundaryHalfExtents+" footprint="+controller.safety.bodyRadius+" start="+controller.startingPoint.position);
        foreach(var mesh in all.Where(x=>x.name.IndexOf("leaf",StringComparison.OrdinalIgnoreCase)>=0 || x.name.IndexOf("plant",StringComparison.OrdinalIgnoreCase)>=0))
            Debug.Log("HABITAT geometry "+mesh.name+" position="+mesh.position+" bounds="+mesh.GetComponent<Renderer>()?.bounds);
        if(!FindDestination(controller,out var target))throw new Exception("No automatically verified habitat route. Manual placement required.");
        Debug.Log("HABITAT verified destination="+target);
    }
    static bool FindDestination(LadybirdPlayerController c,out Vector3 target)
    {
        target=default;var start=c.startingPoint.position;
        if(!c.safety.TryFindStart(c.transform,c.groundSurfaces,start,start,out var safeStart,out var startReason)) { Debug.Log("HABITAT start failed: "+startReason); return false; }
        Debug.Log("HABITAT safe start="+safeStart); start=safeStart;
        // Search straight routes across the existing leaf; every step uses the actual footprint, ground, boundary and obstacle checks.
        for(float distance=3f;distance>=2f;distance-=.25f)for(int direction=0;direction<16;direction++)
        {
            var heading=Quaternion.Euler(0,direction*22.5f,0)*Vector3.forward;var from=start;bool safe=true;
            for(float travelled=0;travelled<distance;travelled+=c.safety.maximumMovementStep)
            {
                var next=from+heading*Mathf.Min(c.safety.maximumMovementStep,distance-travelled);
                if(!c.safety.TryMovementPosition(c.groundSurfaces,from,next,start,out var ground,out var reason) ||
                    !c.safety.PathIsClear(c.transform,ground,heading,0f,out reason,c.groundSurfaces) ||
                    !c.safety.TryObstacleMotion(c.transform,from,ground-from,c.groundSurfaces,out var motion,out _) ||
                    (motion-(ground-from)).sqrMagnitude>.000001f){Debug.Log("HABITAT route "+direction+" failed at "+from+": "+reason);safe=false;break;}
                from=ground;
            }
            if(!safe)continue;
            // Entire arrival region (not just its centre) must remain supported and clear.
            for(int i=0;i<16;i++)
            {
                var sample=from+Quaternion.Euler(0,i*22.5f,0)*Vector3.forward*.45f;
                if(!c.safety.TryPosition(c.groundSurfaces,sample,start,from.y,out var ground,out _) ||
                    !c.safety.PathIsClear(c.transform,ground,Vector3.forward,0f,out _,c.groundSurfaces)){safe=false;break;}
            }
            if(safe){target=from;return true;}
        }
        return false;
    }
}
