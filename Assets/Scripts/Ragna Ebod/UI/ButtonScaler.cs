using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using DG.Tweening;

public class ButtonScaler : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Settings")]
    [SerializeField] private float scaleAmount = 1.2f;
    [SerializeField] private float animationSpeed = 0.1f;
    [SerializeField] private bool shrinkOnHoverExit = false; // New option to prevent "Sticky" selection

    private Vector3 originalScale;
    private Coroutine activeCoroutine;
    private bool isInitialized = false;

    private void Awake()
    {
        // Capture scale in Awake to ensure we get the true "Editor" size
        // before any animations (like OnEnable pop-ins) play.
        originalScale = transform.localScale;
        isInitialized = true;
    }

    // --- CRITICAL FIX: RESET ON DISABLE ---
    private void OnDisable()
    {
        // If the menu closes while the button is big, reset it instantly.
        // Otherwise, it will look "stuck" the next time the menu opens.
        StopAllCoroutines();
        if (isInitialized)
        {
            transform.localScale = originalScale;
        }
    }

    // --- SCALE ON START ---
    public void SelectVisual()
    {
        StartScale(originalScale * scaleAmount);
    }

    // --- GAMEPAD / KEYBOARD LOGIC ---
    public void OnSelect(BaseEventData eventData)
    {
        StartScale(originalScale * scaleAmount);

        SoundManager.Instance.PlayButtonSound();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        StartScale(originalScale);
    }

    // --- MOUSE LOGIC ---
    public void OnPointerEnter(PointerEventData eventData)
    {
        // Only scale up if we aren't already selected (gamepad handles selection scaling)
        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != gameObject)
        {
            StartScale(originalScale * scaleAmount);

            var cg = this.GetComponent<CanvasGroup>();
            if(cg == null)
            {
                Debug.LogWarning("No Canvas Group, skipping!");
            }
            else
            {
                FadeInCanvasGroupActive(cg);
            }
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Safety check
        if (EventSystem.current == null) return;

        // If 'shrinkOnHoverExit' is TRUE, we shrink even if selected. 
        // If FALSE, we keep it big if it's currently selected (default UI behavior).
        if (shrinkOnHoverExit || EventSystem.current.currentSelectedGameObject != gameObject)
        {
            StartScale(originalScale);

            var cg = this.GetComponent<CanvasGroup>();
            if(cg == null)
            {
                Debug.LogWarning("No Canvas Group, skipping!");
            }
            else
            {
                FadeOutCanvasGroupInActive(cg);
            }
        }
    }

    // --- ANIMATION ---
    private void StartScale(Vector3 targetScale)
    {
        if (activeCoroutine != null) StopCoroutine(activeCoroutine);
        activeCoroutine = StartCoroutine(AnimateScale(targetScale));
    }

    private IEnumerator AnimateScale(Vector3 target)
    {
        float time = 0;
        Vector3 start = transform.localScale;

        while (time < animationSpeed)
        {
            time += Time.unscaledDeltaTime;
            transform.localScale = Vector3.Lerp(start, target, time / animationSpeed);
            yield return null;
        }

        transform.localScale = target;
    }

    private void FadeInCanvasGroupActive(CanvasGroup tUI)
    {
        tUI.DOKill();
        tUI.alpha = 0; 
        tUI.DOFade(1, 0.4f).SetUpdate(true); 
    }

    private void FadeOutCanvasGroupInActive(CanvasGroup tUI)
    {
        tUI.DOKill();
        tUI.DOFade(0.5f, 0.4f).SetUpdate(true);
    }
}