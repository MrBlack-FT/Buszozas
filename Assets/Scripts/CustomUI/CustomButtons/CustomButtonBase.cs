using UnityEngine;
using UnityEngine.EventSystems;

public abstract class CustomButtonBase : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler, IPointerExitHandler, IPointerDownHandler
{
    public virtual void OnPointerEnter(PointerEventData eventData){}

    public virtual void OnPointerExit(PointerEventData eventData){}

    public virtual void OnPointerDown(PointerEventData eventData){}

    public virtual void OnPointerClick(PointerEventData eventData){}
}