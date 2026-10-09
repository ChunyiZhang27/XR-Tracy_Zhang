using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class NarrationManager : MonoBehaviour
{
    [Tooltip("Prototype2b opt-in: require uninterrupted playback before counting narration.")]
    public bool verifyPlaybackCompletion;
    private readonly HashSet<AudioClip> completedClips = new HashSet<AudioClip>();
    private Coroutine playbackMonitor;
    public bool BodyNarrationsComplete => ClipComplete(antennaNarration) && ClipComplete(elytraNarration) && ClipComplete(wingNarration);
    public bool LadybirdCompletionNarrationComplete => ClipComplete(ladybirdCompleteNarration);
    private bool ClipComplete(AudioClip clip) => clip == null || completedClips.Contains(clip);

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
        if (antennaPlayed && (!verifyPlaybackCompletion || ClipComplete(antennaNarration)))
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
        if (elytraPlayed && (!verifyPlaybackCompletion || ClipComplete(elytraNarration)))
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
        if (wingPlayed && (!verifyPlaybackCompletion || ClipComplete(wingNarration)))
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


        if (verifyPlaybackCompletion && playbackMonitor != null)
        { StopCoroutine(playbackMonitor); playbackMonitor = null; }

        // 如果其他 narration 正在播放，
        // 用户当前主动触发的语音优先。
        if (narrationSource.isPlaying)
        {
            narrationSource.Stop();
        }


        narrationSource.clip = clip;

        narrationSource.Play();
        if (verifyPlaybackCompletion) playbackMonitor = StartCoroutine(ObservePlayback(clip));
    }


    // =========================
    // RESET BODY PART NARRATIONS
    // =========================

    private IEnumerator ObservePlayback(AudioClip clip)
    {
        yield return null;
        // DSP time distinguishes natural playback from an externally stopped AudioSource.
        double expectedEnd = AudioSettings.dspTime + Mathf.Max(0f, clip.length - narrationSource.time) /
            Mathf.Max(0.01f, Mathf.Abs(narrationSource.pitch));
        while (narrationSource != null && narrationSource.clip == clip && narrationSource.isPlaying) yield return null;
        if (narrationSource != null && narrationSource.clip == clip && AudioSettings.dspTime >= expectedEnd - 0.05)
            completedClips.Add(clip);
        playbackMonitor = null;
    }

    public void ResetNarrations()
    {
        if (verifyPlaybackCompletion)
        {
            if (playbackMonitor != null) StopCoroutine(playbackMonitor);
            playbackMonitor = null;
            completedClips.Clear();
        }
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