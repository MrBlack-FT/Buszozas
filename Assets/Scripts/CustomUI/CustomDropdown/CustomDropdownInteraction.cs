using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class CustomDropdownInteraction : MonoBehaviour
{
    #region Változók

    private TMP_Dropdown dropdown;

    #endregion

    #region Unity metódusok

    private void Awake()
    {
        dropdown = GetComponent<TMP_Dropdown>();
        if (dropdown == null)
        {
            Debug.LogWarning($"CustomDropdownInteraction: No TMP_Dropdown component found on {gameObject.name}");
            return;
        }
    }

    private void Update()
    {
        // Csak akkor működjön, ha a dropdown nyitva van
        if (dropdown != null && dropdown.IsExpanded)
        {
            if (checkArrowsInput())
            {
                GameObject currentSelected = EventSystem.current.currentSelectedGameObject;
                if (currentSelected != null && currentSelected.CompareTag("DropdownItem"))
                {
                    AdjustDropdownScroll(currentSelected);
                }
            }
        }
    }

    #endregion

    #region Metódusok

    private bool checkArrowsInput()
    {
        if
        (
            Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.DownArrow) ||
            Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow) ||
            Input.GetKeyDown(KeyCode.PageUp) || Input.GetKeyDown(KeyCode.PageDown) ||
            Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.DownArrow) ||
            Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.RightArrow) ||
            Input.GetKey(KeyCode.PageUp) || Input.GetKey(KeyCode.PageDown)
        )
        {
            return true;
        }
        return false;
    }

    private void AdjustDropdownScroll(GameObject selectedItem)
    {
        Scrollbar currentScrollbar = dropdown.GetComponentInChildren<Scrollbar>();
        if (currentScrollbar != null)
        {
            int selectedIndex = selectedItem.transform.GetSiblingIndex();
            int itemCount = dropdown.options.Count;
            
            if (itemCount > 1)
            {
                float normalizedPosition = (float)selectedIndex / (itemCount - 1);
                
                currentScrollbar.value = 1f - normalizedPosition;
                
                if (selectedIndex == itemCount - 2) // Utolsó elem-1
                {
                    currentScrollbar.value = 0f; // Teljesen alulra
                }
                else if (selectedIndex == 1) // Első elem+1
                {
                    currentScrollbar.value = 1f; // Teljesen felülre  
                }
            }
        }
        else
        {
            Debug.LogWarning("CustomDropdownInteraction: No Scrollbar found in children of " + gameObject.name);
        }
    }
    #endregion
}