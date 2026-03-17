using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach this script to any UI Button that should act as a "Back" or "Close" button.
/// The UniversalBackInput script will look for active instances of this script.
/// </summary>
[RequireComponent(typeof(Button))]
public class BackButtonIdentifier : MonoBehaviour
{
    private Button _button;

    public Button Button
    {
        get
        {
            if (_button == null)
            {
                _button = GetComponent<Button>();
            }
            return _button;
        }
    }
}
