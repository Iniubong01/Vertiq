using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    // [NEW] Global flag to track menu state
    public static bool IsMenuInteractable = false;

    [Header("Marketplace Integration")]
    [SerializeField] private MarketplacePurchase paymentProcessor; 

    [Header("Token Config")]
    public string playTokenMintAddress; 
    public float playTokenPrice = 50f;  
    public int coinsRewardAmount = 500; 

    [Header("PowerUp Prices ($PLAY Token)")]
    public float priceShield = 10f;
    public float priceBullets = 15f;
    public float priceFullLives = 20f;
    public float priceFreezeTime = 25f;

    [Header("Coin Prices (SOL)")]
    public float pricePack10 = 0.05f;
    public float pricePack50 = 0.2f;
    public float pricePack100 = 0.35f;
    public float pricePack200 = 0.6f;

    [Header("UI References")]
    [SerializeField] private float fadeDuration = 0.7f;
    [SerializeField] private CanvasGroup splashUICG, loadingUICG, menuUICG, storeUICG, solStoreUICG, leaderboardUICG;

    [Header("Loading UI")]
    [SerializeField] private Slider progressBar;
    [SerializeField] private TextMeshProUGUI progressText;

    [Header("Text References")]
    [SerializeField] private TextMeshProUGUI coinText;
    [SerializeField] private TextMeshProUGUI coinTextStore;

    [Header("Buttons")]
    [SerializeField] Button[] powerUpPurchaseButtons;
    [SerializeField] Button[] coinPurchaseButtons;
    [SerializeField] Button buyWithPlayButton; 
    [SerializeField] Button storeBackButton; 

    [Header("PowerUp Indicators")]
    [SerializeField] GameObject[] shieldIndicators;
    [SerializeField] GameObject[] multipleBulletsIndicators;
    [SerializeField] GameObject[] fullLivesIndicators;
    [SerializeField] GameObject[] freezeTimeIndicators;

    public static bool SkipSplashSequence = false;

    private void Start()
    {
        Time.timeScale = 1f;
        IsMenuInteractable = false; // [NEW] Reset at start

        SetupButtonListeners();

        if (SkipSplashSequence)
        {
            ResetAllUI();
            EnableCanvasGroupInstant(menuUICG);
            SkipSplashSequence = false; 
        }
        else
        {
            StartCoroutine(CloseSplashThenLoadMain());
        }

        GiveFirstTimeGift();
        UpdateUI();
        UpdateShieldVisuals();
        UpdateMultipleBulletsVisuals();
        UpdateFreezeTimeVisuals();
        UpdateFullLivesVisuals();

        // Play a different audio in Splash scene
        SoundManager.Instance.EnableSplashAS();
    }

    private void ResetAllUI()
    {
        DisableCanvasGroupInstant(splashUICG);
        DisableCanvasGroupInstant(loadingUICG);
        DisableCanvasGroupInstant(menuUICG);
        DisableCanvasGroupInstant(storeUICG);
        DisableCanvasGroupInstant(solStoreUICG);
        DisableCanvasGroupInstant(leaderboardUICG);
    }

    private void SetupButtonListeners()
    {
        if (powerUpPurchaseButtons.Length >= 4)
        {
            powerUpPurchaseButtons[0].onClick.AddListener(BuyShieldPowerup);
            powerUpPurchaseButtons[1].onClick.AddListener(BuyMultipleBullets);
            powerUpPurchaseButtons[2].onClick.AddListener(BuyFullLives);
            powerUpPurchaseButtons[3].onClick.AddListener(BuyFreezeTime);
        }

        if (coinPurchaseButtons.Length >= 4)
        {
            coinPurchaseButtons[0].onClick.AddListener(BuyCoinsPack10);
            coinPurchaseButtons[1].onClick.AddListener(BuyCoinsPack50);
            coinPurchaseButtons[2].onClick.AddListener(BuyCoinsPack100);
            coinPurchaseButtons[3].onClick.AddListener(BuyCoinsPack200);
        }

        if (buyWithPlayButton != null)
        {
            buyWithPlayButton.onClick.AddListener(BuyCoinsWithPlay);
        }
    }

    private void GiveFirstTimeGift()
    {
        if (!PlayerPrefs.HasKey("FirstTimeCoinGift"))
        {
            ShopData.Instance.AddCoins(750);   
            PlayerPrefs.SetInt("FirstTimeCoinGift", 1);
            PlayerPrefs.Save();
            
            ShopData.Instance.AddPowerup("shield"); ShopData.Instance.AddPowerup("shield");
            ShopData.Instance.AddPowerup("bullets"); ShopData.Instance.AddPowerup("bullets");
            ShopData.Instance.AddPowerup("freezetime"); ShopData.Instance.AddPowerup("freezetime");
            ShopData.Instance.AddPowerup("fulllives"); ShopData.Instance.AddPowerup("fulllives");
        }
    }
    
    private void PlayClickSound() { if (SoundManager.Instance != null) SoundManager.Instance.PlayButtonSound(); }

    #region NAVIGATION & UI
    private IEnumerator CloseSplashThenLoadMain()
    {
        yield return new WaitForSeconds(UnityEngine.Random.Range(4.5f, 8f));
        SetCanvasGroupInActive(splashUICG); 
        yield return new WaitForSeconds(fadeDuration);
        yield return StartCoroutine(ShowLoadingUI(menuUICG));
    }

    public void MenuToStore() { SetCanvasGroupInActive(menuUICG); SetCanvasGroupActive(storeUICG); PlayClickSound(); }
    public void StoreToMenu() { SetCanvasGroupInActive(storeUICG); SetCanvasGroupActive(menuUICG); PlayClickSound(); }
    public void StoreToSolStore() { SetCanvasGroupInActive(storeUICG); SetCanvasGroupActive(solStoreUICG); PlayClickSound(); }
    public void SolStoreToStore() { SetCanvasGroupInActive(solStoreUICG); SetCanvasGroupActive(storeUICG); PlayClickSound(); }
    public void LeaderboardToMenu() { SetCanvasGroupInActive(menuUICG); SetCanvasGroupActive(leaderboardUICG); PlayClickSound(); }
    public void MenuToLeaderboard() { SetCanvasGroupInActive(leaderboardUICG); SetCanvasGroupActive(menuUICG); PlayClickSound(); }

    public IEnumerator ShowLoadingUI(CanvasGroup targetUI)
    {
        if (loadingUICG == null) yield break;
        loadingUICG.gameObject.SetActive(true);
        loadingUICG.alpha = 0;
        progressBar.value = 0f;
        progressText.text = "0%";
        yield return loadingUICG.DOFade(1, fadeDuration).WaitForCompletion();
        float displayProgress = 0f;
        while (displayProgress < 0.99f)
        {
            displayProgress = Mathf.MoveTowards(displayProgress, 1f, Time.deltaTime * 2f);
            progressBar.value = displayProgress;
            progressText.text = Mathf.RoundToInt(displayProgress * 100) + "%";
            yield return null;
        }
        yield return loadingUICG.DOFade(0, fadeDuration).WaitForCompletion();
        loadingUICG.gameObject.SetActive(false);
        if (targetUI != null) SetCanvasGroupActive(targetUI);
    }

    private void SetCanvasGroupActive(CanvasGroup tUI)
    {
        tUI.DOKill(); 
        tUI.gameObject.SetActive(true);
        tUI.alpha = 0; 
        tUI.DOFade(1, fadeDuration).SetUpdate(true).OnComplete(() => 
        {
            tUI.interactable = true;
            tUI.blocksRaycasts = true;

            // [FIX] NOTIFY WALLET CONNECTOR HERE
            // If this is the Menu, tell the Wallet it's okay to show popups now
            if (tUI == menuUICG)
            {
                IsMenuInteractable = true;
                if (WalletConnector.Instance != null)
                {
                    WalletConnector.Instance.AttemptShowUsernamePanel();
                }
            }
        });
    }

    private void SetCanvasGroupInActive(CanvasGroup tUI)
    {
        // [FIX] Disable interaction flag if leaving menu
        if (tUI == menuUICG) IsMenuInteractable = false;

        tUI.DOKill(); 
        tUI.interactable = false;
        tUI.blocksRaycasts = false;
        
        // [NEW FIX] Disable the GameObject IMMEDIATELY so OnEnable/OnDisable work correctly
        // We'll keep it visually visible during the fade by setting alpha first
        tUI.gameObject.SetActive(false);
    }

    private void EnableCanvasGroupInstant(CanvasGroup tUI)
    {
        tUI.DOKill();
        tUI.gameObject.SetActive(true);
        tUI.alpha = 1;
        tUI.interactable = true;
        tUI.blocksRaycasts = true;

        // [FIX] Handle instant load (Skip Splash case)
        if (tUI == menuUICG)
        {
            IsMenuInteractable = true;
            // Use a coroutine to wait 1 frame for WalletConnector to initialize
            StartCoroutine(NotifyWalletReady());
        }
    }

    private IEnumerator NotifyWalletReady()
    {
        yield return null;
        if (WalletConnector.Instance != null)
        {
            WalletConnector.Instance.AttemptShowUsernamePanel();
        }
    }

    private void DisableCanvasGroupInstant(CanvasGroup tUI)
    {
        if (tUI == null) return;
        tUI.DOKill();
        tUI.alpha = 0;
        tUI.interactable = false;
        tUI.blocksRaycasts = false;
        tUI.gameObject.SetActive(false);
    }

    public void QuitGame()
    {
        Application.Quit();
        PlayClickSound();
    }

    public static void LoadMainMenu()
    {
        SkipSplashSequence = true;
        Time.timeScale = 1f; 
        DOTween.KillAll();   
        SceneManager.LoadScene("Splash"); 
    }
    
    private void OnDestroy() { DOTween.KillAll(); }
    #endregion

    #region PURCHASE LOGIC
    public void BuyCoinsWithPlay()
    {
        PlayClickSound();
        if (paymentProcessor != null && !string.IsNullOrEmpty(playTokenMintAddress))
        {
            paymentProcessor.PurchaseWithSplToken(playTokenPrice, playTokenMintAddress, () => 
            {
                ShopData.Instance.AddCoins(coinsRewardAmount);
                UpdateUI();
            });
        }
        else
        {
            Debug.LogError("Setup Error: PaymentProcessor missing or Mint Address empty!");
        }
    }

    public void BuyCoinsPack10() { PlayClickSound(); if(paymentProcessor != null) paymentProcessor.PurchaseWithSol(pricePack10, () => { ShopData.Instance.AddCoins(10); UpdateUI(); }); }
    public void BuyCoinsPack50() { PlayClickSound(); if(paymentProcessor != null) paymentProcessor.PurchaseWithSol(pricePack50, () => { ShopData.Instance.AddCoins(50); UpdateUI(); }); }
    public void BuyCoinsPack100() { PlayClickSound(); if(paymentProcessor != null) paymentProcessor.PurchaseWithSol(pricePack100, () => { ShopData.Instance.AddCoins(100); UpdateUI(); }); }
    public void BuyCoinsPack200() { PlayClickSound(); if(paymentProcessor != null) paymentProcessor.PurchaseWithSol(pricePack200, () => { ShopData.Instance.AddCoins(200); UpdateUI(); }); }
    
    public void BuyShieldPowerup() { BuyPowerup(priceShield, "shield", () => ShopData.Instance.AddPowerup("shield"), UpdateShieldVisuals); }
    public void BuyMultipleBullets() { BuyPowerup(priceBullets, "bullets", () => ShopData.Instance.AddPowerup("bullets"), UpdateMultipleBulletsVisuals); }
    public void BuyFreezeTime() { BuyPowerup(priceFreezeTime, "freezetime", () => ShopData.Instance.AddPowerup("freezetime"), UpdateFreezeTimeVisuals); }
    public void BuyFullLives() { BuyPowerup(priceFullLives, "fulllives", () => ShopData.Instance.AddPowerup("fulllives"), UpdateFullLivesVisuals); }

    private void BuyPowerup(float price, string id, System.Action addAction, System.Action visualAction)
    {
        PlayClickSound();
        if (paymentProcessor == null) return;
        
        // Check limits here if desired, otherwise rely on UpdateUI/Interactivity
        // (Assuming max limit is checked via button interactivity state)
        
        paymentProcessor.PurchaseWithSplToken(price, playTokenMintAddress, () =>
        {
            addAction();
            visualAction();
            UpdateUI();
            StartCoroutine(RefocusSelectionRoutine());
        });
    }
    #endregion

    #region VISUALS & INTERACTIVITY
    public void checkInteractivity_ShieldButton() { powerUpPurchaseButtons[0].interactable = ShopData.Instance.powerupShield < 5; }
    public void checkInteractivity_MultipleBulletButton() { powerUpPurchaseButtons[1].interactable = ShopData.Instance.powerupMultipleBullets < 5; }
    public void checkInteractivity_FullLivesButton() { powerUpPurchaseButtons[2].interactable = ShopData.Instance.powerupFullLives < 5; }
    public void checkInteractivity_FreezeTimeButton() { powerUpPurchaseButtons[3].interactable = ShopData.Instance.powerupFreezeTime < 5; }

    private IEnumerator RefocusSelectionRoutine()
    {
        yield return null; 
        GameObject selectedObj = EventSystem.current.currentSelectedGameObject;
        Button selectedBtn = (selectedObj != null) ? selectedObj.GetComponent<Button>() : null;
        if (selectedBtn == null || !selectedBtn.interactable)
        {
            bool foundNewButton = false;
            foreach (Button btn in powerUpPurchaseButtons)
            {
                if (btn != null && btn.interactable && btn.gameObject.activeInHierarchy)
                {
                    EventSystem.current.SetSelectedGameObject(null);
                    EventSystem.current.SetSelectedGameObject(btn.gameObject);
                    foundNewButton = true;
                    break;
                }
            }
            if (!foundNewButton && storeBackButton != null && storeBackButton.interactable)
            {
                EventSystem.current.SetSelectedGameObject(null);
                EventSystem.current.SetSelectedGameObject(storeBackButton.gameObject);
            }
        }
    }

    private void UpdateShieldVisuals() { UpdateIndicators(ShopData.Instance.powerupShield, shieldIndicators); }
    private void UpdateMultipleBulletsVisuals() { UpdateIndicators(ShopData.Instance.powerupMultipleBullets, multipleBulletsIndicators); }
    private void UpdateFreezeTimeVisuals() { UpdateIndicators(ShopData.Instance.powerupFreezeTime, freezeTimeIndicators); }
    private void UpdateFullLivesVisuals() { UpdateIndicators(ShopData.Instance.powerupFullLives, fullLivesIndicators); }

    private void UpdateIndicators(int count, GameObject[] indicators)
    {
        for (int i = 0; i < indicators.Length; i++)
        {
            indicators[i].SetActive(i < count);
        }
    }
    #endregion

    private void UpdateUI()
    {
        if (ShopData.Instance != null)
        {
            if (coinText != null) coinText.text = ShopData.Instance.coins.ToString();
            if (coinTextStore != null) coinTextStore.text = ShopData.Instance.coins.ToString();
            checkInteractivity_ShieldButton();
            checkInteractivity_MultipleBulletButton();
            checkInteractivity_FullLivesButton();
            checkInteractivity_FreezeTimeButton();
        }
    }
}