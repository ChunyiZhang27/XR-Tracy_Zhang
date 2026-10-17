using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>Prototype2b-only E1 setup. Candidate placement is rejected if unsupported, blocked or outside controller-ray reach.</summary>
public static class Prototype2bFoodSearchSetup
{
    const string ScenePath = "Assets/Project/Scenes/Prototype2b.unity";
    const string AssetFolder = "Assets/Project/FoodSearch";
    public static void ConfigureBatch()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("Batch only.");
        EditorSceneManager.OpenScene(ScenePath); Configure();
    }
    [MenuItem("Tools/Tiny Worlds/Configure Prototype2b Food Search E1")]
    public static void Configure()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (Application.isPlaying || scene.path != ScenePath || scene.isDirty) throw new InvalidOperationException("Open saved Prototype2b outside Play mode.");
        var all = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<Transform>(true)).ToArray();
        var o = all.Select(x => x.GetComponent<LadybirdHabitatObjective>()).Single(x => x != null);
        if (o.foodSearch != null) throw new InvalidOperationException("Food Search already configured.");
        var manager = all.Select(x => x.GetComponent<XRInteractionManager>()).Single(x => x != null);
        if (!AssetDatabase.IsValidFolder(AssetFolder)) AssetDatabase.CreateFolder("Assets/Project", "FoodSearch");
        var aphidMaterial = Material("Aphid", new Color(.3f,.65f,.12f));
        var antMaterial = Material("Ant", new Color(.13f,.07f,.035f));
        var aphidMesh = InsectMesh("Aphid", false); var antMesh = InsectMesh("Ant", true);
        var states = all.ToDictionary(x => x.gameObject, x => x.gameObject.activeSelf);
        Vector3[] positions;
        try
        {
            foreach (var root in scene.GetRootGameObjects()) if (root.name == "OnboardingZone" || root.name == "SelectionZone") root.SetActive(false);
            for (var t = o.transform; t != null; t = t.parent) t.gameObject.SetActive(true);
            for (var t = o.habitatTarget; t != null; t = t.parent) t.gameObject.SetActive(true);
            Physics.SyncTransforms();
            var offsets = new[] { new Vector3(-4.35f,0,-3.57f), new Vector3(-3.45f,0,-3.26f), new Vector3(-2.55f,0,-2.8f), new Vector3(-1.65f,0,-2.2f), new Vector3(-2.55f,0,-1.16f) };
            positions = offsets.Select(offset => ValidatePosition(o, o.habitatTarget.position + offset)).ToArray();
            var eye = o.controller.xrOrigin.Camera.transform.position;
            for (int i=0;i<positions.Length;i++) for (int j=0;j<positions.Length;j++) if(i!=j)
            {
                var centre = positions[i] + Vector3.up * .18f; var other = positions[j] + Vector3.up * .18f;
                var direction = (centre-eye).normalized; var along = Vector3.Dot(other-eye,direction);
                if(along > 0 && along < Vector3.Distance(eye,centre) && Vector3.Distance(other,eye+direction*along)<.42f)
                    throw new Exception("Place E1 targets manually: one target would intercept another's selection ray.");
            }
        }
        finally { foreach (var state in states) state.Key.SetActive(state.Value); }
        var activity = o.gameObject.AddComponent<FoodSearchActivity>(); activity.objective = o; o.foodSearch = activity;
        var area = new GameObject("FoodSearchInsectArea"); area.transform.SetParent(o.habitatTarget.parent, false); area.SetActive(false); activity.insectArea = area;
        activity.targets = new FoodSearchTarget[5];
        for (int i = 0; i < 5; i++)
        {
            bool aphid = i < 3;
            var go = new GameObject(aphid ? $"Aphid_{i+1:00}" : $"Ant_{i-2:00}", typeof(MeshFilter), typeof(MeshRenderer), typeof(SphereCollider));
            go.transform.SetParent(area.transform, false); go.transform.position = positions[i]; go.transform.rotation = Quaternion.Euler(0, i * 33f, 0);
            go.GetComponent<MeshFilter>().sharedMesh = aphid ? aphidMesh : antMesh;
            var renderer = go.GetComponent<MeshRenderer>(); renderer.sharedMaterial = aphid ? aphidMaterial : antMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var collider = go.GetComponent<SphereCollider>(); collider.center = new Vector3(0,.18f,0); collider.radius = .37f;
            var interactable = go.AddComponent<XRSimpleInteractable>(); interactable.interactionManager = manager;
            interactable.colliders.Clear(); interactable.colliders.Add(collider);
            var target = go.AddComponent<FoodSearchTarget>(); target.activity = activity;
            target.category = aphid ? FoodSearchTarget.IdentificationCategory.Aphid : FoodSearchTarget.IdentificationCategory.Ant;
            target.visuals = new Renderer[] { renderer }; activity.targets[i] = target;
            Debug.Log($"E1 TARGET {go.name}: world={go.transform.position:F4}, sphere radius={collider.radius}, category={target.category}, manager={manager.name}");
        }
        var panel = o.foodSearchReadyPanel.transform;
        var title = panel.Find("Title").GetComponent<TMP_Text>(); title.text = "Food Search"; Layout(title,145,45,30);
        var instruction = panel.Find("Description").GetComponent<TMP_Text>(); instruction.text = FoodSearchActivity.Instruction; Layout(instruction,95,55,28);
        activity.progressText = UnityEngine.Object.Instantiate(instruction, panel, false); activity.progressText.name = "AphidProgress";
        activity.progressText.text = "Aphids found: 0 / 2"; Layout(activity.progressText,45,40,28);
        activity.feedbackText = UnityEngine.Object.Instantiate(instruction, panel, false); activity.feedbackText.name = "IdentificationFeedback";
        activity.feedbackText.text = ""; Layout(activity.feedbackText,-30,100,25);
        var button = UnityEngine.Object.Instantiate(o.continueButton, panel, false); button.name = "TryAgainButton";
        var rect = (RectTransform)button.transform; rect.anchoredPosition = new Vector2(0,-130); rect.sizeDelta = new Vector2(570,60);
        var click = button.GetComponent<UnityEngine.UI.Button>();
        for (int i = click.onClick.GetPersistentEventCount()-1; i >= 0; i--) UnityEventTools.RemovePersistentListener(click.onClick, i);
        UnityEventTools.AddPersistentListener(click.onClick, activity.TryAgain);
        click.GetComponentInChildren<TMP_Text>(true).text = "Try Again";
        activity.tryAgainButton = button; button.SetActive(false);
        o.foodSearchReadyPanel.SetActive(false);
        EditorUtility.SetDirty(o); EditorUtility.SetDirty(activity); EditorSceneManager.MarkSceneDirty(scene);
        AssetDatabase.SaveAssets(); if (!EditorSceneManager.SaveScene(scene)) throw new Exception("Save failed.");
        Debug.Log("E1 SETUP complete: five non-grabbable targets, shared XR manager and UI input; original card root transforms preserved.");
    }
    static Vector3 ValidatePosition(LadybirdHabitatObjective o, Vector3 candidate)
    {
        var c = o.controller;
        RaycastHit hit = default; float nearest = float.PositiveInfinity;
        foreach (var ground in c.groundSurfaces)
            if (ground != null && ground.enabled && ground.Raycast(new Ray(candidate + Vector3.up * .6f,Vector3.down), out var sample,1.2f) && sample.distance < nearest)
            { hit = sample; nearest = sample.distance; }
        if (float.IsPositiveInfinity(nearest)) throw new Exception("Place E1 targets manually: unsupported candidate " + candidate);
        candidate.y = hit.point.y + .015f;
        var center = candidate + Vector3.up * .18f;
        if (Vector2.Distance(new Vector2(candidate.x,candidate.z), new Vector2(o.habitatTarget.position.x,o.habitatTarget.position.z)) < c.safety.bodyRadius + o.destinationRadius + .5f)
            throw new Exception("Place E1 targets manually: candidate overlaps arriving Ladybird.");
        foreach (var col in Physics.OverlapSphere(center,.42f,~0,QueryTriggerInteraction.Ignore))
            if (!c.groundSurfaces.Contains(col) && !col.transform.IsChildOf(c.transform)) throw new Exception("Place E1 targets manually: candidate blocked by " + col.name + " at " + candidate);
        var camera = c.xrOrigin.Camera; var origin = camera.transform.position; float distance = Vector3.Distance(origin,center);
        if (distance > 9.7f) throw new Exception("Place E1 targets manually: candidate outside conservative controller-ray reach.");
        foreach (var obstruction in Physics.RaycastAll(origin,(center-origin).normalized,distance-.4f,~0,QueryTriggerInteraction.Ignore))
            if (!obstruction.transform.IsChildOf(c.transform) && !c.groundSurfaces.Contains(obstruction.collider)) throw new Exception("Place E1 targets manually: view blocked by " + obstruction.collider.name);
        return candidate;
    }
    static void Layout(TMP_Text text,float y,float height,float font) { text.rectTransform.anchoredPosition = new Vector2(0,y); text.rectTransform.sizeDelta = new Vector2(660,height); text.fontSize = font; text.raycastTarget = false; }
    static UnityEngine.Material Material(string name,Color color)
    {
        string path = AssetFolder + "/" + name + ".mat"; var mat = AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(path);
        if (mat != null) return mat;
        mat = new UnityEngine.Material(Shader.Find("Universal Render Pipeline/Lit")); mat.SetColor("_BaseColor",color); mat.enableInstancing = true; AssetDatabase.CreateAsset(mat,path); return mat;
    }
    static Mesh InsectMesh(string name,bool ant)
    {
        string path = AssetFolder + "/" + name + "Placeholder.asset"; var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path); if (existing != null) return existing;
        var sphere = Sphere(); var cubeObject = GameObject.CreatePrimitive(PrimitiveType.Cube); var cube = cubeObject.GetComponent<MeshFilter>().sharedMesh;
        var parts = new List<CombineInstance>();
        Action<Vector3,Vector3> body = (p,s) => parts.Add(new CombineInstance { mesh=sphere, transform=Matrix4x4.TRS(p,Quaternion.identity,s) });
        body(new Vector3(0,.17f,-.07f), ant ? new Vector3(.13f,.13f,.2f) : new Vector3(.18f,.16f,.27f));
        if (ant) body(new Vector3(0,.16f,.13f),new Vector3(.08f,.09f,.1f));
        body(new Vector3(0,.17f,ant?.3f:.23f),new Vector3(.1f,.1f,.1f));
        Action<Vector3,Vector3,float> line = (a,b,w) => parts.Add(new CombineInstance { mesh=cube, transform=Matrix4x4.TRS((a+b)*.5f,Quaternion.FromToRotation(Vector3.up,b-a),new Vector3(w,(b-a).magnitude,w)) });
        for (int side=-1;side<=1;side+=2)
        {
            for(int j=0;j<3;j++) line(new Vector3(side*.07f,.12f,-.1f+j*.12f),new Vector3(side*.25f,.025f,-.15f+j*.15f),.018f);
            line(new Vector3(side*.05f,.22f,ant?.34f:.27f),new Vector3(side*.14f,.25f,ant?.5f:.44f),.015f);
        }
        var mesh = new Mesh { name=name+" E1 Placeholder" }; mesh.CombineMeshes(parts.ToArray(),true,true); mesh.RecalculateBounds();
        UnityEngine.Object.DestroyImmediate(cubeObject); UnityEngine.Object.DestroyImmediate(sphere); AssetDatabase.CreateAsset(mesh,path); return mesh;
    }
    static Mesh Sphere()
    {
        var vertices = new List<Vector3>(); var triangles = new List<int>(); const int lat=6, lon=8;
        for(int y=0;y<=lat;y++)for(int x=0;x<=lon;x++)
        { float a=y*Mathf.PI/lat,b=x*Mathf.PI*2/lon; vertices.Add(new Vector3(Mathf.Sin(a)*Mathf.Cos(b),Mathf.Cos(a),Mathf.Sin(a)*Mathf.Sin(b))); }
        for(int y=0;y<lat;y++)for(int x=0;x<lon;x++) { int a=y*(lon+1)+x,b=a+lon+1; triangles.AddRange(new[]{a,a+1,b,a+1,b+1,b}); }
        var mesh = new Mesh(); mesh.SetVertices(vertices); mesh.SetTriangles(triangles,0); mesh.RecalculateNormals(); return mesh;
    }
}
