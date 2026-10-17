using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>Wraps the imported visual without modifying the GLB or any of its subassets.</summary>
public static class Prototype2bImportedAphidSetup
{
    const string ScenePath = "Assets/Project/Scenes/Prototype2b.unity";
    const string ModelPath = "Assets/Project/Models/FoodInsects/Aphids/green_peach_aphid_-_first_model.glb";
    const string PrefabPath = "Assets/Project/FoodSearch/ImportedAphidInteractable.prefab";
    public static void ConfigureBatch()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("Batch only.");
        EditorSceneManager.OpenScene(ScenePath); Configure();
    }
    [MenuItem("Tools/Tiny Worlds/Integrate Imported Aphids In Prototype2b")]
    public static void Configure()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (Application.isPlaying || scene.path != ScenePath || scene.isDirty) throw new InvalidOperationException("Open saved Prototype2b outside Play mode.");
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null) throw new InvalidOperationException("Imported wrapper already exists; inspect before recreating.");
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        if (model == null) throw new InvalidOperationException("The existing glTF importer must expose the GLB as a prefab first. Original file will not be changed.");
        var all = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<Transform>(true)).ToArray();
        var activity = all.Select(x => x.GetComponent<FoodSearchActivity>()).Single(x => x != null);
        var aphids = activity.targets.Where(x => x != null && x.category == FoodSearchTarget.IdentificationCategory.Aphid).ToArray();
        if (aphids.Length != 3) throw new InvalidOperationException("Expected three existing aphid targets.");
        var environment = activity.objective.habitatTarget.parent;
        var placed = environment.Find("Aphid_01");
        GameObject template = placed != null && placed.GetComponentsInChildren<Renderer>(true).Length > 0 ? placed.gameObject : model;
        // A saved scene reference can preserve per-instance material/pose overrides. Otherwise use the real GLB prefab.
        Debug.Log("IMPORTED APHID visual reference: " + (template == model ? ModelPath + " (no imported instance in saved scene)" : placed.name));
        var wrapper = new GameObject("ImportedAphidInteractable"); wrapper.SetActive(false);
        var visual = template == model ? (GameObject)PrefabUtility.InstantiatePrefab(model, wrapper.transform) : UnityEngine.Object.Instantiate(template, wrapper.transform, false);
        visual.name = "ImportedAphidVisual"; visual.transform.localPosition = Vector3.zero;
        // Keep the GLB root conversion rotation as well as its nested node transforms.
        if (visual.GetComponentsInChildren<FoodSearchTarget>(true).Length != 0 || visual.GetComponentsInChildren<Collider>(true).Length != 0)
            throw new InvalidOperationException("Reference must be visual-only; original reference is left unchanged.");
        var raw = VisualBounds(wrapper.transform);
        var size = Mathf.Max(raw.size.x,raw.size.y,raw.size.z);
        if (size < .001f || !float.IsFinite(size)) throw new InvalidOperationException("Imported bounds are invalid.");
        float factor = .65f / size; visual.transform.localScale *= factor;
        var fitted = VisualBounds(wrapper.transform);
        visual.transform.localPosition -= new Vector3(fitted.center.x,fitted.min.y,fitted.center.z);
        fitted = VisualBounds(wrapper.transform);
        var collider = wrapper.AddComponent<SphereCollider>(); collider.center = fitted.center;
        collider.radius = Mathf.Clamp(fitted.extents.magnitude * 1.05f,.28f,.42f);
        var interactable = wrapper.AddComponent<XRSimpleInteractable>(); interactable.colliders.Clear(); interactable.colliders.Add(collider);
        // The shared prefab has no scene object references; scene instances reuse their existing XR manager/activity.
        interactable.interactionManager = null;
        var target = wrapper.AddComponent<FoodSearchTarget>(); target.category = FoodSearchTarget.IdentificationCategory.Aphid;
        target.visuals = visual.GetComponentsInChildren<Renderer>(true);
        foreach (var renderer in target.visuals)
            foreach (var material in renderer.sharedMaterials)
                if (material == null || !AssetDatabase.Contains(material)) throw new InvalidOperationException("Reference contains unsaved materials. Save its imported assets before integration.");
        wrapper.SetActive(true);
        interactable.interactionManager = null;
        var prefab = PrefabUtility.SaveAsPrefabAsset(wrapper,PrefabPath);
        if (prefab == null) throw new InvalidOperationException("Wrapper save failed.");
        Debug.Log($"IMPORTED APHID bounds: raw={raw}, normalization={factor:F6}, fitted={fitted}, sphere centre={collider.center}, radius={collider.radius:F4}; original nested orientation and materials preserved.");
        UnityEngine.Object.DestroyImmediate(wrapper);
        foreach (var previous in aphids)
        {
            int index = Array.IndexOf(activity.targets,previous);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab,previous.transform.parent);
            go.name = previous.name; go.transform.SetPositionAndRotation(previous.transform.position,previous.transform.rotation); go.transform.localScale = Vector3.one;
            go.SetActive(previous.gameObject.activeSelf);
            var replacement = go.GetComponent<FoodSearchTarget>(); replacement.activity = activity;
            replacement.GetComponent<XRSimpleInteractable>().interactionManager = previous.GetComponent<XRSimpleInteractable>().interactionManager;
            activity.targets[index] = replacement;
            UnityEngine.Object.DestroyImmediate(previous.gameObject);
            EditorUtility.SetDirty(replacement);
        }
        EditorUtility.SetDirty(activity); EditorSceneManager.MarkSceneDirty(scene);
        AssetDatabase.SaveAssets(); if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Scene save failed.");
        Debug.Log("IMPORTED APHID integrated: three Aphid categories, one fitted collider each, existing target positions/scoring and two incorrect placeholders retained.");
    }
    // Skinned renderer culling bounds can be loose; measure actual posed vertices instead.
    public static Bounds VisualBounds(Transform root)
    {
        bool hasBounds = false; Bounds bounds = default;
        foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            Mesh mesh; bool temporary = renderer is SkinnedMeshRenderer;
            if (renderer is SkinnedMeshRenderer skinned) { mesh = new Mesh(); skinned.BakeMesh(mesh); }
            else { var filter = renderer.GetComponent<MeshFilter>(); if (filter == null) continue; mesh = filter.sharedMesh; }
            if (mesh == null) continue;
            foreach (var vertex in mesh.vertices)
            {
                var point = root.InverseTransformPoint(renderer.transform.TransformPoint(vertex));
                if (!hasBounds) { bounds = new Bounds(point,Vector3.zero); hasBounds = true; } else bounds.Encapsulate(point);
            }
            if (temporary) UnityEngine.Object.DestroyImmediate(mesh);
        }
        if (!hasBounds) throw new InvalidOperationException("No readable imported mesh geometry.");
        return bounds;
    }
}
