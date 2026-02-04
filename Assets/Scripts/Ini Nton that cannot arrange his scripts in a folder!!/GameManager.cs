using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-1)]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Leaderboards")]
    [SerializeField] private DualLeaderboardManager leaderboardManager;

    [SerializeField] private Player player;
    [SerializeField] private ParticleSystem explosionEffect, enemyExplosionEffect;
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
    [HideInInspector] public bool playerDead;
    [SerializeField] Button shootButton;
    
    // We store the reference privately now
    private CanvasGroup gameOverCanvasGroup; 

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

        // --- AUTOMATICALLY GET CANVAS GROUP ---
        if (gameOverUI != null)
        {
            gameOverCanvasGroup = gameOverUI.GetComponent<CanvasGroup>();
            
            // Safety: If it's missing, add it automatically so the game doesn't break
            if (gameOverCanvasGroup == null)
            {
                gameOverCanvasGroup = gameOverUI.AddComponent<CanvasGroup>();
            }
            
            // Ensure it starts invisible and non-interactive
            gameOverCanvasGroup.alpha = 0f;
        }

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

        // --- FIX FOR NEW INPUT SYSTEM ---
        // Checks if keyboard exists AND if Enter was pressed
        if (lives <= 0 && (Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame))
        {
            NewGame();
        }

        shootButton.interactable = (!playerDead);
    }

    private void NewGame()
    {
        // Clear asteroids
        Asteroid[] asteroids = FindObjectsOfType<Asteroid>();
        for (int i = 0; i < asteroids.Length; i++)
            Destroy(asteroids[i].gameObject);

        gameOverUI.SetActive(false);
        
        // Reset Alpha for new game
        if (gameOverCanvasGroup != null) 
            gameOverCanvasGroup.alpha = 0f;

        currentTime = 0f;
        timerText.text = "0:00";

        playerDead = false;

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

        playerDead = false;
    }

    public void OnAsteroidDestroyed(Asteroid asteroid)
    {
        enemyExplosionEffect.transform.position = asteroid.transform.position;
        enemyExplosionEffect.Play();

        SoundManager.Instance.PlayEnemyDeathClip();

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

        playerDead = true;

        if (lives <= 0)
        {
            gameOverUI.SetActive(true);
            powerUpButtons.SetActive(false);
            
            if (gameOverCanvasGroup != null)
                StartCoroutine(FadeInGameOver());
                
            Debug.Log("Game Over!");

            // [UPDATED] Use the Singleton Instance instead of the inspector reference
            if (DualLeaderboardManager.Instance != null)
            {
                DualLeaderboardManager.Instance.SubmitScoreHybrid(score);
            }
            else
            {
                Debug.LogWarning("Leaderboard System is missing! Did you start from the Splash/Boot scene?");
            }
        }
        else
        {
            Invoke(nameof(Respawn), player.respawnDelay);
        }
    }

    // Coroutine to smoothly fade in the UI
    private IEnumerator FadeInGameOver()
    {
        float duration = 1.0f; // Time in seconds to fade in
        float elapsedTime = 0f;

        gameOverCanvasGroup.alpha = 0f; // Ensure it starts invisible

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            gameOverCanvasGroup.alpha = Mathf.Clamp01(elapsedTime / duration);
            yield return null;
        }

        gameOverCanvasGroup.alpha = 1f; // Ensure it ends fully visible
    }

    public void Restart()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        Debug.Log("Game Restart!");
        SoundManager.Instance.PlayButtonSound();
    }

    public void Pause()
    {
        SoundManager.Instance.PlayButtonSound();
        Time.timeScale = 0;
    }

    public void Continue()
    {
        Time.timeScale = 1;
        SoundManager.Instance.PlayButtonSound();
    }

    public void QuitGame()
    {
        Time.timeScale = 1;
        SoundManager.Instance.PlayButtonSound();
        Application.Quit();
    }

    public void Home()
    {
        Time.timeScale = 1;
        SoundManager.Instance.PlayButtonSound();
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