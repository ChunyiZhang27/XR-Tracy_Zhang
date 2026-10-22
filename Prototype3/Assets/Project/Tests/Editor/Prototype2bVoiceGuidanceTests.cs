using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class Prototype2bVoiceGuidanceTests
{
    GameObject root, video;
    Component voice;
    Type type;
    void Call(string method) => type.GetMethod(method).Invoke(voice, null);
    string Stage => type.GetProperty("CurrentStage").GetValue(voice).ToString();
    string Guidance => (string)type.GetProperty("CurrentGuidance").GetValue(voice);
    [SetUp] public void Setup()
    {
        type=Assembly.Load("Assembly-CSharp").GetType("Prototype2bVoiceGuidance",true);
        root=new GameObject("Voice fixture"); voice=root.AddComponent(type);
        video=new GameObject("Video card");video.SetActive(false);
        type.GetField("videoCard").SetValue(voice,video);
    }
    [TearDown] public void Cleanup() { UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(video); }
    [TestCase("EnterExploration","Exploring")]
    [TestCase("HabitatFound","Habitat")]
    [TestCase("StartFoodSearch","FoodSearch")]
    [TestCase("MissionComplete","Complete")]
    [TestCase("FoodLearning1","FoodLearning1")]
    public void MissingClipsAreSafeAndSetReadableGuidance(string method,string stage)
    { Call(method);Assert.AreEqual(stage,Stage);Assert.IsNotEmpty(Guidance);Assert.IsFalse(root.GetComponent<AudioSource>().isPlaying); }
    [Test] public void RepeatedStageEntryDoesNotReplaceCurrentWrongGrabGuidance()
    {
        Call("StartFoodSearch");Call("IncorrectGrab");var text=Guidance;
        Call("StartFoodSearch");Assert.AreEqual(text,Guidance);
    }
    [Test] public void IncorrectCooldownDoesNotExtendOnRejectedGrabs()
    {
        Call("StartFoodSearch");Call("IncorrectGrab");
        var field=type.GetField("nextIncorrectTime",BindingFlags.NonPublic|BindingFlags.Instance);
        var next=(float)field.GetValue(voice);Assert.Greater(next,Time.unscaledTime);
        field.SetValue(voice,next+10);Call("IncorrectGrab");Assert.AreEqual(next+10,field.GetValue(voice));
        field.SetValue(voice,float.NegativeInfinity);Call("IncorrectGrab");Assert.Less((float)field.GetValue(voice),next+10);
    }
    [Test] public void WrongGrabsOutsideSearchDoNothing()
    { Call("HabitatFound");var text=Guidance;Call("IncorrectGrab");Assert.AreEqual(text,Guidance); }
    [Test] public void StageExitClearsGuidanceAndAllowsReplay()
    {
        Call("StartFoodSearch");Call("MissionComplete");Call("Silence");
        Assert.AreEqual("Silent",Stage);Assert.IsEmpty(Guidance);
        Call("StartFoodSearch");StringAssert.Contains("find ten aphids",Guidance);
    }
    [Test] public void VideoReplayCancelsGuidanceEvenWhenMediaMissing()
    {
        Call("HabitatFound");video.SetActive(false);
        var v=video.AddComponent(Assembly.Load("Assembly-CSharp").GetType("AphidLearningVideo",true));
        v.GetType().GetField("voiceGuidance").SetValue(v,voice);
        video.SetActive(true);v.GetType().GetMethod("Replay").Invoke(v,null);Assert.AreEqual("Silent",Stage);Assert.IsEmpty(Guidance);
    }
    [Test] public void LegacyCountRecordingsAreSuppressedButGuidanceRemainsAvailable()
    {
        type.GetField("recordedStartFoodSearchAphidCount").SetValue(voice,5);
        var clip=AudioClip.Create("In-memory test only",4410,1,44100,false);
        try {
            type.GetField("voiceStartFoodSearch").SetValue(voice,clip);
            type.GetField("voiceMissionComplete").SetValue(voice,clip);
            Call("StartFoodSearch");Assert.IsNull(root.GetComponent<AudioSource>().clip);
            StringAssert.Contains("ten aphids",Guidance);
            Call("MissionComplete");Assert.IsNull(root.GetComponent<AudioSource>().clip);
            StringAssert.Contains("ten aphids",Guidance);
        } finally { UnityEngine.Object.DestroyImmediate(clip); }
    }
    [Test] public void FirstLearningCardCanBeReenteredAfterVideoStage()
    {
        Call("FoodLearning1");StringAssert.Contains("Many ladybirds eat aphids",Guidance);
        Call("Silence");Assert.IsEmpty(Guidance);
        Call("FoodLearning1");Assert.AreEqual("FoodLearning1",Stage);
        Call("StartFoodSearch");StringAssert.Contains("hungry",Guidance);
    }
    [Test] public void SourceIsTwoDimensionalAndDoesNotAutoplay()
    {
        type.GetMethod("Awake",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(voice,null);
        var source=root.GetComponent<AudioSource>();Assert.AreEqual(0,source.spatialBlend);
        Assert.IsFalse(source.playOnAwake);Assert.IsFalse(source.loop);
    }
}
