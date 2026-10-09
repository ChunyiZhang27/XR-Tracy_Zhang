using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIButtonHoverGlow :
    MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [Header("References")]
    [SerializeField]
    private Outline hoverOutline;

    [SerializeField]
    private Image buttonImage;


    [Header("Hover Settings")]
    [SerializeField]
    private Color hoverTint =
        new Color(0.75f, 0.90f, 1.0f, 1.0f);


    private Color originalColor;


    private void Awake()
    {
        // 如果没有手动连接 Image，
        // 自动寻找当前 Button 上的 Image。
        if (buttonImage == null)
        {
            buttonImage = GetComponent<Image>();
        }


        // 如果没有手动连接 Outline，
        // 自动寻找当前 Button 上的 Outline。
        if (hoverOutline == null)
        {
            hoverOutline = GetComponent<Outline>();
        }


        if (buttonImage != null)
        {
            originalColor = buttonImage.color;
        }


        // 游戏开始时蓝色边缘隐藏
        if (hoverOutline != null)
        {
            hoverOutline.enabled = false;
        }
    }


    // =========================
    // RAY ENTER
    // =========================

    public void OnPointerEnter(
        PointerEventData eventData
    )
    {
        if (hoverOutline != null)
        {
            hoverOutline.enabled = true;
        }


        if (buttonImage != null)
        {
            buttonImage.color = hoverTint;
        }
    }


    // =========================
    // RAY EXIT
    // =========================

    public void OnPointerExit(
        PointerEventData eventData
    )
    {
        ResetVisual();
    }


    private void OnDisable()
    {
        ResetVisual();
    }


    private void ResetVisual()
    {
        if (hoverOutline != null)
        {
            hoverOutline.enabled = false;
        }


        if (buttonImage != null)
        {
            buttonImage.color = originalColor;
        }
    }
}