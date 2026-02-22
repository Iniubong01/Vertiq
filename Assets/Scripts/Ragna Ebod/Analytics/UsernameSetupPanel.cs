using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Services.Authentication;
using System.Threading.Tasks;
using System.Collections;

public class UsernameSetupPanel : MonoBehaviour
{
    [Header("UI Components")]
    public TMP_InputField usernameInput;
    public Button saveButton;
    public TextMeshProUGUI errorText;
    
    [Header("Animation Settings")]
    public CanvasGroup panelCanvasGroup;
    public Transform panelContent; // Assign the visual box/background here
    public float animationDuration = 0.4f;
    public AnimationCurve popCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Canvas _canvas;

    private void Awake()
    {
        // Get Canvas component
        _canvas = GetComponentInParent<Canvas>();
        if (_canvas == null)
        {
            Debug.LogError("[USERNAME PANEL] NO CANVAS COMPONENT! Add Canvas to this GameObject!");
        }

        // Ensure it's hidden on start
        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha = 0;
            panelCanvasGroup.interactable = false;
            panelCanvasGroup.blocksRaycasts = false;
        }
        
        // Hide the whole object initially
        gameObject.SetActive(false);

        if (saveButton != null) saveButton.onClick.AddListener(OnSaveClicked);
        if (errorText != null) errorText.text = "";
    }

    public void Show()
    {
        Debug.Log("[USERNAME PANEL] ========== SHOW() CALLED ==========");
        Debug.Log($"[USERNAME PANEL] GameObject: {gameObject.name}, Active before: {gameObject.activeSelf}");
        
        gameObject.SetActive(true);
        
        Debug.Log($"[USERNAME PANEL] Active after SetActive: {gameObject.activeSelf}");

        // ANDROID FIX: Force Canvas to front
        if (_canvas != null)
        {
            _canvas.enabled = true;
            _canvas.sortingOrder = 9999;
            _canvas.overrideSorting = true;
            Debug.Log($"[USERNAME PANEL] Canvas configured: Enabled={_canvas.enabled}, SortOrder={_canvas.sortingOrder}, RenderMode={_canvas.renderMode}");
        }
        else
        {
            Debug.LogError("[USERNAME PANEL] CANVAS IS NULL! Panel will not render!");
        }

        // ANDROID FIX: Ensure EventSystem exists
        if (UnityEngine.EventSystems.EventSystem.current == null)
        {
            Debug.LogWarning("[USERNAME PANEL] No EventSystem found! Creating one...");
            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // Reset CanvasGroup
        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha = 0;
            panelCanvasGroup.interactable = true;
            panelCanvasGroup.blocksRaycasts = true;
            Debug.Log("[USERNAME PANEL] CanvasGroup reset for animation");
        }
        else
        {
            Debug.LogWarning("[USERNAME PANEL] No CanvasGroup assigned!");
        }

        // Clear fields
        if (usernameInput != null)
        {
            string existingUsername = PlayerPrefs.GetString("PlayerUsername", "");
            usernameInput.text = existingUsername;
            Debug.Log($"[USERNAME PANEL] Input field set to: '{existingUsername}'");
        }
        
        if (errorText != null) errorText.text = "";

        Debug.Log("[USERNAME PANEL] Starting animation coroutine...");
        StartCoroutine(AnimatePanel(true));
    }

    public void Hide()
    {
        Debug.Log("[USERNAME PANEL] Hide() called");
        StartCoroutine(AnimatePanel(false, () =>
        {
            // Do NOT call gameObject.SetActive(false) here on Android.
            // Deactivating a Canvas from within an input-event callback can
            // cause Android to destroy the native window and kill the Activity.
            // The CanvasGroup is already set to alpha=0 / non-interactive by
            // AnimatePanel, so the panel is effectively invisible.
            Debug.Log("[USERNAME PANEL] Panel hidden (kept active, CanvasGroup hidden)");
        }));
    }

    private async void OnSaveClicked()
    {
        try
        {
            Debug.Log("[USERNAME PANEL] Save button clicked");

            string newName = usernameInput != null ? usernameInput.text.Trim() : "";

            if (string.IsNullOrEmpty(newName) || newName.Length < 3)
            {
                if (errorText != null) errorText.text = "Username must be at least 3 characters.";
                Debug.LogWarning($"[USERNAME PANEL] Invalid username: '{newName}'");
                return;
            }

            if (saveButton != null) saveButton.interactable = false;

            bool success = await UpdateUsername(newName);

            // Guard: object may have been destroyed while awaiting
            if (this == null || !gameObject.activeInHierarchy) return;

            if (success)
            {
                PlayerPrefs.SetString("CachedUsername", newName);
                PlayerPrefs.SetString("PlayerUsername", newName);
                PlayerPrefs.Save();

                Debug.Log($"[USERNAME PANEL] Username saved successfully: {newName}");
                Hide();
            }
            else
            {
                if (errorText != null) errorText.text = "Failed to update username. Try again.";
                if (saveButton != null) saveButton.interactable = true;
                Debug.LogError("[USERNAME PANEL] Failed to save username");
            }
        }
        catch (System.Exception ex)
        {
            // Catch-all: prevents async void from crashing the app
            Debug.LogError($"[USERNAME PANEL] Unexpected error in OnSaveClicked: {ex.Message}");
            if (errorText != null) errorText.text = "An error occurred. Please try again.";
            if (saveButton != null) saveButton.interactable = true;
        }
    }

    private async Task<bool> UpdateUsername(string name)
    {
        try
        {
            // Guard: auth service might not be initialized yet
            if (AuthenticationService.Instance == null)
            {
                Debug.LogWarning("[UsernameSetupPanel] AuthenticationService not available — saving locally only.");
                return true; // Local-only save still counts as success
            }

            if (AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.UpdatePlayerNameAsync(name);
                Debug.Log($"[UsernameSetupPanel] Cloud username updated to: {name}");
                return true;
            }
            else
            {
                Debug.LogWarning("[UsernameSetupPanel] Not signed in — saving locally only.");
                return true; // Still return true since we save locally
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[UsernameSetupPanel] UpdateUsername failed!\nType: {ex.GetType().Name}\nMessage: {ex.Message}\nStack:\n{ex.StackTrace}");
            return false;
        }
    }

    // --- ANIMATION LOGIC ---
    private IEnumerator AnimatePanel(bool show, System.Action onComplete = null)
    {
        Debug.Log($"[USERNAME PANEL] Animation started: {(show ? "SHOWING" : "HIDING")}");
        
        float timer = 0;
        
        Vector3 startScale = show ? Vector3.one * 0.8f : Vector3.one;
        Vector3 endScale = show ? Vector3.one : Vector3.one * 0.8f;
        
        float startAlpha = show ? 0f : 1f;
        float endAlpha = show ? 1f : 0f;

        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.interactable = show;
            panelCanvasGroup.blocksRaycasts = show;
        }

        while (timer < animationDuration)
        {
            // Guard: object may be destroyed while animating
            if (this == null || !gameObject) yield break;

            timer += Time.deltaTime;
            float t = timer / animationDuration;
            float curveT = popCurve.Evaluate(t);

            if (panelCanvasGroup != null) 
                panelCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, t);
            
            if (panelContent != null) 
                panelContent.localScale = Vector3.Lerp(startScale, endScale, curveT);

            yield return null;
        }

        // Snap to final values
        if (panelCanvasGroup != null) panelCanvasGroup.alpha = endAlpha;
        if (panelContent != null) panelContent.localScale = endScale;

        Debug.Log($"[USERNAME PANEL] Animation complete: {(show ? "SHOWN" : "HIDDEN")}");
        
        onComplete?.Invoke();
    }

    // Debug helper - call this from Unity Inspector or another script
    [ContextMenu("Force Show Panel (Debug)")]
    public void DebugForceShow()
    {
        Show();
    }

    [ContextMenu("Check Panel Setup")]
    public void DebugCheckSetup()
    {
        Debug.Log("===== USERNAME PANEL SETUP CHECK =====");
        Debug.Log($"GameObject: {gameObject.name}");
        Debug.Log($"Active: {gameObject.activeSelf}");
        Debug.Log($"Canvas: {(_canvas != null ? "FOUND" : "MISSING!")}");
        if (_canvas != null)
        {
            Debug.Log($"  - Render Mode: {_canvas.renderMode}");
            Debug.Log($"  - Sort Order: {_canvas.sortingOrder}");
            Debug.Log($"  - Enabled: {_canvas.enabled}");
        }
        Debug.Log($"CanvasGroup: {(panelCanvasGroup != null ? "FOUND" : "MISSING!")}");
        Debug.Log($"Panel Content: {(panelContent != null ? "FOUND" : "MISSING!")}");
        Debug.Log($"Username Input: {(usernameInput != null ? "FOUND" : "MISSING!")}");
        Debug.Log($"Save Button: {(saveButton != null ? "FOUND" : "MISSING!")}");
        Debug.Log($"Error Text: {(errorText != null ? "FOUND" : "MISSING!")}");
        Debug.Log("=====================================");
    }
}