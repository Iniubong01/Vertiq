using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class PowerUpManager : MonoBehaviour
{
    public static PowerUpManager Instance;

    /// <summary>
    /// Fires true when freeze starts, false when it ends.
    /// Asteroid.cs subscribes to this instead of polling per frame — zero Update() cost.
    /// </summary>
    public static event System.Action<bool> OnFreezeChanged;

    [Header("Game State")]
    public bool shieldActive;
    private bool multipleBulletsActive;
    private bool freezeTimeActive;
    private bool fullLives;
    
    // Track pause state locally
    private bool isGamePaused = false; 

    [Header("Spawning Settings")]
    public GameObject[] PowerUps;
    public float trajectoryVariance = 15f, spawnDistance = 12f, spawnRate;

    [SerializeField] private float powerUpDuration;
    private Player player;

    [Header("UI Buttons")]
    [SerializeField] Button SButton;      
    [SerializeField] Button MBButton;     
    [SerializeField] Button FTButton;     
    [SerializeField] Button FLButton;     
    
    private List<Button> powerUpButtons; 

    [HideInInspector] public int shieldPUA, freezeTimePUA, multipleBulletsPUA, fullLivesPUA;

    [SerializeField] TextMeshProUGUI S_AmountText, FT_AmountText, MB_AmountText, FL_AmountText; 

    private int currentSelectionIndex = 0;
    private float lastNavigationTime = 0f;
    private const float NAVIGATION_COOLDOWN = 0.25f; // 250ms between navigation inputs

    // Track active Coroutines to prevent overlaps
    private Coroutine shieldCoroutine;
    private Coroutine mbCoroutine;
    private Coroutine freezeCoroutine;
    private Coroutine livesCoroutine;

    // Store original values safely at the class level
    private int originalPowerLevel; 

    // FIX: Track if a power-up button was just clicked to prevent pause conflicts
    private bool powerUpJustActivated = false;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // Safety check
        GameObject pObj = GameObject.FindGameObjectWithTag("Player");

        if (pObj != null) player = pObj.GetComponent<Player>();
        else Debug.LogError("[PowerUpManager] Player object not found!");

        // FIX: Add listeners that mark when power-ups are activated
        SButton.onClick.AddListener(() => { MarkPowerUpActivation(); ShieldBL(); });
        FTButton.onClick.AddListener(() => { MarkPowerUpActivation(); FreezeTimeBL(); });
        MBButton.onClick.AddListener(() => { MarkPowerUpActivation(); MultipleBulletsBL(); });
        FLButton.onClick.AddListener(() => { MarkPowerUpActivation(); FullLivesBL(); });

        powerUpButtons = new List<Button> { SButton, MBButton, FTButton, FLButton };

        UpdateTexts();
        UpdateSelectionVisuals();
    }

    // NEW: Mark that a power-up was just used (prevents pause conflicts)
    private void MarkPowerUpActivation()
    {
        powerUpJustActivated = true;
        StartCoroutine(ClearPowerUpActivationFlag());
    }

    private IEnumerator ClearPowerUpActivationFlag()
    {
        // Wait a frame to ensure button click is fully processed
        yield return new WaitForEndOfFrame();
        yield return new WaitForSeconds(0.1f); // Small delay to prevent any race conditions
        powerUpJustActivated = false;
    }

    // NEW: Public getter so PauseManager can check this
    public bool WasPowerUpJustActivated() => powerUpJustActivated;

    public void SetPausedState(bool paused)
    {
        isGamePaused = paused;
    }

    private void Update()
    {
        // [FIXED LOGIC]
        // Button is interactable ONLY if:
        // 1. Game is NOT paused
        // 2. You have stock (> 0)
        // 3. The powerup is NOT currently active (!isActive)
        
        SButton.interactable = !isGamePaused && ShopData.Instance.powerupShield > 0 && !shieldActive;
        
        FTButton.interactable = !isGamePaused && ShopData.Instance.powerupFreezeTime > 0 && !freezeTimeActive;
        
        MBButton.interactable = !isGamePaused && ShopData.Instance.powerupMultipleBullets > 0 && !multipleBulletsActive;
        
        FLButton.interactable = !isGamePaused && ShopData.Instance.powerupFullLives > 0 && !fullLives;
    }

    public void Navigate(int direction)
    {
        if (isGamePaused) return; // Don't navigate while paused
        
        // Cooldown check to prevent rapid navigation
        if (Time.unscaledTime - lastNavigationTime < NAVIGATION_COOLDOWN)
        {
            return; // Ignore this navigation input - too soon
        }
        
        lastNavigationTime = Time.unscaledTime;
        
        int startIndex = currentSelectionIndex;
        int attempts = 0;
        int maxAttempts = powerUpButtons.Count; // Prevent infinite loop
        
        do
        {
            currentSelectionIndex += direction;
            
            // Wrap around
            if (currentSelectionIndex >= powerUpButtons.Count) 
                currentSelectionIndex = 0;
            else if (currentSelectionIndex < 0) 
                currentSelectionIndex = powerUpButtons.Count - 1;
            
            attempts++;
            
            // If we've checked all buttons and wrapped back to start, stop
            if (attempts > maxAttempts)
            {
                //Debug.LogWarning("[PowerUpManager] All power-ups are disabled!");
                currentSelectionIndex = startIndex; // Stay on current selection
                break;
            }
            
        } while (!powerUpButtons[currentSelectionIndex].interactable);
        
        //Debug.Log($"[PowerUpManager] Navigated to powerup index: {currentSelectionIndex} ({powerUpButtons[currentSelectionIndex].name})");
        UpdateSelectionVisuals();
    }

    public int GetCurrentIndex() => currentSelectionIndex;

    public void TriggerSelectedPowerUp()
    {
        if (isGamePaused) 
        {
            //Debug.Log("[PowerUpManager] Cannot trigger powerup - game is paused");
            return;
        }
        
        if (currentSelectionIndex < 0 || currentSelectionIndex >= powerUpButtons.Count)
        {
            //Debug.LogWarning($"[PowerUpManager] Invalid selection index: {currentSelectionIndex}");
            return;
        }
        
        Button selectedButton = powerUpButtons[currentSelectionIndex];
        
        if (selectedButton.interactable)
        {
            //Debug.Log($"[PowerUpManager] Triggering powerup: {selectedButton.name}");
            selectedButton.onClick.Invoke();
        }
        else
        {
            //Debug.LogWarning($"[PowerUpManager] Selected powerup is not interactable: {selectedButton.name}");
        }
    }

    private void UpdateSelectionVisuals()
    {
        for (int i = 0; i < powerUpButtons.Count; i++)
        {
            if (i == currentSelectionIndex)
            {
                // Selected button - scale up and highlight
                powerUpButtons[i].transform.localScale = Vector3.one * 1.3f;
                
                // Optional: Change color to indicate selection
                /*var colors = powerUpButtons[i].colors;
                colors.normalColor = new Color(1f, 1f, 0.5f, 1f); // Yellow tint
                powerUpButtons[i].colors = colors;*/
            }
            else
            {
                // Not selected - normal scale and color
                powerUpButtons[i].transform.localScale = Vector3.one;
                
                // Reset to default color
                var colors = powerUpButtons[i].colors;
                colors.normalColor = Color.white;
                powerUpButtons[i].colors = colors;
            }
        }
    }

    // --- POWERUP HANDLERS ---

    // 1. MULTIPLE BULLETS
    public void MultipleBulletsBL()
    {
        if(ShopData.Instance.powerupMultipleBullets <= 0) return;

        ShopData.Instance.UsePowerup("bullets");
        MB_AmountText.text = ShopData.Instance.powerupMultipleBullets.ToString();

        // If active, stop the timer so we can restart it (Extending duration)
        if (mbCoroutine != null) StopCoroutine(mbCoroutine);
        
        // Only save state if this is the FIRST click (not an extension)
        if (!multipleBulletsActive)
        {
            if(player != null) originalPowerLevel = player.powerLevel;
            multipleBulletsActive = true;
            if(player != null) player.powerLevel = 4 + player.bulletUpgradeValue;
            //? Todo: Fix this using player.cs
            // bullets.SetActive(true);
            player.SetBulletVisualActive();
        }

        // Restart timer
        mbCoroutine = StartCoroutine(ResetMultipleBullets(powerUpDuration));
    }

    private IEnumerator ResetMultipleBullets(float duration)
    {
        yield return new WaitForSeconds(duration);
        
        // Restore
        if(player != null) player.powerLevel = originalPowerLevel;
        //? Todo: Fix this using player.cs
        // bullets.SetActive(false);
        player.SetBulletVisualInActive();
        multipleBulletsActive = false;
        mbCoroutine = null;
    }

    // 2. SHIELD
    public void ShieldBL()
    {
        if(ShopData.Instance.powerupShield <= 0) return;

        ShopData.Instance.UsePowerup("shield");
        S_AmountText.text = ShopData.Instance.powerupShield.ToString();

        if (shieldCoroutine != null) StopCoroutine(shieldCoroutine);

        if (!shieldActive)
        {
            shieldActive = true;
            //? Todo: Fix this using player.cs
            // shield.SetActive(true);
            player.SetShieldVisualActive();
        }

        shieldCoroutine = StartCoroutine(ResetShield(powerUpDuration));
    }

    private IEnumerator ResetShield(float duration)
    {
        yield return new WaitForSeconds(duration);
        shieldActive = false;
        //? Todo: Fix this using player.cs
        // shield.SetActive(false);
        player.SetShieldVisualInActive();
        shieldCoroutine = null;
    }

    // 3. FREEZE TIME
    public void FreezeTimeBL()
    {
        if(ShopData.Instance.powerupFreezeTime <= 0) return;

        ShopData.Instance.UsePowerup("freezetime");
        FT_AmountText.text = ShopData.Instance.powerupFreezeTime.ToString();

        if (freezeCoroutine != null) StopCoroutine(freezeCoroutine);
        
        freezeTimeActive = true;
        OnFreezeChanged?.Invoke(true);
        freezeCoroutine = StartCoroutine(ResetFreezeTime(powerUpDuration));
    }

    private IEnumerator ResetFreezeTime(float duration)
    {
        yield return new WaitForSeconds(duration);
        freezeTimeActive = false;
        OnFreezeChanged?.Invoke(false);
        freezeCoroutine = null;
    }

    // 4. FULL LIVES
    public void FullLivesBL()
    {
        if(ShopData.Instance.powerupFullLives <= 0) return;
        
        ShopData.Instance.UsePowerup("fulllives");
        FL_AmountText.text = ShopData.Instance.powerupFullLives.ToString();

        // Lives are instant, just apply them
        GameManager.Instance.SetLives(3);
        
        // Optional visual indicator duration
        if (livesCoroutine != null) StopCoroutine(livesCoroutine);
        livesCoroutine = StartCoroutine(ResetFullLivesIndicator(powerUpDuration));
    }

    private IEnumerator ResetFullLivesIndicator(float duration)
    {
        fullLives = true;
        yield return new WaitForSeconds(duration);
        fullLives = false;
        livesCoroutine = null;
    }

    // Getters
    public bool IsMultipleBulletsActive => multipleBulletsActive;
    public bool IsFreezeTimeActive => freezeTimeActive;
    public bool IsFullLives => fullLives;

    // --- Spawning Logic ---
    public void SpawnPowerUps()
    {
        float spawnPointX = Random.Range(-9, 9);
        float spawnPointY = Random.Range(-4, 4);
        Vector3 spawnPoint = new Vector3(spawnPointX, spawnPointY, 0);
        float variance = Random.Range(-trajectoryVariance, trajectoryVariance);
        Quaternion rotation = Quaternion.AngleAxis(variance, Vector3.forward);

        int index = Random.Range(0, PowerUps.Length);
        Instantiate(PowerUps[index], spawnPoint, rotation);
    }

    // --- External Activator (For pickups on map) ---
    public void ActivatePowerUp(PowerUpType type, float duration)
    {
        // Route external pickups through the same safe logic
        switch (type)
        {
            case PowerUpType.Shield:
                if (shieldCoroutine != null) StopCoroutine(shieldCoroutine);
                if (!shieldActive) { shieldActive = true; player.SetShieldVisualActive(); }
                shieldCoroutine = StartCoroutine(ResetShield(duration));
                break;

            case PowerUpType.MultipleBullets:
                if (mbCoroutine != null) StopCoroutine(mbCoroutine);
                if (!multipleBulletsActive) {
                    if(player != null) originalPowerLevel = player.powerLevel;
                    multipleBulletsActive = true;
                    if(player != null) player.powerLevel = 4;
                    player.SetBulletVisualActive();
                    
                }
                mbCoroutine = StartCoroutine(ResetMultipleBullets(duration));
                break;

            case PowerUpType.FreezeTime:
                if (freezeCoroutine != null) StopCoroutine(freezeCoroutine);
                freezeTimeActive = true;
                OnFreezeChanged?.Invoke(true);
                freezeCoroutine = StartCoroutine(ResetFreezeTime(duration));
                break;

            case PowerUpType.FullLives:
                GameManager.Instance.SetLives(3);
                break;
        }
    }

    void UpdateTexts()
    {
        S_AmountText.text = ShopData.Instance.powerupShield.ToString();
        FT_AmountText.text = ShopData.Instance.powerupFreezeTime.ToString();
        MB_AmountText.text = ShopData.Instance.powerupMultipleBullets.ToString();
        FL_AmountText.text = ShopData.Instance.powerupFullLives.ToString();
    }
}