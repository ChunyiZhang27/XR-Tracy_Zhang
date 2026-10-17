using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class FoodSearchActivityTests
{
    GameObject fixture, area, replay;
    Component activity, feedback, progress;
    Component[] targets;
    Type Runtime(string name) => Assembly.Load("Assembly-CSharp").GetType(name,true);
    void Set(object obj,string name,object value) => obj.GetType().GetField(name).SetValue(obj,value);
    object Call(object obj,string name,params object[] args) => obj.GetType().GetMethod(name).Invoke(obj,args);
    T Get<T>(object obj,string name) => (T)obj.GetType().GetProperty(name).GetValue(obj);
    Component Text(string name)
    {
        var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(fixture.transform,false);
        return go.AddComponent(Assembly.Load("Unity.TextMeshPro").GetType("TMPro.TextMeshProUGUI",true));
    }
    [SetUp] public void Setup()
    {
        fixture=new GameObject("E1 fixture");
        fixture.AddComponent(Assembly.Load("Unity.XR.Interaction.Toolkit").GetType("UnityEngine.XR.Interaction.Toolkit.XRInteractionManager",true));
        area=new GameObject("Area");area.transform.SetParent(fixture.transform,false);area.SetActive(false);
        replay=new GameObject("Try Again");replay.transform.SetParent(fixture.transform,false);
        activity=fixture.AddComponent(Runtime("FoodSearchActivity"));feedback=Text("Feedback");progress=Text("Progress");
        Set(activity,"insectArea",area);Set(activity,"tryAgainButton",replay);Set(activity,"feedbackText",feedback);Set(activity,"progressText",progress);
        targets=new Component[5];var typed=Array.CreateInstance(Runtime("FoodSearchTarget"),5);
        var category=Runtime("FoodSearchTarget").GetNestedType("IdentificationCategory");
        for(int i=0;i<5;i++)
        {
            var go=new GameObject("Misleading name "+i);go.transform.SetParent(area.transform,false);
            targets[i]=go.AddComponent(Runtime("FoodSearchTarget"));Set(targets[i],"activity",activity);
            Set(targets[i],"category",Enum.Parse(category,i<3?"Aphid":"Ant"));typed.SetValue(targets[i],i);
        }
        Set(activity,"targets",typed);
    }
    [TearDown] public void Cleanup() { UnityEngine.Object.DestroyImmediate(fixture); }
    [Test] public void CategoriesAndUniqueTargetsControlScoringAndCompletion()
    {
        Call(activity,"Identify",targets[0]);Assert.AreEqual(0,Get<int>(activity,"FoundCount"));
        Call(activity,"BeginSearch");Assert.AreEqual("Aphids found: 0 / 2",Get<string>(progress,"text"));
        Call(activity,"Identify",targets[3]);Call(activity,"Identify",targets[3]);
        Assert.AreEqual(0,Get<int>(activity,"FoundCount"));Assert.IsTrue(targets[3].gameObject.activeSelf);
        Assert.AreEqual("Not quite! Look for small, soft-bodied insects, often found together on leaves and stems.",Get<string>(feedback,"text"));
        Call(targets[0],"SelectTarget");Call(targets[0],"SelectTarget");
        Assert.AreEqual(1,Get<int>(activity,"FoundCount"));Assert.IsTrue(Get<bool>(targets[0],"Found"));
        Assert.AreEqual("Correct! You've found an aphid. Many ladybirds feed on aphids.",Get<string>(feedback,"text"));
        Call(activity,"Identify",targets[1]);Assert.IsTrue(Get<bool>(activity,"Completed"));Assert.IsTrue(replay.activeSelf);
        Assert.AreEqual("Aphids found: 2 / 2",Get<string>(progress,"text"));
        Assert.AreEqual("Well done! You've discovered what ladybirds eat and how they help plants.",Get<string>(feedback,"text"));
        Call(activity,"Identify",targets[2]);Assert.AreEqual(2,Get<int>(activity,"FoundCount"));
    }
    [Test] public void ReplayAndExitClearAllStates()
    {
        Call(activity,"BeginSearch");Call(activity,"Identify",targets[0]);Call(activity,"Identify",targets[1]);Call(activity,"TryAgain");
        Assert.AreEqual(0,Get<int>(activity,"FoundCount"));Assert.IsFalse(Get<bool>(activity,"Completed"));Assert.IsFalse(replay.activeSelf);
        Assert.AreEqual("",Get<string>(feedback,"text"));Assert.IsFalse(Get<bool>(targets[0],"Found"));
        Call(activity,"Identify",targets[0]);Call(activity,"TryAgain");Assert.AreEqual(1,Get<int>(activity,"FoundCount"));
        Call(activity,"EndSearch");Assert.IsFalse(area.activeSelf);Assert.IsFalse(Get<bool>(activity,"Running"));
        Call(activity,"TryAgain");Call(activity,"Identify",targets[1]);Assert.IsFalse(area.activeSelf);Assert.AreEqual(0,Get<int>(activity,"FoundCount"));
    }
}
