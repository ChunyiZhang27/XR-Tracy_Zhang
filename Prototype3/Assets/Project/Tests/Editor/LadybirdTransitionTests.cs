using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

/// <summary>Runtime types are reflected because the existing scene scripts live in Assembly-CSharp.</summary>
public class LadybirdTransitionTests
{
    GameObject root;
    Type TypeOf(string name) => System.Reflection.Assembly.Load("Assembly-CSharp").GetType(name, true);
    Component Add(string name) => root.AddComponent(TypeOf(name));
    void Set(object item, string name, object value) => item.GetType().GetField(name, BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance).SetValue(item,value);
    object Get(object item, string name) => item.GetType().GetProperty(name).GetValue(item);
    void Call(object item, string name) => item.GetType().GetMethod(name).Invoke(item,null);
    [SetUp] public void Setup() { root = new GameObject("TransitionTests"); }
    [TearDown] public void Cleanup() { UnityEngine.Object.DestroyImmediate(root); }
    [Test] public void SharedScriptsDefaultToLegacyBehaviour()
    {
        foreach(var pair in new[]{new[]{"LadybirdExplorationProgress","verifyTransitionReadiness"},new[]{"NarrationManager","verifyPlaybackCompletion"},new[]{"LadybirdPlayerController","externalTransitionControl"}})
        { var component=Add(pair[0]); Assert.IsFalse((bool)component.GetType().GetField(pair[1]).GetValue(component)); }
    }
    [Test] public void CompletionPromptCannotActivateMovementWithoutRequest()
    {
        var transition=Add("LadybirdEnvironmentTransition"); var controller=Add("LadybirdPlayerController"); var progress=Add("LadybirdExplorationProgress");
        Set(transition,"controller",controller); Set(transition,"progress",progress);
        Set(progress,"<EnvironmentReady>k__BackingField",true);
        Call(transition,"ShowPrompt"); Assert.IsFalse((bool)Get(controller,"ControlActive")); Assert.AreEqual("Body",Get(transition,"CurrentMode").ToString());
    }
    [Test] public void PrematureEntryIsRejected()
    { var transition=Add("LadybirdEnvironmentTransition"); Set(transition,"progress",Add("LadybirdExplorationProgress")); Call(transition,"ExploreEnvironment"); Assert.AreEqual("Body",Get(transition,"CurrentMode").ToString()); }
    [TestCase("Entering")][TestCase("Environment")][TestCase("Returning")]
    public void RepeatedEntryRequestsAreRejected(string mode)
    {
        var transition=Add("LadybirdEnvironmentTransition"); var property=transition.GetType().GetProperty("CurrentMode");
        Set(transition,"<CurrentMode>k__BackingField",Enum.Parse(property.PropertyType,mode));
        Call(transition,"ExploreEnvironment"); Assert.AreEqual(mode,Get(transition,"CurrentMode").ToString());
    }
    [TestCase("Body")][TestCase("Entering")][TestCase("Returning")]
    public void RepeatedOrPrematureReturnRequestsAreRejected(string mode)
    {
        var transition=Add("LadybirdEnvironmentTransition"); var property=transition.GetType().GetProperty("CurrentMode");
        Set(transition,"<CurrentMode>k__BackingField",Enum.Parse(property.PropertyType,mode));
        Call(transition,"ReturnToBodyExploration"); Assert.AreEqual(mode,Get(transition,"CurrentMode").ToString());
    }
    [Test] public void ExitRestoresSavedParentAndRetainsCompletedProgress()
    {
        var transition=Add("LadybirdEnvironmentTransition"); var controller=Add("LadybirdPlayerController"); var progress=Add("LadybirdExplorationProgress");
        Set(transition,"controller",controller); Set(transition,"progress",progress); Set(progress,"<EnvironmentReady>k__BackingField",true);
        Set(transition,"poseSaved",true); Set(transition,"bodyPosition",new Vector3(3,4,5)); Set(transition,"bodyRotation",Quaternion.Euler(0,60,0));
        Call(transition,"ExitImmediately"); Call(transition,"ExitImmediately");
        Assert.AreEqual(new Vector3(3,4,5),root.transform.position); Assert.IsTrue((bool)Get(progress,"EnvironmentReady")); Assert.IsFalse((bool)Get(controller,"ControlActive"));
    }
    [Test] public void StrictProgressRejectsWingSelectionWithoutAcceptedFlap()
    {
        var progress=Add("LadybirdExplorationProgress"); Set(progress,"verifyTransitionReadiness",true); Set(progress,"wingInteraction",Add("WingInteraction"));
        Call(progress,"MarkWingExplored"); Assert.IsFalse((bool)progress.GetType().GetField("wingExplored",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(progress));
    }
    [Test] public void StrictResetClearsReadiness()
    { var progress=Add("LadybirdExplorationProgress"); Set(progress,"verifyTransitionReadiness",true); Set(progress,"<EnvironmentReady>k__BackingField",true); Call(progress,"ResetProgress"); Assert.IsFalse((bool)Get(progress,"EnvironmentReady")); }
    [Test] public void FailedEntryReturnsToBodyWithoutTakingMovementOwnership()
    {
        var transition=Add("LadybirdEnvironmentTransition");
        Set(transition,"<CurrentMode>k__BackingField",Enum.Parse(transition.GetType().GetProperty("CurrentMode").PropertyType,"Entering"));
        var routine=(IEnumerator)transition.GetType().GetMethod("EnterRoutine",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(transition,null);
        Assert.IsFalse(routine.MoveNext()); Assert.AreEqual("Body",Get(transition,"CurrentMode").ToString());
    }
    [Test] public void ExitRestoresExactEnabledStates()
    {
        var transition=Add("LadybirdEnvironmentTransition"); var light=root.AddComponent<Light>(); light.enabled=false;
        var dictionary=(System.Collections.IDictionary)transition.GetType().GetField("disabled",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(transition);
        dictionary.Add(light,true); Call(transition,"ExitImmediately"); Assert.IsTrue(light.enabled); Assert.AreEqual(0,dictionary.Count);
        dictionary.Add(light,false); Call(transition,"ExitImmediately"); Assert.IsFalse(light.enabled);
    }
    [Test] public void BackToSelectionBlocksNewEntryEvenWhenProgressIsReady()
    {
        var transition=Add("LadybirdEnvironmentTransition"); var progress=Add("LadybirdExplorationProgress");
        Set(transition,"progress",progress); Set(progress,"<EnvironmentReady>k__BackingField",true);
        Call(transition,"ExitForSelection"); Call(transition,"ExploreEnvironment");
        Assert.AreEqual("Body",Get(transition,"CurrentMode").ToString());
        Assert.IsTrue((bool)transition.GetType().GetField("leavingSelection",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(transition));
    }
}
