using UnityEngine;
using UnityEngine.UI;

public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager Instance;

    public Button [] powerUpButtons;

    public int Coins;
    public Text coinText;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public bool Spend(int amount)
    {
        if (Coins < amount) return false;
        Coins -= amount;
        return true;
    }

    public void AddCoins(int amount)
    {
        Coins += amount;
    }

    public void Update()
    {
        coinText.text = Coins.ToString();
    }
}
