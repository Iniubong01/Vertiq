using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkinManager : MonoBehaviour
{
    [Header("Player Skin")]
    [SerializeField]  GameObject [] outline;
    [SerializeField] Button [] skinButtons;
    [SerializeField] Image displayImage;
    [SerializeField] Sprite [] skinImage;

    [Header("Purchase Button Settings for Player Skins")]
    [SerializeField] Button purchaseButton;
    [SerializeField] Button deployButton;
    [SerializeField] TextMeshProUGUI priceText;
    [SerializeField] TextMeshProUGUI deployedText;

    [Header("Environment Skin")]
    [SerializeField]  GameObject [] envOutline;
    [SerializeField] Button [] environmentSkinButtons;
    [SerializeField] Image envDisplayImage;
    [SerializeField] Sprite [] environmentSkinImages;

    [Header("Purchase Button Settings for Environment Skins")]
    [SerializeField] Button envPurchaseButton;
    [SerializeField] Button envDeployButton;
    [SerializeField] TextMeshProUGUI envPriceText;
    [SerializeField] TextMeshProUGUI envDeployedText;

    [Header("Purchase Button Settings for Environment Skins")]
    [SerializeField] GameObject [] envDisplayParticles;

    const string PLAYER_SKIN_KEY = "SelectedPlayerSkin";
    const string ENV_SKIN_KEY = "SelectedEnvironmentSkin";

    int selectedPlayerSkinIndex = 0;
    int selectedEnvSkinIndex = 0;

    private string deployTextDefault = "Deploy"; // Default text for deployed status
    private string deployedTextDefault = "Deployed!"; // Default text for deployed status

    private void Awake()
    {
        disableOutline();
        envDisableOutline();
    }

    void Start()
    {
        deployButton.onClick.AddListener(DeployPlayerSkin);
        envDeployButton.onClick.AddListener(DeployEnvironmentSkin);

        for (int i = 0; i < skinButtons.Length; i++)
        {
            int index = i;
            skinButtons[i].onClick.AddListener(() => OnSelect(index));
            skinButtons[i].onClick.AddListener(() => OnSelectionChange(index));
        }

        for (int i = 0; i < environmentSkinButtons.Length; i++)
        {
            int index = i;
            environmentSkinButtons[i].onClick.AddListener(() => envOnSelect(index));
            environmentSkinButtons[i].onClick.AddListener(() => OnEnvironmentSelectionChange(index));
        }

        LoadSavedSelections();
    }

    void disableOutline()
    {
        foreach(var obj in outline)
        {
            obj.SetActive(false);
        }
    }

    void envDisableOutline()
    {
        foreach(var obj in envOutline)
        {
            obj.SetActive(false);
        }
    }

    #region Button Logic
    // For Player Skin
    private void OnSelect(int index)
    {
        selectedPlayerSkinIndex = index;

        disableOutline();
        outline[index].SetActive(true);
    }
    
    private void OnSelectionChange(int index)
    {
        // Todo: Add gameobjects showing the particle effects
        // envDisplayParticles[index].SetActive(true);

        displayImage.sprite = skinImage[index]; // Set display image to the selected skin
 
        // Grab component to access price and lock status
        var playerSkin = skinButtons[index].GetComponent<PlayerSkin>(); 

        priceText.text = playerSkin.price.ToString(); // Display the price of the skin

        playerSkin.isDeployed = PlayerPrefs.GetInt(PLAYER_SKIN_KEY, 0) == index; // Check if the currently selected skin is deployed
        deployedText.text = playerSkin.isDeployed ? deployedTextDefault : deployTextDefault;

        purchaseButton.interactable = !playerSkin.isLocked;
        purchaseButton.gameObject.SetActive(playerSkin.isLocked);
        deployButton.gameObject.SetActive(!playerSkin.isLocked);
    }

    // For Environment Skin
    private void envOnSelect(int index)
    {
        selectedEnvSkinIndex = index;

        envDisableOutline();
        envOutline[index].SetActive(true);
    }

    private void OnEnvironmentSelectionChange(int index)
    {
        envDisplayImage.sprite = environmentSkinImages[index]; // Set display image to the selected skin
 
        // Grab component to access price and lock status
        var playerSkin = environmentSkinButtons[index].GetComponent<PlayerSkin>(); 

        envPriceText.text = playerSkin.price.ToString(); // Display the price of the skin

        playerSkin.isDeployed = PlayerPrefs.GetInt(ENV_SKIN_KEY, 0) == index; // Check if the currently selected skin is deployed
        envDeployedText.text = playerSkin.isDeployed ? deployedTextDefault : deployTextDefault;
        
        envPurchaseButton.interactable = !playerSkin.isLocked;
        envPurchaseButton.gameObject.SetActive(playerSkin.isLocked);
        envDeployButton.gameObject.SetActive(!playerSkin.isLocked);
    }
    #endregion

    #region Button Deploy Logic/ PlayerPrefs Logic
    public void DeployPlayerSkin()
    {
        PlayerPrefs.SetInt(PLAYER_SKIN_KEY, selectedPlayerSkinIndex);
        PlayerPrefs.Save();
        Debug.Log("Player Skin Saved: " + selectedPlayerSkinIndex);

        deployedText.text = deployedTextDefault;
    }

    public void DeployEnvironmentSkin()
    {
        PlayerPrefs.SetInt(ENV_SKIN_KEY, selectedEnvSkinIndex);
        PlayerPrefs.Save();
        Debug.Log("Environment Skin Saved: " + selectedEnvSkinIndex);

        envDeployedText.text = deployedTextDefault;
    }

    void LoadSavedSelections()
    {
        // Player Skin
        int savedPlayerSkin = PlayerPrefs.GetInt(PLAYER_SKIN_KEY, 0);
        OnSelect(savedPlayerSkin);
        OnSelectionChange(savedPlayerSkin);

        // Environment Skin
        int savedEnvSkin = PlayerPrefs.GetInt(ENV_SKIN_KEY, 0);
        envOnSelect(savedEnvSkin);
        OnEnvironmentSelectionChange(savedEnvSkin);

        // Show outline for saved skin
        outline[savedPlayerSkin].SetActive(true);
        envOutline[savedEnvSkin].SetActive(true);
    }
    #endregion

    #region Purchase Logic
    public void PurchasePlayerSkin()
    {
        // Currency logic (If statement)
        // If (SolanaCoins >= playerSkin.price)
        for (int i = 0; i < skinButtons.Length; i++)
        {
            var playerSkin = skinButtons[i].GetComponent<PlayerSkin>();

            if (playerSkin.isLocked)
            {
                playerSkin.isLocked = false; // Unlock the skin
                OnSelectionChange(i); // Update UI after purchase
                Debug.Log("Player Skin Purchased: " + i);   
            }
            return;
        }
    }

    public void PurchaseEnvironmentSkin()
    {
        // Currency logic (If statement)
        // If (SolanaCoins >= playerSkin.price)
        for (int i = 0; i < environmentSkinButtons.Length; i++)
        {
            var playerSkin = environmentSkinButtons[i].GetComponent<PlayerSkin>();

            if (playerSkin.isLocked)
            {
                playerSkin.isLocked = false; // Unlock the skin
                OnSelectionChange(i); // Update UI after purchase
                Debug.Log("Player Skin Purchased: " + i);   
            }
            return;
        }
    }
    #endregion
}