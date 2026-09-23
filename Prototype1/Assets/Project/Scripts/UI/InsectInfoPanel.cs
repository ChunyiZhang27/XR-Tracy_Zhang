using System.Collections;
using UnityEngine;
using TMPro;

public class InsectInfoPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField]
    private GameObject infoPanel;

    [SerializeField]
    private TMP_Text titleText;

    [SerializeField]
    private TMP_Text descriptionText;


    [Header("Hide Settings")]
    [SerializeField]
    private float hideDelay = 0.25f;


    private Coroutine hideCoroutine;


    // =========================
    // ANTENNA
    // =========================

    public void ShowAntennaInfo()
    {
        CancelHide();

        titleText.text = "ANTENNA";

        descriptionText.text =
            "Sense touch and smell.";

        infoPanel.SetActive(true);
    }


    // =========================
    // ELYTRA
    // =========================

    public void ShowElytraInfo()
    {
        CancelHide();

        titleText.text = "ELYTRA";

        descriptionText.text =
            "Protect the flight wings.";

        infoPanel.SetActive(true);
    }


    // =========================
    // FLIGHT WINGS
    // =========================

    public void ShowWingInfo()
    {
        CancelHide();

        titleText.text = "FLIGHT WINGS";

        descriptionText.text =
            "Used for flight.";

        infoPanel.SetActive(true);
    }


    // =========================
    // DELAYED HIDE
    // =========================

    public void HideInfoDelayed()
    {
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
        }

        hideCoroutine =
            StartCoroutine(HideAfterDelay());
    }


    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(hideDelay);

        infoPanel.SetActive(false);

        hideCoroutine = null;
    }


    // =========================
    // IMMEDIATE HIDE
    // =========================

    public void HideInfo()
    {
        CancelHide();

        infoPanel.SetActive(false);
    }


    private void CancelHide()
    {
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);

            hideCoroutine = null;
        }
    }
}