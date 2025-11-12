using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    [Header("UI Screens")]
    [SerializeField] private float fadeDuration = 0.7f;

    [Header("Canvas Groups")]
    [SerializeField] private CanvasGroup splashUICG, loadingUICG, menuUICG, storeUICG;

    [Header("Loading UI")]
    [SerializeField] private Slider progressBar;
    [SerializeField] private TextMeshProUGUI progressText;

    private void Start()
    {
        StartCoroutine(CloseSplashThenLoadMain());
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
    }

    public void StoreToMenu()
    {
            SetCanvasGroupInActive(storeUICG);
        SetCanvasGroupActive(menuUICG);
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
}
