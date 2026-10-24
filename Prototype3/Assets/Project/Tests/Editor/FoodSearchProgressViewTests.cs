using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class FoodSearchProgressViewTests
{
    GameObject root;
    Component activity, view, count, completion, fill;
    const BindingFlags Fields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    Type Runtime(string name) => Assembly.Load("Assembly-CSharp").GetType(name, true);
    void Set(Component component, string field, object value) => component.GetType().GetField(field, Fields).SetValue(component, value);
    object Get(Component component, string property) => component.GetType().GetProperty(property).GetValue(component);
    void Refresh(float dt, bool immediate) => view.GetType().GetMethod("Refresh", Fields).Invoke(view, new object[] { dt, immediate });
    Component UI(string name, string assembly, string type)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(root.transform, false);
        return go.AddComponent(Assembly.Load(assembly).GetType(type, true));
    }
    void AddCatch()
    {
        var go = new GameObject("Unique target"); go.transform.SetParent(root.transform);
        var target = go.AddComponent(Runtime("FoodSearchTarget"));
        var found = activity.GetType().GetField("found", Fields).GetValue(activity);
        found.GetType().GetMethod("Add").Invoke(found, new object[] { target });
    }
    [SetUp] public void Setup()
    {
        root = new GameObject("Progress view fixture"); root.SetActive(false);
        activity = root.AddComponent(Runtime("FoodSearchActivity"));
        view = root.AddComponent(Runtime("FoodSearchProgressView"));
        count = UI("Count", "Unity.TextMeshPro", "TMPro.TextMeshProUGUI");
        completion = UI("Completion", "Unity.TextMeshPro", "TMPro.TextMeshProUGUI");
        fill = UI("Fill", "UnityEngine.UI", "UnityEngine.UI.Image");
        Set(view, "activity", activity); Set(view, "countText", count);
        Set(view, "completionText", completion); Set(view, "fill", fill);
        Refresh(0f, true);
    }
    [TearDown] public void Cleanup() { UnityEngine.Object.DestroyImmediate(root); }

    [Test] public void EveryUniqueCountMatchesMissionDenominator()
    {
        for (int i = 0; i <= 10; i++)
        {
            Refresh(1f, false);
            Assert.That((float)Get(fill, "fillAmount"), Is.EqualTo(i / 10f).Within(.0001f));
            StringAssert.Contains($">{i}</color>", (string)Get(count, "text"));
            StringAssert.Contains(" / 10", (string)Get(count, "text"));
            if (i < 10) AddCatch();
        }
        Assert.That((int)Get(activity, "FoundCount"), Is.EqualTo(10));
        Assert.That(activity.GetType().GetField("requiredAphids").GetValue(activity), Is.EqualTo(10));
    }
    [Test] public void FillInterpolatesAndResetClearsImmediately()
    {
        AddCatch(); Refresh(.04f, false);
        Assert.That((float)Get(fill, "fillAmount"), Is.GreaterThan(0f).And.LessThan(.1f));
        Refresh(1f, false);
        Assert.That((float)Get(fill, "fillAmount"), Is.EqualTo(.1f).Within(.0001f));
        activity.GetType().GetMethod("BeginSearch").Invoke(activity, null);
        Refresh(0f, false);
        Assert.That((float)Get(fill, "fillAmount"), Is.Zero);
        StringAssert.Contains(">0</color>", (string)Get(count, "text"));
    }
    [Test] public void CompletionIsGreenAndOriginalPlainTextCanBeRestyled()
    {
        for (int i = 0; i < 10; i++) AddCatch();
        activity.GetType().GetProperty("Completed").SetValue(activity, true);
        Refresh(0f, false);
        Assert.That((float)Get(fill, "fillAmount"), Is.EqualTo(1f));
        Assert.That((Color)Get(fill, "color"), Is.EqualTo((Color)view.GetType().GetField("completedColor").GetValue(view)));
        StringAssert.Contains("10 / 10", (string)Get(completion, "text"));
        count.GetType().GetProperty("text").SetValue(count, "Aphids caught: 10 / 10");
        Refresh(0f, false);
        StringAssert.StartsWith("<b><size=52>", (string)Get(count, "text"));
    }
}
