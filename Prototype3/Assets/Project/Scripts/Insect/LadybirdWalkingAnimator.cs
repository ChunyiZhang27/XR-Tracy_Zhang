using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Visual-only tripod gait. Original mesh transforms, interaction pivots and colliders stay untouched.</summary>
[DefaultExecutionOrder(1000)]
public sealed class LadybirdWalkingAnimator : MonoBehaviour
{
    [Serializable] public sealed class LegBinding
    {
        public string label;
        public bool tripodA;
        public MeshRenderer[] segments;
        [Tooltip("Joint positions in the visual model's local coordinates, measured from its geometry.")]
        public Vector3 root, hip, knee, ankle, foot;
    }
    public LadybirdPlayerController controller;
    public Transform visualModel;
    public LegBinding[] legs;
    [Min(.1f)] public float gaitFrequency = 2f;
    [Min(.01f)] public float referenceSpeed = .35f;
    [Min(0f)] public float strideAmplitude = .07f;
    [Min(0f)] public float legLift = .035f;
    [Min(.01f)] public float transitionSmoothing = .18f;
    [Tooltip("Optional visual-only bob in metres. Zero disables it; body pivots/colliders do not move.")]
    [Range(0f,.015f)] public float bodyBobHeight;
    [SerializeField] private string gaitStatus = "Standing";
    public string GaitStatus => gaitStatus;
    private sealed class RuntimeLeg
    {
        public LegBinding binding;
        public Transform hip,knee,ankle;
        public Vector3 planted,swingStart,swingEnd,goal;
        public bool wasSwing;
    }
    private sealed class Copy {public MeshRenderer source,copy; public bool wasEnabled;}
    private readonly List<Copy> copies=new List<Copy>();
    private readonly List<Copy> bodyCopies=new List<Copy>();
    private RuntimeLeg[] runtime;
    private GameObject rig;
    private Vector3 lastPosition;
    private Quaternion lastRotation;
    private bool driving;
    private int resetVersion;
    private float phase,blend,blendVelocity;

