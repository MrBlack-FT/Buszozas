using UnityEngine.UI;

public class CustomDropdown_TMP: TMPro.TMP_Dropdown
{
    protected override DropdownItem CreateItem(DropdownItem itemTemplate)
    {
        var item = base.CreateItem(itemTemplate);
        var backgroundImage = item.transform.Find("Item Background")?.GetComponent<Image>();
        if (backgroundImage != null) backgroundImage.enabled = true; // 🔸 Engedélyezzük a képet
        return item;
    }

}
