using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>Scene-only population expansion using the existing aphid and ant wrappers.</summary>
public static class Prototype2bPopulationSetup
{
    static float Radius(FoodSearchTarget t) { var s=t.transform.lossyScale;return t.GetComponent<SphereCollider>().radius*Mathf.Max(s.x,Mathf.Max(s.y,s.z)); }
    static Vector3 Center(FoodSearchTarget t) => t.transform.TransformPoint(t.GetComponent<SphereCollider>().center);
    static bool HasRay(FoodSearchActivity a,FoodSearchTarget t,FoodSearchTarget[] all) => (bool)typeof(Prototype2bTenAphidSetup)
        .GetMethod("HasClearRay",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{a,t,all});
    static bool TemporaryPosition(FoodSearchActivity a,FoodSearchTarget t,Vector3 candidate,FoodSearchTarget[] others,FoodSearchTarget[] previouslyVisible,out Vector3 position)
    {
        var c=a.objective.controller;var ground=c.groundSurfaces.Single();position=candidate;
        if(!ground.Raycast(new Ray(new Vector3(candidate.x,ground.bounds.max.y+1,candidate.z),Vector3.down),out var hit,ground.bounds.size.y+2))return false;
        position.y=hit.point.y+.015f;
        if(Vector2.Distance(new Vector2(position.x,position.z),new Vector2(a.objective.habitatTarget.position.x,a.objective.habitatTarget.position.z))<c.safety.bodyRadius+a.objective.destinationRadius+.5f)return false;
        var old=t.transform.position;t.transform.position=position;
        try {
            var center=Center(t);float radius=Radius(t);float distance=Vector3.Distance(c.xrOrigin.Camera.transform.position,center);
            if(distance<1.5f||distance>9.5f)return false;
            if(others.Any(o=>Vector3.Distance(Center(o),center)<Mathf.Max(.8f,Radius(o)+radius+.08f)))return false;
            if(Physics.OverlapSphere(center,radius+.05f,~0,QueryTriggerInteraction.Ignore).Any(col=>!c.groundSurfaces.Contains(col)&&!col.transform.IsChildOf(c.transform)))return false;
            var all=others.Concat(new[]{t}).ToArray();
            return HasRay(a,t,all)&&previouslyVisible.All(o=>HasRay(a,o,all));
        } finally {t.transform.position=old;}
    }
    public static void ConfigureBatch()
    {
        if(!Application.isBatchMode)throw new InvalidOperationException("Use an isolated batch copy.");
        var scene=EditorSceneManager.OpenScene("Assets/Project/Scenes/Prototype2b.unity");
        var all=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();
        var a=all.Select(t=>t.GetComponent<FoodSearchActivity>()).Single(t=>t!=null);
        var aphids=a.targets.Where(t=>t.category==FoodSearchTarget.IdentificationCategory.Aphid).ToList();
        var ants=a.targets.Where(t=>t.category==FoodSearchTarget.IdentificationCategory.Ant).ToList();
        Debug.Log($"POPULATION inspected {aphids.Count} aphids, {ants.Count} ants; required={a.requiredAphids}");
        if(aphids.Count>15||ants.Count>5||a.requiredAphids!=10)throw new InvalidOperationException("Unexpected population/objective; inspect manually.");
        var states=all.ToDictionary(t=>t.gameObject,t=>t.gameObject.activeSelf);
        var placed=a.targets.ToList();var additions=new List<FoodSearchTarget>();
        var validate=typeof(Prototype2bTenAphidSetup).GetMethod("ValidPosition",BindingFlags.NonPublic|BindingFlags.Static);
        try {
            for(var t=a.objective.habitatTarget;t!=null;t=t.parent)t.gameObject.SetActive(true);
            a.insectArea.SetActive(false);Physics.SyncTransforms();
            var previouslyVisible=placed.Where(t=>HasRay(a,t,placed.ToArray())).ToList();
            foreach(var t in placed.Except(previouslyVisible))Debug.LogWarning("POPULATION existing saved-eye ray needs manual review: "+t.name);
            foreach(var group in new[]{aphids,ants}) {
                int desired=group==aphids?15:5;var reference=group.OrderBy(t=>t.name).First();
                for(int i=group.Count+1;i<=desired;i++) {
                    string name=(group==aphids?"Aphid_":"Ant_")+i.ToString("00");
                    if(all.Any(t=>t.name==name))throw new InvalidOperationException("Unregistered existing target "+name);
                    var go=UnityEngine.Object.Instantiate(reference.gameObject,reference.transform.parent,false);go.name=name;
                    var target=go.GetComponent<FoodSearchTarget>();target.activity=a;
                    var bounds=a.objective.controller.groundSurfaces.Single().bounds;
                    var candidates=new List<Vector3>();
                    for(float x=bounds.min.x;x<=bounds.max.x;x+=.2f)for(float z=bounds.min.z;z<=bounds.max.z;z+=.2f)
                        candidates.Add(new Vector3(x,reference.transform.position.y,z));
                    bool found=false;float usedRegion=0;
                    foreach(float region in new[]{7f,9f,12f}) {
                        foreach(var candidate in candidates.Where(p=>Vector3.Distance(p,a.objective.habitatTarget.position)<=region)
                            .OrderBy(p=>(p-a.objective.habitatTarget.position).sqrMagnitude)) {
                            object[] args={a,target,candidate,placed.ToArray(),Vector3.zero};
                            if(!(bool)validate.Invoke(null,args))continue;
                            target.transform.position=(Vector3)args[4];found=true;usedRegion=region;break;
                        }
                        if(found)break;
                    }
                    if(!found) {
                        foreach(var candidate in candidates.Where(p=>Vector3.Distance(p,a.objective.habitatTarget.position)<=12f)
                            .OrderBy(p=>(p-a.objective.habitatTarget.position).sqrMagnitude)) {
                            if(!TemporaryPosition(a,target,candidate,placed.ToArray(),previouslyVisible.ToArray(),out var position))continue;
                            target.transform.position=position;found=true;usedRegion=12;break;
                        }
                        if(found)Debug.LogWarning("POPULATION temporary arrangement: verify Quest visibility and reach for "+name);
                    }
                    if(!found)throw new InvalidOperationException("No safe temporary position for "+name+"; no scene changes saved.");
                    previouslyVisible.Add(target);
                    group.Add(target);placed.Add(target);additions.Add(target);
                    Debug.Log($"POPULATION added {name}: {target.transform.position:F4}; region={usedRegion}m");
                }
            }
        } finally {foreach(var state in states)if(state.Key!=null)state.Key.SetActive(state.Value);}
        a.targets=aphids.OrderBy(t=>t.name).Concat(ants.OrderBy(t=>t.name)).ToArray();
        a.requiredAphids=10;
        a.searchPanel.transform.Find("Description").GetComponent<TMP_Text>().text=FoodSearchActivity.Instruction;
        a.progressText.text="Aphids caught: 0 / 10";
        foreach(var text in a.searchPanel.GetComponentsInChildren<TMP_Text>(true)) {
            text.ForceMeshUpdate(true,true);if(text.isTextOverflowing)throw new InvalidOperationException("Instruction text overflow");
        }
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        Debug.Log("POPULATION saved: 15 aphids, 5 ants; catch ANY 10. Original target configurations and scene anchors preserved.");
    }
}
