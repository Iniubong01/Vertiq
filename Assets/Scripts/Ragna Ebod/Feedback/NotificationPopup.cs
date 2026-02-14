using UnityEngine;
using TMPro;
using DG.Tweening;
using UnityEngine.UI;

public class NotificationPopup : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private CanvasGroup canvasGroup; 
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private Image statusIcon; 

    [Header("Settings")]
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private float displayDuration = 3.0f;

    private Tween currentTween; // Track the active tween explicitly

    private void Awake()
    {
        // Force hide on start
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false; // Ensure it's not clickable when hidden
        }
    }

    public void Show(string title, string message, Color statusColor)
    {
        // 1. Set Content
        if (titleText != null) titleText.text = title;
        if (messageText != null) messageText.text = message;
        if (statusIcon != null) statusIcon.color = statusColor; 

        if (canvasGroup == null) return;

        // 2. Kill any running animations immediately
        if (currentTween != null) currentTween.Kill();
        canvasGroup.DOKill();

        // 3. Make visible and clickable
        canvasGroup.blocksRaycasts = true; 
        canvasGroup.interactable = true; // FIX: Ensure button can be clicked
        
        // 4. Fade In
        currentTween = canvasGroup.DOFade(1, fadeDuration).OnComplete(() =>
        {
            // Wait, then Fade Out
            // We assign this to 'currentTween' so CloseNotification can kill it specifically
            currentTween = canvasGroup.DOFade(0, fadeDuration)
                .SetDelay(displayDuration)
                .OnComplete(() => 
                {
                    canvasGroup.blocksRaycasts = false;
                    canvasGroup.interactable = false;
                });
        });
    }

    public void CloseNotification()
    {
        if (canvasGroup != null)
        {
            Debug.Log("Close Button Clicked!"); // Debug to verify connection

            // 1. Kill the 'Show' animation sequence immediately
            canvasGroup.DOKill();
            if (currentTween != null) currentTween.Kill();

            // 2. Disable interaction immediately so they can't click twice
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            // 3. Fade out fast (optional: faster than normal fade)
            canvasGroup.DOFade(0, 0.2f);
        }
    }
}