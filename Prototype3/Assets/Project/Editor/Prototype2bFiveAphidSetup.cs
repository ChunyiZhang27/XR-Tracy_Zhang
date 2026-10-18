using System;
using System.Linq;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>Extends the working scene grab target; never edits the imported model or shared prefab assets.</summary>
public static class Prototype2bFiveAphidSetup
{
    const string ScenePath = "Assets/Project/Scenes/Prototype2b.unity";
    public static void ConfigureBatch()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("Batch only.");
        var scene = EditorSceneManager.OpenScene(ScenePath);
        var all = scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<Transform>(true)).ToArray();
        var activity = all.Select(x=>x.GetComponent<FoodSearchActivity>()).Single(x=>x!=null);
        var objective = activity.objective;
        if (activity.completionPanel != null) throw new InvalidOperationException("Mission already configured; inspect rather than duplicating.");
        var reference = activity.targets.Single(x=>x.name=="Aphid_01");
        var referenceGrab = reference.GetComponent<XRGrabInteractable>();
        if (referenceGrab == null || reference.GetComponent<FoodSearchGrabReturn>() == null) throw new InvalidOperationException("Working grab reference missing.");
        var aphids = new List<FoodSearchTarget>{reference};
        // Keep the saved transforms of the existing targets, replacing only their old click-only wrappers.
        foreach (var previous in activity.targets.Where(x=>x.category==FoodSearchTarget.IdentificationCategory.Aphid && x!=reference).ToArray())
        {
            var go = UnityEngine.Object.Instantiate(reference.gameObject,previous.transform.parent,false); go.name=previous.name;
            go.transform.SetPositionAndRotation(previous.transform.position,previous.transform.rotation); go.transform.localScale=previous.transform.localScale;
            var target=go.GetComponent<FoodSearchTarget>(); target.activity=activity; aphids.Add(target);
            UnityEngine.Object.DestroyImmediate(previous.gameObject);
        }
        for(int i=4;i<=5;i++)
        {
            var go=UnityEngine.Object.Instantiate(reference.gameObject,reference.transform.parent,false);go.name=$"Aphid_{i:00}";
            go.GetComponent<FoodSearchTarget>().activity=activity; aphids.Add(go.GetComponent<FoodSearchTarget>());
        }
        var ants = activity.targets.Where(x=>x!=null && x.category!=FoodSearchTarget.IdentificationCategory.Aphid).ToArray();
        foreach(var ant in ants)
        {
            var old = ant.GetComponent<XRSimpleInteractable>();
            if(old!=null) UnityEngine.Object.DestroyImmediate(old);
            var body=ant.GetComponent<Rigidbody>();if(body==null)body=ant.gameObject.AddComponent<Rigidbody>();
            EditorUtility.CopySerialized(reference.GetComponent<Rigidbody>(),body);
            var grab=ant.gameObject.AddComponent<XRGrabInteractable>();EditorUtility.CopySerialized(referenceGrab,grab);
            grab.colliders.Clear();grab.colliders.Add(ant.GetComponent<SphereCollider>());
            ant.gameObject.AddComponent<FoodSearchGrabReturn>();
        }
        activity.targets=aphids.Concat(ants).ToArray(); activity.requiredAphids=5;
        var states=scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<Transform>(true)).ToDictionary(x=>x.gameObject,x=>x.gameObject.activeSelf);
        try
        {
            for(var t=objective.habitatTarget;t!=null;t=t.parent)t.gameObject.SetActive(true);
            // Targets are excluded from placement physics; reserve their centres separately below.
            activity.insectArea.SetActive(false);Physics.SyncTransforms();
            var placed=new List<Vector3>();
            foreach(var target in activity.targets)
            {
                var desired=target.transform.position;
                if(target.name=="Aphid_04")desired=new Vector3(5.4f,desired.y,-1.9f);
                if(target.name=="Aphid_05")desired=new Vector3(6.2f,desired.y,-1.15f);
                var candidates=new List<Vector3>{desired};
                for(int ring=1;ring<=10;ring++)for(int angle=0;angle<24;angle++)
                    candidates.Add(desired+Quaternion.Euler(0,angle*15,0)*Vector3.forward*(ring*.25f));
                // Fallback stays on the Habitat side of the leaf and inside the existing 10m ray range.
                var habitatCandidates=new List<Vector3>();
                for(float x=2.5f;x<=9f;x+=.45f)for(float z=-6f;z<=2f;z+=.45f)
                    habitatCandidates.Add(new Vector3(x,desired.y,z));
                candidates.AddRange(habitatCandidates.OrderBy(p=>(p-desired).sqrMagnitude));
                bool found=false;
                foreach(var candidate in candidates)
                {
                    if(!ValidPosition(objective,target,candidate,placed,out var position))continue;
                    target.transform.position=position;placed.Add(position);found=true;break;
                }
                if(!found)throw new InvalidOperationException("Place insects manually: no verified clear position for "+target.name);
                Debug.Log($"FIVE APHIDS placement {target.name}: {target.transform.position:F4}; distance={Vector3.Distance(objective.controller.xrOrigin.Camera.transform.position,target.transform.position):F2}m");
            }
        }
        finally { foreach(var pair in states)if(pair.Key!=null)pair.Key.SetActive(pair.Value); }
        var search=objective.foodSearchReadyPanel; activity.searchPanel=search;
        search.transform.Find("Description").GetComponent<TMP_Text>().text=FoodSearchActivity.Instruction;
        activity.progressText.text="Aphids caught: 0 / 5";activity.feedbackText.text="";
        // Copy the existing arrival design at the same shared anchor, preserving all existing card transforms.
        var completion=UnityEngine.Object.Instantiate(objective.arrivalCard,objective.arrivalCard.transform.parent,false);
        completion.name="FoodSearchCompletionCard";
        completion.transform.Find("Title").GetComponent<TMP_Text>().text="Well Done!";
        completion.transform.Find("Description").GetComponent<TMP_Text>().text=FoodSearchActivity.CompletionFeedback;
        var retry=completion.GetComponentInChildren<UnityEngine.UI.Button>(true);
        var finish=UnityEngine.Object.Instantiate(retry,completion.transform,false);
        Wire(retry,"TryAgainButton","Try Again",activity.TryAgain,-170,290);
        Wire(finish,"FinishExplorationButton","Finish Exploration",activity.FinishExploration,150,330);
        foreach(var text in completion.GetComponentsInChildren<TMP_Text>(true))text.raycastTarget=false;
        activity.tryAgainButton.SetActive(false); // Retain the previous UI object, without a duplicate replay surface.
        activity.tryAgainButton=retry.gameObject; activity.completionPanel=completion;
        activity.completionTitle=completion.transform.Find("Title").GetComponent<TMP_Text>(); activity.finishExplorationButton=finish;
        completion.SetActive(false);
        EditorUtility.SetDirty(activity);EditorSceneManager.MarkSceneDirty(scene);
        if(!EditorSceneManager.SaveScene(scene))throw new InvalidOperationException("Save failed.");
        Debug.Log("FIVE APHIDS configured: 5 imported visual copies, 2 existing ant placeholders, shared XR manager and completion card anchor.");
    }
    public static void VerifyAndWireFinalStateBatch()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("Batch only.");
        var scene = EditorSceneManager.OpenScene(ScenePath);
        var all = scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<Transform>(true)).ToArray();
        var activity = all.Select(x=>x.GetComponent<FoodSearchActivity>()).Single(x=>x!=null);
        var aphids = activity.targets.Where(x=>x.category==FoodSearchTarget.IdentificationCategory.Aphid).ToArray();
        if (aphids.Length!=5 || activity.requiredAphids!=5 || activity.targets.Length!=7) throw new InvalidOperationException("Expected existing five-aphid mission and two distractors.");
        var reference = aphids.Single(x=>x.name=="Aphid_01").GetComponent<XRGrabInteractable>();
        foreach(var target in activity.targets)
        {
            var grab = target.GetComponent<XRGrabInteractable>();
            var body = target.GetComponent<Rigidbody>();
            if (grab==null || body==null || target.GetComponent<XRSimpleInteractable>()!=null ||
                grab.interactionManager!=reference.interactionManager || grab.interactionLayers.value!=reference.interactionLayers.value ||
                grab.movementType!=reference.movementType || grab.farAttachMode!=reference.farAttachMode ||
                grab.throwOnDetach || body.useGravity || !body.isKinematic || grab.colliders.Count!=1 ||
                target.GetComponent<FoodSearchGrabReturn>()==null || target.activity!=activity)
                throw new InvalidOperationException("Grab configuration mismatch: "+target.name);
            Debug.Log($"CHECKPOINT A existing target {target.name}: category={target.category}, position={target.transform.position:F4}, manager={grab.interactionManager.name}, attach={grab.farAttachMode}, kinematic={body.isKinematic}");
        }
        var card = activity.completionPanel;
        if (card==null)throw new InvalidOperationException("Existing completion card missing.");
        activity.completionTitle=card.transform.Find("Title").GetComponent<TMP_Text>();
        activity.finishExplorationButton=card.transform.Find("FinishExplorationButton").GetComponent<UnityEngine.UI.Button>();
        activity.completionTitle.text="Well Done!";
        EditorUtility.SetDirty(activity);EditorSceneManager.MarkSceneDirty(scene);
        if(!EditorSceneManager.SaveScene(scene))throw new InvalidOperationException("Save failed.");
        Debug.Log("CHECKPOINT A verified existing five grab targets; CHECKPOINT B final UI references wired without moving or duplicating objects.");
    }
    static bool ValidPosition(LadybirdHabitatObjective objective,FoodSearchTarget target,Vector3 candidate,List<Vector3> placed,out Vector3 position)
    {
        position=candidate;var c=objective.controller;var sphere=target.GetComponent<SphereCollider>();
        var ground=c.groundSurfaces.Single();
        if(!ground.Raycast(new Ray(new Vector3(candidate.x,ground.bounds.max.y+1,candidate.z),Vector3.down),out var hit,ground.bounds.size.y+2))return false;
        position.y=hit.point.y+.015f;
        var center=position+target.transform.rotation*Vector3.Scale(sphere.center,target.transform.lossyScale);
        float radius=sphere.radius*Mathf.Max(target.transform.lossyScale.x,Mathf.Max(target.transform.lossyScale.y,target.transform.lossyScale.z));
        foreach(var p in placed) if(Vector2.Distance(new Vector2(p.x,p.z),new Vector2(position.x,position.z))<.92f)return false;
        var habitat=objective.habitatTarget.position;
        if(Vector2.Distance(new Vector2(position.x,position.z),new Vector2(habitat.x,habitat.z))<c.safety.bodyRadius+objective.destinationRadius+.5f)return false;
        foreach(var col in Physics.OverlapSphere(center,radius+.05f,~0,QueryTriggerInteraction.Ignore))
            if(!c.groundSurfaces.Contains(col)&&!col.transform.IsChildOf(c.transform))return false;
        var eye=c.xrOrigin.Camera.transform.position;float distance=Vector3.Distance(eye,center);
        if(distance>9.5f)return false;
        foreach(var hitInfo in Physics.RaycastAll(eye,(center-eye).normalized,distance-radius,~0,QueryTriggerInteraction.Ignore))
            if(!hitInfo.transform.IsChildOf(c.transform)&&!c.groundSurfaces.Contains(hitInfo.collider))return false;
        // Keep each target's centre ray clear of the other forgiving selection spheres.
        foreach(var p in placed)
        {
            var other=p+Vector3.up*.18f;var direction=(center-eye).normalized;float along=Vector3.Dot(other-eye,direction);
            if(along>0 && along<distance && Vector3.Distance(other,eye+direction*along)<.45f)return false;
            direction=(other-eye).normalized;along=Vector3.Dot(center-eye,direction);
            if(along>0&&along<Vector3.Distance(eye,other)&&Vector3.Distance(center,eye+direction*along)<.45f)return false;
        }
        return true;
    }
    static void Wire(UnityEngine.UI.Button button,string name,string label,UnityEngine.Events.UnityAction callback,float x,float width)
    {
        button.name=name;for(int i=button.onClick.GetPersistentEventCount()-1;i>=0;i--)UnityEventTools.RemovePersistentListener(button.onClick,i);
        UnityEventTools.AddPersistentListener(button.onClick,callback);
        button.GetComponentInChildren<TMP_Text>(true).text=label;
        var rect=(RectTransform)button.transform;rect.anchoredPosition=new Vector2(x,-125);rect.sizeDelta=new Vector2(width,65);
        button.GetComponentInChildren<TMP_Text>(true).rectTransform.sizeDelta=new Vector2(width-20,55);
        button.gameObject.SetActive(true);
    }
}
