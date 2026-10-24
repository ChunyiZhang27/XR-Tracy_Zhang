using UnityEngine;

// 身体部位的简单悬停光效开关；只切换指定光效对象，不决定学习进度。
public class HoverGlow : MonoBehaviour
{
    [SerializeField]
    private GameObject glowObject;

    private void Start()
    {
        if (glowObject != null)
        {
            glowObject.SetActive(false);
        }
    }

    public void ShowGlow()
    {
        if (glowObject != null)
        {
            glowObject.SetActive(true);
        }
    }

    public void HideGlow()
    {
        if (glowObject != null)
        {
            glowObject.SetActive(false);
        }
    }
}