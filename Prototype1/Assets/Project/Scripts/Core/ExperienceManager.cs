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
    }


    // START Button 调用
    public void StartExperience()
    {
        onboardingZone.SetActive(false);
        selectionZone.SetActive(true);
        insectExploreZone.SetActive(false);
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