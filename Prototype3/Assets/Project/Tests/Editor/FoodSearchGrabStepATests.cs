using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class FoodSearchGrabStepATests
{
    Type Runtime(string name) => Assembly.Load("Assembly-CSharp").GetType(name, true);
    Type XR(string name) => Assembly.Load("Unity.XR.Interaction.Toolkit").GetType("UnityEngine.XR.Interaction.Toolkit." + name, true);
    void Set(object o, string name, object value) => o.GetType().GetField(name, BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance).SetValue(o,value);
    object Call(object o, string name, params object[] args) => o.GetType().GetMethod(name, BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance).Invoke(o,args);
    [Test] public void GrabTargetRejectsMouseSelection()
    {
        var root = new GameObject("Step A fixture"); root.SetActive(false);
        try
        {
            var activity = root.AddComponent(Runtime("FoodSearchActivity"));
            var insect = new GameObject("Aphid"); insect.transform.SetParent(root.transform);
            insect.AddComponent(XR("Interactables.XRGrabInteractable"));
            var target = insect.AddComponent(Runtime("FoodSearchTarget")); Set(target,"activity",activity);
            var array = Array.CreateInstance(Runtime("FoodSearchTarget"),1); array.SetValue(target,0); Set(activity,"targets",array);
            Call(activity,"BeginSearch"); Call(target,"SelectTarget");
            Assert.AreEqual(0, activity.GetType().GetProperty("FoundCount").GetValue(activity));
            Assert.AreEqual(true, target.GetType().GetProperty("RequiresGrab").GetValue(target));
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }
    [Test] public void HabitatHandoverReleasesAndRestoresPrivateDriveAction()
    {
        var root = new GameObject("Handover fixture"); root.SetActive(false);
        var inputType = Assembly.Load("Unity.InputSystem").GetType("UnityEngine.InputSystem.InputAction",true);
        object action = null;
        try
        {
            var controller = root.AddComponent(Runtime("LadybirdPlayerController"));
            Set(controller,"habitatControllerHandover",true); Set(controller,"controlActive",true);
            var simulator = root.AddComponent(XR("Inputs.Simulation.XRInteractionSimulator")) as Behaviour;
            var mediator = root.AddComponent(XR("Locomotion.LocomotionMediator")) as Behaviour;
            var states = (System.Collections.IDictionary)controller.GetType().GetField("suspended",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(controller);
            states.Add(simulator,true); states.Add(mediator,true);
            simulator.enabled = false; mediator.enabled = false; Set(controller,"ownsMovement",true);
            Call(controller,"CreateDriveAction");
            action = controller.GetType().GetField("driveAction",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(controller);
            Assert.AreEqual(true,inputType.GetProperty("enabled").GetValue(action));
            Call(controller,"SetHabitatStopped",true);
            Assert.AreEqual(false,inputType.GetProperty("enabled").GetValue(action));
            Call(controller,"EnforceExclusiveMovement");
            Assert.IsTrue(simulator.enabled); Assert.IsFalse(mediator.enabled);
            Call(controller,"SetHabitatStopped",false);
            Assert.AreEqual(true,inputType.GetProperty("enabled").GetValue(action));
            Assert.IsFalse(simulator.enabled); Assert.IsFalse(mediator.enabled);
            Call(controller,"EndPlayerControl"); action = null;
            Assert.IsTrue(simulator.enabled); Assert.IsTrue(mediator.enabled);
        }
        finally { if(action is IDisposable disposable) disposable.Dispose(); UnityEngine.Object.DestroyImmediate(root); }
    }
}
