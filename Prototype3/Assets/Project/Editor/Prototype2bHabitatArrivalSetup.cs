using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

/// <summary>Duplicates the saved environment prompt through native Editor APIs; never edits its source.</summary>
public static class Prototype2bHabitatArrivalSetup
{
    const string ScenePath = "Assets/Project/Scenes/Prototype2b.unity";
    public static void ConfigureBatch()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("Batch only.");
        EditorSceneManager.OpenScene(ScenePath);
        Configure();
    }
    [MenuItem("Tools/Tiny Worlds/Configure Prototype2b Habitat Arrival")]
    public static void Configure()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (Application.isPlaying || scene.path != ScenePath || scene.isDirty)
            throw new InvalidOperationException("Open saved Prototype2b outside Play mode first.");
        var all = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<Transform>(true)).ToArray();
        var objective = all.Select(x => x.GetComponent<LadybirdHabitatObjective>()).Single(x => x != null);
        if (objective.arrivalCard != null || all.Any(x => x.name == "HabitatArrivalCanvas"))
            throw new InvalidOperationException("Arrival UI already exists; verify it instead of duplicating it.");
        var source = objective.transition.promptPanel;
        var sourceCanvas = source.GetComponentInParent<Canvas>(true);
        if (sourceCanvas.renderMode != RenderMode.WorldSpace || sourceCanvas.GetComponent<TrackedDeviceGraphicRaycaster>() == null ||
            all.Count(x => x.GetComponent<EventSystem>() != null) != 1)
            throw new InvalidOperationException("Expected the existing world-space XR UI and one EventSystem.");
        var originalTarget = objective.habitatTarget.position;
        var canvasObject = new GameObject("HabitatArrivalCanvas", typeof(RectTransform));
        canvasObject.layer = sourceCanvas.gameObject.layer;
        // Sibling of the stationary navigation canvas, outside the moving Ladybird.
        canvasObject.transform.SetParent(sourceCanvas.transform.parent, false);
        foreach (var component in sourceCanvas.GetComponents<Component>())
            if (component is Canvas || component is CanvasScaler || component is TrackedDeviceGraphicRaycaster)
                EditorUtility.CopySerialized(component, canvasObject.AddComponent(component.GetType()));
        var rect = (RectTransform)canvasObject.transform;
        rect.sizeDelta = new Vector2(700, 360);
        rect.localScale = Vector3.one;
        // 2.8m wide, 1.44m high; compensate for the stationary parent scale.
        var parentScale = rect.parent == null ? Vector3.one : rect.parent.lossyScale;
        rect.localScale = new Vector3(.004f / parentScale.x, .004f / parentScale.y, .004f / parentScale.z);
        var camera = objective.controller.xrOrigin.Camera;
        var towardsPlayer = Vector3.ProjectOnPlane(camera.transform.position - originalTarget, Vector3.up).normalized;
        rect.position = originalTarget + Vector3.up * 1.95f + towardsPlayer * .8f;
        rect.rotation = Quaternion.LookRotation(rect.position - camera.transform.position, Vector3.up);
        canvasObject.GetComponent<Canvas>().worldCamera = camera;
        var card = UnityEngine.Object.Instantiate(source, rect, false);
        card.name = "HabitatArrivalCard";
        var cardRect = (RectTransform)card.transform;
        cardRect.anchoredPosition = Vector2.zero;
        cardRect.localRotation = Quaternion.identity;
        cardRect.localScale = Vector3.one;
        cardRect.sizeDelta = rect.sizeDelta;
        var description = card.GetComponentsInChildren<TMP_Text>(true).Single(x => x.name == "PromptText");
        description.name = "Description";
        description.text = "Great job! Your ladybird has reached a feeding habitat. Ready to discover what ladybirds eat?";
        description.rectTransform.sizeDelta = new Vector2(660, 150);
        description.rectTransform.anchoredPosition = new Vector2(0, 15);
        var title = UnityEngine.Object.Instantiate(description, card.transform, false);
        title.name = "Title"; title.text = "You've Found the Habitat!";
        title.rectTransform.sizeDelta = new Vector2(660, 60);
        title.rectTransform.anchoredPosition = new Vector2(0, 125);
        var button = card.GetComponentInChildren<UnityEngine.UI.Button>(true);
        button.name = "ContinueToFoodDiscoveryButton";
        ((RectTransform)button.transform).anchoredPosition = new Vector2(0, -125);
        for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
            UnityEventTools.RemovePersistentListener(button.onClick, i);
        UnityEventTools.AddPersistentListener(button.onClick, objective.ContinueToFoodDiscovery);
        button.GetComponentInChildren<TMP_Text>(true).text = "Continue to Food Discovery";
        // Retain the navigation panel as the temporary Food Discovery message area.
        var oldButton = objective.continueButton;
        oldButton.SetActive(false);
        objective.continueButton = button.gameObject;
        objective.arrivalCard = card;
        card.SetActive(false);
        if (objective.habitatTarget.position != originalTarget) throw new Exception("Target moved.");
        EditorUtility.SetDirty(objective);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Verify();
    }
    public static void VerifyBatch() { EditorSceneManager.OpenScene(ScenePath); Verify(); }
    public static void Verify()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var all = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<Transform>(true)).ToArray();
        var o = all.Select(x => x.GetComponent<LadybirdHabitatObjective>()).Single(x => x != null);
        var canvas = o.arrivalCard.GetComponentInParent<Canvas>(true);
        var button = o.continueButton.GetComponent<UnityEngine.UI.Button>();
        if (scene.path != ScenePath || o.arrivalCard.activeSelf || canvas.renderMode != RenderMode.WorldSpace ||
            canvas.GetComponent<TrackedDeviceGraphicRaycaster>() == null || canvas.worldCamera == null ||
            canvas.transform.IsChildOf(o.controller.transform) || button.onClick.GetPersistentEventCount() != 1 ||
            button.onClick.GetPersistentTarget(0) != o || button.onClick.GetPersistentMethodName(0) != "ContinueToFoodDiscovery" ||
            !button.targetGraphic.raycastTarget || all.Count(x => x.GetComponent<EventSystem>() != null) != 1 ||
            all.Count(x => x.GetComponent<XRUIInputModule>() != null) != 1)
            throw new Exception("Invalid Habitat arrival XR UI references.");
        Debug.Log($"HABITAT ARRIVAL verified: canvas world position={canvas.transform.position:F6}, rotation={canvas.transform.eulerAngles:F6}, scale={canvas.transform.lossyScale:F6}; target={o.habitatTarget.position:F6}; camera={canvas.worldCamera.transform.position:F6}. Hidden card, single callback, shared EventSystem/XR module.");
    }
}
