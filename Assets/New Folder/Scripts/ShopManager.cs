using UnityEngine;
using UnityEngine.UI;


public class ShopManager : MonoBehaviour
{
    public ShopItem item;

    [SerializeField] private Image icon;
    [SerializeField] private Text costText;
    [SerializeField] private Button buyButton;

    private void Start()
    {
        icon.sprite = item.icon;
        costText.text = item.cost.ToString();
        buyButton.onClick.AddListener(TryBuy);
    }

    void TryBuy()
    {
        if (CurrencyManager.Instance.Spend(item.cost))
        {
            item.Bought();
            Debug.Log($"Bought {item.itemName}");
        }
        else
        {
            Debug.Log("Not enough coins!");
        }
    }
}


