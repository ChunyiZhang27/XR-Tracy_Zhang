using System;
using System.Linq;
using TinyWorlds;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>Explicit native-Editor setup; never edits scene YAML or runs automatically.</summary>
public static class Prototype2bPlayerSetup
{
    private const string ScenePath = "Assets/Project/Scenes/Prototype2b.unity";
    private const string ActionsPath = "Assets/Samples/XR Interaction Toolkit/3.4.1/Starter Assets/XRI Default Input Actions.inputactions";

    [MenuItem("Tools/Tiny Worlds/Configure Prototype2b Player Movement")]
    public static void Configure()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (Application.isPlaying || scene.path != ScenePath || SceneManager.sceneCount != 1)
        { Debug.LogError("Open only Prototype2b.unity, outside Play mode, before configuring player movement."); return; }
        if (scene.isDirty)
        { Debug.LogError("Save your current Prototype2b changes first, then run configuration again."); return; }

        GameObject[] roots = scene.GetRootGameObjects();
        Transform explore = roots.SingleOrDefault(x => x.name == "InsectExploreZone")?.transform;
        Transform owner = explore != null ? explore.Find("LadybirdExploreModel_OLD") : null;
        Transform model = owner != null ? owner.Find("LadybirdExploreModel_New") : null;
        Transform environment = explore != null ? explore.Find("ExploreEnvironment") : null;
        Transform leaf = environment != null ? environment.Find("GiantLeaf") : null;
        XROrigin[] origins = roots.SelectMany(x => x.GetComponentsInChildren<XROrigin>(true)).ToArray();
        ExperienceManager manager = roots.SelectMany(x => x.GetComponentsInChildren<ExperienceManager>(true)).SingleOrDefault();
        GameObject onboarding = roots.SingleOrDefault(x => x.name == "OnboardingZone");
        GameObject selection = roots.SingleOrDefault(x => x.name == "SelectionZone");
        if (owner == null || model == null || leaf == null || environment == null || origins.Length != 1 ||
            manager == null || onboarding == null || selection == null)
        { Debug.LogError("Required Prototype2b objects are missing or ambiguous. No scene changes were made."); return; }
        MeshFilter leafMesh = leaf.GetComponent<MeshFilter>();
        if (leafMesh == null || leafMesh.sharedMesh == null)
        { Debug.LogError("GiantLeaf has no usable mesh. No scene changes were made."); return; }
        InputActionAsset actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ActionsPath);
        InputAction move = actions != null ? actions.FindAction("XRI Left Locomotion/Move") : null;
        InputActionReference reference = AssetDatabase.LoadAllAssetsAtPath(ActionsPath).OfType<InputActionReference>()
            .FirstOrDefault(x => x.action != null && move != null && x.action.id == move.id);
        if (reference == null) { Debug.LogError("Existing XRI left Move action reference could not be resolved. No changes made."); return; }

        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Configure Prototype2b player movement");
        bool exploreWasActive = explore.gameObject.activeSelf;
        try
        {
            Undo.RecordObject(explore.gameObject, "Temporarily enable leaf for ground verification");
            explore.gameObject.SetActive(true);
            MeshCollider ground = leaf.GetComponent<MeshCollider>();
            if (ground == null) ground = Undo.AddComponent<MeshCollider>(leaf.gameObject);
            Undo.RecordObject(ground, "Configure leaf collider");
            ground.sharedMesh = leafMesh.sharedMesh; ground.enabled = true; ground.isTrigger = false; ground.convex = false;

            MeshFilter[] legs = model.GetComponentsInChildren<MeshFilter>(true)
                .Where(x => x.name.StartsWith("legs.", StringComparison.OrdinalIgnoreCase) && x.sharedMesh != null).ToArray();
            if (legs.Length == 0) throw new InvalidOperationException("No leg meshes found for footprint calibration.");
            Bounds footprint = default;
            bool first = true;
            foreach (MeshFilter mesh in legs)
            {
                Bounds bounds = mesh.sharedMesh.bounds;
                for (int corner = 0; corner < 8; corner++)
                {
                    Vector3 local = bounds.center + Vector3.Scale(bounds.extents,
                        new Vector3((corner & 1) == 0 ? -1f : 1f, (corner & 2) == 0 ? -1f : 1f, (corner & 4) == 0 ? -1f : 1f));
                    Vector3 world = mesh.transform.TransformPoint(local);
                    if (first) { footprint = new Bounds(world, Vector3.zero); first = false; } else footprint.Encapsulate(world);
                }
            }
            Vector3 footCenter = new Vector3(footprint.center.x, footprint.min.y, footprint.center.z);
            // Conservative radius covers every leg mesh bound, not just the body mesh.
            float bodyRadius = new Vector2(footprint.extents.x, footprint.extents.z).magnitude + 0.02f;
            float areaRadius = Mathf.Min(1.25f, Mathf.Min(ground.bounds.extents.x, ground.bounds.extents.z) * 0.95f);
            if (bodyRadius >= areaRadius - 0.05f)
                throw new InvalidOperationException("Measured Ladybird footprint cannot fit safely in the leaf exploration area.");

            Transform contact = owner.Find("GroundContact");
            if (contact == null) contact = CreateMarker("GroundContact", owner);
            Undo.RecordObject(contact, "Calibrate ground contact from actual leg bounds");
            contact.position = footCenter;
            Transform start = environment.Find("LadybirdPlayerStart");
            if (start == null) start = CreateMarker("LadybirdPlayerStart", environment);
            Undo.RecordObject(start, "Configure stationary player start");
            start.position = new Vector3(footCenter.x, ground.bounds.max.y, footCenter.z);

            LadybirdPlayerController controller = owner.GetComponent<LadybirdPlayerController>();
            if (controller == null) controller = Undo.AddComponent<LadybirdPlayerController>(owner.gameObject);
            Undo.RecordObject(controller, "Configure Ladybird controller");
            controller.groundContact = contact; controller.facingReference = model; controller.startingPoint = start;
            controller.groundSurfaces = new Collider[] { ground }; controller.xrOrigin = origins[0]; controller.leftThumbstick = reference;
            controller.movementSpeed = 0.15f; controller.turningSpeed = 60f; controller.acceleration = 0.3f; controller.deceleration = 0.5f;
            controller.inputDeadzone = 0.15f; controller.editorKeyboardEnabled = true; controller.enterAfterBodyExploration = true;
            controller.safety = new LadybirdMovementSafety { bodyRadius = bodyRadius, explorationRadius = areaRadius };
            controller.antennaInteraction = model.GetComponentInChildren<AntennaInteraction>(true);
            controller.wingInteraction = model.GetComponentInChildren<WingInteraction>(true);
            controller.elytraInteraction = model.GetComponentInChildren<ElytraInteraction>(true);
            controller.narrationManager = roots.SelectMany(x => x.GetComponentsInChildren<NarrationManager>(true)).SingleOrDefault();
            controller.autonomousExplorer = owner.GetComponent<LadybirdAutonomousExplorer>();
            if (controller.autonomousExplorer != null)
            { Undo.RecordObject(controller.autonomousExplorer, "Disable autonomous test instance"); controller.autonomousExplorer.enabled = false; }
            Physics.SyncTransforms();
            if (!controller.safety.TryFindStart(owner, controller.groundSurfaces, start.position, start.position,
                out Vector3 safeStart, out string reason)) throw new InvalidOperationException("Safe-start verification failed: " + reason);
            start.position = safeStart;

            GameObject testObject = roots.SingleOrDefault(x => x.name == "Prototype2bPlayerTest");
            if (testObject == null)
            { testObject = new GameObject("Prototype2bPlayerTest"); Undo.RegisterCreatedObjectUndo(testObject, "Create Editor test bootstrap"); }
            Prototype2bEditorMovementTest test = testObject.GetComponent<Prototype2bEditorMovementTest>();
            if (test == null) test = Undo.AddComponent<Prototype2bEditorMovementTest>(testObject);
            Undo.RecordObject(test, "Wire Editor-only movement test");
            test.controller = controller; test.experienceManager = manager; test.onboardingZone = onboarding;
            test.selectionZone = selection; test.insectExploreZone = explore.gameObject;
            // Opt-in: normal onboarding remains the default and the flag is absent in Quest builds.
            test.runMovementTestOnPlay = false;
            explore.gameObject.SetActive(exploreWasActive);
            EditorUtility.SetDirty(controller); EditorUtility.SetDirty(test);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new InvalidOperationException("Prototype2b scene could not be saved.");
            Undo.CollapseUndoOperations(group);
            Selection.activeGameObject = owner.gameObject;
            Debug.Log($"Prototype2b configured via Unity APIs. Measured body radius={bodyRadius:F3}m; area radius={areaRadius:F3}m; " +
                $"verified start={safeStart}. No model transforms, interaction events, input assets or XR transforms were changed. " +
                "For independent testing enable Run Movement Test On Play on Prototype2bPlayerTest.", controller);
        }
        catch (Exception error)
        {
            Undo.RevertAllDownToGroup(group);
            Debug.LogError("Prototype2b configuration was rolled back: " + error.Message);
        }
        finally { if (explore != null) explore.gameObject.SetActive(exploreWasActive); }
    }

    private static Transform CreateMarker(string name, Transform parent)
    {
        GameObject marker = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(marker, "Create " + name);
        Undo.SetTransformParent(marker.transform, parent, "Parent " + name);
        marker.transform.localRotation = Quaternion.identity; marker.transform.localScale = Vector3.one;
        return marker.transform;
    }
}
