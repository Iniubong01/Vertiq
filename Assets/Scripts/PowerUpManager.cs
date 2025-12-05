using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TMPro;

public class PowerUpManager : MonoBehaviour
{
    public static PowerUpManager Instance;

    public bool shieldActive;
    private bool multipleBulletsActive;
    private bool freezeTimeActive;
    private bool fullLives;

    public GameObject[] PowerUps;   // Power ups gameobjects
    public float trajectoryVariance = 15f, spawnDistance = 12f, spawnRate;

    [SerializeField] private float powerUpDuration;
    private Player player;
    public GameObject shield, bullets;

    [SerializeField] Button SButton, FTButton, MBButton, FLButton; // Using initials of power up buttons
    [HideInInspector] public int shieldPUA, freezeTimePUA, multipleBulletsPUA, fullLivesPUA; // PUA means Power Up Amount

    [SerializeField] TextMeshProUGUI S_AmountText, FT_AmountText, MB_AmountText, FL_AmountText; 

    private void Awake()
    {
        Instance = this;
    }

    // Start is called on the frame when a script is enabled
    private void Start()
    {
        player = GameObject.Find("Player").GetComponent<Player>();
        SButton.onClick.AddListener(ShieldBL);
        FTButton.onClick.AddListener(FreezeTimeBL);
        MBButton.onClick.AddListener(MultipleBulletsBL);
        FLButton.onClick.AddListener(FullLivesBL);

        UpdateTexts();
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
        Debug.Log("Shield ON");
        shield.SetActive(true);
        yield return new WaitForSeconds(duration);
        shieldActive = false;
        shield.SetActive(false);
        Debug.Log("Shield OFF");
    }

    private IEnumerator HandleMultipleBullets(float duration)
    {
        int currentPowerLevel = player.powerLevel;
        player.powerLevel = 4;
        bullets.SetActive(true);
        // TODO: Any value can be inputed here, I am using 4
        // Guns a' blazing
        yield return new WaitForSeconds(duration);
        bullets.SetActive(false);
        player.powerLevel = currentPowerLevel;
    }

    private IEnumerator HandleTimeFreeze(float duration)
    {
        freezeTimeActive = true;
        // e.g. 
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

    // Read only accessors
    public bool IsMultipleBulletsActive => multipleBulletsActive;
    public bool IsFreezeTimeActive => freezeTimeActive;
    public bool IsFullLives => fullLives;


    public void SpawnPowerUps()
    {
        float spawnPointX = Random.Range(-9, 9);
        float spawnPointY = Random.Range(-4, 4);

        // Choose a random point between the x and y ranges
        Vector3 spawnPoint = new Vector3(spawnPointX, spawnPointY, 0);

        // Calculate a random variance in the asteroid's rotation which will cause its trajectory to change
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

    //? BL means Button Link
    public void FullLivesBL()
    {
        if(ShopData.Instance.powerupFullLives <= 0) return;
        
        UpdateTexts();
        ActivatePowerUp(PowerUpType.FullLives, powerUpDuration);
        ShopData.Instance.UsePowerup("fulllives");
        Debug.Log($"Activated {PowerUpType.FullLives}");
    }

    public void FreezeTimeBL()
    {
        if(ShopData.Instance.powerupFreezeTime <= 0) return;

        UpdateTexts();
        ShopData.Instance.UsePowerup("freezetime");
        ActivatePowerUp(PowerUpType.FreezeTime, powerUpDuration);
        Debug.Log($"Activated {PowerUpType.FreezeTime}");
    }

    public void MultipleBulletsBL()
    {
        if(ShopData.Instance.powerupMultipleBullets <= 0) return;

        UpdateTexts();
        ShopData.Instance.UsePowerup("bullets");
        StartCoroutine(HandleMultipleBullets(powerUpDuration));
        Debug.Log($"Activated {PowerUpType.MultipleBullets}");
    }

    public void ShieldBL()
    {
        if(ShopData.Instance.powerupShield <= 0) return;

        UpdateTexts();
        ShopData.Instance.UsePowerup("shield");
        StartCoroutine(HandleShield(powerUpDuration));
        Debug.Log($"Activated {PowerUpType.Shield}");
    }
}


