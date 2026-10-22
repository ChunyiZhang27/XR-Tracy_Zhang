using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>One-time selection-only URP variants. No source importer edits or runtime components.</summary>
public static class Prototype2bSelectionDesaturation
{
    public const string Output = "Assets/Project/SelectionAppearance";
    public static Color Desaturate(Color srgb)
    {
        var linear=srgb.linear;
        float luminance=.2126f*linear.r+.7152f*linear.g+.0722f*linear.b;
        var result=Color.Lerp(new Color(luminance,luminance,luminance,linear.a),linear,.5f).gamma;
        result.a=srgb.a;return result;
    }
    static void Folder(string parent,string name)
    {
        if(!AssetDatabase.IsValidFolder(parent+"/"+name))AssetDatabase.CreateFolder(parent,name);
    }
    public static void ConfigureBatch()
    {
        if(!Application.isBatchMode)throw new InvalidOperationException("Run in an isolated Unity batch copy with graphics enabled.");
        if(SystemInfo.graphicsDeviceType==GraphicsDeviceType.Null)throw new InvalidOperationException("Texture baking requires graphics, not -nographics.");
        var scene=EditorSceneManager.OpenScene("Assets/Project/Scenes/Prototype2b.unity");
        var zone=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).Single(t=>t.name=="SelectionZone");
        var choices=zone.GetComponentsInChildren<HoverScaleFeedback>(true);
        var ladybird=choices.Single(t=>t.InsectDisplayName=="Seven-spot Ladybird");
        var protectedRenderers=new HashSet<Renderer>(ladybird.GetComponentsInChildren<Renderer>(true));
        Folder("Assets/Project","SelectionAppearance");Folder(Output,"Materials");Folder(Output,"Textures");
        int slots=0;
        foreach(var choice in choices.Where(t=>t!=ladybird))
        {
            string insect=choice.name.Replace("Selection","");
            int rendererIndex=0;
            foreach(var renderer in choice.GetComponentsInChildren<Renderer>(true).Where(r=>r is MeshRenderer||r is SkinnedMeshRenderer))
            {
                if(protectedRenderers.Contains(renderer))throw new InvalidOperationException("Ladybird overlap; nothing saved.");
                var materials=renderer.sharedMaterials;
                for(int slot=0;slot<materials.Length;slot++)
                {
                    var source=materials[slot];
                    if(source==null)continue;
                    if(AssetDatabase.GetAssetPath(source).StartsWith(Output+"/"))throw new InvalidOperationException("Variants already assigned; do not desaturate them twice.");
                    if(source.shader.name!="Universal Render Pipeline/Lit"||source.GetFloat("_Surface")!=0)
                        throw new InvalidOperationException("Unexpected shader/surface; inspect before conversion: "+source.name);
                    string name=$"{insect}_{rendererIndex:00}_{slot:00}_Sat50";
                    var variant=new Material(source){name=name};
                    bool cutout=source.GetFloat("_AlphaClip")>.5f;
                    bool preserveAlpha=cutout||source.GetFloat("_SmoothnessTextureChannel")>.5f;
                    var tint=source.GetColor("_BaseColor");tint.a=1f;
                    var texture=source.GetTexture("_BaseMap");
                    if(texture!=null)
                    {
                        var path=Output+"/Textures/"+name+".png";
                        Bake(texture,tint,preserveAlpha,path);
                        var baked=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                        variant.SetTexture("_BaseMap",baked);variant.SetTexture("_MainTex",baked);
                        variant.SetColor("_BaseColor",Color.white);variant.SetColor("_Color",Color.white);
                    }
                    else
                    {
                        var muted=Desaturate(tint);muted.a=1;
                        variant.SetColor("_BaseColor",muted);variant.SetColor("_Color",muted);
                    }
                    // Emission is black on all audited selection materials. Preserve all other PBR properties/maps.
                    if(source.GetColor("_EmissionColor").maxColorComponent>0)
                        throw new InvalidOperationException("Unexpected coloured emission; inspect before baking.");
                    variant.SetFloat("_Surface",0);variant.SetFloat("_SrcBlend",(float)BlendMode.One);
                    variant.SetFloat("_DstBlend",(float)BlendMode.Zero);variant.SetFloat("_ZWrite",1);
                    variant.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    variant.SetOverrideTag("RenderType",cutout?"TransparentCutout":"Opaque");
                    variant.renderQueue=cutout?(int)RenderQueue.AlphaTest:(int)RenderQueue.Geometry;
                    AssetDatabase.CreateAsset(variant,Output+"/Materials/"+name+".mat");
                    materials[slot]=variant;slots++;
                    Debug.Log("SELECTION SAT50 "+choice.name+"/"+renderer.name+": "+AssetDatabase.GetAssetPath(source)+" -> "+AssetDatabase.GetAssetPath(variant)+" cutout="+cutout);
                }
                renderer.sharedMaterials=materials;
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
                EditorUtility.SetDirty(renderer);rendererIndex++;
            }
        }
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        Debug.Log("SELECTION SAT50 saved "+slots+" material slots; Ladybird untouched; no runtime effect or transparency added.");
    }
    static void Bake(Texture texture,Color tint,bool preserveAlpha,string path)
    {
        var rt=RenderTexture.GetTemporary(texture.width,texture.height,0,RenderTextureFormat.ARGBFloat,RenderTextureReadWrite.Linear);
        var previous=RenderTexture.active;bool oldWrite=GL.sRGBWrite;
        var read=new Texture2D(texture.width,texture.height,TextureFormat.RGBAFloat,false,true);
        Texture2D output=null;
        try
        {
            GL.sRGBWrite=false;Graphics.Blit(texture,rt);RenderTexture.active=rt;
            read.ReadPixels(new Rect(0,0,texture.width,texture.height),0,0);read.Apply();
            var pixels=read.GetPixels();var tintLinear=tint.linear;
            for(int i=0;i<pixels.Length;i++)
            {
                var sampled=QualitySettings.activeColorSpace==ColorSpace.Linear?pixels[i]:pixels[i].linear;
                var c=sampled*tintLinear;float gray=.2126f*c.r+.7152f*c.g+.0722f*c.b;
                var muted=Color.Lerp(new Color(gray,gray,gray,c.a),c,.5f).gamma;
                muted.a=preserveAlpha?pixels[i].a:1f;pixels[i]=muted;
            }
            output=new Texture2D(texture.width,texture.height,TextureFormat.RGBA32,false,false);
            output.SetPixels(pixels);output.Apply();File.WriteAllBytes(path,output.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active=previous;GL.sRGBWrite=oldWrite;RenderTexture.ReleaseTemporary(rt);
            UnityEngine.Object.DestroyImmediate(read);if(output!=null)UnityEngine.Object.DestroyImmediate(output);
        }
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.sRGBTexture=true;importer.alphaSource=preserveAlpha?TextureImporterAlphaSource.FromInput:TextureImporterAlphaSource.None;
        importer.alphaIsTransparency=preserveAlpha;importer.mipmapEnabled=true;importer.isReadable=false;
        importer.wrapMode=texture.wrapMode;importer.filterMode=texture.filterMode;importer.anisoLevel=texture.anisoLevel;
        importer.maxTextureSize=Mathf.Max(texture.width,texture.height);
        importer.textureCompression=TextureImporterCompression.Compressed;importer.compressionQuality=80;
        importer.SaveAndReimport();
    }
}
