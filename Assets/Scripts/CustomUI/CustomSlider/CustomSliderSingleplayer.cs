using UnityEngine;
using UnityEngine.UI;

public class CustomSliderSingleplayer : MonoBehaviour
{
    private Slider slider;
    private GameObject Players;
    private GameObject[] playerGameObjects;

    void Start()
    {
        slider = GetComponent<Slider>();
        
        // Players GameObject megkeresése - Ebben vannak a játékosok InputField-jei [nevek]
        Players = GameObject.Find("Players");
        if (Players != null)
        {
            // Get direct child GameObjects (player prefabs)
            int childCount = Players.transform.childCount;
            playerGameObjects = new GameObject[childCount];
            
            for (int i = 0; i < childCount; i++)
            {
                playerGameObjects[i] = Players.transform.GetChild(i).gameObject;
            }
            
            /*
            Debug.Log($"Player GameObjects found: {playerGameObjects.Length}");
            for (int i = 0; i < Players.transform.childCount; i++)
            {
                Debug.Log($"Child {i}: {Players.transform.GetChild(i).name}");
            }
            */
        }
        else
        {
            Debug.LogWarning("Players GameObject not found!");
        }
        
        if (slider != null)
        {
            UpdatePlayersCount((int)slider.value);

            slider.onValueChanged.AddListener(value => UpdatePlayersCount((int)value));
        }
        else
        {
            Debug.LogWarning($"CustomSliderSingleplayer on {gameObject.name}: Missing Slider component!");
        }

        if (Players == null)
        {
            Debug.LogWarning($"CustomSliderSingleplayer: Players GameObject not found!");
        }
    }

    private void UpdatePlayersCount(int sliderValue)
    {
        if (playerGameObjects == null) return;

        int playerCount = sliderValue;

        for (int i = 0; i < playerGameObjects.Length; i++)
        {
            if (playerGameObjects[i] != null)
            {
                playerGameObjects[i].SetActive(i < playerCount);    // i < playerCount -> boolean
            }
        }

        //Debug!
        string status = "";
        for (int i = 0; i < playerGameObjects.Length; i++)
        {
            if (playerGameObjects[i] != null)
            {
                status += playerGameObjects[i].name + ": " + (playerGameObjects[i].activeSelf ? "Active" : "Inactive") + "\n";
            }
        }
    }
}
