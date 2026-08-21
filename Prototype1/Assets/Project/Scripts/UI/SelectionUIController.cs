using UnityEngine;
using TMPro;

public class SelectionUIController : MonoBehaviour
{
    [SerializeField]
    private TMP_Text selectedInsectName;

    [SerializeField]
    private GameObject confirmButton;

    public void ShowSelection(string insectName)
    {
        selectedInsectName.text = insectName;

        confirmButton.SetActive(true);
    }

    public void ClearSelection()
    {
        selectedInsectName.text = "SELECT AN INSECT";

        confirmButton.SetActive(false);
    }
}