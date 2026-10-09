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

    private SFXManager sfxManager;

    private NarrationManager narrationManager;


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


        // 自动寻找 SFX Manager
        sfxManager =
            FindFirstObjectByType<SFXManager>();


        if (sfxManager == null)
        {
            Debug.LogWarning(
                "SFXManager was not found in the scene."
            );
        }


        // 自动寻找 Narration Manager
        narrationManager =
            FindFirstObjectByType<NarrationManager>();


        if (narrationManager == null)
        {
            Debug.LogWarning(
                "NarrationManager was not found in the scene."
            );
        }
    }


    // =========================
    // HOVER ENTER
    // =========================

    public void OnHoverEntered()
    {
        // 如果已经被正式选中了，
        // Hover 时不要改变 Selected 大小
        // 也不重复播放 Hover 音效
        if (isSelected)
        {
            return;
        }


        // Selection Hover 音效
        if (sfxManager != null)
        {
            sfxManager.PlaySelectionHover();
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


        // =========================
        // AVAILABLE INSECT
        // =========================

        if (availableInPrototype)
        {
            // 正常确认音
            if (sfxManager != null)
            {
                sfxManager.PlaySelectionSelect();
            }
        }


        // =========================
        // COMING SOON INSECT
        // =========================

        else
        {
            // 不可用提示音
            if (sfxManager != null)
            {
                sfxManager.PlaySelectionUnavailable();
            }


            // Coming Soon 语音
            if (narrationManager != null)
            {
                narrationManager.PlayComingSoonNarration();
            }
        }


        // 无论是否 Available，
        // 都继续交给原来的 Selection Manager。
        //
        // Available:
        // 正常选择。
        //
        // Unavailable:
        // 保留原来的 Coming Soon UI 行为。
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