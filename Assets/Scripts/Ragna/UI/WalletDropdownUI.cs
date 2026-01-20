using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class WalletDropdownUI : MonoBehaviour
{
    [Header("UI Assignments")]
    [Tooltip("The arrow/chevron image that rotates")]
    public RectTransform arrowIcon;
    
    [Tooltip("The GameObject containing the Red Disconnect Button")]
    public GameObject disconnectContainer;
    
    [Tooltip("(Optional) Add a CanvasGroup to the Disconnect Button for fading")]
    public CanvasGroup disconnectCanvasGroup;

    [Header("Logic Reference")]
    public WalletConnector walletConnector; // Drag your WalletConnector object here

    private bool isOpen = false;
    private Coroutine currentRoutine;

    private void Start()
    {
        // 1. Initial State: Closed and hidden
        if (disconnectContainer != null) disconnectContainer.SetActive(false);
        if (disconnectCanvasGroup != null) disconnectCanvasGroup.alpha = 0;
        if (arrowIcon != null) arrowIcon.localRotation = Quaternion.identity; // Face down (0)
    }

    // ---------------------------------------------------------
    // ASSIGN THIS TO: The Arrow Button > OnClick()
    // ---------------------------------------------------------
    public void ToggleDropdown()
    {
        if (currentRoutine != null) StopCoroutine(currentRoutine);
        isOpen = !isOpen;
        currentRoutine = StartCoroutine(AnimateDropdown(isOpen));
    }

    // ---------------------------------------------------------
    // ASSIGN THIS TO: The Red Disconnect Button > OnClick()
    // ---------------------------------------------------------
    public void OnDisconnectClicked()
    {
        // 1. Run the Disconnect Logic (from your working script)
        if (walletConnector != null) 
        {
            walletConnector.DisconnectWallet();
        }

        // 2. Immediately close and reset the visual dropdown
        CloseImmediate();
    }

    private void CloseImmediate()
    {
        if (currentRoutine != null) StopCoroutine(currentRoutine);
        isOpen = false;
        if (disconnectContainer != null) disconnectContainer.SetActive(false);
        if (arrowIcon != null) arrowIcon.localRotation = Quaternion.identity;
    }

    private IEnumerator AnimateDropdown(bool show)
    {
        float time = 0;
        float duration = 0.25f; // Animation speed

        Quaternion startRot = arrowIcon.localRotation;
        // Face UP (180) if showing, Face DOWN (0) if hiding
        Quaternion endRot = show ? Quaternion.Euler(0, 0, 180) : Quaternion.identity; 

        // Activate immediately if showing
        if (show && disconnectContainer != null) disconnectContainer.SetActive(true);

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            // Smooth "Ease Out" animation curve
            t = Mathf.Sin(t * Mathf.PI * 0.5f); 

            // 1. Rotate Arrow
            if (arrowIcon != null)
                arrowIcon.localRotation = Quaternion.Lerp(startRot, endRot, t);

            // 2. Fade Button (if CanvasGroup is assigned)
            if (disconnectCanvasGroup != null)
                disconnectCanvasGroup.alpha = show ? t : 1f - t;

            yield return null;
        }

        // Ensure final values are set
        if (arrowIcon != null) arrowIcon.localRotation = endRot;
        if (disconnectCanvasGroup != null) disconnectCanvasGroup.alpha = show ? 1f : 0f;

        // Deactivate if hiding
        if (!show && disconnectContainer != null) disconnectContainer.SetActive(false);
    }
}