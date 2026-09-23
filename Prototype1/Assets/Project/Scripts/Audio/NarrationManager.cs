using UnityEngine;

public class NarrationManager : MonoBehaviour
{
    [Header("Audio Source")]
    [SerializeField]
    private AudioSource narrationSource;


    // =========================
    // EXPERIENCE INTRO
    // =========================

    [Header("Experience Intro")]
    [SerializeField]
    private AudioClip openingIntroNarration;

    [SerializeField]
    private AudioClip selectionIntroNarration;


    // =========================
    // EXPLORE INTRO
    // =========================

    [Header("Explore Intro")]
    [SerializeField]
    private AudioClip exploreIntroNarration;


    // =========================
    // BODY PART NARRATIONS
    // =========================

    [Header("Body Part Narration Clips")]
    [SerializeField]
    private AudioClip antennaNarration;

    [SerializeField]
    private AudioClip elytraNarration;

    [SerializeField]
    private AudioClip wingNarration;


    // =========================
    // PLAYED STATE
    // 身体部位在一次 Explore 中只播放一次
    // =========================

    private bool antennaPlayed = false;
    private bool elytraPlayed = false;
    private bool wingPlayed = false;


    // =========================
    // OPENING INTRO
    // Tiny Worlds 一开始播放
    // =========================

    public void PlayOpeningIntro()
    {
        if (openingIntroNarration == null)
        {
            return;
        }

        PlayNarration(
            openingIntroNarration
        );
    }


    // =========================
    // SELECTION INTRO
    // 点击 START 进入 Selection 时播放
    // =========================

    public void PlaySelectionIntro()
    {
        if (selectionIntroNarration == null)
        {
            return;
        }

        PlayNarration(
            selectionIntroNarration
        );
    }


    // =========================
    // LADYBIRD EXPLORE INTRO
    // =========================

    public void PlayExploreIntro()
    {
        if (exploreIntroNarration == null)
        {
            return;
        }

        PlayNarration(
            exploreIntroNarration
        );
    }


    // =========================
    // ANTENNAE
    // =========================

    public void PlayAntennaNarration()
    {
        if (antennaPlayed)
        {
            return;
        }

        if (antennaNarration == null)
        {
            return;
        }

        PlayNarration(
            antennaNarration
        );

        antennaPlayed = true;
    }


    // =========================
    // ELYTRA
    // =========================

    public void PlayElytraNarration()
    {
        if (elytraPlayed)
        {
            return;
        }

        if (elytraNarration == null)
        {
            return;
        }

        PlayNarration(
            elytraNarration
        );

        elytraPlayed = true;
    }


    // =========================
    // FLIGHT WINGS
    // =========================

    public void PlayWingNarration()
    {
        if (wingPlayed)
        {
            return;
        }

        if (wingNarration == null)
        {
            return;
        }

        PlayNarration(
            wingNarration
        );

        wingPlayed = true;
    }


    // =========================
    // GENERAL PLAY FUNCTION
    // =========================

    private void PlayNarration(AudioClip clip)
    {
        if (narrationSource == null)
        {
            return;
        }


        // 如果上一条 narration 还在播放，
        // 停止它，再播放当前语音。
        if (narrationSource.isPlaying)
        {
            narrationSource.Stop();
        }


        narrationSource.clip = clip;

        narrationSource.Play();
    }


    // =========================
    // RESET BODY PART NARRATIONS
    // 每次进入 Ladybird Explore 时调用
    // =========================

    public void ResetNarrations()
    {
        antennaPlayed = false;
        elytraPlayed = false;
        wingPlayed = false;


        if (narrationSource != null &&
            narrationSource.isPlaying)
        {
            narrationSource.Stop();
        }
    }
}