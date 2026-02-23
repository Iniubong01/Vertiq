using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkinManager : MonoBehaviour
{
    [Header("Marketplace Integration")]
    [SerializeField] private MarketplacePurchase paymentProcessor;
    [SerializeField] private string playTokenMintAddress; // $PLAY token mint address

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
        Debug.Log("[SkinManager] Start() called - Setting up button listeners...");

        // Connect deploy buttons
        deployButton.onClick.AddListener(DeployPlayerSkin);
        envDeployButton.onClick.AddListener(DeployEnvironmentSkin);

        // Connect purchase buttons
        if (purchaseButton != null)
        {
            purchaseButton.onClick.AddListener(PurchasePlayerSkin);
            Debug.Log("[SkinManager] Player skin purchase button listener ADDED");
        }
        else
        {
            Debug.LogError("[SkinManager] Purchase button is NULL! Not assigned in Inspector!");
        }

        if (envPurchaseButton != null)
        {
            envPurchaseButton.onClick.AddListener(PurchaseEnvironmentSkin);
            Debug.Log("[SkinManager] Environment skin purchase button listener ADDED");
        }
        else
        {
            Debug.LogError("[SkinManager] Environment purchase button is NULL! Not assigned in Inspector!");
        }

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

        LoadUnlockStates(); // Load purchased skins from PlayerPrefs
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

        // If skin is LOCKED → show purchase button (interactable)
        // If skin is UNLOCKED → show deploy button
        purchaseButton.interactable = playerSkin.isLocked; // ✅ FIXED: interactable when locked
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
        
        // If skin is LOCKED → show purchase button (interactable)
        // If skin is UNLOCKED → show deploy button
        envPurchaseButton.interactable = playerSkin.isLocked; // ✅ FIXED: interactable when locked
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

    void LoadUnlockStates()
    {
        // Restore player skin unlock states
        for (int i = 0; i < skinButtons.Length; i++)
        {
            string unlockKey = $"PlayerSkin_Unlocked_{i}";
            bool isUnlocked = PlayerPrefs.GetInt(unlockKey, 0) == 1;

            var skinComponent = skinButtons[i].GetComponent<PlayerSkin>();
            if (skinComponent != null && isUnlocked)
            {
                skinComponent.isLocked = false;
            }
        }

        // Restore environment skin unlock states
        for (int i = 0; i < environmentSkinButtons.Length; i++)
        {
            string unlockKey = $"EnvironmentSkin_Unlocked_{i}";
            bool isUnlocked = PlayerPrefs.GetInt(unlockKey, 0) == 1;

            var skinComponent = environmentSkinButtons[i].GetComponent<PlayerSkin>();
            if (skinComponent != null && isUnlocked)
            {
                skinComponent.isLocked = false;
            }
        }
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
        Debug.Log($"[PurchasePlayerSkin] Button clicked! Selected Index: {selectedPlayerSkinIndex}");

        if (paymentProcessor == null)
        {
            Debug.LogError("MarketplacePurchase not assigned!");
            return;
        }

        // Get the SELECTED skin's component
        var selectedSkin = skinButtons[selectedPlayerSkinIndex].GetComponent<PlayerSkin>();

        if (selectedSkin == null || !selectedSkin.isLocked)
        {
            Debug.LogWarning("Skin is already unlocked or component missing.");
            return;
        }

        float price = selectedSkin.price;
        Debug.Log($"Processing purchase for skin #{selectedPlayerSkinIndex} at price {price} $PLAY");

        // Process blockchain payment using $PLAY tokens
        paymentProcessor.PurchaseWithSplToken(price, playTokenMintAddress, () =>
        {
            // SUCCESS CALLBACK: Unlock the skin
            selectedSkin.isLocked = false;

            // Save unlock state to PlayerPrefs
            string unlockKey = $"PlayerSkin_Unlocked_{selectedPlayerSkinIndex}";
            PlayerPrefs.SetInt(unlockKey, 1);
            PlayerPrefs.Save();

            // Update UI
            OnSelectionChange(selectedPlayerSkinIndex);
            Debug.Log($"Player Skin #{selectedPlayerSkinIndex} Purchased Successfully!");
        });
    }

    public void PurchaseEnvironmentSkin()
    {
        Debug.Log($"[PurchaseEnvironmentSkin] Button clicked! Selected Index: {selectedEnvSkinIndex}");

        if (paymentProcessor == null)
        {
            Debug.LogError("MarketplacePurchase not assigned!");
            return;
        }

        // Get the SELECTED environment skin's component
        var selectedSkin = environmentSkinButtons[selectedEnvSkinIndex].GetComponent<PlayerSkin>();

        if (selectedSkin == null || !selectedSkin.isLocked)
        {
            Debug.LogWarning("Skin is already unlocked or component missing.");
            return;
        }

        float price = selectedSkin.price;
        Debug.Log($"Processing purchase for environment skin #{selectedEnvSkinIndex} at price {price} $PLAY");

        // Process blockchain payment using $PLAY tokens
        paymentProcessor.PurchaseWithSplToken(price, playTokenMintAddress, () =>
        {
            // SUCCESS CALLBACK: Unlock the skin
            selectedSkin.isLocked = false;

            // Save unlock state to PlayerPrefs
            string unlockKey = $"EnvironmentSkin_Unlocked_{selectedEnvSkinIndex}";
            PlayerPrefs.SetInt(unlockKey, 1);
            PlayerPrefs.Save();

            // Update UI
            OnEnvironmentSelectionChange(selectedEnvSkinIndex);
            Debug.Log($"Environment Skin #{selectedEnvSkinIndex} Purchased Successfully!");
        });
    }
    #endregion
}