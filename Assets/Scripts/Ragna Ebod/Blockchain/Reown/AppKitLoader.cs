using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class AppKitLoader : MonoBehaviour
{
    [Header("Reference to the Disabled AppKit")]
    public GameObject appKitObject;

    [Header("Settings")]
    public float delayBeforeActivating = 2.0f; // Wait 2 seconds
    public string menuSceneName = "Splash";      // Name of your Menu scene

    private void Awake()
    {
        // 1. Force the Disabled AppKit to survive the scene change
        if (appKitObject != null)
        {
            DontDestroyOnLoad(appKitObject);
        }

        // 2. Force THIS script to survive too (so the timer keeps running)
        DontDestroyOnLoad(this.gameObject);
    }

    private IEnumerator Start()
    {
        // 3. Load the Menu Scene immediately
        SceneManager.LoadScene(menuSceneName);

        // 4. Wait while the player is looking at the Menu
        Debug.Log("[AppKitLoader] Waiting for Menu to settle...");
        yield return new WaitForSeconds(delayBeforeActivating);

        // 5. NOW enable the AppKit
        if (appKitObject != null)
        {
            Debug.Log("[AppKitLoader] Activating AppKit now!");
            appKitObject.SetActive(true);
        }

        // 6. Destroy this loader script since its job is done
        Destroy(this.gameObject);
    }
}