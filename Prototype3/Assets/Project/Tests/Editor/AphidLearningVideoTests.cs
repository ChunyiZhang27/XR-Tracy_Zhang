using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class AphidLearningVideoTests
{
    Type Runtime => Assembly.Load("Assembly-CSharp").GetType("AphidLearningVideo",true);
    GameObject root;
    Component video, player, fallback, image;
    object Call(string name, params object[] args) => Runtime.GetMethod(name,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).Invoke(video,args);
    void Set(string name,object value) => Runtime.GetField(name).SetValue(video,value);
    string State => Runtime.GetProperty("State").GetValue(video).ToString();
    [SetUp] public void Setup()
    {
        root = new GameObject("Card 2 fixture",typeof(RectTransform)); root.SetActive(false);
        player = root.AddComponent(Assembly.Load("UnityEngine.VideoModule").GetType("UnityEngine.Video.VideoPlayer",true));
        video = root.AddComponent(Runtime); Set("player",player);
        var text = new GameObject("Fallback",typeof(RectTransform)); text.transform.SetParent(root.transform,false);
        fallback = text.AddComponent(Assembly.Load("Unity.TextMeshPro").GetType("TMPro.TextMeshProUGUI",true)); Set("fallbackText",fallback);
        var raw = new GameObject("Video",typeof(RectTransform)); raw.transform.SetParent(root.transform,false);
        image = raw.AddComponent(Assembly.Load("UnityEngine.UI").GetType("UnityEngine.UI.RawImage",true)); Set("videoImage",image);
        root.SetActive(true); Call("OnEnable");
    }
    [TearDown] public void Cleanup() { Call("OnDisable"); UnityEngine.Object.DestroyImmediate(root); }
    [Test] public void MissingMediaAndReplayRemainUsefulWithoutPlayback()
    {
        Assert.AreEqual("MissingMedia",State);
        Assert.AreEqual(Runtime.GetField("MissingMediaMessage").GetRawConstantValue(),fallback.GetType().GetProperty("text").GetValue(fallback));
        Assert.IsTrue(((Behaviour)fallback).enabled); Assert.IsFalse(((Behaviour)image).enabled);
        Call("Replay"); Call("Replay"); Assert.AreEqual("MissingMedia",State);
        Assert.IsFalse((bool)player.GetType().GetProperty("isPlaying").GetValue(player));
    }
    [Test] public void HiddenCardStopsAndRejectsReplayAndLateCallbacks()
    {
        root.SetActive(false); Call("OnDisable");
        Call("Replay"); Call("Prepared",player); Call("FrameReady",player,0L); Call("Finished",player);
        Assert.AreEqual("Hidden",State); Assert.IsFalse(((Behaviour)image).enabled);
        Assert.IsFalse((bool)player.GetType().GetProperty("isPlaying").GetValue(player));
        root.SetActive(true); Call("OnEnable"); Assert.AreEqual("MissingMedia",State);
    }
    [TestCase(0,"Aphids are tiny insects that feed on plant sap.")]
    [TestCase(3.99,"Aphids are tiny insects that feed on plant sap.")]
    [TestCase(4,"Look for a pear-shaped body, six legs and antennae.")]
    [TestCase(8.99,"Look for a pear-shaped body, six legs and antennae.")]
    [TestCase(9,"Can you recognise and find five aphids?")]
    [TestCase(15,"Can you recognise and find five aphids?")]
    public void SubtitlesFollowStoryboard(double seconds,string expected)
    { Assert.AreEqual(expected,Runtime.GetMethod("SubtitleAt").Invoke(null,new object[]{seconds})); }
}
