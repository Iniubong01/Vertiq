using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartGame : MonoBehaviour
{
    private Button startButton;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    private void Start()
    {
        startButton = GetComponent<Button>();
        startButton.onClick.AddListener(LoadGame);
    }
    
    private void LoadGame() { SceneManager.LoadScene("Game"); SoundManager.Instance.PlayButtonSound(); }
}
