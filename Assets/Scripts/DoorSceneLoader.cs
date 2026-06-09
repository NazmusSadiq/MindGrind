using UnityEngine;
using UnityEngine.SceneManagement;

public class DoorSceneLoader : MonoBehaviour
{
    [SerializeField] private string sceneName;
    private bool hasLoadedScene;

    public void LoadNextMinigameScene()
    {
        if (hasLoadedScene) return;

        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("DoorSceneLoader is missing a scene name.", this);
            return;
        }

        hasLoadedScene = true;

        Time.timeScale = 1f;

        SceneManager.LoadScene(sceneName);
        MainMenu.PauseGameAndShowDetailsPanel();
    }
}