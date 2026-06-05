using UnityEngine;
using UnityEngine.SceneManagement;

public class DoorSceneLoader : MonoBehaviour
{
    [SerializeField] private string sceneName;

    private bool hasLoadedScene;

    private void OnTriggerEnter(Collider other)
    {
        TryLoadScene(other);
    }

    public void TryLoadScene(Collider other)
    {
        if (hasLoadedScene)
        {
            return;
        }

        if (other.GetComponentInParent<PlayerController>() == null)
        {
            return;
        }

        if (EnemyController.IsAnyEnemyAware())
        {
            return;
        }

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
