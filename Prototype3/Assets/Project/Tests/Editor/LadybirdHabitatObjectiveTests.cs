using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class LadybirdHabitatObjectiveTests
{
    GameObject fixture, owner, panel, button, marker;
    Transform target;
    Component controller, objective;
    Component text;
    string TextValue => (string)text.GetType().GetProperty("text").GetValue(text);
    Vector3 start=new Vector3(8000,100,8000);
    Type Runtime(string name)=>Assembly.Load("Assembly-CSharp").GetType(name,true);
    void Set(object obj,string field,object value)=>obj.GetType().GetField(field,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance).SetValue(obj,value);
    object Call(object obj,string name,params object[] args)=>obj.GetType().GetMethod(name,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance).Invoke(obj,args);
    bool Property(object obj,string name)=>(bool)obj.GetType().GetProperty(name).GetValue(obj);
    void Evaluate()=>Call(objective,"Evaluate");
    GameObject Object(string name){var go=new GameObject(name);go.transform.SetParent(fixture.transform,false);return go;}
    void Arrive(){owner.transform.position=target.position;Physics.SyncTransforms();Evaluate();}
    [SetUp] public void Setup()
    {
        fixture=new GameObject("Habitat fixture");owner=Object("Controlled Ladybird");owner.transform.position=start;
        var ground=Object("Walkable surface");ground.transform.position=start-Vector3.up*.1f;
        var surface=ground.AddComponent<BoxCollider>();surface.size=new Vector3(10,.2f,10);
        var contact=Object("GroundContact").transform;contact.SetParent(owner.transform,false);
        var facing=Object("Facing").transform;facing.SetParent(owner.transform,false);
        var startingPoint=Object("Start").transform;startingPoint.position=start;
        target=Object("HabitatTarget").transform;target.position=start+Vector3.forward*1.5f;
        controller=owner.AddComponent(Runtime("LadybirdPlayerController"));
        Set(controller,"groundContact",contact);Set(controller,"facingReference",facing);Set(controller,"startingPoint",startingPoint);
        Set(controller,"groundSurfaces",new Collider[]{surface});
        Set(controller,"safety",new TinyWorlds.LadybirdMovementSafety{bodyRadius=.1f,explorationRadius=5f});
        Set(controller,"controlActive",true);Set(controller,"editorTest",true);Set(controller,"initialized",true);
        Set(controller,"initialParentPosition",start);Set(controller,"initialParentRotation",Quaternion.identity);Set(controller,"boundaryCenter",start);
        panel=Object("Objective panel");button=Object("Continue");marker=Object("Destination marker");
        text=new GameObject("Text",typeof(RectTransform)).AddComponent(Assembly.Load("Unity.TextMeshPro").GetType("TMPro.TextMeshProUGUI",true));text.transform.SetParent(panel.transform,false);
        objective=owner.AddComponent(Runtime("LadybirdHabitatObjective"));Set(objective,"controller",controller);Set(objective,"habitatTarget",target);
        Set(objective,"enableInEditorMovementTest",true);Set(objective,"objectivePanel",panel);Set(objective,"continueButton",button);Set(objective,"destinationMarker",marker);Set(objective,"objectiveText",text);
        Physics.SyncTransforms();
    }
    [TearDown] public void Cleanup(){UnityEngine.Object.DestroyImmediate(fixture);Physics.SyncTransforms();}
    [Test] public void ObjectiveRequiresActiveEnvironmentOrExplicitEditorOptIn()
    {
        Set(controller,"controlActive",false);Evaluate();Assert.IsFalse(panel.activeSelf);Assert.IsFalse(Property(objective,"Arrived"));
        Set(controller,"controlActive",true);Set(objective,"enableInEditorMovementTest",false);Evaluate();Assert.IsFalse(panel.activeSelf);
        Set(objective,"enableInEditorMovementTest",true);Evaluate();Assert.IsTrue(panel.activeSelf);Assert.IsFalse(button.activeSelf);Assert.IsTrue(marker.activeSelf);
        Assert.IsFalse(Property(controller,"HabitatStopped"));
    }
    [Test] public void HandsAndCameraAtDestinationCannotTriggerArrival()
    {
        var camera=Object("VR camera");camera.transform.position=target.position;var hand=Object("Hand");hand.transform.position=target.position;
        Evaluate();Assert.IsFalse(Property(objective,"Arrived"));Assert.IsFalse(Property(controller,"HabitatStopped"));
    }
    [Test] public void ArrivalStopsDriveAndTurnWhileEnvironmentRemainsOwned()
    {
        Evaluate();Arrive();Assert.IsTrue(Property(objective,"Arrived"));Assert.IsTrue(Property(controller,"HabitatStopped"));
        Assert.IsTrue(Property(controller,"ControlActive"));Assert.IsTrue(button.activeSelf);Assert.IsFalse(marker.activeSelf);
        Assert.AreEqual("Habitat Found! You've reached a place where ladybirds can search for food.",TextValue);
        var pose=owner.transform.localToWorldMatrix;
        for(int i=0;i<20;i++)Call(controller,"SimulateMovement",Vector2.one,.1f);
        Assert.AreEqual(pose,owner.transform.localToWorldMatrix);
    }
    [Test] public void ResetRearmsAttemptAndExitClearsHoldWithoutProgressWrites()
    {
        Evaluate();Arrive();Assert.IsTrue((bool)Call(controller,"ResetToStartingPoint"));Evaluate();
        Assert.IsFalse(Property(objective,"Arrived"));Assert.IsFalse(Property(controller,"HabitatStopped"));Assert.IsFalse(button.activeSelf);Assert.IsTrue(marker.activeSelf);
        Arrive();Call(controller,"EndPlayerControl");Evaluate();Assert.IsFalse(panel.activeSelf);Assert.IsFalse(Property(controller,"HabitatStopped"));
        Set(controller,"controlActive",true);owner.transform.position=start;Evaluate();Assert.IsFalse(Property(objective,"Arrived"));
    }
    [Test] public void RepeatedArrivalAndContinueRequestsAreIdempotent()
    {
        Evaluate();Call(objective,"ContinueToFoodDiscovery");Assert.AreNotEqual("Let's discover what ladybirds eat!",TextValue);
        Arrive();for(int i=0;i<10;i++)Evaluate();
        for(int i=0;i<3;i++)Call(objective,"ContinueToFoodDiscovery");
        Assert.AreEqual("Let's discover what ladybirds eat!",TextValue);Assert.IsTrue(Property(controller,"HabitatStopped"));
        Call(objective,"OnDisable");Assert.IsFalse(Property(controller,"HabitatStopped"));Assert.IsFalse(panel.activeSelf);
    }
    [Test] public void ArrivalCardIsOneShotAndContinueEmitsOncePerAttempt()
    {
        var card=Object("Arrival card");card.SetActive(false);button.transform.SetParent(card.transform,false);
        Set(objective,"arrivalCard",card);
        var click=button.AddComponent(Assembly.Load("UnityEngine.UI").GetType("UnityEngine.UI.Button",true));
        var onClick=(UnityEngine.Events.UnityEvent)click.GetType().GetProperty("onClick").GetValue(click);
        onClick.AddListener(()=>Call(objective,"ContinueToFoodDiscovery"));
        int events=0;
        ((UnityEngine.Events.UnityEvent)objective.GetType().GetField("foodDiscoveryStarted").GetValue(objective)).AddListener(()=>events++);
        Evaluate();Assert.IsFalse(card.activeSelf);onClick.Invoke();Assert.AreEqual(0,events);
        Arrive();Assert.IsTrue(card.activeSelf);Assert.IsFalse(panel.activeSelf);
        onClick.Invoke();onClick.Invoke();Evaluate();
        Assert.AreEqual(1,events);Assert.IsTrue(Property(objective,"FoodDiscoveryStarted"));
        Assert.IsFalse(card.activeSelf);Assert.IsFalse(button.activeSelf);Assert.IsTrue(panel.activeSelf);
        Assert.AreEqual("Let's discover what ladybirds eat!",TextValue);Assert.IsTrue(Property(controller,"HabitatStopped"));
        Call(controller,"ResetToStartingPoint");Evaluate();
        Assert.IsFalse(card.activeSelf);Assert.IsFalse(Property(objective,"FoodDiscoveryStarted"));
        Arrive();Assert.IsTrue(card.activeSelf);onClick.Invoke();Assert.AreEqual(2,events);
        Call(controller,"EndPlayerControl");Evaluate();
        Assert.IsFalse(card.activeSelf);Assert.IsFalse(panel.activeSelf);
    }
    [Test] public void ArrivalAwakeAddsMouseRaycasterWithoutReplacingXRRaycasterOrCallback()
    {
        var canvasObject=new GameObject("Arrival canvas",typeof(RectTransform),typeof(Canvas));
        canvasObject.transform.SetParent(fixture.transform,false);
        canvasObject.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;
        var trackedType=Assembly.Load("Unity.XR.Interaction.Toolkit").GetType("UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster",true);
        var tracked=canvasObject.AddComponent(trackedType);
        var card=Object("Arrival card");card.transform.SetParent(canvasObject.transform,false);
        button.transform.SetParent(card.transform,false);Set(objective,"arrivalCard",card);
        var buttonType=Assembly.Load("UnityEngine.UI").GetType("UnityEngine.UI.Button",true);
        var click=button.AddComponent(buttonType);
        var onClick=(UnityEngine.Events.UnityEvent)buttonType.GetProperty("onClick").GetValue(click);
        onClick.AddListener(()=>Call(objective,"ContinueToFoodDiscovery"));
        var graphicType=Assembly.Load("UnityEngine.UI").GetType("UnityEngine.UI.GraphicRaycaster",true);
        Assert.IsNull(canvasObject.GetComponent(graphicType));
        Call(objective,"Awake");
        Assert.IsNotNull(canvasObject.GetComponent(graphicType));Assert.AreSame(tracked,canvasObject.GetComponent(trackedType));
        Assert.IsFalse(card.activeSelf);
        Evaluate();Arrive();onClick.Invoke();
        Assert.IsTrue(Property(objective,"FoodDiscoveryStarted"));Assert.IsFalse(card.activeSelf);
        Assert.AreEqual("Let's discover what ladybirds eat!",TextValue);Assert.IsTrue(Property(controller,"HabitatStopped"));
    }
    [Test] public void ReturningToBodyHidesUnclickedArrivalCard()
    {
        var card=Object("Arrival card");card.SetActive(false);Set(objective,"arrivalCard",card);
        Set(controller,"editorTest",false);
        var transition=owner.AddComponent(Runtime("LadybirdEnvironmentTransition"));Set(objective,"transition",transition);
        var modeType=Runtime("LadybirdEnvironmentTransition").GetNestedType("Mode");
        Set(transition,"<CurrentMode>k__BackingField",Enum.Parse(modeType,"Environment"));
        Evaluate();Arrive();Assert.IsTrue(card.activeSelf);
        Set(transition,"<CurrentMode>k__BackingField",Enum.Parse(modeType,"Body"));Evaluate();
        Assert.IsFalse(card.activeSelf);Assert.IsFalse(Property(controller,"HabitatStopped"));
    }
    [Test] public void UnsupportedOrObstructedDestinationCannotComplete()
    {
        target.position=start+Vector3.forward*6;Arrive();Assert.IsFalse(Property(objective,"Arrived"));
        target.position=start+Vector3.forward*1.5f;owner.transform.position=target.position;
        var rock=Object("Rock");rock.transform.position=target.position+Vector3.up*.15f;rock.AddComponent<BoxCollider>().size=Vector3.one*.3f;
        Physics.SyncTransforms();Evaluate();Assert.IsFalse(Property(objective,"Arrived"));
    }
    [Test] public void ArrivalUsesWorldGroundContactWithRadiusAndHeightTolerance()
    {
        Assert.IsTrue((bool)Call(objective,"InsideArrivalRegion",target.position+Vector3.right*.44f));
        Assert.IsFalse((bool)Call(objective,"InsideArrivalRegion",target.position+Vector3.right*.46f));
        Assert.IsFalse((bool)Call(objective,"InsideArrivalRegion",target.position+Vector3.up*.2f));
        var parent=Object("Translated rotated habitat parent").transform;parent.position=start+Vector3.right*2;parent.rotation=Quaternion.Euler(0,40,0);
        target.SetParent(parent,true);Assert.IsTrue((bool)Call(objective,"InsideArrivalRegion",target.position));
        Evaluate();Assert.IsFalse(Property(objective,"Arrived"));
    }
    [Test] public void NormalTransitionOnlyRunsObjectiveAfterExplicitEnvironmentEntry()
    {
        Set(controller,"editorTest",false);
        var transition=owner.AddComponent(Runtime("LadybirdEnvironmentTransition"));Set(objective,"transition",transition);
        var modeType=Runtime("LadybirdEnvironmentTransition").GetNestedType("Mode");
        foreach(var mode in new[]{"Body","Entering"})
        {
            Set(transition,"<CurrentMode>k__BackingField",Enum.Parse(modeType,mode));Evaluate();Assert.IsFalse(panel.activeSelf);
        }
        Set(transition,"<CurrentMode>k__BackingField",Enum.Parse(modeType,"Environment"));Evaluate();
        Assert.IsTrue(panel.activeSelf);Assert.IsFalse(Property(controller,"HabitatStopped"));
        Arrive();Assert.IsTrue(Property(controller,"HabitatStopped"));
        Set(transition,"<CurrentMode>k__BackingField",Enum.Parse(modeType,"Returning"));Evaluate();
        Assert.IsFalse(panel.activeSelf);Assert.IsFalse(Property(controller,"HabitatStopped"));
    }
}
