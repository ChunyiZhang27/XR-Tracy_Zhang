using System.Collections;
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

    [SerializeField]
    private float openingIntroDelay = 2f;


    // 用于判断 Scene Reload 后
    // 是否应该跳过 Onboarding
    private static bool skipOnboardingAfterReload = false;


    // 防止用户连续点击 Back
    private bool isReturningToSelection = false;


    // 保存 Opening Intro 的等待 Coroutine
    // 如果用户提前按 START，可以取消
    private Coroutine openingIntroCoroutine;


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


        // 如果之前有等待中的 Opening Intro，
        // 先停止，避免重复启动。
        if (openingIntroCoroutine != null)
        {
            StopCoroutine(
                openingIntroCoroutine
            );

            openingIntroCoroutine = null;
        }


        // BGM 会因为 Play On Awake 立即开始。
        // Opening narration 延迟播放。
        openingIntroCoroutine =
            StartCoroutine(
                PlayOpeningIntroAfterDelay()
            );
    }


    private IEnumerator PlayOpeningIntroAfterDelay()
    {
        // 先让 BGM 播放一小段时间
        yield return new WaitForSeconds(
            openingIntroDelay
        );


        if (narrationManager != null)
        {
            narrationManager.PlayOpeningIntro();
        }


        openingIntroCoroutine = null;
    }


    // =========================
    // START EXPERIENCE
    // START Button 调用
    // =========================

    public void StartExperience()
    {
        // 如果用户在 Opening Intro 开始前
        // 已经按下 START，
        // 就取消等待中的 Opening Intro。
        if (openingIntroCoroutine != null)
        {
            StopCoroutine(
                openingIntroCoroutine
            );

            openingIntroCoroutine = null;
        }


        onboardingZone.SetActive(false);
        selectionZone.SetActive(true);
        insectExploreZone.SetActive(false);


        // 点击 START 后，
        // 播放昆虫选择页面引导语音
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


        // 每次进入 Ladybird Explore：
        // 1. 重置身体部位 narration 状态
        // 2. 播放 Explore 引导语音
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
        // 防止连续点击 Back
        if (isReturningToSelection)
        {
            return;
        }


        StartCoroutine(
            BackToSelectionRoutine()
        );
    }


    private IEnumerator BackToSelectionRoutine()
    {
        isReturningToSelection = true;


        // =========================
        // PLAY BACK NARRATION
        // =========================

        if (narrationManager != null)
        {
            narrationManager.PlayBackToInsectsNarration();


            // 等一帧，让 AudioSource 真正开始播放
            yield return null;


            // 等 Back narration 完整播放结束
            while (narrationManager.IsNarrationPlaying)
            {
                yield return null;
            }
        }


        // =========================
        // RETURN TO SELECTION
        // =========================

        // Reload 后跳过 Onboarding，
        // 直接显示昆虫 Selection。
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