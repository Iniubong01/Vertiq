using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using System.Collections;

public class CategorySelector : MonoBehaviour
{
    [SerializeField] private CanvasGroup powerUpCG, PlayerSkinCG, EnvironmentSkinCG;
    [SerializeField] private float fadeDuration = 0.7f;
    [SerializeField] Button powerUpButton, playerSkinButton, environmentSkinButton;
    [SerializeField] private GameObject [] onSelectImage;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        powerUpButton.onClick.AddListener(FadeInPowerUpCG);
        playerSkinButton.onClick.AddListener(FadeInPlayerUpCG);
        environmentSkinButton.onClick.AddListener(FadeInEnvironnmetCG);
    }

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
    }
}