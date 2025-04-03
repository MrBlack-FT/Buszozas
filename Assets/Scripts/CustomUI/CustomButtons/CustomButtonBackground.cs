using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;

public class CustomButtonBackground : CustomButtonBase
{
    #region Változók

    private Color _backgroundColor;
    [SerializeField] private float duration;

    #endregion

    #region Getterek és Setterek
    
    public Color BackgroundColor{get => _backgroundColor; set => _backgroundColor = value;}
    public float Duration{get => duration; set => duration = value;}

    #endregion

    #region Awake

    private void Awake()
    {
        BackgroundColor = GetComponent<Image>().color;
    }

    #endregion

    #region Metódusok

    public override void OnPointerEnter(PointerEventData eventData)
    {
        if (!GetComponent<Button>().interactable) return;

        //Debug.Log("Pointer Enter");
        EventSystem.current.SetSelectedGameObject(gameObject);
        //GetComponent<Image>().color = new Color(1, 0.5f, 0);


        GetComponent<Image>().DOFade(1, duration).SetEase(Ease.InOutSine);

        /*
        Kísérlet későbbre...
        GetComponent<Image>().DOColor(new Color(1, 0.5f, 0), Duration).SetEase(Ease.InBack).OnComplete(() =>
        {
            GetComponent<Image>().DOColor(BackgroundColor, Duration).SetEase(Ease.OutBack);
        });
        */
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        if (!GetComponent<Button>().interactable) return;

        //Debug.Log("Pointer Down");
        GetComponent<Image>().color = Color.green;
        GetComponent<Button>()?.onClick.Invoke();
        GetComponent<Image>().color = BackgroundColor;
    }

    public override void OnPointerClick(PointerEventData eventData)
    {
        if (!GetComponent<Button>().interactable) return;

        //Debug.Log("Pointer Click");
        GetComponent<Image>().color = Color.green;
        GetComponent<Button>()?.onClick.Invoke();
        GetComponent<Image>().color = BackgroundColor;
    }

    public override void OnPointerExit(PointerEventData eventData)
    {
        //Debug.Log("Pointer Exit");
        //GetComponent<Image>().color = BackgroundColor;

        GetComponent<Image>().DOFade(0, duration).SetEase(Ease.InOutSine);

        /*
        //Kísérlet későbbre...
        GetComponent<Image>().DOFade(0, duration).SetEase(Ease.InOutSine).OnComplete(() =>
        {
            GetComponent<Image>().DOFade(1, duration).SetEase(Ease.InOutSine);
        });
        */
    }
    
    #endregion
}
