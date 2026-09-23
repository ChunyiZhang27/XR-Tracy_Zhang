using UnityEngine;

public class SFXManager : MonoBehaviour
{
    [Header("Audio Source")]
    [SerializeField]
    private AudioSource sfxSource;


    [Header("SFX Clips")]
    [SerializeField]
    private AudioClip wingFlapSFX;


    // =========================
    // WING FLAP
    // =========================

    public void PlayWingFlap()
    {
        if (sfxSource == null)
        {
            return;
        }

        if (wingFlapSFX == null)
        {
            return;
        }

        // PlayOneShot 不会修改 AudioSource 当前 clip
        // 很适合这种短暂动作音效
        sfxSource.PlayOneShot(
            wingFlapSFX
        );
    }
}