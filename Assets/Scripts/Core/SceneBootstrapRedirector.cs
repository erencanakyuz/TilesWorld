using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Attach to any scene that requires Bootstrap initialization.
/// If Bootstrap hasn't run, redirects to Bootstrap scene first.
/// </summary>
public class SceneBootstrapRedirector : MonoBehaviour
{
    [SerializeField] private string bootstrapSceneName = "Bootstrap";
    
    void Awake()
    {
        // Check if Bootstrap has already run by looking for GameManager
        if (GameManager.Instance == null)
        {
            Debug.Log($"[SceneBootstrapRedirector] Bootstrap not initialized. Redirecting to {bootstrapSceneName}...");
            
            // Store current scene to return after bootstrap
            string currentScene = SceneManager.GetActiveScene().name;
            PlayerPrefs.SetString("BootstrapReturnScene", currentScene);
            PlayerPrefs.Save();
            
            // Load Bootstrap scene
            SceneManager.LoadScene(bootstrapSceneName);
        }
        else
        {
            // Bootstrap already ran, destroy this redirector
            Destroy(gameObject);
        }
    }
}
