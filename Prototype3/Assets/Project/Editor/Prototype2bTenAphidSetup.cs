using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>One-time scene-only expansion. Preserves the original seven targets and all imported assets.</summary>
public static class Prototype2bTenAphidSetup
{
    static Vector3 Center(FoodSearchTarget target) => target.transform.TransformPoint(target.GetComponent<SphereCollider>().center);
    static float Radius(FoodSearchTarget target)
    {
        var s=target.transform.lossyScale;return target.GetComponent<SphereCollider>().radius*Mathf.Max(Mathf.Abs(s.x),Mathf.Abs(s.y),Mathf.Abs(s.z));
    }
    static bool ValidPosition(FoodSearchActivity a,FoodSearchTarget target,Vector3 candidate,FoodSearchTarget[] others,out Vector3 position)
    {
        var c=a.objective.controller;var ground=c.groundSurfaces.Single();position=candidate;
        if(!ground.Raycast(new Ray(new Vector3(candidate.x,ground.bounds.max.y+1,candidate.z),Vector3.down),out var hit,ground.bounds.size.y+2))return false;
        position.y=hit.point.y+.015f;
        var habitat=a.objective.habitatTarget.position;
        if(Vector2.Distance(new Vector2(position.x,position.z),new Vector2(habitat.x,habitat.z))<c.safety.bodyRadius+a.objective.destinationRadius+.5f)return false;
        var previous=target.transform.position;target.transform.position=position;
        try {
            var center=Center(target);float radius=Radius(target);var eye=c.xrOrigin.Camera.transform.position;
            if(Vector3.Distance(eye,center)>9.5f || Vector3.Distance(eye,center)<1.5f)return false;
            foreach(var other in others)if(Vector3.Distance(Center(other),center)<Radius(other)+radius+.08f)return false;
            foreach(var col in Physics.OverlapSphere(center,radius+.05f,~0,QueryTriggerInteraction.Ignore))
                if(!c.groundSurfaces.Contains(col)&&!col.transform.IsChildOf(c.transform))return false;
            // The forgiving selection spheres can mask each other's centre rays. Require a clear
            // selectable point on every target rather than rejecting otherwise distinct insects.
            var all=others.Concat(new[]{target}).ToArray();
            return all.All(t=>HasClearRay(a,t,all));
        } finally { target.transform.position=previous; }
    }
    static bool HasClearRay(FoodSearchActivity a,FoodSearchTarget target,FoodSearchTarget[] all)
    {
        var c=a.objective.controller;var eye=c.xrOrigin.Camera.transform.position;var center=Center(target);var radius=Radius(target);
        var right=Vector3.Cross(Vector3.up,(center-eye).normalized).normalized;var up=Vector3.Cross((center-eye).normalized,right).normalized;
        foreach(var x in new[]{0f,-.65f,.65f})foreach(var y in new[]{0f,-.65f,.65f}) {
            var direction=(center+radius*(right*x+up*y)-eye).normalized;
            if(!SphereHit(eye,direction,center,radius,out var distance))continue;
            if(all.Any(other=>other!=target&&SphereHit(eye,direction,Center(other),Radius(other),out var near)&&near<distance))continue;
            if(Physics.RaycastAll(eye,direction,distance,~0,QueryTriggerInteraction.Ignore).Any(h=>!c.groundSurfaces.Contains(h.collider)&&!h.transform.IsChildOf(c.transform)))continue;
            return true;
        }
        return false;
    }
    static bool SphereHit(Vector3 origin,Vector3 direction,Vector3 center,float radius,out float distance)
    {
        var v=center-origin;float along=Vector3.Dot(v,direction);float side=v.sqrMagnitude-along*along;
        distance=0;if(along<0||side>radius*radius)return false;
        distance=along-Mathf.Sqrt(Mathf.Max(0,radius*radius-side));return distance>0;
    }
    public static void ConfigureBatch()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("Run in a batch validation copy.");
        var scene=EditorSceneManager.OpenScene("Assets/Project/Scenes/Prototype2b.unity");
        var all=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();
        var a=all.Select(t=>t.GetComponent<FoodSearchActivity>()).Single(t=>t!=null);
        if(a.targets.Length!=7 || a.requiredAphids!=5) throw new InvalidOperationException("Expected the saved five-aphid mission; nothing was saved.");
        var reference=a.targets.Single(t=>t.name=="Aphid_01");
        var original=a.targets.ToArray();var added=new List<FoodSearchTarget>();
        var states=all.ToDictionary(t=>t.gameObject,t=>t.gameObject.activeSelf);
        try
        {
            for(var t=a.objective.habitatTarget;t!=null;t=t.parent)t.gameObject.SetActive(true);
            a.insectArea.SetActive(false);Physics.SyncTransforms();
            for(int i=6;i<=10;i++)
            {
                var go=UnityEngine.Object.Instantiate(reference.gameObject,reference.transform.parent,false);go.name=$"Aphid_{i:00}";
                var target=go.GetComponent<FoodSearchTarget>();target.activity=a;
                var candidates=new List<Vector3>();
                var groundBounds=a.objective.controller.groundSurfaces.Single().bounds;
                Debug.Log("PLACEMENT bounds="+groundBounds+" eye="+a.objective.controller.xrOrigin.Camera.transform.position+" sphere="+reference.GetComponent<SphereCollider>().radius+" scale="+reference.transform.lossyScale+" bodyRadius="+a.objective.controller.safety.bodyRadius);
                for(float x=groundBounds.min.x;x<=groundBounds.max.x;x+=.25f)
                    for(float z=groundBounds.min.z;z<=groundBounds.max.z;z+=.25f)
                        if(Vector2.Distance(new Vector2(x,z),new Vector2(a.objective.habitatTarget.position.x,a.objective.habitatTarget.position.z))<=7f)
                            candidates.Add(new Vector3(x,reference.transform.position.y,z));
                bool found=false;
                foreach(var candidate in candidates.OrderBy(p=>(p-a.objective.habitatTarget.position).sqrMagnitude))
                {
                    if(!ValidPosition(a,target,candidate,original.Concat(added).ToArray(),out var position))continue;
                    target.transform.position=position;found=true;break;
                }
                if(!found)throw new InvalidOperationException("No verified clear position for "+go.name+". Manual placement needed; scene not saved.");
                added.Add(target);
                Debug.Log("TEN APHIDS placed "+go.name+" at "+go.transform.position.ToString("F4"));
            }
        }
        finally {foreach(var state in states)if(state.Key!=null)state.Key.SetActive(state.Value);}
        a.targets=original.Where(t=>t.category==FoodSearchTarget.IdentificationCategory.Aphid).Concat(added)
            .Concat(original.Where(t=>t.category!=FoodSearchTarget.IdentificationCategory.Aphid)).ToArray();
        a.requiredAphids=10;
        a.searchPanel.transform.Find("Title").GetComponent<TMP_Text>().text="Catch Ten Aphids";
        a.searchPanel.transform.Find("Description").GetComponent<TMP_Text>().text=FoodSearchActivity.Instruction;
        a.progressText.text="Aphids caught: 0 / 10";
        a.completionPanel.transform.Find("Description").GetComponent<TMP_Text>().text=FoodSearchActivity.CompletionFeedback;
        a.voiceGuidance.foodMissionAphidCount=10;
        a.voiceGuidance.recordedFoodMissionAphidCount=5; // Retain legacy assets but do not play count-mismatched narration.
        var grab=reference.GetComponent<XRGrabInteractable>();
        foreach(var t in a.targets) {
            var g=t.GetComponent<XRGrabInteractable>();
            if(g==null||g.interactionManager!=grab.interactionManager||g.farAttachMode!=grab.farAttachMode||g.interactionLayers.value!=grab.interactionLayers.value||t.activity!=a)
                throw new InvalidOperationException("Grab mismatch: "+t.name);
        }
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        Debug.Log("TEN APHIDS configured; original targets, UI transforms, HabitatTarget and media preserved.");
    }
}
