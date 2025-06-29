using UnityEngine;
using UnityEngine.SceneManagement;

public class Bootstrap : MonoBehaviour
{
    // Bu sahne, Build Settings'de MainScene'in bir üstündeki sahne olmalıdır.
    private const string TargetSceneName = "MainScene";

    void Start()
    {
        // Ana sahnenin zaten yüklü olup olmadığını kontrol et.
        // Bu, geliştirme sırasında Bootstrap sahnesinden değil de doğrudan başka bir sahneden başlarsak diye bir önlem.
        if (SceneManager.GetSceneByName(TargetSceneName).isLoaded)
        {
            Debug.LogWarning($"'{TargetSceneName}' sahnesi zaten yüklü. Bootstrap işlemi atlanıyor.");
            // İsteğe bağlı olarak, bu objeyi yok edebiliriz çünkü görevi tamamlandı.
            Destroy(gameObject);
            return;
        }

        // Ana sahneyi yükle
        LoadTargetScene();
    }

    private void LoadTargetScene()
    {
        // Debug.Log($"'{TargetSceneName}' sahnesi yükleniyor...");
        // Sahneyi asenkron olarak yükle, böylece oyun donmaz.
        SceneManager.LoadSceneAsync(TargetSceneName, LoadSceneMode.Additive);
    }
}