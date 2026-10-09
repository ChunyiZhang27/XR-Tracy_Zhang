using System.Reflection;
using NUnit.Framework;
using TinyWorlds;
using UnityEngine;

/// <summary>Deterministic controller calls in Edit Mode; no VR or Play Mode claim.</summary>
public class LadybirdBoundaryRecoveryTests
{
    GameObject owner, surface;
    Component controller;
    LadybirdMovementSafety safety;
    Vector3 start=new Vector3(6000,100,6000);
    void Set(string field,object value) => controller.GetType().GetField(field,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).SetValue(controller,value);
    void Move(Vector2 input,int frames)
    { for(int i=0;i<frames;i++) controller.GetType().GetMethod("SimulateMovement").Invoke(controller,new object[]{input,.1f}); }
    [SetUp] public void Setup()
    {
        surface=new GameObject("Test ground"); surface.transform.position=start-Vector3.up*.1f;
        var collider=surface.AddComponent<BoxCollider>();collider.size=new Vector3(8,.2f,8);
        owner=new GameObject("Test controlled parent"); owner.transform.position=start;
        var contact=new GameObject("Contact").transform; contact.SetParent(owner.transform,false);
        var facing=new GameObject("Facing").transform; facing.SetParent(owner.transform,false);
        controller=owner.AddComponent(Assembly.Load("Assembly-CSharp").GetType("LadybirdPlayerController",true));
        safety=new LadybirdMovementSafety {bodyRadius=.1f,explorationRadius=1.1f};
        Set("safety",safety);Set("groundContact",contact);Set("facingReference",facing);Set("groundSurfaces",new Collider[]{collider});
        Set("boundaryCenter",start);Set("controlActive",true);Set("movementSpeed",.35f);Set("acceleration",.7f);Set("deceleration",1.4f);
        Set("initialized",true);Set("initialParentPosition",start);Set("initialParentRotation",Quaternion.identity);Set("editorTest",true);
        Physics.SyncTransforms();
    }
    [TearDown] public void Cleanup() { Object.DestroyImmediate(owner);Object.DestroyImmediate(surface);Physics.SyncTransforms(); }
    [TestCase(false)][TestCase(true)] public void RepeatedBoundaryBlocksStillAllowTurningAndReversing(bool rectangular)
    {
        if(rectangular){safety.boundaryShape=LadybirdMovementSafety.BoundaryShape.WorldRectangle;safety.boundaryHalfExtents=new Vector2(2f,1.1f);}
        Move(Vector2.up,100);var blockedPosition=owner.transform.position;
        Assert.That(blockedPosition.z-start.z,Is.GreaterThan(.9f).And.LessThanOrEqualTo(1.001f));
        var rotation=owner.transform.rotation; Move(Vector2.right,5);
        Assert.That(Quaternion.Angle(rotation,owner.transform.rotation),Is.GreaterThan(20f));
        Move(Vector2.down,10);
        Assert.That(owner.transform.position.z,Is.LessThan(blockedPosition.z-.1f));
    }
    [Test] public void ResetReturnsToVerifiedStartAfterBoundaryTurnAndReverse()
    {
        safety.boundaryShape=LadybirdMovementSafety.BoundaryShape.WorldRectangle;safety.boundaryHalfExtents=new Vector2(2f,1.1f);
        Move(Vector2.up,100);Move(Vector2.right,5);Move(Vector2.down,10);
        Assert.IsTrue((bool)controller.GetType().GetMethod("ResetToStartingPoint").Invoke(controller,null));
        Assert.That(Vector3.Distance(start,owner.transform.position),Is.LessThan(.001f));
        Assert.That(Quaternion.Angle(Quaternion.identity,owner.transform.rotation),Is.LessThan(.001f));
    }
}
