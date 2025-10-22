using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CustomDropdownForeground : MonoBehaviour
{
    #region Változók

    private TextMeshProUGUI _foregroundText;
    private Color _foregroundColor;
    private bool _isInteractive = true;

    #endregion

    #region Getterek és Setterek

    public TextMeshProUGUI ForegroundText {get => _foregroundText;set => _foregroundText = value;}
    public Color ForegroundColor {get => _foregroundColor;set => _foregroundColor = value;}
    public bool  IsInteractive   {get => _isInteractive;  set => _isInteractive = value;}

    #endregion

    #region Awake

    private void Awake()
    {
        // Keresd meg a "Label" nevű gyereket, és szerezd meg a Text komponenst
        Transform foregroundTransform = transform.Find("Label");
        if (foregroundTransform != null)
        {
            ForegroundText = foregroundTransform.GetComponent<TextMeshProUGUI>();
            if (ForegroundText != null)
            {
                ForegroundColor = ForegroundText.color;
            }
            else
            {
                Debug.LogWarning("No Text component found on Label child of " + gameObject.name);
            }
        }
        else
        {
            Debug.LogWarning("No Label child found on " + gameObject.name);
        }
    }

    #endregion

    #region Metódusok

    public void ChangeInteractiveState()
    {
        if (IsInteractive)
        {
            ForegroundText.color = new Color(96f / 255f, 96f / 255f, 96f / 255f, 1f);
            IsInteractive = false;
            GetComponent<Dropdown>().interactable = false;
        }
        else
        {
            ForegroundText.color = ForegroundColor;
            IsInteractive = true;
            GetComponent<Dropdown>().interactable = true;
        }
    }
    
    #endregion
}