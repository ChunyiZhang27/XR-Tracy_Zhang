using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class FoodSearchActivityTests
{
    GameObject fixture, area, panel, completion, replay, returnPanel;
    Component returnButton, originalBackAction;
    Component activity, feedback, progress, hand;
    Component[] targets, grabs;
    Type Runtime(string name) => Assembly.Load("Assembly-CSharp").GetType(name,true);
    Type XR(string name) => Assembly.Load("Unity.XR.Interaction.Toolkit").GetType("UnityEngine.XR.Interaction.Toolkit."+name,true);
    void Set(object obj,string name,object value) => obj.GetType().GetField(name,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance).SetValue(obj,value);
    object Call(object obj,string name,params object[] args) => obj.GetType().GetMethod(name,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance).Invoke(obj,args);
    T Get<T>(object obj,string name) => (T)obj.GetType().GetProperty(name).GetValue(obj);
    Component Text(string name)
    {
        var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(fixture.transform,false);
        return go.AddComponent(Assembly.Load("Unity.TextMeshPro").GetType("TMPro.TextMeshProUGUI",true));
    }
    [SetUp] public void Setup()
    {
        fixture=new GameObject("Ten-catch fixture");fixture.SetActive(false);
        fixture.AddComponent(XR("XRInteractionManager"));
        area=new GameObject("Area");area.transform.SetParent(fixture.transform,false);
        panel=new GameObject("Search");panel.transform.SetParent(fixture.transform,false);
        completion=new GameObject("Completion");completion.transform.SetParent(fixture.transform,false);
        replay=new GameObject("Try Again");replay.transform.SetParent(completion.transform,false);
        hand=new GameObject("Synthetic controller").AddComponent(XR("Interactors.XRDirectInteractor"));hand.transform.SetParent(fixture.transform,false);
        activity=fixture.AddComponent(Runtime("FoodSearchActivity"));feedback=Text("Feedback");progress=Text("Progress");
        Set(activity,"insectArea",area);Set(activity,"searchPanel",panel);Set(activity,"completionPanel",completion);
        Set(activity,"tryAgainButton",replay);Set(activity,"feedbackText",feedback);Set(activity,"progressText",progress);
        returnPanel=new GameObject("Adjacent return card");returnPanel.transform.SetParent(fixture.transform,false);
        var uiButton=Assembly.Load("UnityEngine.UI").GetType("UnityEngine.UI.Button",true);
        returnButton=new GameObject("Back",typeof(RectTransform)).AddComponent(uiButton);returnButton.transform.SetParent(returnPanel.transform,false);
        originalBackAction=new GameObject("Original back action",typeof(RectTransform)).AddComponent(uiButton);originalBackAction.transform.SetParent(fixture.transform,false);
        Set(activity,"backToInsectsPanel",returnPanel);Set(activity,"backToInsectsButton",returnButton);Set(activity,"existingBackToInsectsAction",originalBackAction);
        targets=new Component[20];grabs=new Component[20];var typed=Array.CreateInstance(Runtime("FoodSearchTarget"),20);
        var category=Runtime("FoodSearchTarget").GetNestedType("IdentificationCategory");
        for(int i=0;i<20;i++)
        {
            var go=new GameObject("Name does not determine category "+i);go.transform.SetParent(area.transform,false);
            grabs[i]=go.AddComponent(XR("Interactables.XRGrabInteractable"));
            targets[i]=go.AddComponent(Runtime("FoodSearchTarget"));Set(targets[i],"activity",activity);
            Set(targets[i],"category",Enum.Parse(category,i<15?"Aphid":"Ant"));typed.SetValue(targets[i],i);
        }
        Set(activity,"targets",typed);fixture.SetActive(true);
        Call(activity,"EndSearch");Call(activity,"BeginSearch");
    }
    [TearDown] public void Cleanup() { UnityEngine.Object.DestroyImmediate(fixture); }
    object Args(int i,bool selected)
    {
        // Unit fixture supplies explicit live-selection state. Full manager/attach/release is covered in Play Mode validation.
        XR("Interactables.XRBaseInteractable").GetProperty("isSelected").SetValue(grabs[i],selected);
        var selectors=(IList)grabs[i].GetType().GetProperty("interactorsSelecting").GetValue(grabs[i]);selectors.Clear();if(selected)selectors.Add(hand);
        var args=Activator.CreateInstance(XR("SelectEnterEventArgs"));
        args.GetType().GetProperty("interactorObject",BindingFlags.Public|BindingFlags.Instance|BindingFlags.DeclaredOnly).SetValue(args,hand);
        args.GetType().GetProperty("interactableObject",BindingFlags.Public|BindingFlags.Instance|BindingFlags.DeclaredOnly).SetValue(args,grabs[i]);return args;
    }
    void Catch(int i) { Call(activity,"ConfirmGrab",targets[i],Args(i,true)); Args(i,false); }
    void ShowCompletion()
    {
        Call(activity,"CancelCompletion");
        var routine=(IEnumerator)Call(activity,"ShowCompletionAfterGrab");
        for(int i=0;i<5&&routine.MoveNext();i++) { }
    }
    [Test] public void RequiresTenUniqueConfirmedGrabsAndRejectsClicksOrInvalidEvents()
    {
        Assert.AreEqual("Aphids caught: 0 / 10",Get<string>(progress,"text"));
        Call(targets[0],"SelectTarget");Call(activity,"Identify",targets[0]);Call(activity,"ConfirmGrab",targets[0],Args(0,false));
        Assert.AreEqual(0,Get<int>(activity,"FoundCount"));
        for(int i=0;i<5;i++)Catch(i);
        Assert.AreEqual(5,Get<int>(activity,"FoundCount"));Assert.IsFalse(Get<bool>(activity,"Completed"));
        for(int i=5;i<9;i++)Catch(i);
        Assert.AreEqual(9,Get<int>(activity,"FoundCount"));Assert.IsFalse(Get<bool>(activity,"Completed"));
        Catch(0);Assert.AreEqual(9,Get<int>(activity,"FoundCount"));Catch(9);
        Assert.AreEqual(10,Get<int>(activity,"FoundCount"));Assert.IsTrue(Get<bool>(activity,"Completed"));
        Assert.AreEqual("Aphids caught: 10 / 10",Get<string>(progress,"text"));
        ShowCompletion();Assert.IsTrue(completion.activeSelf);Assert.IsFalse(panel.activeSelf);Assert.IsTrue(area.activeSelf);
    }
    [Test] public void IncorrectGrabsRemainAvailableAndDoNotReduceProgress()
    {
        Catch(0);Catch(15);Catch(15);Catch(19);
        Assert.AreEqual(1,Get<int>(activity,"FoundCount"));Assert.IsFalse(Get<bool>(targets[15],"Found"));Assert.IsTrue(targets[15].gameObject.activeSelf);
        Assert.AreEqual("Not quite! That's an ant. Look for small, soft-bodied aphids with pear-shaped bodies.",Get<string>(feedback,"text"));
    }
    [Test] public void CompletionDefersUiAndReplayClearsAllUniqueScores()
    {
        for(int i=0;i<9;i++)Catch(i);
        Call(activity,"ConfirmGrab",targets[9],Args(9,true));
        Call(activity,"TryAgain");Assert.AreEqual(10,Get<int>(activity,"FoundCount"));Assert.IsFalse(Get<bool>(activity,"CompletionVisible"));
        Args(9,false);ShowCompletion();Call(activity,"TryAgain");Call(activity,"TryAgain");
        Assert.AreEqual(0,Get<int>(activity,"FoundCount"));Assert.IsFalse(Get<bool>(activity,"Completed"));
        Assert.IsFalse(completion.activeSelf);Assert.IsTrue(area.activeSelf);Assert.IsTrue(panel.activeSelf);
        Assert.AreEqual("",Get<string>(feedback,"text"));
        foreach(var target in targets)Assert.IsFalse(Get<bool>(target,"Found"));
        Catch(0);Assert.AreEqual(1,Get<int>(activity,"FoundCount"));
        Call(activity,"EndSearch");Catch(1);Assert.AreEqual(0,Get<int>(activity,"FoundCount"));Assert.IsFalse(area.activeSelf);
    }
    [Test] public void FinishKeepsFinalCardAndScoresUntilExplicitMissionReplay()
    {
        var title=Text("Completion title"); Set(activity,"completionTitle",title);
        var finishObject=new GameObject("Finish",typeof(RectTransform));finishObject.transform.SetParent(completion.transform,false);
        var button=finishObject.AddComponent(Assembly.Load("UnityEngine.UI").GetType("UnityEngine.UI.Button",true));
        Set(activity,"finishExplorationButton",button);
        for(int i=0;i<10;i++)Catch(i);ShowCompletion();
        Call(activity,"FinishExploration");Call(activity,"FinishExploration");
        Assert.IsTrue(Get<bool>(activity,"Finished"));Assert.IsFalse(Get<bool>(activity,"Running"));
        Assert.IsTrue(completion.activeSelf);Assert.IsTrue(returnPanel.activeSelf);Assert.IsFalse(panel.activeSelf);Assert.IsFalse(area.activeSelf);
        Assert.AreEqual(10,Get<int>(activity,"FoundCount"));Assert.AreEqual("Aphids caught: 10 / 10",Get<string>(progress,"text"));
        Assert.AreEqual("Exploration Complete!",Get<string>(title,"text"));Assert.IsFalse(Get<bool>(button,"interactable"));
        Catch(0);Assert.AreEqual(10,Get<int>(activity,"FoundCount"));
        Call(activity,"TryAgain");Assert.IsFalse(Get<bool>(activity,"Finished"));Assert.IsTrue(Get<bool>(activity,"Running"));
        Assert.IsFalse(completion.activeSelf);Assert.IsFalse(returnPanel.activeSelf);Assert.IsTrue(panel.activeSelf);Assert.AreEqual(0,Get<int>(activity,"FoundCount"));
        Assert.IsTrue(Get<bool>(button,"interactable"));Assert.AreEqual("Well Done!",Get<string>(title,"text"));
    }
    [Test] public void IncorrectFeedbackIsThrottledAndReplayClearsCooldown()
    {
        Catch(15);Assert.AreEqual(Runtime("FoodSearchActivity").GetField("IncorrectFeedback").GetRawConstantValue(),Get<string>(feedback,"text"));
        feedback.GetType().GetProperty("text").SetValue(feedback,"Keep current feedback");
        Catch(15);Catch(19);Assert.AreEqual("Keep current feedback",Get<string>(feedback,"text"));
        Assert.AreEqual(0,Get<int>(activity,"FoundCount"));
        Call(activity,"EndSearch");Call(activity,"BeginSearch");Catch(19);
        Assert.AreEqual(Runtime("FoodSearchActivity").GetField("IncorrectFeedback").GetRawConstantValue(),Get<string>(feedback,"text"));
    }
    [Test] public void ReturnIsExplicitOneShotAndInvokesTheExistingActionAfterCleanup()
    {
        int calls=0;
        var click=originalBackAction.GetType().GetProperty("onClick").GetValue(originalBackAction);
        UnityEngine.Events.UnityAction listener=()=>calls++;
        click.GetType().GetMethod("AddListener").Invoke(click,new object[]{listener});
        Call(activity,"BackToInsects");Assert.AreEqual(0,calls);Assert.IsFalse(returnPanel.activeSelf);
        for(int i=0;i<10;i++)Catch(i);ShowCompletion();Assert.IsFalse(returnPanel.activeSelf);
        Call(activity,"FinishExploration");Assert.IsTrue(returnPanel.activeSelf);
        Call(activity,"BackToInsects");Call(activity,"BackToInsects");Call(activity,"TryAgain");
        Assert.AreEqual(10,Get<int>(activity,"FoundCount"));Assert.IsFalse(Get<bool>(returnButton,"interactable"));
        Call(activity,"StopAllCoroutines");
        var routine=(IEnumerator)Call(activity,"ReturnToInsectsRoutine");for(int i=0;i<5&&routine.MoveNext();i++){}
        Assert.AreEqual(1,calls);Assert.IsFalse(completion.activeSelf);Assert.IsFalse(returnPanel.activeSelf);Assert.IsFalse(area.activeSelf);Assert.IsFalse(panel.activeSelf);
        Assert.AreEqual(0,Get<int>(activity,"FoundCount"));Call(activity,"BackToInsects");Assert.AreEqual(1,calls);
    }
    [Test] public void AnyTenOfFifteenCompleteAndRemainingTargetsCannotScore()
    {
        // A non-contiguous set includes the newly added aphids and leaves five uncaught.
        foreach(int i in new[]{14,12,10,8,6,4,2,0,13,11}) Catch(i);
        Assert.IsTrue(Get<bool>(activity,"Completed"));Assert.AreEqual(10,Get<int>(activity,"FoundCount"));
        ShowCompletion();Assert.IsTrue(area.activeSelf);
        int uncaught=0;for(int i=0;i<15;i++)if(!Get<bool>(targets[i],"Found"))uncaught++;
        Assert.AreEqual(5,uncaught);Catch(1);Catch(15);Assert.AreEqual(10,Get<int>(activity,"FoundCount"));
        Call(activity,"TryAgain");Assert.AreEqual(0,Get<int>(activity,"FoundCount"));
        foreach(var target in targets) { Assert.IsTrue(target.gameObject.activeSelf);Assert.IsFalse(Get<bool>(target,"Found")); }
        for(int i=5;i<15;i++)Catch(i);Assert.IsTrue(Get<bool>(activity,"Completed"));
    }
    [Test] public void EveryAntIsIncorrect()
    {
        for(int i=15;i<20;i++)Catch(i);
        Assert.AreEqual(0,Get<int>(activity,"FoundCount"));
        for(int i=15;i<20;i++)Assert.IsFalse(Get<bool>(targets[i],"Found"));
    }
    [Test] public void MismatchedEventDoesNotScoreAnotherTarget()
    {
        var args=Args(0,true);Call(activity,"ConfirmGrab",targets[1],args);
        Assert.AreEqual(0,Get<int>(activity,"FoundCount"));Args(0,false);
    }
}
