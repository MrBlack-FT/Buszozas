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


    #region Unity metódusok

    private void Start()
    {
        // Ha az interactivePanels nincs beállítva az Inspectorban, figyelmeztetést adunk
        if (uiVars.InteractivePanels == null || uiVars.InteractivePanels.Length == 0)
        {
            Debug.LogWarning("Interactive panels are not set in the Inspector!");
        }

        foreach (GameObject panel in uiVars.InteractivePanels)
        {
            // Keresd meg az összes Selectable komponenst a panel gyermekeiben - (true) => inaktívakkal együtt
            Selectable[] selectables = panel.GetComponentsInChildren<Selectable>(true);

            foreach (Selectable selectable in selectables)
            {
                if (selectable.name == "Item" || selectable.CompareTag("DropdownItem") || selectable.CompareTag("DropdownScrollBar")) continue;

                Image image = selectable.GetComponent<Image>();

                if (selectable.CompareTag("Slider"))
                {
                    image = selectable.GetComponent<Slider>().handleRect.gameObject.GetComponent<Image>();
                }
                else if (selectable.CompareTag("Toggle"))
                {
                    image = selectable.transform.Find("Background").gameObject.GetComponent<Image>();
                }

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
                    Debug.LogWarning($"Found Selectable GameObject without Image component in \"Start()\":" +
                                     $"\t\t GameObject: \"{selectable.gameObject.name}\"" +
                                     $"\t\t Parent: {selectable.gameObject.transform.parent.name}" +
                                     $"\t\t Grandparent: {selectable.gameObject.transform.parent.parent?.name}");
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

    /*
    public void RemoveColorFromDictionary(GameObject gameObject)
    {
        if (originalColors.ContainsKey(gameObject))
        {
            originalColors.Remove(gameObject);
        }
    }

    public void RemoveNullEntriesFromDictionary(List<GameObject> gameObjects)
    {
        foreach (GameObject go in gameObjects)
        {
            if (go == null && originalColors.ContainsKey(go))
            {
                originalColors.Remove(go);
            }
        }
    }
    */

    public void SaveGOColorToDictionary(GameObject gameObject)
    {
        Image image = gameObject.GetComponent<Image>();
        if (image != null)
        {
            AddColorToDictionary(gameObject, image.color);
        }
        else
        {
            Debug.LogWarning($"Found Selectable GameObject without Image component during \"SaveGOColorToDictionary\":" +
                             $"\t\t GameObject: \"{gameObject.name}\"" +
                             $"\t\t Parent: {gameObject.transform.parent.name}" +
                             $"\t\t Grandparent: {gameObject.transform.parent.parent?.name}");
        }
    }

    public void ChangeColor(GameObject gameObject, Color color)
    {
        if (gameObject == null) return;

        //if (gameObject.name == "Blocker") return;

        if (originalColors.ContainsKey(gameObject))
        {
            Image image = gameObject.GetComponent<Image>();

            if (image != null)
            {
                image.DOColor(color, 0.5f).SetEase(Ease.OutCubic);
            }
            else
            {
                Debug.LogWarning($"ChangeColor - GameObject \"{gameObject.name}\" does not have an Image component.");
            }
        }
        else if (gameObject.CompareTag("DropdownItemBackground"))
        {
            Image image = gameObject.GetComponent<Image>();
            if (image != null)
            {
                //image.DOColor(color, 0.5f).SetEase(Ease.OutCubic);
                image.color = color;
            }
            else
            {
                Debug.LogWarning($"ChangeColor - GameObject \"{gameObject.name}\" does not have an Image component.");
            }
        }
        else if (gameObject.CompareTag("DropdownScrollBar"))
        {
            Image image = gameObject.transform.Find("Sliding Area/Handle").GetComponent<Image>();
            if (image != null)
            {
                //image.DOColor(color, 0.5f).SetEase(Ease.OutCubic);
                image.color = color;
            }
            else
            {
                Debug.LogWarning($"ChangeColor - GameObject \"{gameObject.name}\" does not have an Image component.");
            }
        }
        else
        {
            Debug.LogWarning($"ChangeColor - GameObject \"{gameObject.name}\" is not in the original colors dictionary!  |  nincs az eredeti színek szótárában!");
        }
    }

    public void ResetColor(GameObject gameObject)
    {
        if (gameObject == null) return;

        //if (gameObject.name == "Blocker") return;

        if (originalColors.ContainsKey(gameObject))
        {
            Image image = gameObject.GetComponent<Image>();

            if (image != null)
            {
                image.DOColor(originalColors[gameObject], 0.5f).SetEase(Ease.OutCubic);
            }
            else
            {
                Debug.LogWarning($"ResetColor - GameObject {gameObject.name} does not have an Image component.");
            }
        }
        else if (gameObject.CompareTag("DropdownItemBackground"))
        {
            Image image = gameObject.GetComponent<Image>();

            if (image != null)
            {
                //image.DOColor(new Color(0f, 0f, 0f, 0f), 0.5f).SetEase(Ease.OutCubic);
                image.color = new Color(0f, 0f, 0f, 255f);
            }
            else
            {
                Debug.LogWarning($"ResetColor - GameObject {gameObject.name} does not have an Image component.");
            }
        }
        else if (gameObject.CompareTag("DropdownScrollBar"))
        {
            Image image = gameObject.transform.Find("Sliding Area/Handle").GetComponent<Image>();

            if (image != null)
            {
                //image.DOColor(new Color(1f, 1f, 1f, 1f), 0.5f).SetEase(Ease.OutCubic);
                image.color = new Color(1f, 1f, 1f, 255f);
            }
            else
            {
                Debug.LogWarning($"ResetColor - GameObject {gameObject.name} does not have an Image component.");
            }
        }
        else
        {
            Debug.LogWarning($"ResetColor - GameObject {gameObject.name} is not in the original colors dictionary!  |  nincs az eredeti színek szótárában!");
        }
    }

    //DEBUG!
    public void PrintDictionary()
    {
        string dictionaryText = "";
        foreach (var kvp in originalColors)
        {
            if (kvp.Key != null)
            {
                dictionaryText += $"{kvp.Key.name}:\t\t {kvp.Value}\n";
            }
        }
        Debug.Log($"Original Colors Dictionary:\n{dictionaryText}");
    }

    #endregion
}
