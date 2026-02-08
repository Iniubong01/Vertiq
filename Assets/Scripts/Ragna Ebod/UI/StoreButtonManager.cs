using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class StoreButtonManager : MonoBehaviour
{
    [SerializeField] private Button[] storeButtons;
    
    private void Start()
    {
        // Auto-add listeners to all buttons
        foreach (Button btn in storeButtons)
        {
            btn.onClick.AddListener(() => HandleButtonClick(btn));
        }
        
        // Select first active button
        SelectFirstActiveButton();
    }
    
    private void HandleButtonClick(Button clickedButton)
    {
        StartCoroutine(RestoreSelectionAfterClick(clickedButton));
    }
    
    private IEnumerator RestoreSelectionAfterClick(Button clickedButton)
    {
        yield return new WaitForEndOfFrame();
        
        // If the button is still interactable, keep it selected
        if (clickedButton.interactable)
        {
            EventSystem.current.SetSelectedGameObject(clickedButton.gameObject);
        }
        else
        {
            // Button was disabled, select the next available one
            SelectFirstActiveButton();
        }
    }
    
    private void SelectFirstActiveButton()
    {
        foreach (Button btn in storeButtons)
        {
            if (btn.gameObject.activeInHierarchy && btn.interactable)
            {
                EventSystem.current.SetSelectedGameObject(btn.gameObject);
                return;
            }
        }
    }
}