using TMPro;
using UnityEngine;

public class ExploreGuidanceUI : MonoBehaviour
{
    [Header("UI Reference")]
    [SerializeField]
    private TMP_Text guidanceText;


    private bool antennaDone = false;
    private bool elytraDone = false;
    private bool wingDone = false;


    // 每次重新进入 Explore 时重置提示
    private void OnEnable()
    {
        ResetGuidance();
    }


    // =========================
    // ANTENNA
    // =========================

    public void MarkAntennaExplored()
    {
        antennaDone = true;

        UpdateGuidance();
    }


    // =========================
    // ELYTRA
    // =========================

    public void MarkElytraExplored()
    {
        elytraDone = true;

        UpdateGuidance();
    }


    // =========================
    // FLIGHT WINGS
    // =========================

    public void MarkWingExplored()
    {
        wingDone = true;

        UpdateGuidance();
    }


    // =========================
    // RESET
    // =========================

    public void ResetGuidance()
    {
        antennaDone = false;
        elytraDone = false;
        wingDone = false;

        UpdateGuidance();
    }


    // =========================
    // UPDATE TEXT
    // =========================

    private void UpdateGuidance()
    {
        if (guidanceText == null)
        {
            return;
        }


        if (!antennaDone)
        {
            guidanceText.text =
                "Explore the antennae.\nPoint at them and press G.";

            return;
        }


        if (!elytraDone)
        {
            guidanceText.text =
                "Open the elytra.\nPoint at the red shell and press G.";

            return;
        }


        if (!wingDone)
        {
            guidanceText.text =
                "Explore the flight wings.\nPoint at a wing and press G.";

            return;
        }


        guidanceText.text =
            "Great! You explored the ladybird's anatomy.";
    }
}