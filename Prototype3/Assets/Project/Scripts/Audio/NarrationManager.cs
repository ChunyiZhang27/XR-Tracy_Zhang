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
    // SELECTION FEEDBACK
    // =========================

    [Header("Selection Feedback")]
    [SerializeField]
    private AudioClip comingSoonNarration;


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
    // LADYBIRD COMPLETION
    // =========================

    [Header("Ladybird Completion")]
    [SerializeField]
    private AudioClip ladybirdCompleteNarration;


    // =========================
    // NAVIGATION
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
    // COMING SOON
    // 未开放昆虫点击时调用
    // =========================

    public void PlayComingSoonNarration()
    {
        if (comingSoonNarration == null)
        {
            return;
        }

        PlayNarration(
            comingSoonNarration
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
    // LADYBIRD COMPLETE
    // =========================

    public void PlayLadybirdCompleteNarration()
    {
        if (ladybirdCompleteNarration == null)
        {
            return;
        }

        PlayNarration(
            ladybirdCompleteNarration
        );
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


        // 如果其他 narration 正在播放，
        // 用户当前主动触发的语音优先。
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