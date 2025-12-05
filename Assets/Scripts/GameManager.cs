using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

[DefaultExecutionOrder(-1)]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private Player player;
    [SerializeField] private ParticleSystem explosionEffect;
    [SerializeField] private GameObject gameOverUI, powerUpButtons;
    [SerializeField] private Text livesText;

    // Score + lives
    public int score { get; private set; } = 0;
    public int lives = 3;

    [SerializeField] int TotalVortiqCoins = 500;

    // UI (Time, Best Time, Score, Best Score)
    [SerializeField] TextMeshProUGUI timerText, timeText;
    [SerializeField] TextMeshProUGUI BestTimeText;
    [SerializeField] TextMeshProUGUI scoreText, inGameScoreText;
    [SerializeField] TextMeshProUGUI bestScoreText;

    // Runtime tracking
    private float currentTime = 0f;
    private int bestScore = 0;
    private float bestTime = 99999f;  // Large default start

    private void Awake()
    {
        if (Instance != null)
        {
            DestroyImmediate(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Start()
    {
        // Load saved stats
        bestScore = PlayerPrefs.GetInt("BestScore", 0);
        bestTime = PlayerPrefs.GetFloat("BestTime", 99999f);

        bestScoreText.text = bestScore.ToString();
        BestTimeText.text = FormatTime(bestTime);

        NewGame();
    }

    private void Update()
    {
        // Timer runs only when alive
        if (lives > 0)
        {
            currentTime += Time.deltaTime;
            timerText.text = FormatTime(currentTime);
            timeText.text = FormatTime(currentTime);
        }

        if (lives <= 0 && Input.GetKeyDown(KeyCode.Return))
            NewGame();
    }

    private void NewGame()
    {
        // Clear asteroids
        Asteroid[] asteroids = FindObjectsOfType<Asteroid>();
        for (int i = 0; i < asteroids.Length; i++)
            Destroy(asteroids[i].gameObject);

        gameOverUI.SetActive(false);

        currentTime = 0f;
        timerText.text = "0:00";

        SetScore(0);
        SetLives(3);
        Respawn();
    }

    private void SetScore(int newScore)
    {
        score = newScore;
        scoreText.text = score.ToString();
        inGameScoreText.text = score.ToString();

        // Check highscore
        if (score > bestScore)
        {
            bestScore = score;
            bestScoreText.text = bestScore.ToString();
            PlayerPrefs.SetInt("BestScore", bestScore);

            // Best time only updates if this score beats previous highscore
            if (currentTime < bestTime)
            {
                bestTime = currentTime;
                BestTimeText.text = FormatTime(bestTime);
                PlayerPrefs.SetFloat("BestTime", bestTime);
            }

            PlayerPrefs.Save();
        }
    }

    public void SetLives(int liveSet)
    {
        lives = liveSet;
        livesText.text = lives.ToString();
    }

    private void Respawn()
    {
        player.transform.position = Vector3.zero;
        player.gameObject.SetActive(true);
    }

    public void OnAsteroidDestroyed(Asteroid asteroid)
    {
        explosionEffect.transform.position = asteroid.transform.position;
        explosionEffect.Play();

        if (asteroid.size < 0.7f)
        {
            SetScore(score + 100);     // small asteroid
        }
        else if (asteroid.size < 1.4f)
        {
            SetScore(score + 50);      // medium asteroid
        }
        else
        {
            SetScore(score + 25);      // large asteroid
        }
    }

    public void OnPlayerDeath(Player player)
    {
        player.gameObject.SetActive(false);

        explosionEffect.transform.position = player.transform.position;
        explosionEffect.Play();

        SoundManager.Instance.PlayDeathClip();

        SetLives(lives - 1);

        if (lives <= 0)
        {
            gameOverUI.SetActive(true);
            powerUpButtons.SetActive(false);
            Debug.Log("Game Over!");
        }
        else
        {
            Invoke(nameof(Respawn), player.respawnDelay);
        }
    }

    public void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        Debug.Log("Game Restart!");
    }

    public void Pause()
    {
        Time.timeScale = 0;
    }

    public void Continue()
    {
        Time.timeScale = 1;
    }

    public void QuitGame()
    {
        Time.timeScale = 1;
        Application.Quit();
    }

    public void Home()
    {
       StartCoroutine(LoadHome());
    }

    private IEnumerator LoadHome()
    {
        Time.timeScale = 1;
        yield return new WaitForSeconds(0.2f);
        SceneManager.LoadScene("Splash");
    }

    // Formats time as M:SS
    private string FormatTime(float t)
    {
        if (t >= 99999f)
            return "--:--";

        int minutes = Mathf.FloorToInt(t / 60f);
        int seconds = Mathf.FloorToInt(t % 60f);
        return $"{minutes}:{seconds:00}";
    }
}
