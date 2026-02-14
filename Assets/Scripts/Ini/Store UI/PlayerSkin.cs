using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PlayerSkin : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Sprite Settings")]
    [SerializeField] GameObject outline;
    [SerializeField] GameObject lockIcon;
    CanvasGroup canvasGroup;
    public int price;

    public bool isLocked = false;
    public bool isDeployed = false;

    private void Awake()
    {
        DisableOutline();
        canvasGroup = GetComponent<CanvasGroup>();
    }

    private void Start()
    {
        UpdateLockUI();
    }

    // --- GAMEPAD / KEYBOARD LOGIC ---
    public void OnSelect(BaseEventData eventData)
    {
        EnableOutline();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        DisableOutline();
    }

    #region Selection logic
    public void OnPointerEnter(PointerEventData eventData)
    {
        // Safety check
        if (EventSystem.current == null) return;
        // EnableOutline(); 
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Safety check
        if (EventSystem.current == null) return;
            // DisableOutline();
    }
    #endregion

    #region Unlock logic
    void UpdateLockUI()
    {
        lockIcon.SetActive(isLocked);

        if (canvasGroup != null)
        {
            canvasGroup.alpha = isLocked ? 0.5f : 1f;  // Tenary: Dim the skin a bit if it's locked
            // canvasGroup.interactable = !isLocked; // Disable interaction if locked 
        }
    }

    public void SetLocked(bool value)
    {
        isLocked = value;
        UpdateLockUI();
    }
    #endregion

    #region Outline logic
    private void EnableOutline()
    {
        if (outline != null)
        {
            outline.SetActive(true);
        }
    }

    private void DisableOutline()
    {
        if (outline != null)
        {
            outline.SetActive(false);
        }
    }
    #endregion
}
