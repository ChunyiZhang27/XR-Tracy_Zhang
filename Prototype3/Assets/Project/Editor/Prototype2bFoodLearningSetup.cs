using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit.UI;

/// <summary>Scene-only variants of the existing arrival card. No camera, environment or XR changes.</summary>
public static class Prototype2bFoodLearningSetup
{
    const string ScenePath = "Assets/Project/Scenes/Prototype2b.unity";
    public static void ConfigureBatch()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("Batch only.");
        EditorSceneManager.OpenScene(ScenePath);
        Configure();
    }
    [MenuItem("Tools/Tiny Worlds/Configure Prototype2b Food Learning Cards")]
    public static void Configure()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (Application.isPlaying || scene.path != ScenePath || scene.isDirty)
            throw new InvalidOperationException("Open saved Prototype2b outside Play mode.");
        var all = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<Transform>(true)).ToArray();
        var objective = all.Select(x => x.GetComponent<LadybirdHabitatObjective>()).Single(x => x != null);
        if (objective.foodLearningCard1 != null || all.Any(x => x.name == "FoodLearningCard1"))
            throw new InvalidOperationException("Food learning UI already exists; do not duplicate it.");
        var canvas = objective.arrivalCard.GetComponentInParent<Canvas>(true);
        if (canvas.renderMode != RenderMode.WorldSpace || canvas.worldCamera == null ||
            canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>() == null || canvas.GetComponent<TrackedDeviceGraphicRaycaster>() == null ||
            canvas.transform.IsChildOf(objective.controller.transform) ||
            all.Count(x => x.GetComponent<EventSystem>() != null) != 1 || all.Count(x => x.GetComponent<XRUIInputModule>() != null) != 1)
            throw new InvalidOperationException("Expected the existing stationary mouse/XR arrival canvas and one input system.");
        var first = Copy(objective.arrivalCard, "FoodLearningCard1", "What Does a Ladybird Eat?",
            "Many ladybird species eat aphids. Aphids are tiny insects that feed on plant sap. Ladybirds help keep aphid populations under control.");
        Wire(first.GetComponentInChildren<UnityEngine.UI.Button>(true), "NextButton", "Next", objective.NextFoodLearningCard);
        var second = Copy(objective.arrivalCard, "FoodLearningCard2", "How to Recognise Aphids",
            "Aphids are small, soft-bodied insects, often green, yellow, brown or black. Look for pear-shaped bodies and groups gathering on plant stems and leaves.");
        var back = second.GetComponentInChildren<UnityEngine.UI.Button>(true);
        var start = UnityEngine.Object.Instantiate(back, second.transform, false);
        Wire(back, "BackButton", "Back", objective.BackToFirstFoodLearningCard);
        Wire(start, "StartFoodSearchButton", "Start Food Search", objective.StartFoodSearch);
        ButtonRect(back, -205, 180); ButtonRect(start, 100, 390);
        var ready = Copy(objective.arrivalCard, "FoodSearchReadyMessage", "Food Search Ready", "Find the aphids hiding among the insects!");
        UnityEngine.Object.DestroyImmediate(ready.GetComponentInChildren<UnityEngine.UI.Button>(true).gameObject);
        objective.foodLearningCard1 = first; objective.foodLearningCard2 = second; objective.foodSearchReadyPanel = ready;
        EditorUtility.SetDirty(objective); EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Save failed.");
        Debug.Log("FOOD LEARNING configured: shared arrival canvas, original arrival placement preserved; reused style, hidden cards, four persistent navigation callbacks.");
    }
    static GameObject Copy(GameObject source, string name, string title, string body)
    {
        var card = UnityEngine.Object.Instantiate(source, source.transform.parent, false); card.name = name;
        AlignCard(source.GetComponentInParent<Canvas>(true).GetComponent<RectTransform>(), card);
        card.transform.Find("Title").GetComponent<TMP_Text>().text = title;
        card.transform.Find("Description").GetComponent<TMP_Text>().text = body;
        card.SetActive(false); return card;
    }
    public static void AlignBatch()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("Batch only.");
        EditorSceneManager.OpenScene(ScenePath);
        AlignExistingCards();
    }
    [MenuItem("Tools/Tiny Worlds/Align Food Cards To Habitat Canvas")]
    public static void AlignExistingCards()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (Application.isPlaying || scene.path != ScenePath || scene.isDirty)
            throw new InvalidOperationException("Open saved Prototype2b outside Play mode.");
        var objective = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<LadybirdHabitatObjective>(true)).Single();
        var reference = objective.arrivalCard.GetComponentInParent<Canvas>(true).GetComponent<RectTransform>();
        var cards = new[] { objective.arrivalCard, objective.foodLearningCard1, objective.foodLearningCard2, objective.foodSearchReadyPanel };
        if (cards.Any(x => x == null)) throw new InvalidOperationException("Missing card references.");
        foreach (var card in cards) AlignCard(reference, card);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Save failed.");
        Debug.Log($"FOOD ALIGNMENT reference={reference.name}, parent={reference.parent.name}, localPosition={reference.localPosition:F6}, localRotation={reference.localEulerAngles:F6}, worldPosition={reference.position:F6}, worldRotation={reference.eulerAngles:F6}, worldScale={reference.lossyScale:F6}, size={reference.rect.size:F6}; all four card roots share its pose and scale.");
    }
    static void AlignCard(RectTransform reference, GameObject card)
    {
        var rect = card.GetComponent<RectTransform>();
        rect.SetParent(reference, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = reference.pivot;
        rect.anchoredPosition3D = Vector3.zero;
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one;
        rect.sizeDelta = reference.rect.size;
    }
    static void Wire(UnityEngine.UI.Button button, string name, string label, UnityAction action)
    {
        button.name = name;
        for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--) UnityEventTools.RemovePersistentListener(button.onClick, i);
        UnityEventTools.AddPersistentListener(button.onClick, action);
        button.GetComponentInChildren<TMP_Text>(true).text = label;
    }
    static void ButtonRect(UnityEngine.UI.Button button, float x, float width)
    {
        var rect = (RectTransform)button.transform;
        rect.anchoredPosition = new Vector2(x, rect.anchoredPosition.y); rect.sizeDelta = new Vector2(width, rect.sizeDelta.y);
        var label = button.GetComponentInChildren<TMP_Text>(true).rectTransform;
        label.sizeDelta = new Vector2(width - 20, label.sizeDelta.y);
    }
}
