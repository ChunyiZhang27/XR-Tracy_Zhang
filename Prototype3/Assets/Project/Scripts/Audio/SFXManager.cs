using UnityEngine;

// 共用短音效播放器，使用 PlayOneShot；正确捕获音效由 FoodSearchActivity 的独立引用管理。
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

    // 以下入口由选择与身体交互调用；未配置音源或音效时安全跳过，不阻止交互。
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