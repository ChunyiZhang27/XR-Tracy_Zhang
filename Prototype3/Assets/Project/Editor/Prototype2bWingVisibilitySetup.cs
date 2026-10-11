using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public static class Prototype2bWingVisibilitySetup
{
    public static void ConfigureBatch()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Project/Scenes/Prototype2b.unity");
        var all = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<Transform>(true)).ToArray();
        var root = all.Single(x => x.name == "LadybirdExploreModel_OLD");
        var transition = root.GetComponent<LadybirdEnvironmentTransition>();
        var walking = root.GetComponent<LadybirdWalkingAnimator>();
        var model = walking.visualModel;
        var renderers = model.GetComponentsInChildren<MeshRenderer>(true);
        var flight = new[]{"LeftWing","RightWing"}.Select(n => renderers.Single(x => x.name == n)).ToArray();
        foreach (var mesh in flight)
        {
            if (mesh.GetComponent<MeshFilter>()?.sharedMesh == null) throw new Exception("Missing flight wing mesh");
            Debug.Log("Flight wing renderer: " + mesh.name + " parent=" + mesh.transform.parent.name);
        }
        var helper = root.GetComponent<LadybirdMovementWingVisibility>() ?? root.gameObject.AddComponent<LadybirdMovementWingVisibility>();
        helper.wings = transition.wings; helper.elytra = transition.elytra; helper.walking = walking;
        helper.flightWingRenderers = flight;
        helper.wingShellInteractables = transition.bodyInteractables;
        transition.movementWings = helper;
        var test = all.Select(x => x.GetComponent<Prototype2bEditorMovementTest>()).Single(x => x != null);
        test.movementWings = helper;
        EditorUtility.SetDirty(transition); EditorUtility.SetDirty(test); EditorUtility.SetDirty(helper);
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        Debug.Log("Configured reversible movement wing visibility; elytra renderer assignments unchanged.");
    }
}
