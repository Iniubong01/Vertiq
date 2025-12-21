using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class PowerUpManager : MonoBehaviour
{
    public static PowerUpManager Instance;

    [Header("Game State")]
    public bool shieldActive;
    private bool multipleBulletsActive;
    private bool freezeTimeActive;
    private bool fullLives;

    [Header("Spawning Settings")]
    public GameObject[] PowerUps;
    public float trajectoryVariance = 15f, spawnDistance = 12f, spawnRate;

    [SerializeField] private float powerUpDuration;
    private Player player;
    public GameObject shield, bullets;

    [Header("UI Buttons - Order them LEFT to RIGHT in Inspector")]
    [SerializeField] Button SButton;      // Shield (leftmost)
    [SerializeField] Button MBButton;     // Bullets (second from left)
    [SerializeField] Button FTButton;     // Freeze (third from left)
    [SerializeField] Button FLButton;     // Lives (rightmost)
    
    private List<Button> powerUpButtons; 

    [HideInInspector] public int shieldPUA, freezeTimePUA, multipleBulletsPUA, fullLivesPUA;

    [SerializeField] TextMeshProUGUI S_AmountText, FT_AmountText, MB_AmountText, FL_AmountText; 

    private int currentSelectionIndex = 0;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        player = GameObject.Find("Player").GetComponent<Player>();

        SButton.onClick.AddListener(ShieldBL);
        FTButton.onClick.AddListener(FreezeTimeBL);
        MBButton.onClick.AddListener(MultipleBulletsBL);
        FLButton.onClick.AddListener(FullLivesBL);

        // CORRECTED ORDER: Shield -> Bullets -> Freeze -> Lives (Left to Right)
        powerUpButtons = new List<Button> { SButton, MBButton, FTButton, FLButton };

        UpdateTexts();
        UpdateSelectionVisuals();
        
        Debug.Log("PowerUp Order (Left to Right): Shield, Bullets, Freeze, Lives");
    }

    public void Navigate(int direction)
    {
        currentSelectionIndex += direction;

        if (currentSelectionIndex >= powerUpButtons.Count) 
            currentSelectionIndex = 0;
        else if (currentSelectionIndex < 0) 
            currentSelectionIndex = powerUpButtons.Count - 1;

        UpdateSelectionVisuals();
        
        Debug.Log($"Selected: {powerUpButtons[currentSelectionIndex].name} (Index: {currentSelectionIndex})");
    }

    public int GetCurrentIndex()
    {
        return currentSelectionIndex;
    }

    public void TriggerSelectedPowerUp()
    {
        Button currentButton = powerUpButtons[currentSelectionIndex];

        if (currentButton.interactable)
        {
            currentButton.onClick.Invoke();
            Debug.Log($"Activated: {currentButton.name}");
        }
        else
        {
            Debug.Log($"PowerUp Empty: {currentButton.name}");
        }
    }

    private void UpdateSelectionVisuals()
    {
        for (int i = 0; i < powerUpButtons.Count; i++)
        {
            if (i == currentSelectionIndex)
            {
                powerUpButtons[i].transform.localScale = Vector3.one * 1.3f;
            }
            else
            {
                powerUpButtons[i].transform.localScale = Vector3.one;
            }
        }
    }

    public void ActivatePowerUp(PowerUpType type, float duration)
    {
        switch (type)
        {
            case PowerUpType.Shield:
                StartCoroutine(HandleShield(duration));
                break;
            case PowerUpType.MultipleBullets:
                StartCoroutine(HandleMultipleBullets(duration));
                break;
            case PowerUpType.FreezeTime:
                StartCoroutine(HandleTimeFreeze(duration));
                break;
            case PowerUpType.FullLives:
                StartCoroutine(HandleLiveAddition(duration));
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

    private IEnumerator HandleShield(float duration)
    {
        shieldActive = true;
        shield.SetActive(true);
        yield return new WaitForSeconds(duration);
        shieldActive = false;
        shield.SetActive(false);
    }

    private IEnumerator HandleMultipleBullets(float duration)
    {
        int currentPowerLevel = player.powerLevel;
        player.powerLevel = 4;
        bullets.SetActive(true);
        yield return new WaitForSeconds(duration);
        bullets.SetActive(false);
        player.powerLevel = currentPowerLevel;
    }

    private IEnumerator HandleTimeFreeze(float duration)
    {
        freezeTimeActive = true;
        yield return new WaitForSeconds(duration);
        freezeTimeActive = false;
    }

    private IEnumerator HandleLiveAddition(float duration)
    {
        fullLives = true;
        GameManager.Instance.SetLives(3);
        yield return new WaitForSeconds(duration);
        fullLives = false;
    }

    public bool IsMultipleBulletsActive => multipleBulletsActive;
    public bool IsFreezeTimeActive => freezeTimeActive;
    public bool IsFullLives => fullLives;

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

    private void Update()
    {
        SButton.interactable = ShopData.Instance.powerupShield > 0;
        FTButton.interactable = ShopData.Instance.powerupFreezeTime > 0;
        MBButton.interactable = ShopData.Instance.powerupMultipleBullets > 0;
        FLButton.interactable = ShopData.Instance.powerupFullLives > 0;
    }

    public void FullLivesBL()
    {
        if(ShopData.Instance.powerupFullLives <= 0) return;
        
        ActivatePowerUp(PowerUpType.FullLives, powerUpDuration);
        ShopData.Instance.UsePowerup("fulllives");
        FL_AmountText.text = ShopData.Instance.powerupFullLives.ToString();
    }

    public void FreezeTimeBL()
    {
        if(ShopData.Instance.powerupFreezeTime <= 0) return;

        ShopData.Instance.UsePowerup("freezetime");
        ActivatePowerUp(PowerUpType.FreezeTime, powerUpDuration);
        FT_AmountText.text = ShopData.Instance.powerupFreezeTime.ToString();
    }

    public void MultipleBulletsBL()
    {
        if(ShopData.Instance.powerupMultipleBullets <= 0) return;

        ShopData.Instance.UsePowerup("bullets");
        StartCoroutine(HandleMultipleBullets(powerUpDuration));
        MB_AmountText.text = ShopData.Instance.powerupMultipleBullets.ToString();
    }

    public void ShieldBL()
    {
        if(ShopData.Instance.powerupShield <= 0) return;

        ShopData.Instance.UsePowerup("shield");
        StartCoroutine(HandleShield(powerUpDuration));
        S_AmountText.text = ShopData.Instance.powerupShield.ToString();
    }
}