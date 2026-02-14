using UnityEngine;
using UnityEngine.UI;

public class SkinLoader : MonoBehaviour
{
    [SerializeField] GameObject default_Player;
     [SerializeField] GameObject secondary_Player;
    [SerializeField] GameObject head_Player;
    [SerializeField] Sprite [] playerSkins;
    [SerializeField] GameObject [] environmentSkins;

    private int skinIndex;
    private int envSkinIndex;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        // Load the saved skin indices from PlayerPrefs, using default values of 0 if no saved data is found
        skinIndex = PlayerPrefs.GetInt("SelectedPlayerSkin", 0);
        envSkinIndex = PlayerPrefs.GetInt("SelectedEnvironmentSkin", 0);

        // Debug the loaded skin indices to verify that they are being retrieved correctly
        Debug.LogWarning(("Loaded Player Skin Index: " + skinIndex));
        Debug.LogWarning(("Loaded Environment Skin Index: " + envSkinIndex));

        // Apply the loaded skins to the player and environment based on the loaded indices
        // --------------------------------------------------------------------------------
        // If the skin index is between 0 and 3, it means we are using a default player skin,
        // so we activate the default player and set the sprite accordingly, while deactivating the head player.
        if (skinIndex == 0)
        {
            default_Player.SetActive(true);
            secondary_Player.SetActive(false);
            head_Player.SetActive(false);
        }

        // If the skin index is between 1 and 3, it means we are using a secondary player skin,
        // so we activate the secondary player and set the sprite accordingly, while deactivating the default player.
        // Because of different sprite sizes
        else if (skinIndex > 0 && skinIndex <= 3)
        {
            secondary_Player.SetActive(true);
            default_Player.SetActive(false);
            head_Player.SetActive(false);

            secondary_Player.GetComponent<SpriteRenderer>().sprite = playerSkins[skinIndex];
        }
        
        // If the skin index is 4 or higher, it means we are using a head skin,
        // so we activate the head player and set the sprite accordingly, while deactivating the default player.
        else if (skinIndex >= 4)
        {
            head_Player.SetActive(true);
            default_Player.SetActive(false);
            secondary_Player.SetActive(false);

            head_Player.GetComponent<SpriteRenderer>().sprite = playerSkins[skinIndex];
        }

        // We first deactivate all environment skins to ensure that only the selected one is active.
        foreach(var obj in environmentSkins)
        {
            obj.SetActive(false);
        }

        // If the environment skin index is valid, we activate the corresponding environment skin.
        if (envSkinIndex >= 0 && envSkinIndex < environmentSkins.Length)
        {
            environmentSkins[envSkinIndex].SetActive(true);
        }
    }  
}
