using UnityEngine;

public class HoverScaleFeedback : MonoBehaviour
{
    [SerializeField]
    private string insectDisplayName;

    public string InsectDisplayName => insectDisplayName;

    [SerializeField]
    private float hoverScaleMultiplier = 1.25f;

    [SerializeField]
    private float selectedScaleMultiplier = 1.5f;

    private Vector3 originalScale;
    private bool isSelected = false;

    private InsectSelectionManager selectionManager;

    private void Awake()
    {
        originalScale = transform.localScale;

        selectionManager =
            FindFirstObjectByType<InsectSelectionManager>();
    }

    public void OnHoverEntered()
    {
        if (!isSelected)
        {
            transform.localScale =
                originalScale * hoverScaleMultiplier;
        }
    }

    public void OnHoverExited()
    {
        if (!isSelected)
        {
            transform.localScale = originalScale;
        }
    }

    public void OnSelected()
    {
        if (selectionManager != null)
        {
            selectionManager.SelectInsect(this);
        }
        else
        {
            Debug.LogWarning(
                "InsectSelectionManager was not found."
            );
        }
    }

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
            transform.localScale = originalScale;
        }
    }

    public void ResetSelection()
    {
        SetSelected(false);
    }
}