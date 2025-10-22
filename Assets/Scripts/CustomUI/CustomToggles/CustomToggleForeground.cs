using UnityEngine;
using UnityEngine.UI;

public class CustomToggleForeground : MonoBehaviour
{
    #region Változók

    private Transform _background;
    private Image _foregroundImage;
    private Color _foregroundColor;
    private bool _isInteractive = true;

    #endregion

    #region Getterek és Setterek

    public Transform Background     {get => _background;     set => _background = value;}
    public Image ForegroundImage    {get => _foregroundImage;set => _foregroundImage = value;}
    public Color ForegroundColor    {get => _foregroundColor;set => _foregroundColor = value;}
    public bool  IsInteractive      {get => _isInteractive;  set => _isInteractive = value;}

    #endregion

    #region Awake

    private void Awake()
    {
        // Először megkeressük a Background-ot
        Background = transform.Find("Background");
        if (Background != null)
        {
            Transform foregroundTransform = Background.Find("Checkmark");
            if (foregroundTransform != null)
            {
                ForegroundImage = foregroundTransform.GetComponent<Image>();
                if (ForegroundImage != null)
                {
                    ForegroundColor = ForegroundImage.color;
                }
                else
                {
                    Debug.LogWarning($"No Image component found on Checkmark child of {gameObject.name}");
                }
            }
            else
            {
                Debug.LogWarning($"No Checkmark child found in Background of {gameObject.name}");
            }
        }
        else
        {
            Debug.LogWarning($"No Background child found on {gameObject.name}");
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
            GetComponent<Toggle>().interactable = false;
        }
        else
        {
            ForegroundImage.color = ForegroundColor;
            IsInteractive = true;
            GetComponent<Toggle>().interactable = true;
        }
    }
    
    #endregion
}