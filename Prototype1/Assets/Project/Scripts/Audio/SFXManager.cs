using UnityEngine;

public class SFXManager : MonoBehaviour
{
    [Header("Audio Source")]
    [SerializeField]
    private AudioSource sfxSource;


    [Header("Wing SFX")]
    [SerializeField]
    private AudioClip wingFlapSFX;


    [Header("Elytra SFX")]
    [SerializeField]
    private AudioClip elytraOpenSFX;

    [SerializeField]
    private AudioClip elytraCloseSFX;


    // =========================
    // WING FLAP
    // =========================

    public void PlayWingFlap()
    {
        if (sfxSource == null || wingFlapSFX == null)
        {
            return;
        }

        sfxSource.PlayOneShot(
            wingFlapSFX
        );
    }


    // =========================
    // ELYTRA OPEN
    // =========================

    public void PlayElytraOpen()
    {
        if (sfxSource == null || elytraOpenSFX == null)
        {
            return;
        }

        sfxSource.PlayOneShot(
            elytraOpenSFX
        );
    }


    // =========================
    // ELYTRA CLOSE
    // =========================

    public void PlayElytraClose()
    {
        if (sfxSource == null || elytraCloseSFX == null)
        {
            return;
        }

        sfxSource.PlayOneShot(
            elytraCloseSFX
        );
    }
}