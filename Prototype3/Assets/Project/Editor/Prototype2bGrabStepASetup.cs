using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Attachment;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>Scene-only Step A conversion. Imported assets and shared aphid prefab stay untouched.</summary>
public static class Prototype2bGrabStepASetup
{
    public static void ConfigureBatch()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("Batch only.");
        var scene = EditorSceneManager.OpenScene("Assets/Project/Scenes/Prototype2b.unity");
        var all = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<Transform>(true)).ToArray();
        var activity = all.Select(x => x.GetComponent<FoodSearchActivity>()).Single(x => x != null);
        var controller = activity.objective.controller;
        controller.habitatControllerHandover = true; EditorUtility.SetDirty(controller);
        var target = activity.targets.Single(x => x.name == "Aphid_01");
        // Scene-specific unpack preserves the nested imported visual and all original asset files.
        if (PrefabUtility.IsPartOfPrefabInstance(target))
            PrefabUtility.UnpackPrefabInstance(target.gameObject, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);
        var old = target.GetComponent<XRSimpleInteractable>();
        var manager = old.interactionManager; var layers = old.interactionLayers;
        UnityEngine.Object.DestroyImmediate(old);
        var body = target.gameObject.AddComponent<Rigidbody>();
        body.useGravity = false; body.isKinematic = true; body.mass = .02f;
        var grab = target.gameObject.AddComponent<XRGrabInteractable>();
        grab.interactionManager = manager; grab.interactionLayers = layers;
        grab.colliders.Clear(); grab.colliders.Add(target.GetComponent<SphereCollider>());
        grab.movementType = XRBaseInteractable.MovementType.Kinematic;
        grab.throwOnDetach = false; grab.useDynamicAttach = false;
        grab.farAttachMode = InteractableFarAttachMode.Near;
        target.gameObject.AddComponent<FoodSearchGrabReturn>();
        foreach (var interactor in controller.xrOrigin.GetComponentsInChildren<NearFarInteractor>(true))
            Debug.Log($"STEP A INTERACTOR {interactor.name}: enabled={interactor.enabled}, active={interactor.gameObject.activeSelf}, near={interactor.enableNearCasting}, far={interactor.enableFarCasting}, UI={interactor.enableUIInteraction}, select={interactor.selectInput.inputActionReferencePerformed?.action?.name}");
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Save failed.");
        Debug.Log("STEP A configured Aphid_01 only; original count and other targets retained for Step B.");
    }
}
