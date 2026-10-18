using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class FoodSearchActivityTests
{
    GameObject fixture, area, panel, completion, replay;
    Component activity, feedback, progress, hand;
    Component[] targets, grabs;
    Type Runtime(string name) => Assembly.Load("Assembly-CSharp").GetType(name,true);
    Type XR(string name) => Assembly.Load("Unity.XR.Interaction.Toolkit").GetType("UnityEngine.XR.Interaction.Toolkit."+name,true);
    void Set(object obj,string name,object value) => obj.GetType().GetField(name,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance).SetValue(obj,value);
    object Call(object obj,string name,params object[] args) => obj.GetType().GetMethod(name,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance).Invoke(obj,args);
    T Get<T>(object obj,string name) => (T)obj.GetType().GetProperty(name).GetValue(obj);
    Component Text(string name)
    {
        var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(fixture.transform,false);
        return go.AddComponent(Assembly.Load("Unity.TextMeshPro").GetType("TMPro.TextMeshProUGUI",true));
    }
    [SetUp] public void Setup()
    {
        fixture=new GameObject("Five-catch fixture");fixture.SetActive(false);
        fixture.AddComponent(XR("XRInteractionManager"));
        area=new GameObject("Area");area.transform.SetParent(fixture.transform,false);
        panel=new GameObject("Search");panel.transform.SetParent(fixture.transform,false);
        completion=new GameObject("Completion");completion.transform.SetParent(fixture.transform,false);
        replay=new GameObject("Try Again");replay.transform.SetParent(completion.transform,false);
        hand=new GameObject("Synthetic controller").AddComponent(XR("Interactors.XRDirectInteractor"));hand.transform.SetParent(fixture.transform,false);
        activity=fixture.AddComponent(Runtime("FoodSearchActivity"));feedback=Text("Feedback");progress=Text("Progress");
        Set(activity,"insectArea",area);Set(activity,"searchPanel",panel);Set(activity,"completionPanel",completion);
        Set(activity,"tryAgainButton",replay);Set(activity,"feedbackText",feedback);Set(activity,"progressText",progress);
        targets=new Component[7];grabs=new Component[7];var typed=Array.CreateInstance(Runtime("FoodSearchTarget"),7);
        var category=Runtime("FoodSearchTarget").GetNestedType("IdentificationCategory");
        for(int i=0;i<7;i++)
        {
            var go=new GameObject("Name does not determine category "+i);go.transform.SetParent(area.transform,false);
            grabs[i]=go.AddComponent(XR("Interactables.XRGrabInteractable"));
            targets[i]=go.AddComponent(Runtime("FoodSearchTarget"));Set(targets[i],"activity",activity);
            Set(targets[i],"category",Enum.Parse(category,i<5?"Aphid":"Ant"));typed.SetValue(targets[i],i);
        }
        Set(activity,"targets",typed);fixture.SetActive(true);
        Call(activity,"EndSearch");Call(activity,"BeginSearch");
    }
    [TearDown] public void Cleanup() { UnityEngine.Object.DestroyImmediate(fixture); }
    object Args(int i,bool selected)
    {
        // Unit fixture supplies explicit live-selection state. Full manager/attach/release is covered in Play Mode validation.
        XR("Interactables.XRBaseInteractable").GetProperty("isSelected").SetValue(grabs[i],selected);
        var selectors=(IList)grabs[i].GetType().GetProperty("interactorsSelecting").GetValue(grabs[i]);selectors.Clear();if(selected)selectors.Add(hand);
        var args=Activator.CreateInstance(XR("SelectEnterEventArgs"));
        args.GetType().GetProperty("interactorObject",BindingFlags.Public|BindingFlags.Instance|BindingFlags.DeclaredOnly).SetValue(args,hand);
        args.GetType().GetProperty("interactableObject",BindingFlags.Public|BindingFlags.Instance|BindingFlags.DeclaredOnly).SetValue(args,grabs[i]);return args;
    }
    void Catch(int i) { Call(activity,"ConfirmGrab",targets[i],Args(i,true)); Args(i,false); }
    void ShowCompletion()
    {
        Call(activity,"CancelCompletion");
        var routine=(IEnumerator)Call(activity,"CompleteAfterRelease");
        for(int i=0;i<5&&routine.MoveNext();i++) { }
    }
    [Test] public void RequiresFiveUniqueConfirmedGrabsAndRejectsClicksOrInvalidEvents()
    {
        Assert.AreEqual("Aphids caught: 0 / 5",Get<string>(progress,"text"));
        Call(targets[0],"SelectTarget");Call(activity,"Identify",targets[0]);Call(activity,"ConfirmGrab",targets[0],Args(0,false));
        Assert.AreEqual(0,Get<int>(activity,"FoundCount"));
        for(int i=0;i<4;i++)Catch(i);
        Assert.AreEqual(4,Get<int>(activity,"FoundCount"));Assert.IsFalse(Get<bool>(activity,"Completed"));
        Catch(0);Assert.AreEqual(4,Get<int>(activity,"FoundCount"));Catch(4);
        Assert.AreEqual(5,Get<int>(activity,"FoundCount"));Assert.IsTrue(Get<bool>(activity,"Completed"));
        Assert.AreEqual("Aphids caught: 5 / 5",Get<string>(progress,"text"));
        ShowCompletion();Assert.IsTrue(completion.activeSelf);Assert.IsFalse(panel.activeSelf);Assert.IsFalse(area.activeSelf);
    }
    [Test] public void IncorrectGrabsRemainAvailableAndDoNotReduceProgress()
    {
        Catch(0);Catch(5);Catch(5);Catch(6);
        Assert.AreEqual(1,Get<int>(activity,"FoundCount"));Assert.IsFalse(Get<bool>(targets[5],"Found"));Assert.IsTrue(targets[5].gameObject.activeSelf);
        Assert.AreEqual("Not quite! Look for tiny, soft-bodied insects with pear-shaped bodies and antennae.",Get<string>(feedback,"text"));
    }
    [Test] public void CompletionWaitsForReleaseAndReplayClearsAllUniqueScores()
    {
        for(int i=0;i<4;i++)Catch(i);
        Call(activity,"ConfirmGrab",targets[4],Args(4,true));
        Call(activity,"TryAgain");Assert.AreEqual(5,Get<int>(activity,"FoundCount"));Assert.IsFalse(Get<bool>(activity,"CompletionVisible"));
        Args(4,false);ShowCompletion();Call(activity,"TryAgain");Call(activity,"TryAgain");
        Assert.AreEqual(0,Get<int>(activity,"FoundCount"));Assert.IsFalse(Get<bool>(activity,"Completed"));
        Assert.IsFalse(completion.activeSelf);Assert.IsTrue(area.activeSelf);Assert.IsTrue(panel.activeSelf);
        Assert.AreEqual("",Get<string>(feedback,"text"));
        foreach(var target in targets)Assert.IsFalse(Get<bool>(target,"Found"));
        Catch(0);Assert.AreEqual(1,Get<int>(activity,"FoundCount"));
        Call(activity,"EndSearch");Catch(1);Assert.AreEqual(0,Get<int>(activity,"FoundCount"));Assert.IsFalse(area.activeSelf);
    }
    [Test] public void MismatchedEventDoesNotScoreAnotherTarget()
    {
        var args=Args(0,true);Call(activity,"ConfirmGrab",targets[1],args);
        Assert.AreEqual(0,Get<int>(activity,"FoundCount"));Args(0,false);
    }
}
