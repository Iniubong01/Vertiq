using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

public class SetFirstButton : MonoBehaviour
{
    [Header("The Button to highlight when this panel opens")]
    [SerializeField] private GameObject firstButton;

    private void OnEnable()
    {
        Debug.Log($"[SetFirstButton] OnEnable called for {gameObject.name} - Starting button selection");
        
        // Use a coroutine to ensure the selection happens after the panel is fully active
        if (firstButton != null)
        {
            StartCoroutine(SelectButtonDelayed());
        }
    }

    private IEnumerator SelectButtonDelayed()
    {
        // Wait for end of frame to ensure all UI elements are fully initialized
        yield return new WaitForEndOfFrame();
        
        // Verify EventSystem exists
        if (EventSystem.current == null)
        {
            Debug.LogWarning("[SetFirstButton] No EventSystem found in scene!");
            yield break;
        }

        // Verify the button is active and interactable
        if (!firstButton.activeInHierarchy)
        {
            Debug.LogWarning($"[SetFirstButton] Button {firstButton.name} is not active!");
            yield break;
        }

        // Check if it's a Button component and if it's interactable
        var button = firstButton.GetComponent<Button>();
        if (button != null && !button.interactable)
        {
            Debug.LogWarning($"[SetFirstButton] Button {firstButton.name} is not interactable!");
            yield break;
        }

        // Clear current selection to force a fresh selection
        EventSystem.current.SetSelectedGameObject(null);
        
        // Wait one more frame after clearing
        yield return null;
        
        // Set the new selection
        EventSystem.current.SetSelectedGameObject(firstButton);
        
        // Verify the selection was successful
        if (EventSystem.current.currentSelectedGameObject == firstButton)
        {
            Debug.Log($"[SetFirstButton] ✅ Successfully selected {firstButton.name}");
        }
        else
        {
            Debug.LogWarning($"[SetFirstButton] ❌ Failed to select {firstButton.name}");
        }
    }
}