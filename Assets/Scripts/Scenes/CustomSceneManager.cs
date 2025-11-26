using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class CustomSceneManager : MonoBehaviour
{
    void OnApplicationQuit()
    {
        DOTween.KillAll();

        if (Mirror.NetworkManager.singleton != null)
        {
            Mirror.NetworkManager.singleton.StopAllCoroutines();
            Mirror.NetworkManager.singleton.StopHost();
            Mirror.NetworkManager.singleton.StopClient();
        }
    }
    void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "MainMenu")
        {
            StartCoroutine(InitializeGameVarsUIAfterDelay());
        }
    }

    private System.Collections.IEnumerator InitializeGameVarsUIAfterDelay()
    {
        yield return new WaitForEndOfFrame();
        
        // Keresd meg a GameVarsUI-t a scene-ben
        GameVarsUI gameVarsUI = FindFirstObjectByType<GameVarsUI>();
        if (gameVarsUI != null && GameVars.Instance != null)
        {
            gameVarsUI.InitializeUI();
        }
    }

    public void LoadScene(string sceneName)
    {
        DOVirtual.DelayedCall(0.5f, () =>
        {
            DOTween.KillAll();

            if (sceneName == "MainMenu" && GameVars.Instance != null)
            {
                GameVars.Instance.ResetToDefaults();
            }

            if (IsSceneInBuildSettings(sceneName))
            {
                SceneManager.LoadScene(sceneName);
            }
            else
            {
                Debug.LogError($"Scene \"{sceneName}\" not found in build settings! Returning to Main Menu!");
                SceneManager.LoadScene("MainMenu");
            }
        });
    }

    public void LoadScene(int sceneIndex)
    {
        DOTween.KillAll();

        string targetSceneName = SceneUtility.GetScenePathByBuildIndex(sceneIndex);
        if (System.IO.Path.GetFileNameWithoutExtension(targetSceneName) == "MainMenu" && GameVars.Instance != null)
        {
            GameVars.Instance.ResetToDefaults();
        }

        if (sceneIndex >= 0 && sceneIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(sceneIndex);
        }
        else
        {
            Debug.LogError($"Scene {sceneIndex} index out of range! Returning to Main Menu!");
            SceneManager.LoadScene("MainMenu");
        }
    }

    public void LoadGameSceneIfValid()
    {
        if (GameVars.Instance != null && GameVars.Instance.ValidateAndStartSinglePlayerGame())
        {
            LoadScene("Game");
        }
        else
        {
            // Validáció sikertelen - mutasd a warning-ot
            GameVarsUI gameVarsUI = FindFirstObjectByType<GameVarsUI>();
            if (gameVarsUI != null)
            {
                gameVarsUI.ShowWarning("JÁTÉKOS NEVE NEM LEHET ÜRES!");
            }
        }
    }

    public void ReloadCurrentScene()
    {
        DOTween.KillAll();
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.buildIndex);
    }

    private bool IsSceneInBuildSettings(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            string scene = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            if (scene == sceneName)
            {
                return true;
            }
        }
        return false;
    }

    public void ExitGame()
    {
        DOTween.KillAll();

        if (Mirror.NetworkManager.singleton != null)
        {
            Mirror.NetworkManager.singleton.StopAllCoroutines();
            Mirror.NetworkManager.singleton.StopHost();
            Mirror.NetworkManager.singleton.StopClient();
        }

        Application.Quit();
    }
}
