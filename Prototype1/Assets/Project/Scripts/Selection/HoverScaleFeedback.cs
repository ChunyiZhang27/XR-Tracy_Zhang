using UnityEngine;

public class HoverScaleFeedback : MonoBehaviour
{
    [Header("Insect Information")]
    [SerializeField]
    private string insectDisplayName;

    [SerializeField]
    private bool availableInPrototype = false;


    [Header("Scale Feedback")]
    [SerializeField]
    private float hoverScaleMultiplier = 1.25f;

    [SerializeField]
    private float selectedScaleMultiplier = 1.5f;


    private Vector3 originalScale;

    private bool isSelected = false;

    private InsectSelectionManager selectionManager;


    // =========================
    // PUBLIC PROPERTIES
    // =========================

    public string InsectDisplayName
    {
        get
        {
            return insectDisplayName;
        }
    }


    public bool AvailableInPrototype
    {
        get
        {
            return availableInPrototype;
        }
    }


    // =========================
    // UNITY
    // =========================

    private void Start()
    {
        // 记录昆虫最开始的大小
        originalScale = transform.localScale;


        // 自动寻找 Selection Manager
        selectionManager =
            FindFirstObjectByType<InsectSelectionManager>();


        if (selectionManager == null)
        {
            Debug.LogWarning(
                "InsectSelectionManager was not found in the scene."
            );
        }
    }


    // =========================
    // HOVER ENTER
    // =========================

    public void OnHoverEntered()
    {
        // 如果已经被正式选中了，
        // Hover 时不要改变它的 Selected 大小
        if (isSelected)
        {
            return;
        }


        transform.localScale =
            originalScale * hoverScaleMultiplier;
    }


    // =========================
    // HOVER EXIT
    // =========================

    public void OnHoverExited()
    {
        // 已选中的昆虫继续保持 Selected Scale
        if (isSelected)
        {
            return;
        }


        transform.localScale =
            originalScale;
    }


    // =========================
    // SELECT
    // =========================

    public void OnSelected()
    {
        if (selectionManager == null)
        {
            Debug.LogWarning(
                "Cannot select insect because InsectSelectionManager is missing."
            );

            return;
        }


        selectionManager.SelectInsect(this);
    }


    // =========================
    // SET SELECTED STATE
    // =========================

    public void SetSelected(bool selected)
    {
        isSelected = selected;


        if (isSelected)
        {
            transform.localScale =
                originalScale * selectedScaleMultiplier;
        }
        else
        {
            transform.localScale =
                originalScale;
        }
    }


    // =========================
    // RESET
    // =========================

    public void ResetSelection()
    {
        isSelected = false;

        transform.localScale =
            originalScale;
    }
}