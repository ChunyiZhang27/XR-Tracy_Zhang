using UnityEngine;

public class SFXManager : MonoBehaviour
{
    [Header("Audio Source")]
    [SerializeField]
    private AudioSource sfxSource;


    // =========================
    // SELECTION SFX
    // =========================

    [Header("Selection SFX")]
    [SerializeField]
    private AudioClip selectionHoverSFX;

    [SerializeField]
    private AudioClip selectionSelectSFX;

    [SerializeField]
    private AudioClip selectionUnavailableSFX;


    // =========================
    // WING SFX
    // =========================

    [Header("Wing SFX")]
    [SerializeField]
    private AudioClip wingFlapSFX;


    // =========================
    // ELYTRA SFX
    // =========================

    [Header("Elytra SFX")]
    [SerializeField]
    private AudioClip elytraOpenSFX;

    [SerializeField]
    private AudioClip elytraCloseSFX;


    // =========================
    // SELECTION HOVER
    // =========================

    public void PlaySelectionHover()
    {
        if (sfxSource == null ||
            selectionHoverSFX == null)
        {
            return;
        }

        sfxSource.PlayOneShot(
            selectionHoverSFX
        );
    }


    // =========================
    // SELECTION CONFIRM
    // =========================

    public void PlaySelectionSelect()
    {
        if (sfxSource == null ||
            selectionSelectSFX == null)
        {
            return;
        }

        sfxSource.PlayOneShot(
            selectionSelectSFX
        );
    }


    // =========================
    // SELECTION UNAVAILABLE
    // =========================

    public void PlaySelectionUnavailable()
    {
        if (sfxSource == null ||
            selectionUnavailableSFX == null)
        {
            return;
        }

        sfxSource.PlayOneShot(
            selectionUnavailableSFX
        );
    }


    // =========================
    // WING FLAP
    // =========================

    public void PlayWingFlap()
    {
        if (sfxSource == null ||
            wingFlapSFX == null)
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
        if (sfxSource == null ||
            elytraOpenSFX == null)
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
        if (sfxSource == null ||
            elytraCloseSFX == null)
        {
            return;
        }

        sfxSource.PlayOneShot(
            elytraCloseSFX
        );
    }
}