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


    private void Start()
    {
        ClearSelection();
    }


    // 选择管理器调用：显示昆虫名称，只给当前可探索昆虫显示确认按钮。
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


    // 首次启动或取消选择时恢复默认提示，隐藏确认按钮。
    public void ClearSelection()
    {
        selectedInsectName.text =
            "SELECT AN INSECT";

        selectionHint.text =
            "Choose one to explore";

        confirmButton.SetActive(false);
    }
}