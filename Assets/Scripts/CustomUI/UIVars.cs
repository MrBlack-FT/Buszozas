using UnityEngine;

public class UIVars : MonoBehaviour
{
    #region Változók

    [SerializeField] private        GameObject[] interactivePanels;
                     public   enum  Direction { UP, DOWN, LEFT, RIGHT };
                     private        Direction _direction = Direction.UP;

    #endregion


    #region Getterek és Setterek

    public GameObject[] InteractivePanels{get => interactivePanels;}
    public Direction CurrentDirection { get => _direction; set => _direction = value; }

    #endregion


    #region Start

    private void Start()
    {
        // Ha az interactivePanels nincs beállítva az Inspectorban, figyelmeztetést adunk
        if (interactivePanels == null || interactivePanels.Length == 0)
        {
            Debug.LogWarning("Interactive panels are not set in the Inspector!");
        }
    }

    #endregion


    #region Metódusok



    #endregion
}