    public bool ValidateBindings(out string reason)
    {
        if(controller==null || visualModel==null || !visualModel.IsChildOf(controller.transform) || legs==null || legs.Length!=6)
        {reason="Assign controller, separate visual model and exactly six legs";return false;}
        var scale=visualModel.lossyScale;
        if(Mathf.Abs(scale.x-scale.y)>.001f || Mathf.Abs(scale.x-scale.z)>.001f || scale.x<=0f)
        {reason="Uniform positive model scale is required";return false;}
        var unique=new HashSet<MeshRenderer>();int a=0;
        foreach(var leg in legs)
        {
            if(leg==null || leg.segments==null || leg.segments.Length!=4 ||
                (leg.knee-leg.hip).sqrMagnitude<.0000001f || (leg.ankle-leg.knee).sqrMagnitude<.0000001f)
            {reason="Each leg needs four mesh sections and distinct hip/knee/ankle points";return false;}
            if(leg.tripodA)a++;
            foreach(var mesh in leg.segments)
                if(mesh==null || !mesh.transform.IsChildOf(visualModel) || !unique.Add(mesh) ||
                    mesh.GetComponent<MeshFilter>()?.sharedMesh==null || mesh.GetComponent<MeshFilter>().sharedMesh.vertexCount==0)
                {reason="Leg meshes must be unique, nonempty children of the visual model";return false;}
        }
        reason=a==3?"Ready":"Each tripod needs three legs";return a==3;
    }
    private Transform Pivot(string name,Transform parent,Vector3 world)
    {
        var pivot=new GameObject(name).transform;pivot.SetParent(parent,false);pivot.position=world;pivot.rotation=visualModel.rotation;return pivot;
    }
    private Copy Clone(MeshRenderer source,Transform parent)
    {
        var go=new GameObject(source.name+"_WalkingVisual",typeof(MeshFilter),typeof(MeshRenderer));
        // First copy the exact original local pose, then preserve its world pose under a visual control.
        go.transform.SetParent(source.transform.parent,false);
        go.transform.localPosition=source.transform.localPosition;go.transform.localRotation=source.transform.localRotation;go.transform.localScale=source.transform.localScale;
        go.transform.SetParent(parent,true);
        go.GetComponent<MeshFilter>().sharedMesh=source.GetComponent<MeshFilter>().sharedMesh;
        var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterials=source.sharedMaterials;
        renderer.shadowCastingMode=source.shadowCastingMode;renderer.receiveShadows=source.receiveShadows;
        renderer.lightProbeUsage=source.lightProbeUsage;renderer.reflectionProbeUsage=source.reflectionProbeUsage;
        var copy=new Copy{source=source,copy=renderer,wasEnabled=source.enabled};return copy;
    }
    private bool Build()
    {
        if(!ValidateBindings(out string reason)){gaitStatus="Setup required: "+reason;Debug.LogError(gaitStatus,this);enabled=false;return false;}
        rig=new GameObject("LadybirdWalkingVisuals_Runtime");rig.transform.SetParent(visualModel,false);
        runtime=new RuntimeLeg[6];var legMeshes=new HashSet<MeshRenderer>();
        for(int i=0;i<6;i++)
        {
            var b=legs[i];var root=Pivot(b.label+" Root",rig.transform,visualModel.TransformPoint(b.root));
            var hip=Pivot("Hip",root,visualModel.TransformPoint(b.hip));var knee=Pivot("Knee",hip,visualModel.TransformPoint(b.knee));
            var ankle=Pivot("Ankle",knee,visualModel.TransformPoint(b.ankle));
            Transform[] controls={root,hip,knee,ankle};
            for(int j=0;j<4;j++){copies.Add(Clone(b.segments[j],controls[j]));legMeshes.Add(b.segments[j]);}
            runtime[i]=new RuntimeLeg{binding=b,hip=hip,knee=knee,ankle=ankle};
        }
        // Only ordinary body renderers are copied for optional bob; glow objects remain authored interactions.
        foreach(var renderer in visualModel.GetComponentsInChildren<MeshRenderer>(true))
            if(!renderer.transform.IsChildOf(rig.transform) && !legMeshes.Contains(renderer) &&
                renderer.GetComponent<MeshFilter>()?.sharedMesh!=null && !HasGlowAncestor(renderer.transform))
                bodyCopies.Add(Clone(renderer,rig.transform));
        rig.SetActive(false);return true;
    }
    private bool HasGlowAncestor(Transform item)
    {while(item!=null && item!=visualModel){if(item.name.IndexOf("glow",StringComparison.OrdinalIgnoreCase)>=0)return true;item=item.parent;}return false;}
    private bool Ground(Vector3 point,out Vector3 grounded)
    {
        grounded=point;float nearest=float.PositiveInfinity;
        var ray=new Ray(point+Vector3.up*controller.safety.probeHeight,Vector3.down);
        foreach(var surface in controller.groundSurfaces)
            if(surface!=null && surface.enabled && !surface.isTrigger && surface.gameObject.activeInHierarchy &&
                surface.Raycast(ray,out var hit,controller.safety.probeHeight+controller.safety.probeDepth) && hit.distance<nearest)
            {nearest=hit.distance;grounded=hit.point;}
        return !float.IsPositiveInfinity(nearest);
    }
    private void ResetPose()
    {
        phase=blend=blendVelocity=0f;
        foreach(var leg in runtime)
        {
            leg.hip.localRotation=leg.knee.localRotation=leg.ankle.localRotation=Quaternion.identity;
            var rest=visualModel.TransformPoint(leg.binding.foot);leg.planted=Ground(rest,out var ground)?ground:rest;
            leg.swingStart=leg.swingEnd=leg.goal=leg.planted;leg.wasSwing=false;
        }
        foreach(var copy in bodyCopies) if(copy.source!=null)
            copy.copy.transform.SetPositionAndRotation(copy.source.transform.position,copy.source.transform.rotation);
        lastPosition=controller.transform.position;lastRotation=controller.transform.rotation;resetVersion=controller.ResetVersion;
    }
    private void Show(bool active)
    {
        foreach(var copy in copies) if(copy.source!=null){copy.source.enabled=active?false:copy.wasEnabled;copy.copy.enabled=active&&copy.wasEnabled&&copy.source.gameObject.activeInHierarchy;}
        bool bob=active&&bodyBobHeight>0f;
        foreach(var copy in bodyCopies) if(copy.source!=null){copy.source.enabled=bob?false:copy.wasEnabled;copy.copy.enabled=bob&&copy.wasEnabled&&copy.source.gameObject.activeInHierarchy;}
        rig.SetActive(active);
    }
    private void LateUpdate()
    {
        if(!Application.isPlaying || controller==null)return;
        if(rig==null && !Build())return;
        bool active=controller.ControlActive;
        if(!active){if(driving){Show(false);driving=false;}gaitStatus="Standing (body exploration)";return;}
        if(!driving){ResetPose();driving=true;Show(true);}
        if(resetVersion!=controller.ResetVersion){ResetPose();gaitStatus="Standing (reset)";return;}
        float dt=Time.deltaTime;if(dt<=0f)return;
        Vector3 position=controller.transform.position;Quaternion rotation=controller.transform.rotation;
        Vector3 velocity=(position-lastPosition)/dt;velocity.y=0f;
        float yaw=Mathf.DeltaAngle(lastRotation.eulerAngles.y,rotation.eulerAngles.y)*Mathf.Deg2Rad/dt;
        lastPosition=position;lastRotation=rotation;
        if(velocity.magnitude>Mathf.Max(2f,controller.movementSpeed*4f)){ResetPose();return;}
        AnimateMotion(velocity,yaw,dt);
    }
    private void AnimateMotion(Vector3 velocity,float yaw,float dt)
    {
        float effective=Mathf.Max(velocity.magnitude,Mathf.Abs(yaw)*controller.safety.bodyRadius*.6f);
        float target=effective>.005f?1f:0f;
        blend=Mathf.SmoothDamp(blend,target,ref blendVelocity,transitionSmoothing,Mathf.Infinity,dt);
        float frequency=gaitFrequency*Mathf.Clamp(effective/Mathf.Max(.01f,referenceSpeed),0f,2f);
        phase=Mathf.Repeat(phase+frequency*dt,1f);
        foreach(var leg in runtime)
        {
            var rest=visualModel.TransformPoint(leg.binding.foot);
            Vector3 footVelocity=velocity+Vector3.Cross(Vector3.up*yaw,rest-controller.groundContact.position);
            float legPhase=Mathf.Repeat(phase+(leg.binding.tripodA?0f:.5f),1f);bool swing=legPhase>=.5f;
            if(swing && !leg.wasSwing)
            {
                leg.swingStart=leg.planted;
                Vector3 desired=rest+(footVelocity.sqrMagnitude>.000001f?footVelocity.normalized:Vector3.zero)*strideAmplitude;
                leg.swingEnd=Ground(desired,out var ground)?ground:leg.planted;
            }
            if(!swing && leg.wasSwing)leg.planted=leg.swingEnd;
            leg.wasSwing=swing;
            var goal=leg.planted;
            if(swing)
            {float t=(legPhase-.5f)*2f;goal=Vector3.Lerp(leg.swingStart,leg.swingEnd,Mathf.SmoothStep(0f,1f,t))+Vector3.up*Mathf.Sin(t*Mathf.PI)*legLift;}
            leg.goal=Vector3.Lerp(rest,goal,blend);
            Solve(leg,leg.goal);
        }
        // Never let visual leg bounds expand beyond the movement system's conservative footprint.
        for(int attempt=0;attempt<5 && !WithinFootprint();attempt++)
            foreach(var leg in runtime)
            {var rest=visualModel.TransformPoint(leg.binding.foot);leg.goal=Vector3.Lerp(rest,leg.goal,.5f);Solve(leg,leg.goal);}
        if(!WithinFootprint()) foreach(var leg in runtime)leg.hip.localRotation=leg.knee.localRotation=leg.ankle.localRotation=Quaternion.identity;
        float bob=bodyBobHeight*blend*Mathf.Sin(phase*Mathf.PI*4f);
        Show(true);
        foreach(var copy in bodyCopies)
        {
            var source=copy.source.transform;copy.copy.transform.SetPositionAndRotation(source.position+Vector3.up*bob,source.rotation);
            copy.copy.transform.localScale=source.lossyScale/visualModel.lossyScale.x;
        }
        gaitStatus=target==0f?(blend<.001f?"Standing":"Settling to standing"):velocity.sqrMagnitude<.000025f?"Turning tripod gait":"Walking tripod gait (actual movement)";
    }
    private bool WithinFootprint()
    {
        var center=controller.groundContact.position;float radius=controller.safety.bodyRadius;
        foreach(var copy in copies)
        {
            var bounds=copy.copy.bounds;
            for(int i=0;i<8;i++)
            {
                var point=bounds.center+Vector3.Scale(bounds.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
                point.y=center.y;if((point-center).sqrMagnitude>radius*radius)return false;
            }
        }
        return true;
    }
    private void Solve(RuntimeLeg leg,Vector3 footGoal)
    {
        var b=leg.binding;leg.hip.localRotation=leg.knee.localRotation=leg.ankle.localRotation=Quaternion.identity;
        Vector3 hip=visualModel.TransformPoint(b.hip),restKnee=visualModel.TransformPoint(b.knee),restAnkle=visualModel.TransformPoint(b.ankle);
        Vector3 target=footGoal-visualModel.TransformVector(b.foot-b.ankle);
        float upper=Vector3.Distance(hip,restKnee),lower=Vector3.Distance(restKnee,restAnkle);
        Vector3 delta=target-hip;float distance=Mathf.Clamp(delta.magnitude,Mathf.Abs(upper-lower)+.0001f,upper+lower-.0001f);
        Vector3 direction=delta.sqrMagnitude>.000001f?delta.normalized:(restAnkle-hip).normalized;target=hip+direction*distance;
        Vector3 bend=Vector3.ProjectOnPlane(restKnee-hip,direction);
        if(bend.sqrMagnitude<.000001f)bend=Vector3.ProjectOnPlane(Vector3.up,direction);
        float along=(upper*upper-lower*lower+distance*distance)/(2f*distance);
        Vector3 knee=hip+direction*along+bend.normalized*Mathf.Sqrt(Mathf.Max(0f,upper*upper-along*along));
        leg.hip.rotation=Quaternion.FromToRotation(restKnee-hip,knee-hip)*visualModel.rotation;
        leg.knee.rotation=Quaternion.FromToRotation(restAnkle-restKnee,target-knee)*visualModel.rotation;
        leg.ankle.rotation=visualModel.rotation;
    }
    private void OnDisable()
    {
        if(rig!=null)Show(false);
        foreach(var copy in copies)if(copy.source!=null)copy.source.enabled=copy.wasEnabled;
        foreach(var copy in bodyCopies)if(copy.source!=null)copy.source.enabled=copy.wasEnabled;
        if(rig!=null){if(Application.isPlaying)Destroy(rig);else DestroyImmediate(rig);}
        rig=null;runtime=null;copies.Clear();bodyCopies.Clear();driving=false;
    }
}
