using UnityEngine;

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