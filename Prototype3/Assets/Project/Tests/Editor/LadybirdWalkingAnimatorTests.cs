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
}
