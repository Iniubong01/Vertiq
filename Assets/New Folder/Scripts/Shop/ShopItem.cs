using UnityEngine;

[CreateAssetMenu(fileName = "ShopItem", menuName = "Shop/Item")]
public class ShopItem : ScriptableObject
{
    public string itemName;
    public int cost;

    public Sprite icon;

    // What happens when you buy the item
    public void Bought()
    {
        Debug.Log("This happens when bought!");
    }
}
