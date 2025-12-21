using UnityEngine;
using UnityEngine.EventSystems; // Required to detect "Selection"
using System.Collections;

public class ButtonScaler : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Settings")]
    [SerializeField] private float scaleAmount = 1.2f; // How big to grow (1.2 = 20% bigger)
    [SerializeField] private float animationSpeed = 0.1f; // How fast to pop

    private Vector3 originalScale;
    private Coroutine activeCoroutine;

    private void Start()
    {
        // Remember the size we started at
        originalScale = transform.localScale;
    }

    // --- GAMEPAD LOGIC ---
    // Called when the EventSystem highlights this button
    public void OnSelect(BaseEventData eventData)
    {
        StartScale(originalScale * scaleAmount);
    }

    // Called when the EventSystem moves to another button
    public void OnDeselect(BaseEventData eventData)
    {
        StartScale(originalScale);
    }

    // --- MOUSE LOGIC (Optional, for testing) ---
    public void OnPointerEnter(PointerEventData eventData)
    {
        // Only scale if not already selected to avoid double-scaling
        if (EventSystem.current.currentSelectedGameObject != gameObject)
            StartScale(originalScale * scaleAmount);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // FIX: Check if EventSystem.current is null before accessing it.
        // This happens if the UI is being disabled or the scene is unloading.
        if (EventSystem.current == null) return;

        if (EventSystem.current.currentSelectedGameObject != gameObject)
            StartScale(originalScale);
    }

    // --- SMOOTH ANIMATION ---
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
            time += Time.unscaledDeltaTime; // Use unscaled so it works in Pause Menus!
            transform.localScale = Vector3.Lerp(start, target, time / animationSpeed);
            yield return null;
        }

        transform.localScale = target;
    }
}