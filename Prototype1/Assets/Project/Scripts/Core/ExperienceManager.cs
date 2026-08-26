using UnityEngine;
using UnityEngine.SceneManagement;

public class ExperienceManager : MonoBehaviour
{
    [SerializeField]
    private GameObject selectionZone;

    [SerializeField]
    private GameObject insectExploreZone;

    private void Start()
    {
        ShowSelection();
    }

    public void ShowSelection()
    {
        selectionZone.SetActive(true);
        insectExploreZone.SetActive(false);
    }

    public void StartInsectExploration()
    {
        selectionZone.SetActive(false);
        insectExploreZone.SetActive(true);

        Debug.Log("Entering insect exploration mode.");
    }

    public void RestartPrototype()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }
}