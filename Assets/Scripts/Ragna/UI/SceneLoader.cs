using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

public class SceneLoader : MonoBehaviour
{
    // The singleton instance
    private static SceneLoader _instance;

    [Header("UI References")]
    [SerializeField] private GameObject loadingCanvas; 
    [SerializeField] private Slider progressBar;       
    [SerializeField] private TextMeshProUGUI progressText;

    private void Awake()
    {
        // Ensure only one exists
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Hide initially
        if(loadingCanvas) loadingCanvas.SetActive(false);
    }

    // --- THE MAGIC METHOD ---
    // --- THE FIXED METHOD ---
    public static void LoadScene(string sceneName)
    {
        // 1. Check if the Loading Screen already exists
        if (_instance == null)
        {
            GameObject prefab = Resources.Load<GameObject>("LoadingScreenPrefab");
            
            if (prefab == null)
            {
                Debug.LogError("CRITICAL: Missing 'LoadingScreenPrefab' in Resources folder!");
                SceneManager.LoadScene(sceneName);
                return;
            }

            GameObject instanceGO = Instantiate(prefab);
            instanceGO.name = "LoadingScreen_System";
            // _instance is set in Awake(), which runs immediately upon Instantiate
        }

        // 2. CRITICAL FIX: Ensure the GameObject is Active BEFORE starting the Coroutine
        // If we don't do this, Unity throws the "Coroutine couldn't be started" error.
        if (_instance.loadingCanvas != null)
        {
            _instance.loadingCanvas.SetActive(true);
        }
        
        // Ensure the root object itself is active just in case
        _instance.gameObject.SetActive(true);

        // 3. Now it is safe to start the Coroutine
        _instance.StartCoroutine(_instance.LoadSceneAsync(sceneName));
    }

    private IEnumerator LoadSceneAsync(string sceneName)
    {
        // Safety: Ensure objects exist before using them
        if(loadingCanvas != null) loadingCanvas.SetActive(true);
        if(progressBar != null) progressBar.value = 0;
        
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = false;

        while (!operation.isDone)
        {
            float progress = Mathf.Clamp01(operation.progress / 0.9f);
            
            // FIX: Check if progressBar is null before setting it
            if (progressBar != null) 
            {
                progressBar.value = progress;
            }
            else
            {
                // If the UI was destroyed, stop the coroutine to prevent error loop
                yield break; 
            }

            if (progressText != null) 
                progressText.text = (progress * 100).ToString("F0") + "%";

            if (operation.progress >= 0.9f)
            {
                yield return new WaitForSeconds(0.5f);
                operation.allowSceneActivation = true;
            }

            yield return null;
        }

        if(loadingCanvas != null) loadingCanvas.SetActive(false);
    }
}