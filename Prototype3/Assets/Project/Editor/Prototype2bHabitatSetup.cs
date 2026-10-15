using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TMPro;
using UnityEditor.Events;
using UnityEngine.XR.Interaction.Toolkit.UI;

/// <summary>Explicit verification/configuration of the saved Prototype2b habitat; never moves the target.</summary>
public static class Prototype2bHabitatSetup
{
    const string ScenePath="Assets/Project/Scenes/Prototype2b.unity";
    public static void InspectBatch() { Inspect(false); }
    public static void ConfigureBatch() { Inspect(true); }
    static void Inspect(bool configure)
    {
        var scene=EditorSceneManager.OpenScene(ScenePath);
        var all=scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<Transform>(true)).ToArray();
        var controller=all.Select(x=>x.GetComponent<LadybirdPlayerController>()).Single(x=>x!=null);
        var target=all.Single(x=>x.name=="HabitatTarget");
        var active=all.ToDictionary(x=>x.gameObject,x=>x.gameObject.activeSelf);
        try
        {
            scene.GetRootGameObjects().Single(x=>x.name=="OnboardingZone").SetActive(false);
            scene.GetRootGameObjects().Single(x=>x.name=="SelectionZone").SetActive(false);
            for(var t=controller.transform;t!=null;t=t.parent)t.gameObject.SetActive(true);
            for(var t=target;t!=null;t=t.parent)t.gameObject.SetActive(true);
            Physics.SyncTransforms();
            Debug.Log($"HABITAT saved local={target.localPosition:F6}, world={target.position:F6}; GroundContact local={controller.groundContact.localPosition:F6}, world={controller.groundContact.position:F6}; parent={target.parent.name}");
            var center=controller.startingPoint.position;
            if(!controller.safety.TryFindStart(controller.transform,controller.groundSurfaces,center,center,out var start,out var reason))throw new Exception("Starting point: "+reason);
            if(!Valid(controller,target.position,center,target.position.y,out var goal,out reason))throw new Exception("Saved habitat is unsafe: "+reason);
            for(int i=0;i<32;i++)
            {
                var sample=goal+Quaternion.Euler(0,i*360f/32f,0)*Vector3.forward*.45f;
                if(!Valid(controller,sample,center,goal.y,out _,out reason))throw new Exception("Habitat arrival region is unsafe: "+reason);
            }
            if(!Route(controller,start,goal,center,out float length,out int samples))throw new Exception("No fully verified route to the saved habitat.");
            Debug.Log($"HABITAT verified full 0.45m region; grounded centre={goal:F6}; verified route length={length:F2}m; reached grid samples={samples}; every edge sampled at <=0.02m with actual ground, full footprint, boundary and obstacle checks.");
            foreach(var state in active)state.Key.SetActive(state.Value);
            if(configure)Configure(scene,all,controller,target,goal);
        }
        finally {foreach(var state in active)if(state.Key!=null)state.Key.SetActive(state.Value);}
    }
    static bool Valid(LadybirdPlayerController c,Vector3 p,Vector3 center,float height,out Vector3 ground,out string reason)
    {
        return c.safety.TryPosition(c.groundSurfaces,p,center,height,out ground,out reason) &&
            c.safety.PathIsClear(c.transform,ground,Vector3.forward,0f,out reason,c.groundSurfaces);
    }
    static bool Segment(LadybirdPlayerController c,Vector3 from,Vector3 to,Vector3 center,out Vector3 end)
    {
        end=from;var delta=to-from;delta.y=0;
        int steps=Mathf.Max(1,Mathf.CeilToInt(delta.magnitude/Mathf.Min(.02f,c.safety.maximumMovementStep)));
        for(int i=1;i<=steps;i++)
        {
            var next=Vector3.Lerp(from,to,(float)i/steps);next.y=end.y;
            if(!c.safety.TryMovementPosition(c.groundSurfaces,end,next,center,out var ground,out _) ||
                !c.safety.TryObstacleMotion(c.transform,end,ground-end,c.groundSurfaces,out var motion,out _) ||
                (motion-(ground-end)).sqrMagnitude>1e-8f)return false;
            end=ground;
        }
        return true;
    }
    static bool Route(LadybirdPlayerController c,Vector3 start,Vector3 goal,Vector3 center,out float length,out int count)
    {
        var cells=new Dictionary<Vector2Int,Vector3>{{Vector2Int.zero,start}};
        var distances=new Dictionary<Vector2Int,float>{{Vector2Int.zero,0f}};
        var queue=new Queue<Vector2Int>();queue.Enqueue(Vector2Int.zero);
        var directions=new[]{Vector2Int.right,Vector2Int.up,Vector2Int.down,Vector2Int.left};
        while(queue.Count>0)
        {
            var key=queue.Dequeue();var from=cells[key];
            if(Vector3.Distance(from,goal)<.8f && Segment(c,from,goal,center,out _))
            {length=distances[key]+Vector3.Distance(from,goal);count=cells.Count;return true;}
            foreach(var direction in directions)
            {
                var nextKey=key+direction;if(cells.ContainsKey(nextKey))continue;
                var next=start+new Vector3(nextKey.x*.5f,0,nextKey.y*.5f);next.y=from.y;
                if(!Segment(c,from,next,center,out var ground))continue;
                cells.Add(nextKey,ground);distances.Add(nextKey,distances[key]+.5f);queue.Enqueue(nextKey);
            }
        }
        length=0;count=cells.Count;return false;
    }
    static void Configure(UnityEngine.SceneManagement.Scene scene,Transform[] all,LadybirdPlayerController c,Transform target,Vector3 ground)
    {
        if(c.GetComponent<LadybirdHabitatObjective>() != null)throw new Exception("Habitat objective already configured; inspect before changing it.");
        var canvas=all.Single(x=>x.name=="ExploreNavigationUI");
        if(canvas.GetComponent<Canvas>().renderMode!=RenderMode.WorldSpace || canvas.GetComponent<TrackedDeviceGraphicRaycaster>()==null)
            throw new Exception("Expected existing world-space XR canvas/raycaster.");
        var container=canvas.Find("EnvironmentTransitionUI");
        if(container==null || container.GetComponentsInParent<UnityEngine.UI.RectMask2D>(true).Length!=0 ||
            container.GetComponentsInParent<UnityEngine.UI.Mask>(true).Length!=0)throw new Exception("Unexpected UI container/clipping.");
        var objective=c.gameObject.AddComponent<LadybirdHabitatObjective>();
        objective.controller=c;objective.transition=c.GetComponent<LadybirdEnvironmentTransition>();objective.habitatTarget=target;
        objective.destinationRadius=.45f;objective.enableInEditorMovementTest=true;
        var panel=Rect("HabitatObjectiveUI",container,new Vector2(700,280),new Vector2(0,320));
        var bg=panel.gameObject.AddComponent<UnityEngine.UI.Image>();bg.color=new Color(.025f,.045f,.06f,.96f);bg.raycastTarget=false;
        var text=Text("HabitatMessage",panel,LadybirdHabitatObjective.NavigationPrompt,new Vector2(660,160),new Vector2(0,45),28);
        var buttonRect=Rect("ContinueToFoodDiscoveryButton",panel,new Vector2(620,72),new Vector2(0,-85));
        var image=buttonRect.gameObject.AddComponent<UnityEngine.UI.Image>();image.color=new Color(.08f,.28f,.18f,1);image.raycastTarget=true;
        var button=buttonRect.gameObject.AddComponent<UnityEngine.UI.Button>();button.targetGraphic=image;
        var nav=button.navigation;nav.mode=UnityEngine.UI.Navigation.Mode.None;button.navigation=nav;
        UnityEventTools.AddPersistentListener(button.onClick,objective.ContinueToFoodDiscovery);
        Text("Label",buttonRect,"Continue to Food Discovery",new Vector2(600,68),Vector2.zero,28);
        objective.objectivePanel=panel.gameObject;objective.objectiveText=text;objective.continueButton=buttonRect.gameObject;
        panel.gameObject.SetActive(false);buttonRect.gameObject.SetActive(false);
        // Marker is entirely visual: no collider or rigidbody and no change to HabitatTarget's saved transform.
        var marker=new GameObject("HabitatDestinationMarker");marker.transform.SetParent(target,false);
        marker.transform.position=ground+Vector3.up*.025f;
        var ring=marker.AddComponent<LineRenderer>();ring.useWorldSpace=false;ring.loop=true;ring.positionCount=64;
        ring.widthMultiplier=.025f;ring.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;ring.receiveShadows=false;
        const string materialPath="Assets/Project/Materials/HabitatMarker.mat";
        if(AssetDatabase.LoadAssetAtPath<Material>(materialPath)!=null)throw new Exception("Marker material already exists; inspect before replacing.");
        var shader=Shader.Find("Universal Render Pipeline/Unlit");if(shader==null)throw new Exception("Existing URP Unlit shader unavailable.");
        var material=new Material(shader);material.SetColor("_BaseColor",new Color(.65f,1f,.55f,1));
        AssetDatabase.CreateAsset(material,materialPath);ring.sharedMaterial=material;
        for(int i=0;i<64;i++){float angle=i*Mathf.PI*2/64;ring.SetPosition(i,new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*.45f);}
        var label=new GameObject("HabitatLabel",typeof(TextMeshPro));label.transform.SetParent(marker.transform,false);label.transform.localPosition=Vector3.up*.5f;
        var caption=label.GetComponent<TextMeshPro>();caption.font=TMP_Settings.defaultFontAsset;caption.text="Leafy habitat";caption.fontSize=2.4f;
        caption.color=new Color(.85f,1f,.75f);caption.alignment=TextAlignmentOptions.Center;caption.rectTransform.sizeDelta=new Vector2(2.5f,.6f);
        label.transform.rotation=Quaternion.LookRotation(label.transform.position-c.xrOrigin.Camera.transform.position);
        objective.destinationMarker=marker;marker.SetActive(false);
        if(panel.IsChildOf(c.transform) || marker.GetComponentsInChildren<Collider>(true).Length!=0)throw new Exception("Invalid stationary UI/marker hierarchy.");
        if(button.onClick.GetPersistentEventCount()!=1 || text.font==null || caption.font==null)throw new Exception("Incomplete UI references.");
        EditorUtility.SetDirty(objective);EditorSceneManager.MarkSceneDirty(scene);
        AssetDatabase.SaveAssets();if(!EditorSceneManager.SaveScene(scene))throw new Exception("Scene save failed.");
        Debug.Log("HABITAT configured: stationary existing XR canvas, reversible movement hold, visual-only marker, independent Editor opt-in; target unchanged.");
    }
    static RectTransform Rect(string name,Transform parent,Vector2 size,Vector2 position)
    {
        var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rect.SetParent(parent,false);
        rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.sizeDelta=size;rect.anchoredPosition=position;return rect;
    }
    static TMP_Text Text(string name,Transform parent,string value,Vector2 size,Vector2 position,float fontSize)
    {
        var text=Rect(name,parent,size,position).gameObject.AddComponent<TextMeshProUGUI>();text.font=TMP_Settings.defaultFontAsset;
        text.text=value;text.fontSize=fontSize;text.color=Color.white;text.alignment=TextAlignmentOptions.Center;text.raycastTarget=false;return text;
    }
}
