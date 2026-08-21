using UnityEngine;

public class HoverScaleFeedback : MonoBehaviour
{
    [SerializeField]
    private float hoverScaleMultiplier = 1.25f;

    [SerializeField]
    private float selectedScaleMultiplier = 1.5f;

    private Vector3 originalScale;
    private bool isSelected = false;

    private void Awake()
    {
        originalScale = transform.localScale;
    }

    public void OnHoverEntered()
    {
        if (!isSelected)
        {
            transform.localScale = originalScale * hoverScaleMultiplier;
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
        isSelected = true;
        transform.localScale = originalScale * selectedScaleMultiplier;
    }

    public void ResetSelection()
    {
        isSelected = false;
        transform.localScale = originalScale;
    }
}