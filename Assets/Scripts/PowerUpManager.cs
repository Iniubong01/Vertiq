using UnityEngine;
using System.Collections;

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

    private void Awake()
    {
        Instance = this;
    }

    // Start is called on the frame when a script is enabled
    private void Start()
    {
        player = GameObject.Find("Player").GetComponent<Player>();
        // InvokeRepeating(nameof(SpawnPowerUps), spawnRate, spawnRate);
        // Line responsible for spawning the powerups, commented out as we are using a different approach
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
        shield.SetActive(true);
        // TODO: Any value can be inputed here, I am using 4
        // Guns a' blazing
        yield return new WaitForSeconds(duration);
        shield.SetActive(false);
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

    // BL means Button Link
    public void FullLivesBL()
    {
        ActivatePowerUp(PowerUpType.FullLives, powerUpDuration);
        Debug.Log($"Activated {PowerUpType.FullLives}");
    }

    public void FreezeTimeBL()
    {
        ActivatePowerUp(PowerUpType.FreezeTime, powerUpDuration);
        Debug.Log($"Activated {PowerUpType.FreezeTime}");
    }

    public void MultipleBulletsBL()
    {
        StartCoroutine(HandleMultipleBullets(powerUpDuration));
        Debug.Log($"Activated {PowerUpType.MultipleBullets}");
    }

    public void ShieldBL()
    {
        StartCoroutine(HandleShield(powerUpDuration));
        Debug.Log($"Activated {PowerUpType.Shield}");
    }
}


