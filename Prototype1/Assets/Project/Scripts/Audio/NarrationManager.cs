using UnityEngine;

public class NarrationManager : MonoBehaviour
{
    [Header("Audio Source")]
    [SerializeField]
    private AudioSource narrationSource;


    [Header("Narration Clips")]
    [SerializeField]
    private AudioClip antennaNarration;

    [SerializeField]
    private AudioClip elytraNarration;

    [SerializeField]
    private AudioClip wingNarration;


    // =========================
    // PLAYED STATE
    // 每条 narration 每次运行只自动播放一次
    // =========================

    private bool antennaPlayed = false;
    private bool elytraPlayed = false;
    private bool wingPlayed = false;


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


        // 如果上一条 narration 还在播放，
        // 先停止，再播放当前知识点。
        if (narrationSource.isPlaying)
        {
            narrationSource.Stop();
        }


        narrationSource.clip = clip;

        narrationSource.Play();
    }


    // =========================
    // RESET
    // 之后如果我们想在重新进入 Explore 时
    // 允许 narration 再播放一次，可以调用这个函数。
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