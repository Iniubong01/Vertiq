using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

public class UISelectionKeeper : MonoBehaviour
{
    private GameObject lastSelected;
    [SerializeField] private GameObject defaultSelection; // Set this in Inspector to your first button
    [SerializeField] private Button[] allButtons; // Drag all buttons in your UI here

    void Start()
    {
        // Select the default button on start
        if (defaultSelection != null)
        {
            SelectButton(defaultSelection);
        }
        
        // If allButtons is not set, try to find all buttons automatically
        if (allButtons == null || allButtons.Length == 0)
        {
            allButtons = GetComponentsInChildren<Button>(true);
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
                if (btn != null && btn.interactable)
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
            
            // Last resort: use default selection
            if (defaultSelection != null && defaultSelection.activeInHierarchy)
            {
                Button btn = defaultSelection.GetComponent<Button>();
                if (btn != null && btn.interactable)
                {
                    SelectButton(defaultSelection);
                    lastSelected = defaultSelection;
                }
            }
        }
    }

    // Find the first active and interactable button
    private GameObject FindFirstActiveButton()
    {
        if (allButtons == null) return null;
        
        foreach (Button btn in allButtons)
        {
            if (btn != null && btn.gameObject.activeInHierarchy && btn.interactable)
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
            if (foundCurrent && btn != null && btn.gameObject.activeInHierarchy && btn.interactable)
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
            if (btn != null && btn.interactable)
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
        
        EventSystem.current.SetSelectedGameObject(null); // Clear first
        EventSystem.current.SetSelectedGameObject(button); // Then set
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
            if (btn != null && btn.interactable)
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
    }
}