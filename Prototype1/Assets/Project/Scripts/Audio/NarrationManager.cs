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
    // BACK TO INSECTS
    // =========================

    [Header("Navigation Narration")]
    [SerializeField]
    private AudioClip backToInsectsNarration;


    // =========================
    // PLAYED STATE
    // =========================

    private bool antennaPlayed = false;
    private bool elytraPlayed = false;
    private bool wingPlayed = false;


    // =========================
    // PUBLIC STATE
    // 后面 ExperienceManager 会用它判断
    // Back narration 是否已经播放结束
    // =========================

    public bool IsNarrationPlaying
    {
        get
        {
            return narrationSource != null &&
                   narrationSource.isPlaying;
        }
    }


    // =========================
    // OPENING INTRO
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
    // BACK TO INSECTS
    // =========================

    public void PlayBackToInsectsNarration()
    {
        if (backToInsectsNarration == null)
        {
            return;
        }

        PlayNarration(
            backToInsectsNarration
        );
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


        if (narrationSource.isPlaying)
        {
            narrationSource.Stop();
        }


        narrationSource.clip = clip;

        narrationSource.Play();
    }


    // =========================
    // RESET BODY PART NARRATIONS
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