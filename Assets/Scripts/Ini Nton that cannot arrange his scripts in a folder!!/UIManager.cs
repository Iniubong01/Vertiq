using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine.EventSystems; 

public class UIManager : MonoBehaviour
{
    [Header("Marketplace Integration")]
    [SerializeField] private MarketplacePurchase paymentProcessor; 

    [Header("Coin Prices (SOL)")]
    public float pricePack10 = 0.05f;
    public float pricePack50 = 0.2f;
    public float pricePack100 = 0.35f;
    public float pricePack200 = 0.6f;

    [Header("UI Screens")]
    [SerializeField] private float fadeDuration = 0.7f;
    [SerializeField] private CanvasGroup splashUICG, loadingUICG, menuUICG, storeUICG, solStoreUICG;

    [Header("Loading UI")]
    [SerializeField] private Slider progressBar;
    [SerializeField] private TextMeshProUGUI progressText;

    [Header("Text References")]
    [SerializeField] private TextMeshProUGUI coinText;
    [SerializeField] private TextMeshProUGUI coinTextStore;

    [Header("Buttons")]
    [SerializeField] Button[] powerUpPurchaseButtons;
    [SerializeField] Button[] coinPurchaseButtons;
    [SerializeField] Button storeBackButton; 

    [Header("PowerUp Indicators")]
    [SerializeField] GameObject[] shieldIndicators;
    [SerializeField] GameObject[] multipleBulletsIndicators;
    [SerializeField] GameObject[] fullLivesIndicators;
    [SerializeField] GameObject[] freezeTimeIndicators;

    private void Start()
    {
        StartCoroutine(CloseSplashThenLoadMain());

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
        
        GiveFirstTimeGift();
        UpdateUI();
        
        // Initialize all indicators on start
        UpdateShieldVisuals();
        UpdateMultipleBulletsVisuals();
        UpdateFreezeTimeVisuals();
        UpdateFullLivesVisuals();
    }

    private void GiveFirstTimeGift()
    {
        if (!PlayerPrefs.HasKey("FirstTimeCoinGift"))
        {
            ShopData.Instance.AddCoins(750);   
            PlayerPrefs.SetInt("FirstTimeCoinGift", 1);
            PlayerPrefs.Save();
            
            ShopData.Instance.AddPowerup("shield");
            ShopData.Instance.AddPowerup("shield");
            ShopData.Instance.AddPowerup("bullets");
            ShopData.Instance.AddPowerup("bullets");
            ShopData.Instance.AddPowerup("freezetime");
            ShopData.Instance.AddPowerup("freezetime");
            ShopData.Instance.AddPowerup("fulllives");
            ShopData.Instance.AddPowerup("fulllives");

            Debug.Log("First-time 750 coin and power ups granted!");
        }
    }

