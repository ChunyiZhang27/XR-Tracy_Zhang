using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class FoodSearchPlayerLocomotionTests
{
    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    GameObject root;
    Component mode;
    void Set(string field, object value) => mode.GetType().GetField(field, Flags).SetValue(mode, value);
    object Call(string method, params object[] args) => mode.GetType().GetMethod(method, Flags).Invoke(mode, args);
    [SetUp] public void Setup()
    {
        root = new GameObject("Food locomotion tests");
        mode = root.AddComponent(Assembly.Load("Assembly-CSharp").GetType("FoodSearchPlayerLocomotion", true));
        Set("entry", Vector3.zero);
        Set("habitatBounds", new Bounds(Vector3.zero, new Vector3(10, 1, 10)));
        Set("maximumTravel", 3f);
    }
    [TearDown] public void Cleanup() { Object.DestroyImmediate(root); }
    [Test] public void IndependentPlayerBoundaryRestrictsTravelAndHabitatRectangle()
    {
        Assert.That(Call("InsideArea", new Vector3(1, 2, 1)), Is.True);
        Assert.That(Call("InsideArea", new Vector3(4, 0, 0)), Is.False);
        Set("maximumTravel", 20f);
        Assert.That(Call("InsideArea", new Vector3(6, 0, 0)), Is.False);
    }
    [Test] public void NoMissionNeverStartsAndCannotApplyQueuedMotion()
    {
        Call("Update");
        Assert.That(mode.GetType().GetProperty("Active").GetValue(mode), Is.False);
        Assert.That(Call("MoveBody", Vector3.forward), Is.EqualTo(CollisionFlags.None));
        Assert.That(root.transform.position, Is.EqualTo(Vector3.zero));
    }
    [Test] public void HandoverAllowsOnlyMoveAndMediatorWhileLadybirdIsStopped()
    {
        root.SetActive(false);
        var runtime = Assembly.Load("Assembly-CSharp");
        var player = root.AddComponent(runtime.GetType("LadybirdPlayerController", true));
        var xr = Assembly.Load("Unity.XR.Interaction.Toolkit");
        var provider = (Behaviour)root.AddComponent(xr.GetType("UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement.ContinuousMoveProvider", true));
        var mediator = (Behaviour)root.GetComponent(xr.GetType("UnityEngine.XR.Interaction.Toolkit.Locomotion.LocomotionMediator", true));
        if (mediator == null) mediator = (Behaviour)root.AddComponent(xr.GetType("UnityEngine.XR.Interaction.Toolkit.Locomotion.LocomotionMediator", true));
        var suspended = (System.Collections.IDictionary)player.GetType().GetField("suspended", Flags).GetValue(player);
        suspended.Add(provider, true); suspended.Add(mediator, true);
        player.GetType().GetField("controlActive", Flags).SetValue(player, true);
        player.GetType().GetField("ownsMovement", Flags).SetValue(player, true);
        var handover = player.GetType().GetMethod("SetFoodLocomotion");
        handover.Invoke(player, new object[] { provider, mediator, true });
        Assert.That(provider.enabled, Is.False, "No handover while the Ladybird is driving");
        player.GetType().GetMethod("SetHabitatStopped").Invoke(player, new object[] { true });
        handover.Invoke(player, new object[] { provider, mediator, true });
        player.GetType().GetMethod("EnforceExclusiveMovement", Flags).Invoke(player, null);
        Assert.That(provider.enabled && mediator.enabled, Is.True);
        handover.Invoke(player, new object[] { provider, mediator, false });
        Assert.That(provider.enabled || mediator.enabled, Is.False);
        player.GetType().GetMethod("EndPlayerControl").Invoke(player, null);
        Assert.That(provider.enabled && mediator.enabled, Is.True, "Restore original selection configuration");
    }
}
