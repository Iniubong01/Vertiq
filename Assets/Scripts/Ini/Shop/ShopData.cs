using TMPro;
using UnityEngine;
using UnityEngine.Timeline;

public class ShopData : MonoBehaviour
{
    public static ShopData Instance { get; private set; }

    // Persistent Data
    [HideInInspector] public int coins;
    [HideInInspector] public int powerupShield;
    [HideInInspector] public int powerupMultipleBullets;
    [HideInInspector] public int powerupFreezeTime;
    [HideInInspector] public int powerupFullLives;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadData();
    }

    public void AddCoins(int amount)
    {
        coins += amount;
        SaveData();
    }

    public bool SpendCoins(int amount)
    {
        if (coins < amount) return false;

        coins -= amount;
        SaveData();
        return true;
    }

    public void AddPowerup(string id)
    {
        switch (id)
        {
            case "shield": powerupShield++; break;
            case "bullets": powerupMultipleBullets++; break;
            case "freezetime": powerupFreezeTime++; break;
            case "fulllives": powerupFullLives++; break;
        }

        SaveData();
    }

    public void UsePowerup(string id)
    {
        switch (id)
        {
            case "shield": if (powerupShield > 0) powerupShield--; break;
            case "bullets": if (powerupMultipleBullets > 0) powerupMultipleBullets--; break;
            case "freezetime": if (powerupFreezeTime > 0) powerupFreezeTime--; break;
            case "fulllives": if (powerupFullLives > 0) powerupFullLives--; break;
        }

        SaveData();
    }

    private void SaveData()
    {
        PlayerPrefs.SetInt("Coins", coins);
        PlayerPrefs.SetInt("PU_Shield", powerupShield);
        PlayerPrefs.SetInt("PU_MultipleBullets", powerupMultipleBullets);
        PlayerPrefs.SetInt("PU_FreezeTime", powerupFreezeTime);
        PlayerPrefs.SetInt("PU_FullLives", powerupFullLives);
        PlayerPrefs.Save();
    }

    private void LoadData()
    {
        coins = PlayerPrefs.GetInt("Coins", 0);
        powerupShield = PlayerPrefs.GetInt("PU_Shield", 0);
        powerupMultipleBullets = PlayerPrefs.GetInt("PU_MultipleBullets", 0);
        powerupFreezeTime = PlayerPrefs.GetInt("PU_FreezeTime", 0);
        powerupFullLives = PlayerPrefs.GetInt("PU_FullLives", 0);
    }
}