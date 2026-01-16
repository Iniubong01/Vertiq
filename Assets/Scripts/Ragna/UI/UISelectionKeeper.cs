using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class UISelectionKeeper : MonoBehaviour
{
    private GameObject lastSelected;
    [SerializeField] private GameObject defaultSelection;
    [SerializeField] private Button[] allButtons; // Only include buttons you WANT to be auto-selected
    
    // 🎯 NEW: Explicitly exclude these buttons from auto-selection
    [SerializeField] private Button[] excludedButtons; // Drag pause button, home button, etc. here

    private HashSet<Button> excludedButtonSet;

    void Start()
    {
        // Build a HashSet for fast lookup of excluded buttons
        excludedButtonSet = new HashSet<Button>();
        if (excludedButtons != null)
        {
            foreach (Button btn in excludedButtons)
            {
                if (btn != null) excludedButtonSet.Add(btn);
            }
        }

        // Select the default button on start
        if (defaultSelection != null)
        {
            SelectButton(defaultSelection);
        }
        
        // If allButtons is not set, try to find all buttons automatically
        // BUT exclude the ones in excludedButtons
        if (allButtons == null || allButtons.Length == 0)
        {
            Button[] foundButtons = GetComponentsInChildren<Button>(true);
            List<Button> filteredButtons = new List<Button>();
            
            foreach (Button btn in foundButtons)
            {
                if (!excludedButtonSet.Contains(btn))
                {
                    filteredButtons.Add(btn);
                }
            }
            
            allButtons = filteredButtons.ToArray();
        }
    }

    void Update()
    {
        // 1. If we currently have a selection, remember it.
        if (EventSystem.current.currentSelectedGameObject != null)
        {
            lastSelected = EventSystem.current.currentSelectedGameObject;
        }
        // 2. If the selection is lost (null)...
        else
        {
            // Try to restore the last selected if it's still active and interactable
            if (lastSelected != null && lastSelected.activeInHierarchy)
            {
                Button btn = lastSelected.GetComponent<Button>();
                if (btn != null && btn.interactable && !IsExcluded(btn))
                {
                    SelectButton(lastSelected);
                    return;
                }
            }
            
            // If last selected is not available, find the first active, interactable button
            GameObject nextButton = FindFirstActiveButton();
            if (nextButton != null)
            {
                SelectButton(nextButton);
                lastSelected = nextButton;
                return;
            }
            
            // Last resort: use default selection (if not excluded)
            if (defaultSelection != null && defaultSelection.activeInHierarchy)
            {
                Button btn = defaultSelection.GetComponent<Button>();
                if (btn != null && btn.interactable && !IsExcluded(btn))
                {
                    SelectButton(defaultSelection);
                    lastSelected = defaultSelection;
                }
            }
        }
    }

    // 🎯 NEW: Check if a button should be excluded from auto-selection
    private bool IsExcluded(Button button)
    {
        return excludedButtonSet != null && excludedButtonSet.Contains(button);
    }

    // Find the first active and interactable button (excluding excluded buttons)
    private GameObject FindFirstActiveButton()
    {
        if (allButtons == null) return null;
        
        foreach (Button btn in allButtons)
        {
            if (btn != null && btn.gameObject.activeInHierarchy && btn.interactable && !IsExcluded(btn))
            {
                return btn.gameObject;
            }
        }
        
        return null;
    }

    // Find the next active button after the current one
    private GameObject FindNextActiveButton(GameObject currentButton)
    {
        if (allButtons == null) return null;
        
        bool foundCurrent = false;
        
        // Try to find the next button after the current one
        foreach (Button btn in allButtons)
        {
            if (foundCurrent && btn != null && btn.gameObject.activeInHierarchy && btn.interactable && !IsExcluded(btn))
            {
                return btn.gameObject;
            }
            
            if (btn != null && btn.gameObject == currentButton)
            {
                foundCurrent = true;
            }
        }
        
        // If no next button found, try from the beginning
        return FindFirstActiveButton();
    }

    // Public method to manually restore selection (call this after button clicks)
    public void RestoreSelection()
    {
        // Try last selected
        if (lastSelected != null && lastSelected.activeInHierarchy)
        {
            Button btn = lastSelected.GetComponent<Button>();
            if (btn != null && btn.interactable && !IsExcluded(btn))
            {
                SelectButton(lastSelected);
                return;
            }
        }
        
        // If last selected is disabled, find next active button
        GameObject nextButton = FindNextActiveButton(lastSelected);
        if (nextButton != null)
        {
            SelectButton(nextButton);
            lastSelected = nextButton;
        }
    }

    // Helper to properly select a button
    private void SelectButton(GameObject button)
    {
        if (button == null) return;
        
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(button);
    }

    // Call this from any button's OnClick event in the Inspector
    public void OnButtonClicked()
    {
        GameObject clickedButton = EventSystem.current.currentSelectedGameObject;
        StartCoroutine(RestoreAfterFrame(clickedButton));
    }

    private IEnumerator RestoreAfterFrame(GameObject clickedButton)
    {
        yield return new WaitForEndOfFrame();
        
        // Check if the clicked button is still active and interactable
        if (clickedButton != null && clickedButton.activeInHierarchy)
        {
            Button btn = clickedButton.GetComponent<Button>();
            if (btn != null && btn.interactable && !IsExcluded(btn))
            {
                SelectButton(clickedButton);
                yield break;
            }
        }
        
        // If the clicked button is now disabled, select the next available one
        GameObject nextButton = FindNextActiveButton(clickedButton);
        if (nextButton != null)
        {
            SelectButton(nextButton);
            lastSelected = nextButton;
        }
        else
        {
            // 🎯 NEW: If no valid button found, clear selection entirely
            // This prevents accidentally selecting the pause button
            EventSystem.current.SetSelectedGameObject(null);
        }
    }
}