    private void PlayClickSound()
    {
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayButtonSound();
        }
    }

    #region PURCHASE LOGIC (SOLANA)
    public void BuyCoinsPack10()
    {
        PlayClickSound();
        if(paymentProcessor != null)
        {
            paymentProcessor.PurchaseWithSol(pricePack10, () => { ShopData.Instance.AddCoins(10); UpdateUI(); });
        }
    }
    public void BuyCoinsPack50()
    {
        PlayClickSound();
        if(paymentProcessor != null)
        {
            paymentProcessor.PurchaseWithSol(pricePack50, () => { ShopData.Instance.AddCoins(50); UpdateUI(); });
        }
    }
    public void BuyCoinsPack100()
    {
        PlayClickSound();
        if(paymentProcessor != null)
        {
            paymentProcessor.PurchaseWithSol(pricePack100, () => { ShopData.Instance.AddCoins(100); UpdateUI(); });
        }
    }
    public void BuyCoinsPack200()
    {
        PlayClickSound();
        if(paymentProcessor != null)
        {
            paymentProcessor.PurchaseWithSol(pricePack200, () => { ShopData.Instance.AddCoins(200); UpdateUI(); });
        }
    }
    #endregion

    #region NAVIGATION & UI
    private IEnumerator CloseSplashThenLoadMain()
    {
        yield return new WaitForSeconds(Random.Range(4.5f, 8f));
        splashUICG.DOFade(0, fadeDuration);
        SetCanvasGroupInActive(splashUICG);
        yield return new WaitForSeconds(fadeDuration);
        yield return StartCoroutine(ShowLoadingUI(menuUICG));
    }

    public void MenuToStore() { SetCanvasGroupInActive(menuUICG); SetCanvasGroupActive(storeUICG); PlayClickSound(); }
    public void StoreToMenu() { SetCanvasGroupInActive(storeUICG); SetCanvasGroupActive(menuUICG); PlayClickSound(); }
    public void StoreToSolStore() { SetCanvasGroupInActive(storeUICG); SetCanvasGroupActive(solStoreUICG); PlayClickSound(); }
    public void SolStoreToStore() { SetCanvasGroupInActive(solStoreUICG); SetCanvasGroupActive(storeUICG); PlayClickSound(); }

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
        tUI.gameObject.SetActive(true);
        tUI.alpha = 0;
        tUI.DOFade(1, fadeDuration);
        tUI.interactable = true;
        tUI.blocksRaycasts = true;
    }

    private void SetCanvasGroupInActive(CanvasGroup tUI)
    {
        tUI.gameObject.SetActive(false);
        tUI.alpha = 0;
        tUI.DOFade(0, fadeDuration);
        tUI.interactable = false;
        tUI.blocksRaycasts = false;
    }

    public void QuitGame()
    {
        Application.Quit();
        PlayClickSound();
    }
    #endregion

    #region POWERUPS & VISUALS
    public void checkInteractivity_ShieldButton() { powerUpPurchaseButtons[0].interactable = ShopData.Instance.coins >= 50 && ShopData.Instance.powerupShield < 5; }
    public void checkInteractivity_MultipleBulletButton() { powerUpPurchaseButtons[1].interactable = ShopData.Instance.coins >= 75 && ShopData.Instance.powerupMultipleBullets < 5; }
    public void checkInteractivity_FullLivesButton() { powerUpPurchaseButtons[2].interactable = ShopData.Instance.coins >= 115 && ShopData.Instance.powerupFullLives < 5; }
    public void checkInteractivity_FreezeTimeButton() { powerUpPurchaseButtons[3].interactable = ShopData.Instance.coins >= 145 && ShopData.Instance.powerupFreezeTime < 5; }

    public void BuyShieldPowerup()
    {
        PlayClickSound();
        
        // Check if player has enough coins AND hasn't maxed out this powerup
        if (ShopData.Instance.SpendCoins(50) && ShopData.Instance.powerupShield < 5)
        {
            ShopData.Instance.AddPowerup("shield");
            UpdateShieldVisuals(); // This now correctly updates based on the NEW count
            UpdateUI();
            StartCoroutine(RefocusSelectionRoutine());
        }
    }

    public void BuyMultipleBullets()
    {
        PlayClickSound();
        
        if (ShopData.Instance.SpendCoins(75) && ShopData.Instance.powerupMultipleBullets < 5)
        {
            ShopData.Instance.AddPowerup("bullets");
            UpdateMultipleBulletsVisuals();
            UpdateUI();
            StartCoroutine(RefocusSelectionRoutine());
        }
    }

    public void BuyFreezeTime()
    {
        PlayClickSound();
        
        if (ShopData.Instance.SpendCoins(145) && ShopData.Instance.powerupFreezeTime < 5)
        {
            ShopData.Instance.AddPowerup("freezetime");
            UpdateFreezeTimeVisuals();
            UpdateUI();
            StartCoroutine(RefocusSelectionRoutine());
        }
    }

    public void BuyFullLives()
    {
        PlayClickSound();
        
        if (ShopData.Instance.SpendCoins(115) && ShopData.Instance.powerupFullLives < 5)
        {
            ShopData.Instance.AddPowerup("fulllives");
            UpdateFullLivesVisuals();
            UpdateUI();
            StartCoroutine(RefocusSelectionRoutine());
        }
    }

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