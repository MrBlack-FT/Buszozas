using UnityEngine;
using UnityEngine.UI;

public class CustomSliderInteraction : MonoBehaviour
{
    private Slider slider;
    private TMPro.TextMeshProUGUI tmpText;

    void Start()
    {
        slider = GetComponent<Slider>();
        tmpText = GetComponentInChildren<TMPro.TextMeshProUGUI>();
        
        if (slider != null && tmpText != null)
        {
            tmpText.text = slider.value.ToString();
            slider.onValueChanged.AddListener(value => tmpText.text = value.ToString());
        }
        else
        {
            Debug.LogWarning($"CustomSliderInteraction on {gameObject.name}: Missing Slider or TMP component!");
        }
    }
}