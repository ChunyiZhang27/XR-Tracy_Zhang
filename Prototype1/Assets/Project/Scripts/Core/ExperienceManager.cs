using UnityEngine;
using UnityEngine.SceneManagement;

public class ExperienceManager : MonoBehaviour
{
    [Header("Experience Zones")]
    [SerializeField]
    private GameObject onboardingZone;

    [SerializeField]
    private GameObject selectionZone;

    [SerializeField]
    private GameObject insectExploreZone;


    [Header("Audio")]
    [SerializeField]
    private NarrationManager narrationManager;


    // 用于判断 Scene Reload 后
    // 是否应该跳过 Onboarding
    private static bool skipOnboardingAfterReload = false;


    private void Start()
    {
        // 如果是从 Explore 按 BACK TO INSECTS 回来的，
        // Reload Scene 后直接回 Selection。
        if (skipOnboardingAfterReload)
        {
            skipOnboardingAfterReload = false;

            ShowSelection();
        }
        else
        {
            // 正常第一次进入 Prototype
            ShowOnboarding();
        }
    }


    // =========================
    // ONBOARDING
    // =========================

    public void ShowOnboarding()
    {
        onboardingZone.SetActive(true);
        selectionZone.SetActive(false);
        insectExploreZone.SetActive(false);


        // 进入 Tiny Worlds 时播放欢迎介绍
        if (narrationManager != null)
        {
            narrationManager.PlayOpeningIntro();
        }
    }


    // =========================
    // START EXPERIENCE
    // START Button 调用
    // =========================

    public void StartExperience()
    {
        onboardingZone.SetActive(false);
        selectionZone.SetActive(true);
        insectExploreZone.SetActive(false);


        // 点击 START 后，
        // 播放昆虫选择页面的引导语音
        if (narrationManager != null)
        {
            narrationManager.PlaySelectionIntro();
        }
    }


    // =========================
    // SELECTION
    // =========================

    public void ShowSelection()
    {
        onboardingZone.SetActive(false);
        selectionZone.SetActive(true);
        insectExploreZone.SetActive(false);
    }


    // =========================
    // LADYBIRD EXPLORATION
    // =========================

    public void StartInsectExploration()
    {
        onboardingZone.SetActive(false);
        selectionZone.SetActive(false);
        insectExploreZone.SetActive(true);


        // 每次真正进入 Explore 时：
        // 1. 重置三个身体部位的 narration 状态
        // 2. 播放 Ladybird Explore 引导语音
        if (narrationManager != null)
        {
            narrationManager.ResetNarrations();
            narrationManager.PlayExploreIntro();
        }


        Debug.Log(
            "Entering insect exploration mode."
        );
    }


    // =========================
    // BACK TO INSECTS
    // =========================

    public void BackToSelection()
    {
        // Scene Reload 可以确保：
        // Elytra、Wing、Glow、Info Card 等状态全部重置。
        //
        // 但这次 Reload 后不显示 Onboarding，
        // 而是直接返回 Selection。

        skipOnboardingAfterReload = true;

        ReloadCurrentScene();
    }


    // =========================
    // COMPLETE TEST RESET
    // =========================

    public void RestartPrototype()
    {
        // 用于完全重新开始，例如下一位 tester。
        // Reload 后重新显示 Onboarding。

        skipOnboardingAfterReload = false;

        ReloadCurrentScene();
    }


    // =========================
    // SCENE RELOAD
    // =========================

    private void ReloadCurrentScene()
    {
        Scene currentScene =
            SceneManager.GetActiveScene();

        SceneManager.LoadScene(
            currentScene.name
        );
    }
}