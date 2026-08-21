using UnityEngine;

public class InsectSelectionManager : MonoBehaviour
{
    [SerializeField]
    private SelectionUIController selectionUI;

    private HoverScaleFeedback currentSelection;

    public void SelectInsect(HoverScaleFeedback newSelection)
    {
        if (newSelection == null)
            return;

        if (currentSelection == newSelection)
            return;

        if (currentSelection != null)
        {
            currentSelection.SetSelected(false);
        }

        currentSelection = newSelection;
        currentSelection.SetSelected(true);

        if (selectionUI != null)
        {
            selectionUI.ShowSelection(
                currentSelection.InsectDisplayName
            );
        }

        Debug.Log(
            "Selected insect: " +
            currentSelection.InsectDisplayName
        );
    }

    public void ClearSelection()
    {
        if (currentSelection != null)
        {
            currentSelection.SetSelected(false);
            currentSelection = null;
        }

        if (selectionUI != null)
        {
            selectionUI.ClearSelection();
        }
    }
}