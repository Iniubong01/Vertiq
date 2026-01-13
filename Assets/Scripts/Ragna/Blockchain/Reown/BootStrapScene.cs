using UnityEngine;
using UnityEngine.SceneManagement;

public class BootStrapScene : MonoBehaviour
{
    void Start()
    {
        // Load your Menu Scene immediately
        SceneManager.LoadScene("Splash"); // Replace "Menu" with your actual menu scene name
    }
}
