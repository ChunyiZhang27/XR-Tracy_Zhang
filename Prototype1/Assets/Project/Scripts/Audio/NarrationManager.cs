using UnityEngine;

public class NarrationManager : MonoBehaviour
{
    [Header("Audio Source")]
    [SerializeField]
    private AudioSource narrationSource;


    [Header("Explore Intro")]
    [SerializeField]
    private AudioClip exploreIntroNarration;


    [Header("Body Part Narration Clips")]
    [SerializeField]
    private AudioClip antennaNarration;

    [SerializeField]
    private AudioClip elytraNarration;

    [SerializeField]
    private AudioClip wingNarration;


    // =========================
    // PLAYED STATE
    // 每个身体部位在一次 Explore 中只自动播放一次
    // =========================

    private bool antennaPlayed = false;
    private bool elytraPlayed = false;
    private bool wingPlayed = false;


    // =========================
    // EXPLORE INTRO
    // 每次进入 Explore 时可以调用
    // =========================

    public void PlayExploreIntro()
    {
        if (exploreIntroNarration == null)
        {
            return;
        }

        PlayNarration(exploreIntroNarration);
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

        PlayNarration(antennaNarration);

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

        PlayNarration(elytraNarration);

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

        PlayNarration(wingNarration);

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

        // 如果另一条 narration 还在播放，
        // 当前交互优先，停止上一条。
        if (narrationSource.isPlaying)
        {
            narrationSource.Stop();
        }

        narrationSource.clip = clip;
        narrationSource.Play();
    }


    // =========================
    // RESET
    // 每次重新进入 Explore 时调用
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