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

    private void Awake()
    {
        // Force hide on start so it doesn't block the screen
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0;
            canvasGroup.blocksRaycasts = false;
        }
    }

    public void Show(string title, string message, Color statusColor)
    {
        // 1. Set Content
        if (titleText != null) titleText.text = title;
        if (messageText != null) messageText.text = message;
        if (statusIcon != null) statusIcon.color = statusColor; 

        if (canvasGroup == null)
        {
            Debug.LogError("NotificationPopup: CanvasGroup is missing in Inspector!");
            return;
        }

        // 2. Animation Logic
        canvasGroup.DOKill();
        canvasGroup.blocksRaycasts = true; 
        
        // Fade In
        canvasGroup.DOFade(1, fadeDuration).OnComplete(() =>
        {
            // Wait, then Fade Out
            canvasGroup.DOFade(0, fadeDuration)
                .SetDelay(displayDuration)
                .OnComplete(() => canvasGroup.blocksRaycasts = false);
        });
    }

    public void CloseNotification()
    {
        if (canvasGroup != null)
        {
            canvasGroup.DOKill();
            canvasGroup.DOFade(0, fadeDuration).OnComplete(() => canvasGroup.blocksRaycasts = false);
        }
    }
}