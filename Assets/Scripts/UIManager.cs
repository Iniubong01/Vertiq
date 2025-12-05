using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;

public class UIManager : MonoBehaviour
{
    [Header("UI Screens")]
    [SerializeField] private float fadeDuration = 0.7f;

    [Header("Canvas Groups")]
    [SerializeField] private CanvasGroup splashUICG, loadingUICG, menuUICG, storeUICG, solStoreUICG;

    [Header("Loading UI")]
    [SerializeField] private Slider progressBar;
    [SerializeField] private TextMeshProUGUI progressText;

    [SerializeField] private TextMeshProUGUI coinText, coinTextStore;
    [SerializeField] Button [] powerUpPurchaseButtons;
    [SerializeField] Button [] coinPurchaseButtons;

    [SerializeField] GameObject [] shieldIndicators;
    [SerializeField] GameObject [] multipleBulletsIndicators;
    [SerializeField] GameObject [] fullLivesIndicators;
    [SerializeField] GameObject [] freezeTimeIndicators;

    private void Start()
    {
        StartCoroutine(CloseSplashThenLoadMain());

        powerUpPurchaseButtons[0].onClick.AddListener(BuyShieldPowerup);
            powerUpPurchaseButtons[1].onClick.AddListener(BuyMultipleBullets);
                powerUpPurchaseButtons[2].onClick.AddListener(BuyFullLives);
                    powerUpPurchaseButtons[3].onClick.AddListener(BuyFreezeTime);

                    coinPurchaseButtons[0].onClick.AddListener(BuyCoinsPack10);
                coinPurchaseButtons[1].onClick.AddListener(BuyCoinsPack50);
            coinPurchaseButtons[2].onClick.AddListener(BuyCoinsPack100);
        coinPurchaseButtons[3].onClick.AddListener(BuyCoinsPack200);

        // Instead of updating this every frame, I've made them to be checked with buttons for optimization
        checkInteractivity_PowerUpPurchaseButtons();
        checkInteractivity_CoinPurchaseButton();

        UpdateShieldVisuals();
        UpdateFreezeTimeVisuals();
        UpdateFullLivesVisuals();
        UpdateMultipleBulletsVisuals();

        UpdateUI();
    }

    private IEnumerator CloseSplashThenLoadMain()
    {
        yield return new WaitForSeconds(Random.Range(4.5f, 8f));

        splashUICG.DOFade(0, fadeDuration);
        SetCanvasGroupInActive(splashUICG);
        yield return new WaitForSeconds(fadeDuration);

        yield return StartCoroutine(ShowLoadingUI(menuUICG));
    }

    public void MenuToStore()
    {
        SetCanvasGroupInActive(menuUICG);
            SetCanvasGroupActive(storeUICG);
                SoundManager.Instance.PlayButtonSound();
    }

    public void StoreToMenu()
    {
                SetCanvasGroupInActive(storeUICG);
            SetCanvasGroupActive(menuUICG);
        SoundManager.Instance.PlayButtonSound();
    }

    public void StoreToSolStore()
    {
        SetCanvasGroupInActive(storeUICG);
            SetCanvasGroupActive(solStoreUICG);
                SoundManager.Instance.PlayButtonSound();
    }

    public void SolStoreToStore()
    {
                SetCanvasGroupInActive(solStoreUICG);
            SetCanvasGroupActive(storeUICG);
        SoundManager.Instance.PlayButtonSound();
    }

    public IEnumerator ShowLoadingUI(CanvasGroup targetUI)
    {
        if (loadingUICG == null || progressBar == null || progressText == null)
        {
            Debug.LogWarning("UIManager: Missing references in the inspector!");
            yield break;
        }

        loadingUICG.gameObject.SetActive(true);
        loadingUICG.alpha = 0;
        progressBar.value = 0f;
        progressText.text = "0%";

        yield return loadingUICG.DOFade(1, fadeDuration).WaitForCompletion();

        float displayProgress = 0f;
        float targetProgress = 0f;

        while (displayProgress < 0.99f)
        {
            targetProgress = Mathf.Min(targetProgress + Time.deltaTime * Random.Range(0.3f, 0.6f), 1f);
            displayProgress = Mathf.MoveTowards(displayProgress, targetProgress, Time.deltaTime * 2f);

            progressBar.value = displayProgress;
            progressText.text = Mathf.RoundToInt(displayProgress * 100) + "%";

            yield return null;
        }

        displayProgress = 1f;
        progressBar.value = 1f;
        progressText.text = "100%";

        float briefWaitSimulation = Random.Range(1f, 3);
        yield return new WaitForSeconds(briefWaitSimulation);

        yield return loadingUICG.DOFade(0, fadeDuration).WaitForCompletion();
        loadingUICG.gameObject.SetActive(false);

        if (targetUI != null)
        {
            targetUI.gameObject.SetActive(true);
            targetUI.alpha = 0;
            targetUI.DOFade(1, fadeDuration);
            targetUI.interactable = true;
            targetUI.blocksRaycasts = true;
        }
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
        SoundManager.Instance.PlayButtonSound();
    }

