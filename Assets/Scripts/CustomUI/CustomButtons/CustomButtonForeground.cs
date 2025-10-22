using UnityEngine;
using UnityEngine.UI;

public class CustomButtonForeground : MonoBehaviour
{
    #region Változók

    private Image _foregroundImage;
    private Color _foregroundColor;
    private bool _isInteractive = true;

    #endregion

    #region Getterek és Setterek

    public Image ForegroundImage {get => _foregroundImage;set => _foregroundImage = value;}
    public Color ForegroundColor {get => _foregroundColor;set => _foregroundColor = value;}
    public bool  IsInteractive   {get => _isInteractive;  set => _isInteractive = value;}

    #endregion

    #region Awake

    private void Awake()
    {
        // Keresd meg a "Foreground" nevű gyereket, és szerezd meg az Image komponenst
        Transform foregroundTransform = transform.Find("Foreground - Image");
        if (foregroundTransform != null)
        {
            ForegroundImage = foregroundTransform.GetComponent<Image>();
            if (ForegroundImage != null)
            {
                ForegroundColor = ForegroundImage.color;
            }
            else
            {
                Debug.LogWarning("No Image component found on Foreground child of " + gameObject.name);
            }
        }
        else
        {
            Debug.LogWarning("No Foreground child found on " + gameObject.name);
        }
    }

    #endregion

    #region Metódusok

    public void SetInteractiveState(bool state)
    {
        if (state != IsInteractive)
        {
            ChangeInteractiveState();
        }
    }

    public void ChangeInteractiveState()
    {
        if (IsInteractive)
        {
            ForegroundImage.color = new Color(96f / 255f, 96f / 255f, 96f / 255f, 1f);
            IsInteractive = false;
            GetComponent<Button>().interactable = false;
        }
        else
        {
            ForegroundImage.color = ForegroundColor;
            IsInteractive = true;
            GetComponent<Button>().interactable = true;
        }
    }
    #endregion
}