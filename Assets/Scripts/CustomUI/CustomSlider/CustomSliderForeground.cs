using UnityEngine;
using UnityEngine.UI;

public class CustomSliderForeground : MonoBehaviour
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
        // Handle megkeresése a Slider-ben
        Slider slider = GetComponent<Slider>();
        if (slider != null && slider.handleRect != null)
        {
            ForegroundImage = slider.handleRect.GetComponent<Image>();
            if (ForegroundImage != null)
            {
                ForegroundColor = ForegroundImage.color;
            }
            else
            {
                Debug.LogWarning("No Image component found on Handle of " + gameObject.name);
            }
        }
        else
        {
            Debug.LogWarning("No Slider component or Handle found on " + gameObject.name);
        }
    }

    #endregion

    #region Metódusok

    public void ChangeInteractiveState()
    {
        if (IsInteractive)
        {
            ForegroundImage.color = new Color(96f / 255f, 96f / 255f, 96f / 255f, 1f);
            IsInteractive = false;
            GetComponent<Slider>().interactable = false;
        }
        else
        {
            ForegroundImage.color = ForegroundColor;
            IsInteractive = true;
            GetComponent<Slider>().interactable = true;
        }
    }
    
    #endregion
}