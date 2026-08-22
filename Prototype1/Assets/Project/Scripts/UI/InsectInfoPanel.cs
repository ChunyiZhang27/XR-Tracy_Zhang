using UnityEngine;
using TMPro;

public class InsectInfoPanel : MonoBehaviour
{
    [SerializeField]
    private GameObject infoPanel;

    [SerializeField]
    private TMP_Text titleText;

    [SerializeField]
    private TMP_Text descriptionText;

    public void ShowAntennaInfo()
    {
        titleText.text = "ANTENNA";

        descriptionText.text =
            "Antennae help the ladybird sense its surroundings, including smells and touch.";

        infoPanel.SetActive(true);
    }

    public void HideInfo()
    {
        infoPanel.SetActive(false);
    }
}