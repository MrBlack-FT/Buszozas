using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private CardManager cardManager;
    [SerializeField] private PlayerManager playerManager;
    private Deck deck;


    void Awake()
    {
        deck = new Deck();
    }

    void Start()
    {
        
    }


    void Update()
    {
        
    }
}
