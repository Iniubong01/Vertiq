using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class UISelectionKeeper : MonoBehaviour
{
    // Static reference to whichever keeper is currently active.
    // NotificationPopup (and any other script) can call UISelectionKeeper.Current?.TakeoverFocus()
    // to restore gamepad focus after a popup closes — no Inspector wiring needed.
    public static UISelectionKeeper Current { get; private set; }

    private GameObject lastSelected;
    [SerializeField] private GameObject defaultSelection;
    [SerializeField] private Button[] allButtons; 
    
    [SerializeField] private Button[] excludedButtons; 

    private HashSet<Button> excludedButtonSet;

    void Start()
    {
        // 1. Setup Exclusions
        excludedButtonSet = new HashSet<Button>();
        if (excludedButtons != null)
        {
            foreach (Button btn in excludedButtons)
                if (btn != null) excludedButtonSet.Add(btn);
        }

        // 2. Auto-Fill Buttons if empty
        if (allButtons == null || allButtons.Length == 0)
        {
            Button[] foundButtons = GetComponentsInChildren<Button>(true);
            List<Button> filteredButtons = new List<Button>();
            foreach (Button btn in foundButtons)
            {
                if (!excludedButtonSet.Contains(btn)) filteredButtons.Add(btn);
            }
            allButtons = filteredButtons.ToArray();
        }
    }

    // --- AUTO FOCUS ON PANEL ACTIVATION ---
    // Fires every time this GameObject (or a parent) is activated via SetActive(true).
    // This is the KEY FIX: when WalletConnector switches to the login panel,
    // OnEnable fires here and automatically restores gamepad focus.
    void OnEnable()
    {
        // Register as the currently active keeper so other scripts (e.g. NotificationPopup)
        // can restore focus here without needing a direct reference.
        Current = this;

        // excludedButtonSet may not be ready yet on the very first enable (before Start).
        // Guard against that by only running after Start has completed.
        if (excludedButtonSet == null) return;
        StartCoroutine(TakeoverFocusDelayed());
    }

    void OnDisable()
    {
        // Only clear if we are the current one (don't clear if another panel took over)
        if (Current == this) Current = null;
    }

    private IEnumerator TakeoverFocusDelayed()
    {
        yield return null; // Wait one frame so Unity finishes activating all children
        TakeoverFocus();
    }

    /// <summary>
    /// Grabs gamepad/keyboard focus for this panel. Call this whenever this panel
    /// becomes active and you need focus to land here automatically.
    /// </summary>
    public void TakeoverFocus()
    {
        // Prefer the last known valid button, then first active, then default
        GameObject target = null;
        if (IsValidButton(lastSelected))        target = lastSelected;
        else if (FindFirstActiveButton() != null) target = FindFirstActiveButton();
        else if (IsValidButton(defaultSelection)) target = defaultSelection;

        if (target != null)
        {
            StartCoroutine(ForceSelection(target));
            lastSelected = target;
        }
    }

    // Initial selection is now handled by OnEnable (fires after Start sets up the button list)
    // so we no longer need a separate ForceSelection call in Start.

    void Update()
    {
        // 1. Track current valid selection
        if (EventSystem.current.currentSelectedGameObject != null)
        {
            lastSelected = EventSystem.current.currentSelectedGameObject;
        }
        // 2. If selection is LOST (The "Tap Screen" Issue)
        else
        {
            // A. Try restoring the last known button if it's still valid
            if (IsValidButton(lastSelected))
            {
                SelectButton(lastSelected);
                return;
            }
            
            // B. If last button is dead/disabled, find the Next active one
            GameObject nextButton = FindFirstActiveButton();
            if (nextButton != null)
            {
                SelectButton(nextButton);
                lastSelected = nextButton;
                return;
            }
            
            // C. Panic Fallback: Go to Default
            FallbackToDefault();
        }
    }

    private bool IsValidButton(GameObject obj)
    {
        if (obj == null || !obj.activeInHierarchy) return false;
        Button btn = obj.GetComponent<Button>();
        return btn != null && btn.interactable && !IsExcluded(btn);
    }

    private bool IsExcluded(Button button)
    {
        return excludedButtonSet != null && excludedButtonSet.Contains(button);
    }

    private GameObject FindFirstActiveButton()
    {
        if (allButtons == null) return null;
        foreach (Button btn in allButtons)
        {
            if (btn != null && btn.gameObject.activeInHierarchy && btn.interactable && !IsExcluded(btn))
                return btn.gameObject;
        }
        return null;
    }

    private GameObject FindNextActiveButton(GameObject currentButton)
    {
        if (allButtons == null) return null;
        bool foundCurrent = false;
        
        foreach (Button btn in allButtons)
        {
            if (foundCurrent && btn != null && btn.gameObject.activeInHierarchy && btn.interactable && !IsExcluded(btn))
                return btn.gameObject;
            
            if (btn != null && btn.gameObject == currentButton) foundCurrent = true;
        }
        return FindFirstActiveButton();
    }

    // --- SELECTION LOGIC ---

    public void RestoreSelection()
    {
        if (IsValidButton(lastSelected))
        {
            SelectButton(lastSelected);
        }
        else
        {
            GameObject nextButton = FindNextActiveButton(lastSelected);
            if (nextButton != null)
            {
                SelectButton(nextButton);
                lastSelected = nextButton;
            }
            else
            {
                FallbackToDefault();
            }
        }
    }

    private void FallbackToDefault()
    {
        if (IsValidButton(defaultSelection))
        {
            SelectButton(defaultSelection);
            lastSelected = defaultSelection;
        }
    }

    private void SelectButton(GameObject button)
    {
        if (button == null) return;
        EventSystem.current.SetSelectedGameObject(null); // Clear first
        EventSystem.current.SetSelectedGameObject(button);
    }

    // --- BUTTON CLICK HANDLER ---

    public void OnButtonClicked()
    {
        GameObject clickedButton = EventSystem.current.currentSelectedGameObject;
        // Use a more robust forced selection coroutine
        StartCoroutine(ForceRestoreSequence(clickedButton));
    }

    // 🎯 CRITICAL FIX: Tries to select multiple times to ensure Unity catches it
    private IEnumerator ForceRestoreSequence(GameObject clickedButton)
    {
        yield return new WaitForEndOfFrame();
        
        // 1. Try to keep focus on the clicked button if it's still alive
        if (IsValidButton(clickedButton))
        {
            yield return StartCoroutine(ForceSelection(clickedButton));
            yield break;
        }
        
        // 2. If button died (was disabled), find the next one
        GameObject nextButton = FindNextActiveButton(clickedButton);
        if (nextButton != null)
        {
            yield return StartCoroutine(ForceSelection(nextButton));
            lastSelected = nextButton;
        }
        else
        {
            // 3. If all else fails, go default
            FallbackToDefault();
        }
    }

    private IEnumerator ForceSelection(GameObject obj)
    {
        SelectButton(obj);
        yield return null; // Wait a frame
        if (EventSystem.current.currentSelectedGameObject != obj)
        {
            SelectButton(obj); // Force it again if it missed
        }
    }
}