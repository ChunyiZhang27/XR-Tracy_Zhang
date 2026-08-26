using UnityEngine;
using TMPro;

public class SelectionUIController : MonoBehaviour
{
    [Header("Text")]
    [SerializeField]
    private TMP_Text selectedInsectName;

    [SerializeField]
    private TMP_Text selectionHint;


    [Header("Button")]
    [SerializeField]
    private GameObject confirmButton;


    public void ShowSelection(
        string insectName,
        bool canExplore
    )
    {
        selectedInsectName.text =
            insectName.ToUpper();


        if (canExplore)
        {
            selectionHint.text =
                "Ready to explore";

            confirmButton.SetActive(true);
        }
        else
        {
            selectionHint.text =
                "COMING SOON";

            confirmButton.SetActive(false);
        }
    }


    public void ClearSelection()
    {
        selectedInsectName.text =
            "SELECT AN INSECT";

        selectionHint.text =
            "Choose one to explore";

        confirmButton.SetActive(false);
    }
}