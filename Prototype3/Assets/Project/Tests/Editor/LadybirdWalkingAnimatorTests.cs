using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class LadybirdWalkingAnimatorTests
{
 GameObject owner;Component animator,controller;Transform model;
 readonly List<Transform> original=new List<Transform>();
 readonly List<Matrix4x4> poses=new List<Matrix4x4>();
 MeshRenderer wing, shell;
 Type Runtime(string name)=>Assembly.Load("Assembly-CSharp").GetType(name,true);
 void Set(object target,string field,object value)=>target.GetType().GetField(field,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).SetValue(target,value);
 object Call(string name,params object[] args)=>animator.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).Invoke(animator,args);
 void Animate(Vector3 velocity,int frames){for(int i=0;i<frames;i++)Call("AnimateMotion",velocity,0f,.1f);}
 [SetUp] public void Setup()
 {
  owner=new GameObject("Walking test owner");controller=owner.AddComponent(Runtime("LadybirdPlayerController"));
  model=new GameObject("Visual model").transform;model.SetParent(owner.transform,false);
  var contact=new GameObject("Ground contact").transform;contact.SetParent(owner.transform,false);Set(controller,"groundContact",contact);
  ((TinyWorlds.LadybirdMovementSafety)controller.GetType().GetField("safety").GetValue(controller)).bodyRadius=2f;
  animator=owner.AddComponent(Runtime("LadybirdWalkingAnimator"));Set(animator,"controller",controller);Set(animator,"visualModel",model);
  var legType=Runtime("LadybirdWalkingAnimator").GetNestedType("LegBinding");var array=Array.CreateInstance(legType,6);
  for(int i=0;i<6;i++)
  {
   var leg=Activator.CreateInstance(legType);float sign=(i%2)==0?-1:1;float z=(i/2-1)*.3f;
   Set(leg,"label","Leg "+i);Set(leg,"tripodA",i<3);
   Vector3[] anchors={new Vector3(sign*.1f,.3f,z),new Vector3(sign*.2f,.2f,z),new Vector3(sign*.4f,.25f,z),new Vector3(sign*.5f,.1f,z),new Vector3(sign*.6f,0,z)};
   string[] fields={"root","hip","knee","ankle","foot"};for(int k=0;k<5;k++)Set(leg,fields[k],anchors[k]);
   var meshes=new MeshRenderer[4];
   for(int k=0;k<4;k++)
   {
    var mesh=GameObject.CreatePrimitive(PrimitiveType.Cube);UnityEngine.Object.DestroyImmediate(mesh.GetComponent<Collider>());
    mesh.transform.SetParent(model,false);mesh.transform.localPosition=anchors[k];mesh.transform.localScale=Vector3.one*.03f;
    original.Add(mesh.transform);poses.Add(mesh.transform.localToWorldMatrix);meshes[k]=mesh.GetComponent<MeshRenderer>();
   }
   Set(leg,"segments",meshes);array.SetValue(leg,i);
  }
  foreach(var name in new[]{"LeftWing","shell.L"})
  {
   var body=GameObject.CreatePrimitive(PrimitiveType.Cube);UnityEngine.Object.DestroyImmediate(body.GetComponent<Collider>());
   body.name=name;body.transform.SetParent(model,false);
   if(name=="LeftWing")wing=body.GetComponent<MeshRenderer>();else shell=body.GetComponent<MeshRenderer>();
  }
  Set(animator,"legs",array);Assert.IsTrue((bool)Call("Build"));Call("ResetPose");
 }
 [TearDown] public void Cleanup(){UnityEngine.Object.DestroyImmediate(owner);original.Clear();poses.Clear();}
 [Test] public void RuntimeRigPreservesEveryOriginalMeshTransform()
 {
  Animate(Vector3.forward*.35f,10);
  for(int i=0;i<original.Count;i++)Assert.AreEqual(poses[i],original[i].localToWorldMatrix);
  Assert.AreEqual(Vector3.zero,owner.transform.position);Assert.AreEqual(Vector3.zero,model.localPosition);
 }
 [Test] public void TripodsHaveOpposingSwingAndSupportPhases()
 {
  Animate(Vector3.forward*.35f,1);
  var runtime=(Array)animator.GetType().GetField("runtime",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(animator);
  bool swingA=(bool)runtime.GetValue(0).GetType().GetField("wasSwing").GetValue(runtime.GetValue(0));
  bool swingB=(bool)runtime.GetValue(3).GetType().GetField("wasSwing").GetValue(runtime.GetValue(3));
  Assert.AreNotEqual(swingA,swingB);
  for(int i=0;i<3;i++)Assert.AreEqual(swingA,runtime.GetValue(i).GetType().GetField("wasSwing").GetValue(runtime.GetValue(i)));
 }
 [Test] public void NoActualMovementDoesNotAdvanceGait()
 {
  Animate(Vector3.zero,10);
  Assert.AreEqual(0f,animator.GetType().GetField("phase",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(animator));
  Assert.AreEqual("Standing",animator.GetType().GetProperty("GaitStatus").GetValue(animator));
 }
 [Test] public void WalkingBlendsBackToStandingWhenMotionStops()
 {
  Animate(Vector3.back*.35f,10);Animate(Vector3.zero,40);
  Assert.AreEqual("Standing",animator.GetType().GetProperty("GaitStatus").GetValue(animator));
  Assert.That((float)animator.GetType().GetField("blend",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(animator),Is.LessThan(.001f));
 }
 [Test] public void VisualRigHasNoCollidersAndDisableRestoresOriginalRenderers()
 {
  Animate(Vector3.forward*.35f,5);
  var rig=(GameObject)animator.GetType().GetField("rig",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(animator);
  Assert.AreEqual(0,rig.GetComponentsInChildren<Collider>(true).Length);
  Call("OnDisable");foreach(var mesh in original)Assert.IsTrue(mesh.GetComponent<MeshRenderer>().enabled);
 }
 [Test] public void TurningAnimatesWithoutMovingOwnerOrVisualModel()
 {
  Call("AnimateMotion",Vector3.zero,1f,.1f);
  Assert.AreEqual(Vector3.zero,owner.transform.position);Assert.AreEqual(Quaternion.identity,owner.transform.rotation);
  Assert.AreEqual(Vector3.zero,model.localPosition);
  Assert.AreEqual("Turning tripod gait",animator.GetType().GetProperty("GaitStatus").GetValue(animator));
 }
 [Test] public void ResetClearsPhaseAndRestoresStandingBlend()
 {
  Animate(Vector3.forward*.35f,5);Call("ResetPose");
  Assert.AreEqual(0f,animator.GetType().GetField("phase",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(animator));
  Assert.AreEqual(0f,animator.GetType().GetField("blend",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(animator));
 }
 [TestCase(0f)] [TestCase(.01f)] public void WingSuppressionSurvivesGaitAndRestoresRepeatedly(float bob)
 {
  Set(animator,"bodyBobHeight",bob);
  for(int cycle=0;cycle<3;cycle++)
  {
   Call("SuppressRenderer",wing,true,true);Animate(Vector3.forward*.35f,3);
   Assert.IsFalse(wing.enabled);
   var body=(IEnumerable)animator.GetType().GetField("bodyCopies",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(animator);
   foreach(var copy in body)
   {
    var source=(MeshRenderer)copy.GetType().GetField("source").GetValue(copy);
    var visual=(MeshRenderer)copy.GetType().GetField("copy").GetValue(copy);
    if(source==wing)Assert.IsFalse(visual.enabled);
    if(source==shell)Assert.IsTrue(bob>0f?visual.enabled:source.enabled);
   }
   Call("Show",false);Call("SuppressRenderer",wing,false,true);Assert.IsTrue(wing.enabled);
  }
 }
 [Test] public void PreviouslyHiddenWingStaysHiddenAfterRestore()
 {
  Call("SuppressRenderer",wing,true,false);Animate(Vector3.forward*.35f,3);
  Call("SuppressRenderer",wing,false,false);Call("Show",false);Assert.IsFalse(wing.enabled);
 }
 [Test] public void MovementLockRejectsDirectWingAndShellRequestsWithoutStartingAnimation()
 {
  var wings=owner.AddComponent(Runtime("WingInteraction"));var shells=owner.AddComponent(Runtime("ElytraInteraction"));
  foreach(var component in new[]{wings,shells})component.GetType().GetProperty("MovementInteractionLocked").SetValue(component,true);
  foreach(var method in new[]{"OpenWings","FlapWings"})wings.GetType().GetMethod(method).Invoke(wings,null);
  shells.GetType().GetMethod("ToggleElytra").Invoke(shells,null);
  Assert.IsFalse((bool)wings.GetType().GetProperty("IsAnimating").GetValue(wings));
  Assert.IsFalse((bool)shells.GetType().GetProperty("IsAnimating").GetValue(shells));
  Assert.IsFalse((bool)shells.GetType().GetProperty("LastInteractionAccepted").GetValue(shells));
 }
 [Test] public void MovementPostureRestoresMixedWingStatesAndLocksAcrossRepeatedEntries()
 {
  var wings=owner.AddComponent(Runtime("WingInteraction"));var shells=owner.AddComponent(Runtime("ElytraInteraction"));
  var helper=owner.AddComponent(Runtime("LadybirdMovementWingVisibility"));
  Set(helper,"wings",wings);Set(helper,"elytra",shells);Set(helper,"walking",animator);
  var second=GameObject.CreatePrimitive(PrimitiveType.Cube);second.transform.SetParent(model,false);
  var hidden=second.GetComponent<MeshRenderer>();hidden.enabled=false;
  Set(helper,"flightWingRenderers",new[]{wing,hidden});
  for(int i=0;i<3;i++)
  {
   var prepare=(IEnumerator)helper.GetType().GetMethod("Prepare").Invoke(helper,null);
   Assert.IsFalse(prepare.MoveNext()); // Closed, idle pose requires no animation wait.
   Animate(Vector3.forward*.35f,2);Assert.IsFalse(wing.enabled);Assert.IsFalse(hidden.enabled);
   Assert.IsTrue((bool)wings.GetType().GetProperty("MovementInteractionLocked").GetValue(wings));
   helper.GetType().GetMethod("Restore").Invoke(helper,null);Call("Show",false);
   Assert.IsTrue(wing.enabled);Assert.IsFalse(hidden.enabled);
   Assert.IsFalse((bool)wings.GetType().GetProperty("MovementInteractionLocked").GetValue(wings));
  }
 }
 [Test] public void HalfMetrePerSecondUsesTunedCadenceAndSettlesWithoutMovingOriginalMeshes()
 {
  Set(controller,"movementSpeed",.5f);Set(animator,"referenceSpeed",.5f);
  Set(animator,"gaitFrequency",2.5f);Set(animator,"strideAmplitude",.08f);
  Animate(Vector3.forward*.5f,1);
  Assert.That((float)animator.GetType().GetField("phase",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(animator),Is.EqualTo(.25f).Within(.0001f));
  Animate(Vector3.back*.5f,15);Animate(Vector3.zero,40);
  Assert.AreEqual("Standing",animator.GetType().GetProperty("GaitStatus").GetValue(animator));
  for(int i=0;i<original.Count;i++)Assert.AreEqual(poses[i],original[i].localToWorldMatrix);
  Assert.AreEqual(Vector3.zero,owner.transform.position);
 }
}
