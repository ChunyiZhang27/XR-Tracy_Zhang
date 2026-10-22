using System.Reflection;
using NUnit.Framework;
using UnityEngine;
public class SelectionDesaturationTests
{
    Color Muted(Color c) => (Color)Assembly.Load("Assembly-CSharp-Editor").GetType("Prototype2bSelectionDesaturation",true)
        .GetMethod("Desaturate").Invoke(null,new object[]{c});
    float Luma(Color c) => .2126f*c.r+.7152f*c.g+.0722f*c.b;
    [Test] public void DesaturationHalvesLinearColourDifferencesAndPreservesLuminance()
    {
        var original=new Color(.9f,.2f,.1f,1);var a=original.linear;var b=Muted(original).linear;
        Assert.AreEqual((a.r-a.g)*.5f,b.r-b.g,1e-5f);
        Assert.AreEqual((a.g-a.b)*.5f,b.g-b.b,1e-5f);
        Assert.AreEqual(Luma(a),Luma(b),1e-5f);Assert.AreEqual(1,Muted(original).a);
    }
    [Test] public void GrayscaleRemainsUnchangedAndCutoutAlphaCanBePreserved()
    {
        var c=new Color(.45f,.45f,.45f,.37f);var b=Muted(c);
        Assert.AreEqual(c.r,b.r,1e-5f);Assert.AreEqual(c.g,b.g,1e-5f);Assert.AreEqual(c.a,b.a);
    }
}