    public void checkInteractivity_PowerUpPurchaseButtons()
    {
        foreach(var btn in powerUpPurchaseButtons)
        {
            btn.interactable = ShopData.Instance.coins >= 50;
        }
    }
    public void checkInteractivity_CoinPurchaseButton()
    {
        foreach(var btn in coinPurchaseButtons)
        {
            // TODO: Change here to "ShopData.Instance.Solana or so.."
            // btn.interactable = ShopData.Instance.coins >= 50;
        }
    }

    //? <purchase/* Normal_Puchases/> *VillageHead you can replace this with Solana integration*
    //TODO: VILLAGEHEAD YOU'LL CHECK IF PLAYER HAS ENOUGH SOLANA, IF YES BUY COINS, IF NO, HARD PASS
    #region PURCHASE COINS
    public void BuyCoinsPack10()
    {
        checkInteractivity_CoinPurchaseButton();
        ShopData.Instance.AddCoins(10);
        UpdateUI();
        SoundManager.Instance.PlayButtonSound();
    }

    public void BuyCoinsPack50()
    {
        checkInteractivity_CoinPurchaseButton();
        ShopData.Instance.AddCoins(50);
        UpdateUI();
        SoundManager.Instance.PlayButtonSound();
    }
    public void BuyCoinsPack100()
    {
        checkInteractivity_CoinPurchaseButton();
        ShopData.Instance.AddCoins(100);
        UpdateUI();
        SoundManager.Instance.PlayButtonSound();
    }
    public void BuyCoinsPack200()
    {
        checkInteractivity_CoinPurchaseButton();
        ShopData.Instance.AddCoins(200);
        UpdateUI();
        SoundManager.Instance.PlayButtonSound();
    }
    #endregion

    #region PURCHASE POWERUPS
    public void BuyShieldPowerup()
    {
        checkInteractivity_PowerUpPurchaseButtons();
        SoundManager.Instance.PlayButtonSound();

        int cost = 50;
        if (ShopData.Instance.SpendCoins(cost))
        {
            ShopData.Instance.AddPowerup("shield");

            foreach(var obj in shieldIndicators)
            {
                obj.SetActive(false);

                shieldIndicators[0].gameObject.SetActive(true);
            }

            UpdateUI();
            UpdateShieldVisuals();
        }   else    { Debug.Log("Not enough coins!"); }
    }
    private void UpdateShieldVisuals()
    {
        int count = ShopData.Instance.powerupShield;

        for (int i = 0; i < shieldIndicators.Length; i++)
        {
            shieldIndicators[i].SetActive(i < count);
        }
    }

    public void BuyMultipleBullets()
    {
        checkInteractivity_PowerUpPurchaseButtons();
        SoundManager.Instance.PlayButtonSound();

        int cost = 50;
        if (ShopData.Instance.SpendCoins(cost))
        {
            ShopData.Instance.AddPowerup("bullets");
            UpdateUI();
            UpdateMultipleBulletsVisuals();
        }   else    { Debug.Log("Not enough coins!"); }
    }
    
    private void UpdateMultipleBulletsVisuals()
    {
        int count = ShopData.Instance.powerupMultipleBullets;

        for (int i = 0; i < multipleBulletsIndicators.Length; i++)
        {
            multipleBulletsIndicators[i].SetActive(i < count);
        }
    }

    public void BuyFreezeTime()
    {
        checkInteractivity_PowerUpPurchaseButtons();
        SoundManager.Instance.PlayButtonSound();

        int cost = 50;
        if (ShopData.Instance.SpendCoins(cost))
        {
            ShopData.Instance.AddPowerup("freezetime");
            UpdateUI();
            UpdateFreezeTimeVisuals();
        }   else    { Debug.Log("Not enough coins!"); }
    }

    private void UpdateFreezeTimeVisuals()
    {
        int count = ShopData.Instance.powerupFreezeTime;

        for (int i = 0; i < freezeTimeIndicators.Length; i++)
        {
            freezeTimeIndicators[i].SetActive(i < count);
        }
    }

    public void BuyFullLives()
    {
        checkInteractivity_PowerUpPurchaseButtons();
        SoundManager.Instance.PlayButtonSound();

        int cost = 50;
        if (ShopData.Instance.SpendCoins(cost))
        {
            ShopData.Instance.AddPowerup("fulllives");
            UpdateUI();
            UpdateFullLivesVisuals();
        }   else    { Debug.Log("Not enough coins!"); }
    }   

    private void UpdateFullLivesVisuals()
    {
        int count = ShopData.Instance.powerupFullLives;

        for (int i = 0; i < fullLivesIndicators.Length; i++)
        {
            fullLivesIndicators[i].SetActive(i < count);
        }
    }
    #endregion

    private void UpdateUI()
    {
        coinText.text = ShopData.Instance.coins.ToString();
        coinTextStore.text = ShopData.Instance.coins.ToString();
    }
}