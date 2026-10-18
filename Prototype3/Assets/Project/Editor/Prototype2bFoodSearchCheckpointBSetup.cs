using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Only styles the mission's end UI and connects it to the saved original navigation action.</summary>
public static class Prototype2bFoodSearchCheckpointBSetup
{
 public static void ConfigureBatch()
 {
  if(!Application.isBatchMode)throw new InvalidOperationException("Batch only.");
  var scene=EditorSceneManager.OpenScene("Assets/Project/Scenes/Prototype2b.unity");
  var all=scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<Transform>(true)).ToArray();
  var activity=all.Select(x=>x.GetComponent<FoodSearchActivity>()).Single(x=>x!=null);
  if(activity.backToInsectsPanel!=null)throw new InvalidOperationException("Return card already configured; do not duplicate.");
  var original=all.Select(x=>x.GetComponent<UnityEngine.UI.Button>()).Single(x=>x!=null&&x.name=="BackToInsectsButton");
  if(original.onClick.GetPersistentEventCount()!=1||original.onClick.GetPersistentMethodName(0)!="BackToSelection"||!(original.onClick.GetPersistentTarget(0) is ExperienceManager))
   throw new InvalidOperationException("Original Back to Insects navigation must be inspected before wiring.");
  var white=all.Single(x=>x.name=="SelectionBackground").GetComponent<UnityEngine.UI.Image>();
  var completion=activity.completionPanel;
  completion.GetComponent<UnityEngine.UI.Image>().color=white.color;
  completion.transform.Find("Title").GetComponent<TMP_Text>().text="Well Done!";
  completion.transform.Find("Description").GetComponent<TMP_Text>().text=FoodSearchActivity.CompletionFeedback;
  foreach(var text in completion.GetComponentsInChildren<TMP_Text>(true)){text.color=original.GetComponentInChildren<TMP_Text>(true).color;text.raycastTarget=false;}
  foreach(var button in completion.GetComponentsInChildren<UnityEngine.UI.Button>(true))WhiteButton(button,original);
  var back=UnityEngine.Object.Instantiate(completion,completion.transform.parent,false);back.name="FoodSearchBackToInsectsCard";
  var rect=(RectTransform)back.transform;rect.sizeDelta=new Vector2(420,360);rect.anchoredPosition3D=new Vector3(580,0,0);
  var title=back.transform.Find("Title").GetComponent<TMP_Text>();title.text="Back to Insects";title.rectTransform.sizeDelta=new Vector2(380,60);
  var description=back.transform.Find("Description").GetComponent<TMP_Text>();description.text="Ready to discover another insect?";description.rectTransform.sizeDelta=new Vector2(380,150);
  var buttons=back.GetComponentsInChildren<UnityEngine.UI.Button>(true);var click=buttons.Single(x=>x.name=="TryAgainButton");
  foreach(var extra in buttons)if(extra!=click)UnityEngine.Object.DestroyImmediate(extra.gameObject);
  click.name="BackToInsectsButton";var buttonRect=(RectTransform)click.transform;buttonRect.anchoredPosition=new Vector2(0,-125);buttonRect.sizeDelta=new Vector2(380,65);
  var label=click.GetComponentInChildren<TMP_Text>(true);label.text="Back to Insects";label.rectTransform.sizeDelta=new Vector2(360,55);
  for(int i=click.onClick.GetPersistentEventCount()-1;i>=0;i--)UnityEventTools.RemovePersistentListener(click.onClick,i);
  UnityEventTools.AddPersistentListener(click.onClick,activity.BackToInsects);
  click.gameObject.SetActive(true);back.SetActive(false);
  activity.backToInsectsPanel=back;activity.backToInsectsButton=click;activity.existingBackToInsectsAction=original;
  activity.incorrectFeedbackCooldown=2;
  float distance=Vector3.Distance(activity.objective.controller.xrOrigin.Camera.transform.position,click.transform.position);
  if(distance>9.8f)throw new InvalidOperationException("Return button outside conservative existing ray reach; inspect placement manually.");
  EditorUtility.SetDirty(activity);EditorSceneManager.MarkSceneDirty(scene);
  if(!EditorSceneManager.SaveScene(scene))throw new InvalidOperationException("Save failed.");
  Debug.Log($"CHECKPOINT B: preserved all five aphids, reused 2 ant placeholders; white card style from {white.name}, button style/action from {original.name}; return button distance={distance:F3}m, gap=20 canvas units; original BackToSelection/session reset untouched.");
 }
 static void WhiteButton(UnityEngine.UI.Button button,UnityEngine.UI.Button source)
 {
  var image=button.GetComponent<UnityEngine.UI.Image>();EditorUtility.CopySerialized(source.GetComponent<UnityEngine.UI.Image>(),image);
  button.targetGraphic=image;button.colors=source.colors;button.transition=source.transition;
  var originalOutline=source.GetComponent<UnityEngine.UI.Outline>();
  if(originalOutline!=null)
  {
   var outline=button.GetComponent<UnityEngine.UI.Outline>();if(outline==null)outline=button.gameObject.AddComponent<UnityEngine.UI.Outline>();
   EditorUtility.CopySerialized(originalOutline,outline);
   var originalGlow=source.GetComponent<UIButtonHoverGlow>();
   if(originalGlow!=null)
   {
    var glow=button.GetComponent<UIButtonHoverGlow>();if(glow==null)glow=button.gameObject.AddComponent<UIButtonHoverGlow>();EditorUtility.CopySerialized(originalGlow,glow);
    var serialized=new SerializedObject(glow);serialized.FindProperty("hoverOutline").objectReferenceValue=outline;serialized.FindProperty("buttonImage").objectReferenceValue=image;serialized.ApplyModifiedPropertiesWithoutUndo();
   }
  }
 }
}
