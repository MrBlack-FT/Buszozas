using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using DG.Tweening;

public class CustomPainter : MonoBehaviour
{
    #region Változók

    [SerializeField] private UIVars uiVars;

    // GameObject és annak a kép komponensének színét tároló Dictionary.
    private Dictionary<GameObject, Color> originalColors = new Dictionary<GameObject, Color>();

    #endregion


    #region Getterek és Setterek

    public Dictionary<GameObject, Color> OriginalColors { get => originalColors; }

    #endregion


    #region Start

    private void Start()
    {
        // Ha az interactivePanels nincs beállítva az Inspectorban, figyelmeztetést adunk
        if (uiVars.InteractivePanels == null || uiVars.InteractivePanels.Length == 0)
        {
            Debug.LogWarning("Interactive panels are not set in the Inspector!");
        }

        foreach (GameObject panel in uiVars.InteractivePanels)
        {
            // Keresd meg az összes Selectable komponenst a panel gyermekeiben
            Selectable[] selectables = panel.GetComponentsInChildren<Selectable>();

            foreach (Selectable selectable in selectables)
            {
                // Ellenőrizd, hogy van-e Image komponens
                Image image = selectable.GetComponent<Image>();
                if (image != null)
                {
                    // Tárold el az eredeti színt a Dictionary-ben
                    if (!originalColors.ContainsKey(selectable.gameObject))
                    {
                        originalColors[selectable.gameObject] = image.color;
                    }
                }
                else
                {
                    Debug.LogWarning($"Found Selectable GameObject without Image component: {selectable.gameObject.name}");
                }
            }
        }
    }

    #endregion


    #region Metódusok

    public void AddColorToDictionary(GameObject gameObject, Color color)
    {
        if (!originalColors.ContainsKey(gameObject))
        {
            originalColors[gameObject] = color;
        }
    }

    public void SaveGOColorToDictionary(GameObject gameObject)
    {
        Image image = gameObject.GetComponent<Image>();
        if (image != null)
        {
            AddColorToDictionary(gameObject, image.color);
        }
        else
        {
            Debug.LogWarning($"Found Selectable GameObject without Image component: {gameObject.name}");
        }
    }

    public void ChangeColor(GameObject gameObject, Color color)
    {
        if (originalColors.ContainsKey(gameObject))
        {
            Image image = gameObject.GetComponent<Image>();
            if (image != null)
            {
                image.DOColor(color, 0.5f).SetEase(Ease.OutCubic);
            }
            else
            {
                Debug.LogWarning($"GameObject {gameObject.name} does not have an Image component.");
            }
        }
        else
        {
            Debug.LogWarning($"GameObject {gameObject.name} is not in the original colors dictionary.");
        }
    }

    /*
    Image lastImage = LastSelected.GetComponent<Image>();
    if (lastImage != null && customPainter.OriginalColors.TryGetValue(LastSelected, out Color p_originalColor))
    {
        lastImage.color = p_originalColor;
    }
    */
    public void ResetColor(GameObject gameObject)
    {
        if (originalColors.ContainsKey(gameObject))
        {
            Image image = gameObject.GetComponent<Image>();
            if (image != null)
            {
                image.DOColor(originalColors[gameObject], 0.5f).SetEase(Ease.OutCubic);
            }
            else
            {
                Debug.LogWarning($"GameObject {gameObject.name} does not have an Image component.");
            }
        }
        else
        {
            Debug.LogWarning($"GameObject {gameObject.name} is not in the original colors dictionary.");
        }
    }

    #endregion
}
