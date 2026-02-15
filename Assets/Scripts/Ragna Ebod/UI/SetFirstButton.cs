using UnityEngine;
using UnityEngine.EventSystems;

public class SetFirstButton : MonoBehaviour
{
    [Header("The Button to highlight when this panel opens")]
    [SerializeField] private GameObject firstButton;

    private void OnEnable()
    {
        // When this panel is turned ON (SetActive true), force the selection
        if (firstButton != null)
        {
            // Clear current selection first to avoid glitches
            EventSystem.current.SetSelectedGameObject(null);
            
            // Set the new selection
            // EventSystem.current.SetSelectedGameObject(firstButton);
        }
    }
}