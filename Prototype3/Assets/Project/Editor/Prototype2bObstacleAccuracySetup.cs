using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>Scene-specific decoration classification. Keeps all rock meshes and safety dimensions intact.</summary>
public static class Prototype2bObstacleAccuracySetup
{
    const string ScenePath = "Assets/Project/Scenes/Prototype2b.unity";
    public static void ConfigureBatch()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("Batch only.");
        var scene = EditorSceneManager.OpenScene(ScenePath);
        var all = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<Transform>(true)).ToArray();
        var controller = all.Select(x => x.GetComponent<LadybirdPlayerController>()).Single(x => x != null);
        var environment = all.Single(x => x.name == "ExploreEnvironment");
        var grass = environment.Cast<Transform>().Where(x => x.name.StartsWith("Grass_6_Billboard_Mesh",StringComparison.Ordinal)).ToArray();
        var decorations = grass.SelectMany(x => x.GetComponentsInChildren<Collider>(true)).ToArray();
        if (decorations.Length == 0 || decorations.Any(x => !(x is MeshCollider mesh) || mesh.sharedMesh == null ||
            mesh.sharedMesh.name != "Quad" || mesh.sharedMesh.triangles.Length != 6 || controller.groundSurfaces.Contains(x)))
            throw new InvalidOperationException("Only audited two-triangle grass billboard colliders may be classified decorative.");
        var rocks = environment.Cast<Transform>().Where(x => x.name.StartsWith("Rock_",StringComparison.Ordinal))
            .SelectMany(x => x.GetComponentsInChildren<Collider>(true)).ToArray();
        foreach (var rock in rocks)
        {
            var filter = rock.GetComponent<MeshFilter>();
            if (!(rock is MeshCollider mesh) || filter == null || mesh.sharedMesh != filter.sharedMesh || !rock.enabled || rock.isTrigger)
                throw new InvalidOperationException("Rock collider/visible mesh mismatch: " + rock.name);
        }
        var saved = controller.safety.ignoredObstacles ?? Array.Empty<Collider>();
        if (rocks.Any(saved.Contains)) throw new InvalidOperationException("Solid rocks must never be ignored.");
        // Activate only for in-memory physics validation, and restore before saving.
        var active = new Dictionary<GameObject,bool>();
        for (var t = environment; t != null; t = t.parent) { active[t.gameObject] = t.gameObject.activeSelf; t.gameObject.SetActive(true); }
        Physics.SyncTransforms();
        var ground = controller.groundSurfaces.Single();
        var points = new List<Vector3>(); var wasClear = new List<bool>(); var oldBlockers = new List<string>();
        for (float x = -3; x <= 18; x += .25f) for (float z = -8; z <= 6; z += .25f)
        {
            if (!ground.Raycast(new Ray(new Vector3(x, ground.bounds.max.y + 1, z),Vector3.down),out var hit,ground.bounds.size.y + 2)) continue;
            if (!controller.safety.TryPosition(controller.groundSurfaces,hit.point,controller.startingPoint.position,hit.point.y,out var p,out _)) continue;
            points.Add(p); wasClear.Add(controller.safety.PathIsClear(controller.transform,p,Vector3.forward,0,out var reason,controller.groundSurfaces)); oldBlockers.Add(reason);
        }
        controller.safety.ignoredObstacles = saved.Concat(decorations).Where(x => x != null).Distinct().ToArray();
        int freed = 0; var report = new List<string>();
        for (int i = 0; i < points.Count; i++)
        {
            bool clear = controller.safety.PathIsClear(controller.transform,points[i],Vector3.forward,0,out _,controller.groundSurfaces);
            if (wasClear[i] && !clear) throw new InvalidOperationException("A previously clear pose regressed.");
            if (!wasClear[i] && clear)
            {
                freed++;
                if (report.Count < 12) report.Add("Newly clear grounded pose " + points[i] + "; before=" + oldBlockers[i]);
            }
        }
        if (freed == 0) throw new InvalidOperationException("No false blocking was removed; investigate before saving.");
        int rockHits = 0;
        foreach (var rock in rocks)
        {
            var point = rock.bounds.center;
            if (!ground.Raycast(new Ray(new Vector3(point.x,ground.bounds.max.y+1,point.z),Vector3.down),out var hit,ground.bounds.size.y+2)) continue;
            if (controller.safety.PathIsClear(controller.transform,hit.point,Vector3.forward,0,out _,controller.groundSurfaces))
                throw new InvalidOperationException("Solid rock no longer blocks: " + rock.name);
            rockHits++;
        }
        foreach (var pair in active) pair.Key.SetActive(pair.Value);
        Physics.SyncTransforms();
        report.Insert(0,$"Grass objects={grass.Length}; decorative quad colliders={decorations.Length}; added ignores={controller.safety.ignoredObstacles.Length-saved.Length}; rock colliders={rocks.Length}; rock-centre blocking checks={rockHits}; supported samples={points.Count}; formerly false-blocked now clear={freed}; radius={controller.safety.bodyRadius}; clearance={controller.safety.obstacleClearance}. No rock shapes, ground, transforms or movement tuning changed.");
        File.WriteAllLines("/tmp/rock-accuracy-validation.txt",report);
        EditorUtility.SetDirty(controller); EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Save failed.");
        Debug.Log("OBSTACLE ACCURACY " + report[0]);
    }
}
