using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
public sealed class ImportedAphidMaterialTests
{
    [Test] public void ImportedMaterialsRemainUnchangedThroughHoverFoundAndReplay()
    {
        const string path="Assets/Project/Models/FoodInsects/Aphids/green_peach_aphid_-_first_model.glb";
        var materials=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Material>().ToArray();
        Assert.GreaterOrEqual(materials.Length,4,"Expected the actual imported GLB materials.");
        var root=new GameObject("Imported material test");root.SetActive(false);
        try
        {
            var type=Assembly.Load("Assembly-CSharp").GetType("FoodSearchTarget",true);
            var target=root.AddComponent(type);
            var child=new GameObject("Imported multi-material visual",typeof(MeshRenderer));child.transform.SetParent(root.transform,false);
            var renderer=child.GetComponent<MeshRenderer>();renderer.sharedMaterials=materials;
            type.GetField("visuals").SetValue(target,new Renderer[]{renderer});
            var original=materials.Select(x=>x.GetColor("baseColorFactor")).ToArray();
            type.GetMethod("SetMouseHover").Invoke(target,new object[]{true});
            for(int i=0;i<materials.Length;i++)
            {
                Assert.AreEqual(original[i],materials[i].GetColor("baseColorFactor"));
                var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block,i);
                Assert.AreNotEqual(original[i],block.GetColor("baseColorFactor"));
            }
            type.GetMethod("SetMouseHover").Invoke(target,new object[]{false});
            type.GetMethod("SetFound").Invoke(target,new object[]{true});
            type.GetMethod("SetFound").Invoke(target,new object[]{false});
            for(int i=0;i<materials.Length;i++)
            {
                Assert.AreSame(materials[i],renderer.sharedMaterials[i]);
                Assert.AreEqual(original[i],materials[i].GetColor("baseColorFactor"));
                var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block,i);
                Assert.That(Vector4.Distance((Vector4)original[i],(Vector4)block.GetColor("baseColorFactor")),Is.LessThan(0.00001f),"Property-block color should return to the imported value within float precision.");
            }
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }
}
