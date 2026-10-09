using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.UI;

public static class Prototype2bTransitionSetup
{
    const string Path = "Assets/Project/Scenes/Prototype2b.unity";
    public static void ConfigureBatch()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("Batch only.");
        EditorSceneManager.OpenScene(Path, OpenSceneMode.Single);
        Configure();
    }
    public static void VerifyBatch()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("Batch only.");
        var scene = EditorSceneManager.OpenScene(Path, OpenSceneMode.Single);
        var all = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<Transform>(true)).ToArray();
        var coordinator = all.Select(x=>x.GetComponent<LadybirdEnvironmentTransition>()).Single(x=>x!=null);
        var manager = all.Select(x=>x.GetComponent<ExperienceManager>()).Single(x=>x!=null);
        for (int i=manager.beforeBackToSelection.GetPersistentEventCount()-1;i>=0;i--)
            if (manager.beforeBackToSelection.GetPersistentTarget(i)==coordinator)
                UnityEventTools.RemovePersistentListener(manager.beforeBackToSelection,i);
        UnityEventTools.AddPersistentListener(manager.beforeBackToSelection,coordinator.ExitForSelection);
        if (coordinator.controller == null || !coordinator.controller.externalTransitionControl ||
            coordinator.progress == null || !coordinator.progress.verifyTransitionReadiness ||
            !coordinator.narration.verifyPlaybackCompletion || coordinator.bodyInteractables.Length == 0 ||
            coordinator.promptPanel == null || coordinator.movementPanel == null || coordinator.statusText == null)
            throw new InvalidOperationException("Incomplete transition configuration.");
        if (coordinator.promptPanel.transform.IsChildOf(coordinator.controller.transform)) throw new InvalidOperationException("UI cannot move with Ladybird.");
        var status=coordinator.statusText.rectTransform;
        if(status.parent.Find("StatusBackground")==null)
        {
            var background=Rect("StatusBackground",status.parent,status.sizeDelta,status.anchoredPosition);
            Background(background); background.SetSiblingIndex(status.GetSiblingIndex());
        }
        // Keep feedback below the existing Back button, clear of both button rows.
        status.anchoredPosition = new Vector2(0,-360);
        ((RectTransform)status.parent).sizeDelta = new Vector2(700,900);
        coordinator.statusBackground=status.parent.Find("StatusBackground").gameObject;
        ((RectTransform)coordinator.statusBackground.transform).anchoredPosition=status.anchoredPosition;
        coordinator.statusBackground.SetActive(false); EditorUtility.SetDirty(coordinator);
        foreach(var button in coordinator.promptPanel.GetComponentsInChildren<UnityEngine.UI.Button>(true).Concat(coordinator.movementPanel.GetComponentsInChildren<UnityEngine.UI.Button>(true)))
            if(button.onClick.GetPersistentEventCount()!=1 || !button.targetGraphic.raycastTarget) throw new InvalidOperationException("Invalid XR button.");
        EditorUtility.SetDirty(manager); EditorSceneManager.MarkSceneDirty(scene);
        if(!EditorSceneManager.SaveScene(scene,Path)) throw new InvalidOperationException("Save failed.");
        Debug.Log("Prototype2b transition references, UI buttons and Back cleanup verified through native Editor APIs.");
    }
    [MenuItem("Tools/Tiny Worlds/Configure Prototype2b Environment Transition")]
    public static void Configure()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (Application.isPlaying || scene.path != Path || SceneManager.sceneCount != 1 || scene.isDirty)
            throw new InvalidOperationException("Open only saved Prototype2b outside Play mode.");
        var all = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<Transform>(true)).ToArray();
        var owner = all.Single(x => x.name == "LadybirdExploreModel_OLD");
        var canvas = all.Single(x => x.name == "ExploreNavigationUI");
        if (canvas.GetComponent<Canvas>().renderMode != RenderMode.WorldSpace || canvas.GetComponent<TrackedDeviceGraphicRaycaster>() == null)
            throw new InvalidOperationException("Existing XR canvas is invalid.");
        if (all.Count(x => x.GetComponent<UnityEngine.EventSystems.EventSystem>() != null) != 1)
            throw new InvalidOperationException("Expected one existing EventSystem.");
        if (canvas.Find("EnvironmentTransitionUI") != null) throw new InvalidOperationException("Transition UI already exists; inspect it before reconfiguring.");
        int group = Undo.GetCurrentGroup();
        try
        {
            var player = owner.GetComponent<LadybirdPlayerController>();
            var progress = all.Select(x => x.GetComponent<LadybirdExplorationProgress>()).Single(x => x != null);
            var narrator = player.narrationManager;
            var manager = all.Select(x => x.GetComponent<ExperienceManager>()).Single(x => x != null);
            Undo.RecordObjects(new UnityEngine.Object[] {player, progress, narrator, manager}, "Configure opt-in transition");
            player.externalTransitionControl = true;
            progress.verifyTransitionReadiness = true;
            progress.antennaInteraction = player.antennaInteraction; progress.wingInteraction = player.wingInteraction; progress.elytraInteraction = player.elytraInteraction;
            narrator.verifyPlaybackCompletion = true;
            var coordinator = Undo.AddComponent<LadybirdEnvironmentTransition>(owner.gameObject);
            coordinator.progress = progress; coordinator.controller = player; coordinator.narration = narrator;
            coordinator.antenna = player.antennaInteraction; coordinator.wings = player.wingInteraction; coordinator.elytra = player.elytraInteraction;
            coordinator.bodyInteractables = owner.GetComponentsInChildren<XRBaseInteractable>(true);
            coordinator.infoPanel = all.Select(x => x.GetComponent<InsectInfoPanel>()).Single(x => x != null);
            UnityEventTools.AddPersistentListener(manager.beforeBackToSelection, coordinator.ExitForSelection);
            var container = Rect("EnvironmentTransitionUI", canvas, new Vector2(700,900), new Vector2(0,240));
            var prompt = Rect("EnvironmentPrompt", container, new Vector2(700,290), Vector2.zero);
            Background(prompt);
            Text("PromptText", prompt, "You've explored the Ladybird! Ready to discover its environment?", new Vector2(660,130), new Vector2(0,60), 30);
            Button("ExploreEnvironmentButton", prompt, "Explore Environment", new Vector2(0,-75), coordinator.ExploreEnvironment);
            var movement = Rect("EnvironmentControls", container, new Vector2(700,290), Vector2.zero);
            Background(movement);
            Text("MovementInstructions", movement, "Left thumbstick: up/down to walk, left/right to turn.\nYour viewpoint stays in place.\nEditor: W/S walk · A/D turn · Space stop", new Vector2(660,140), new Vector2(0,55), 27);
            Button("ReturnToBodyButton", movement, "Return to Body Exploration", new Vector2(0,-80), coordinator.ReturnToBodyExploration);
            coordinator.statusText = Text("TransitionStatus", container, "", new Vector2(700,90), new Vector2(0,-360), 25);
            var statusBackground = Rect("StatusBackground",container,new Vector2(700,90),new Vector2(0,-360));
            Background(statusBackground); statusBackground.SetSiblingIndex(coordinator.statusText.transform.GetSiblingIndex());
            coordinator.statusBackground=statusBackground.gameObject; statusBackground.gameObject.SetActive(false);
            coordinator.promptPanel = prompt.gameObject; coordinator.movementPanel = movement.gameObject;
            prompt.gameObject.SetActive(false); movement.gameObject.SetActive(false);
            var test = all.Select(x => x.GetComponent<Prototype2bEditorMovementTest>()).Single(x => x != null);
            Undo.RecordObject(test, "Default to full experience flow"); test.runMovementTestOnPlay = false;
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, Path)) throw new InvalidOperationException("Save failed.");
            Undo.CollapseUndoOperations(group);
            Debug.Log("Prototype2b transition configured natively. UI stationary, original button callbacks retained, shared flags enabled only here.");
        }
        catch { Undo.RevertAllDownToGroup(group); throw; }
    }
    static RectTransform Rect(string name, Transform parent, Vector2 size, Vector2 position)
    {
        var go = new GameObject(name, typeof(RectTransform)); Undo.RegisterCreatedObjectUndo(go, "Create transition UI");
        var rect = go.GetComponent<RectTransform>(); rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f,.5f); rect.sizeDelta = size; rect.anchoredPosition = position;
        return rect;
    }
    static void Background(RectTransform rect)
    { var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>(); image.color = new Color(.025f,.045f,.06f,.96f); image.raycastTarget = false; }
    static TMP_Text Text(string name, Transform parent, string value, Vector2 size, Vector2 position, float fontSize)
    {
        var rect = Rect(name,parent,size,position); var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = TMP_Settings.defaultFontAsset; text.text = value; text.fontSize = fontSize; text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center; text.raycastTarget = false; return text;
    }
    static void Button(string name, Transform parent, string label, Vector2 position, UnityEngine.Events.UnityAction action)
    {
        var rect = Rect(name,parent,new Vector2(570,72),position);
        var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>(); image.color = new Color(.08f,.28f,.18f,1); image.raycastTarget = true;
        var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>(); button.targetGraphic = image;
        var nav = button.navigation; nav.mode = UnityEngine.UI.Navigation.Mode.None; button.navigation = nav;
        UnityEventTools.AddPersistentListener(button.onClick,action);
        Text("Label",rect,label,new Vector2(550,68),Vector2.zero,28);
    }
}
