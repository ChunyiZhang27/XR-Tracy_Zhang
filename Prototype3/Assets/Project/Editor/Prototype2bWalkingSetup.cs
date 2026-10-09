using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Explicit setup in the test scene only. Does not create or reparent imported mesh transforms.</summary>
public static class Prototype2bWalkingSetup
{
 private const string GeometryMapJson = @"{""legs"": [{""label"": ""Left Front"", ""tripodA"": true, ""meshNames"": [""legs.012"", ""legs.011"", ""legs.010"", ""legs.026""], ""root"": {""x"": -0.0024320154916495085, ""y"": 0.015928308479487896, ""z"": 0.018758886493742466}, ""hip"": {""x"": -0.004716758072997133, ""y"": 0.01183196265871326, ""z"": 0.02234894409775734}, ""knee"": {""x"": -0.01568204474945863, ""y"": 0.012227685889229178, ""z"": 0.02308019467939933}, ""ankle"": {""x"": -0.020202275986472767, ""y"": 0.004497006069868803, ""z"": 0.023870573999981087}, ""foot"": {""x"": -0.03184954263269901, ""y"": -4.6290917756171744e-05, ""z"": 0.024899902598311503}}, {""label"": ""Right Middle"", ""tripodA"": true, ""meshNames"": [""legs.017"", ""legs.021"", ""legs.019"", ""legs.022""], ""root"": {""x"": 0.0027388724265620112, ""y"": 0.016436031088232994, ""z"": 0.011147536337375641}, ""hip"": {""x"": 0.007014658224458496, ""y"": 0.01159684748078386, ""z"": 0.014025303535163403}, ""knee"": {""x"": 0.02075781246336798, ""y"": 0.013963646333043775, ""z"": 0.016840367888410885}, ""ankle"": {""x"": 0.02521296396541099, ""y"": 0.004505941896544148, ""z"": 0.018665602430701256}, ""foot"": {""x"": 0.04005980119109154, ""y"": -0.00019435952466058856, ""z"": 0.01808366070811947}}, {""label"": ""Left Rear"", ""tripodA"": true, ""meshNames"": [""legs.003"", ""legs.002"", ""legs.001"", ""legs.004""], ""root"": {""x"": -0.007503453642129898, ""y"": 0.012292393948882818, ""z"": -0.00372649566270411}, ""hip"": {""x"": -0.010226638211558262, ""y"": 0.006405913115789493, ""z"": -0.0078070076027264195}, ""knee"": {""x"": -0.02538698973755042, ""y"": 0.010438464193915328, ""z"": -0.010059433368345102}, ""ankle"": {""x"": -0.026890396140515804, ""y"": 0.008053869203043481, ""z"": -0.02187852282077074}, ""foot"": {""x"": -0.021179188663760822, ""y"": -0.0004902638465864584, ""z"": -0.03590362425893545}}, {""label"": ""Right Front"", ""tripodA"": false, ""meshNames"": [""legs.023"", ""legs.025"", ""legs.024"", ""legs.006""], ""root"": {""x"": 0.002425048965960741, ""y"": 0.01593007892370224, ""z"": 0.018761307932436466}, ""hip"": {""x"": 0.004709483473561704, ""y"": 0.011833053237448135, ""z"": 0.022349985005954903}, ""knee"": {""x"": 0.015675227157771587, ""y"": 0.012228697771206498, ""z"": 0.02308217001458009}, ""ankle"": {""x"": 0.02019551924119393, ""y"": 0.004498186055570841, ""z"": 0.023873311777909596}, ""foot"": {""x"": 0.0318420867746075, ""y"": -4.5550090362667106e-05, ""z"": 0.024901093759884436}}, {""label"": ""Left Middle"", ""tripodA"": false, ""meshNames"": [""legs.015"", ""legs.014"", ""legs.013"", ""legs.016""], ""root"": {""x"": -0.0027451261412352324, ""y"": 0.016432985663414, ""z"": 0.011144510004669428}, ""hip"": {""x"": -0.007021449428672592, ""y"": 0.011594950376699368, ""z"": 0.014024702987323204}, ""knee"": {""x"": -0.020764554385095835, ""y"": 0.013962583344740173, ""z"": 0.0168391780462116}, ""ankle"": {""x"": -0.025218134202683967, ""y"": 0.004504757487059881, ""z"": 0.01864866338049372}, ""foot"": {""x"": -0.04006710276007652, ""y"": -0.00019515333769959398, ""z"": 0.018081756308674812}}, {""label"": ""Right Rear"", ""tripodA"": false, ""meshNames"": [""legs.009"", ""legs.005"", ""legs.007"", ""legs.008""], ""root"": {""x"": 0.007503793342038989, ""y"": 0.01229307847097516, ""z"": -0.0037189368158578873}, ""hip"": {""x"": 0.010223142026613155, ""y"": 0.006405346328392625, ""z"": -0.0078036227108289795}, ""knee"": {""x"": 0.025376414104054373, ""y"": 0.010441225177297989, ""z"": -0.010047134982111553}, ""ankle"": {""x"": 0.026893924145648878, ""y"": 0.0080477709028249, ""z"": -0.021877142678325374}, ""foot"": {""x"": 0.021180979597071808, ""y"": -0.0004976288764737546, ""z"": -0.035903387082119785}}]}";
 [Serializable] public class Map {public Leg[] legs;}
 [Serializable] public class Leg {public string label;public bool tripodA;public string[] meshNames;public Vector3 root,hip,knee,ankle,foot;}
 public static void VerifyBatch()
 {
  if(!Application.isBatchMode)throw new InvalidOperationException("Batch only.");
  var scene=EditorSceneManager.OpenScene("Assets/Project/Scenes/Prototype2b.unity",OpenSceneMode.Single);
  var explore=scene.GetRootGameObjects().Single(x=>x.name=="InsectExploreZone");explore.SetActive(true);
  var animator=explore.GetComponentInChildren<LadybirdWalkingAnimator>(true);
  if(!animator.ValidateBindings(out string reason))throw new InvalidOperationException(reason);
  if(animator.visualModel.GetComponentsInChildren<Animator>(true).Length!=0 || animator.visualModel.GetComponentsInChildren<Animation>(true).Length!=0)
   throw new InvalidOperationException("An authored animation component may conflict.");
  var original=animator.legs.SelectMany(x=>x.segments).Select(x=>new {mesh=x,position=x.transform.localPosition,rotation=x.transform.localRotation,scale=x.transform.localScale,parent=x.transform.parent}).ToArray();
  var controller=animator.controller;
  foreach(var root in scene.GetRootGameObjects())if(root.name=="SelectionZone" || root.name=="OnboardingZone")root.SetActive(false);
  Physics.SyncTransforms();
  if(!controller.safety.TryFindStart(controller.transform,controller.groundSurfaces,controller.startingPoint.position,controller.startingPoint.position,out var safeStart,out reason))throw new InvalidOperationException(reason);
  controller.transform.position+=safeStart-controller.groundContact.position;
  var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
  Func<string,System.Reflection.MethodInfo> method=name=>animator.GetType().GetMethod(name,flags);
  if(!(bool)method("Build").Invoke(animator,null))throw new InvalidOperationException("Rig build failed.");
  method("ResetPose").Invoke(animator,null);
  var heading=controller.facingReference.forward;heading.y=0;heading.Normalize();
  for(int i=0;i<20;i++)
  {
   controller.transform.position+=heading*.035f;
   method("AnimateMotion").Invoke(animator,new object[]{heading*.35f,0f,.1f});
  }
  foreach(var item in original)
   if(item.mesh.transform.localPosition!=item.position || item.mesh.transform.localRotation!=item.rotation || item.mesh.transform.localScale!=item.scale || item.mesh.transform.parent!=item.parent)
    throw new InvalidOperationException("An imported mesh transform was changed.");
  var rig=animator.visualModel.Find("LadybirdWalkingVisuals_Runtime");
  if(rig.GetComponentsInChildren<Collider>(true).Length!=0)throw new InvalidOperationException("Visual rig cannot contain colliders.");
  method("OnDisable").Invoke(animator,null);
  Debug.Log("Actual-model Edit Mode verification: six legs/24 meshes valid, 20 deterministic gait steps evaluated, original local transforms and parents preserved, no authored animation conflict, no visual rig colliders. No scene saved and no Play Mode claim.");
 }
 public static void ConfigureBatch()
 {
  if(!Application.isBatchMode)throw new InvalidOperationException("Batch only.");
  var scene=EditorSceneManager.OpenScene("Assets/Project/Scenes/Prototype2b.unity",OpenSceneMode.Single);
  var owner=scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<LadybirdPlayerController>(true)).Single();
  var model=owner.transform.Find("LadybirdExploreModel_New");
  var animator=owner.GetComponent<LadybirdWalkingAnimator>();
  if(animator!=null)throw new InvalidOperationException("Walking animator already configured; inspect before overwriting.");
  var map=JsonUtility.FromJson<Map>(GeometryMapJson);
  animator=owner.gameObject.AddComponent<LadybirdWalkingAnimator>();animator.controller=owner;animator.visualModel=model;
  animator.legs=map.legs.Select(b=>new LadybirdWalkingAnimator.LegBinding{label=b.label,tripodA=b.tripodA,
   root=b.root,hip=b.hip,knee=b.knee,ankle=b.ankle,foot=b.foot,
   segments=b.meshNames.Select(name=>model.GetComponentsInChildren<MeshRenderer>(true).Single(x=>x.name==name)).ToArray()}).ToArray();
  if(!animator.ValidateBindings(out string reason))throw new InvalidOperationException(reason);
  animator.bodyBobHeight=0f;
  EditorUtility.SetDirty(animator);EditorSceneManager.MarkSceneDirty(scene);
  if(!EditorSceneManager.SaveScene(scene))throw new InvalidOperationException("Save failed.");
  Debug.Log("Prototype2b six-leg gait bound to 24 unique nonempty meshes. Imported transforms unchanged; visual controls are runtime-only. Bob disabled by default.");
 }
}
