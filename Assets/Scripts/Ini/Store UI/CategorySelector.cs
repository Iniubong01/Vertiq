using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using System.Collections;

public class CategorySelector : MonoBehaviour
{
    [Header("Settings for Upgrade Panels")]
    [SerializeField] private CanvasGroup powerUpCG, PlayerSkinCG, EnvironmentSkinCG;
    [SerializeField] Button powerUpButton, playerSkinButton, environmentSkinButton;
    [SerializeField] private GameObject [] onSelectImage;

    [Header("Settings for Player Skin Sub Panel")]
    [SerializeField] private CanvasGroup cursorCG, rareCG;
    [SerializeField] Button cursorButton, rareButton;
    [SerializeField] private GameObject [] skinPanelsOnSelectImage;

    [Header("Settings for Enviroment Skin Sub Panel")]
    [SerializeField] private CanvasGroup e_cursorCG, e_rareCG;  // e_ denoting environment
    [SerializeField] Button e_cursorButton, e_rareButton;
    [SerializeField] private GameObject [] e_skinPanelsOnSelectImage;
    
    [Header("General Settings")]
    [SerializeField] private float fadeDuration = 0.7f;

    // Button Canvas Group Settings
    [SerializeField] private CanvasGroup powerUpButtonCG, playerSkinButtonCG, environmentSkinButtonCG;
    [SerializeField] private CanvasGroup cursorButtonCG, rareButtonCG;
    [SerializeField] private CanvasGroup e_cursorButtonCG, e_rareButtonCG;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        powerUpButton.onClick.AddListener(FadeInPowerUpCG);
        playerSkinButton.onClick.AddListener(FadeInPlayerUpCG);
        environmentSkinButton.onClick.AddListener(FadeInEnvironnmetCG);

        cursorButton.onClick.AddListener(FadeInCursorCG);
        rareButton.onClick.AddListener(FadeInRareCG);

        e_cursorButton.onClick.AddListener(e_FadeInCursorCG);
        e_rareButton.onClick.AddListener(e_FadeInRareCG);
    }

    #region CanvasGroup Fade Control Methods
    private void SetCanvasGroupActive(CanvasGroup tUI)
    {
        tUI.DOKill(); 
        tUI.gameObject.SetActive(true);
        tUI.alpha = 0; 
        tUI.DOFade(1, fadeDuration).SetUpdate(true); 
        tUI.interactable = true;
        tUI.blocksRaycasts = true;
    }

    private void SetCanvasGroupInActive(CanvasGroup tUI)
    {
        tUI.DOKill(); 
        tUI.interactable = false;
        tUI.blocksRaycasts = false;
        tUI.DOFade(0, fadeDuration)
            .SetUpdate(true) 
            .OnComplete(() => { tUI.gameObject.SetActive(false); });
    }

    private void FadeInCanvasGroupActive(CanvasGroup tUI)
    {
        tUI.DOKill();
        tUI.alpha = 0; 
        tUI.DOFade(1, fadeDuration).SetUpdate(true); 
    }

    private void FadeOutCanvasGroupInActive(CanvasGroup tUI)
    {
        tUI.DOKill();
        tUI.DOFade(0.5f, fadeDuration).SetUpdate(true);
    }
    #endregion

    #region Main Panels
    void FadeInPowerUpCG()
    {
        foreach(var obj in onSelectImage)
        {
            obj.SetActive(false);
        }

        onSelectImage[0].SetActive(true);
        SetCanvasGroupActive(powerUpCG);
        SetCanvasGroupInActive(PlayerSkinCG);
        SetCanvasGroupInActive(EnvironmentSkinCG);

        // Fade button Canvas Group depending on selection
        FadeInCanvasGroupActive(powerUpButtonCG);
        FadeOutCanvasGroupInActive(playerSkinButtonCG);
        FadeOutCanvasGroupInActive(environmentSkinButtonCG);
    }

    void FadeInPlayerUpCG()
    {        
        foreach(var obj in onSelectImage)
        {
            obj.SetActive(false);
        }

        onSelectImage[1].SetActive(true);
        SetCanvasGroupActive(PlayerSkinCG);
        SetCanvasGroupInActive(powerUpCG);
        SetCanvasGroupInActive(EnvironmentSkinCG);

        // Fade button Canvas Group depending on selection
        FadeOutCanvasGroupInActive(powerUpButtonCG);
        FadeInCanvasGroupActive(playerSkinButtonCG);
        FadeOutCanvasGroupInActive(environmentSkinButtonCG);
    }

    void FadeInEnvironnmetCG()
    {        
        foreach(var obj in onSelectImage)
        {
            obj.SetActive(false);
        }

        onSelectImage[2].SetActive(true);
        SetCanvasGroupActive(EnvironmentSkinCG);
        SetCanvasGroupInActive(PlayerSkinCG);
        SetCanvasGroupInActive(powerUpCG);

        // Fade button Canvas Group depending on selection
        FadeOutCanvasGroupInActive(powerUpButtonCG);
        FadeOutCanvasGroupInActive(playerSkinButtonCG);
        FadeInCanvasGroupActive(environmentSkinButtonCG);
    }
    #endregion

    #region Sub Panels - PS
    // PS - Player Skin
    void FadeInCursorCG()
    {
        foreach(var obj in skinPanelsOnSelectImage)
        {
            obj.SetActive(false);
        }

        skinPanelsOnSelectImage[0].SetActive(true);
        SetCanvasGroupActive(cursorCG);
        SetCanvasGroupInActive(rareCG);

        // Fade button Canvas Group depending on selection
        FadeOutCanvasGroupInActive(rareButtonCG);
        FadeInCanvasGroupActive(cursorButtonCG);
    }

    void FadeInRareCG()
    {
        foreach(var obj in skinPanelsOnSelectImage)
        {
            obj.SetActive(false);
        }

        skinPanelsOnSelectImage[1].SetActive(true);
        SetCanvasGroupActive(rareCG);
        SetCanvasGroupInActive(cursorCG);

        // Fade button Canvas Group depending on selection
        FadeOutCanvasGroupInActive(cursorButtonCG);
        FadeInCanvasGroupActive(rareButtonCG);
    }
    #endregion

    #region Sub Panels - ES
    // ES - Environment Skin
    void e_FadeInCursorCG()
    {
        foreach(var obj in e_skinPanelsOnSelectImage)
        {
            obj.SetActive(false);
        }

        e_skinPanelsOnSelectImage[0].SetActive(true);
        SetCanvasGroupActive(e_cursorCG);
        SetCanvasGroupInActive(e_rareCG);
        
        // Fade button Canvas Group depending on selection
        FadeOutCanvasGroupInActive(e_rareButtonCG);
        FadeInCanvasGroupActive(e_cursorButtonCG);
    }

    void e_FadeInRareCG()
    {
        foreach(var obj in e_skinPanelsOnSelectImage)
        {
            obj.SetActive(false);
        }

        e_skinPanelsOnSelectImage[1].SetActive(true);
        SetCanvasGroupActive(e_rareCG);
        SetCanvasGroupInActive(e_cursorCG);

        // Fade button Canvas Group depending on selection
        FadeOutCanvasGroupInActive(e_cursorButtonCG);
        FadeInCanvasGroupActive(e_rareButtonCG);
    }
    #endregion
}