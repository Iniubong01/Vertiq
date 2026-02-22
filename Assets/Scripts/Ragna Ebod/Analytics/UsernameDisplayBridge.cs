using UnityEngine;
using TMPro;

[RequireComponent(typeof(TMP_Text))]
public class UsernameDisplayBridge : MonoBehaviour
{
    private TMP_Text _text;

    private void Awake()
    {
        _text = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        if (UsernameUI.Instance != null)
            UsernameUI.Instance.RegisterDisplay(_text);
    }

    private void OnDisable()
    {
        if (UsernameUI.Instance != null)
            UsernameUI.Instance.UnregisterDisplay(_text);
    }
}