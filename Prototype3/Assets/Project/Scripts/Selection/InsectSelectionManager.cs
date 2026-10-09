using UnityEngine;

public class InsectSelectionManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField]
    private SelectionUIController selectionUI;

    private HoverScaleFeedback currentSelection;


    // =========================
    // SELECT INSECT
    // =========================

    public void SelectInsect(HoverScaleFeedback insect)
    {
        if (insect == null)
        {
            return;
        }

        // 如果之前已经选择了其他昆虫，
        // 先取消之前的选择状态
        if (currentSelection != null &&
            currentSelection != insect)
        {
            currentSelection.SetSelected(false);
        }

        // 保存新的选择
        currentSelection = insect;

        // 当前昆虫保持 Selected 放大状态
        currentSelection.SetSelected(true);


        Debug.Log(
            "Selected insect: " +
            currentSelection.InsectDisplayName
        );


        // 更新 Selection UI
        if (selectionUI != null)
        {
            selectionUI.ShowSelection(
                currentSelection.InsectDisplayName,
                currentSelection.AvailableInPrototype
            );
        }
        else
        {
            Debug.LogWarning(
                "SelectionUIController has not been assigned."
            );
        }
    }


    // =========================
    // CLEAR SELECTION
    // =========================

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