using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using TMPro;

public class GameVars : MonoBehaviour
{
    #region Változók
    [SerializeField] private Debugger debugger;

    private string busName = "SINGLEPLAYER";
    private int numberOfPlayers = 10;
    private bool reversedPyramidMode;
    private string[] playerNames = new string[10] { "Player 1", "Player 2", "Player 3", "Player 4", "Player 5", "Player 6", "Player 7", "Player 8", "Player 9", "Player 10" };

    [SerializeField] private TMP_InputField[] playerNameInputFieldsSinglePlayer;
    [SerializeField] private TMP_InputField busNameInputFieldMultiPlayer;
    [SerializeField] private Slider numberOfPlayersSliderSinglePlayer;
    [SerializeField] private Slider numberOfPlayersSliderMultiPlayer;
    [SerializeField] private Toggle revPyramidModeToggleSinglePlayer;
    [SerializeField] private Toggle revPyramidModeToggleMultiPlayer;

    #endregion

    #region Getterek és Setterek

    public string BusName { get => busName; set => busName = value; }
    public bool ReversedPyramidMode
    {
        get => reversedPyramidMode;
        set
        {
            reversedPyramidMode = value;
            SyncToggleStates();
        }
    }
    public int NumberOfPlayersInGame
    {
        get => numberOfPlayers;
        set
        {
            numberOfPlayers = Mathf.Clamp(value, 1, 10);
            SyncSliderValues();
        }
    }

    #endregion

    #region Unity metódusok

    void Awake()
    {
        if (debugger == null)
        {
            debugger = Resources.FindObjectsOfTypeAll<Debugger>().FirstOrDefault();
        }

        if (revPyramidModeToggleSinglePlayer != null)
        {
            revPyramidModeToggleSinglePlayer.isOn = ReversedPyramidMode;
        }

        if (revPyramidModeToggleMultiPlayer != null)
        {
            revPyramidModeToggleMultiPlayer.isOn = ReversedPyramidMode;
        }
    }

    void Start()
    {
        if (playerNameInputFieldsSinglePlayer != null)
        {
            for (int i = 0; i < playerNameInputFieldsSinglePlayer.Length; i++)
            {
                int index = i;
                playerNameInputFieldsSinglePlayer[i].onValueChanged.AddListener((value) =>
                {
                    if (index < numberOfPlayers)
                        SetPlayerName(index, value);
                });
            }
        }
        
        if (busNameInputFieldMultiPlayer != null)
        {
            busNameInputFieldMultiPlayer.onValueChanged.AddListener((value) =>
            {
                BusName = value;
            });
        }
    }

    // Update is called once per frame
    void Update()
    {
        debugger.UpdatePersistentLog("NumberOfPlayersInGame", NumberOfPlayersInGame.ToString());
        debugger.UpdatePersistentLog("ReversedPyramidMode", ReversedPyramidMode.ToString());
        debugger.UpdatePersistentLog("PlayerNames", string.Join("\n\n", playerNames));
    }

    #endregion

    #region Metódusok

    public string GetPlayerName(int index)
    {
        if (index >= 0 && index < playerNames.Length)
        {
            return playerNames[index];
        }
        else
        {
            Debug.LogWarning($"GetPlayerName: Invalid index {index}. Returning default name.");
            return $"Player {index + 1}";
        }
    }
    public void SetPlayerName(int index, string name)
    {
        if (index >= 0 && index < playerNames.Length)
        {
            if (playerNameInputFieldsSinglePlayer != null && index < playerNameInputFieldsSinglePlayer.Length)
            {
                playerNameInputFieldsSinglePlayer[index].text = playerNames[index];
            }
        }
        else
        {
            Debug.LogWarning($"SetPlayerName: Invalid index {index}. Name not set.");
        }
    }

    // MULTIPLAYER
    public void SetPlayerNameFromNetwork(int clientId, string name)
    {
        SetPlayerName(clientId, name);
    }

    public void ToggleReversedPyramidMode()
    {
        ReversedPyramidMode = !ReversedPyramidMode;
    }

    public void SetReversedPyramidMode(bool value)
    {
        ReversedPyramidMode = value;
        if (revPyramidModeToggleSinglePlayer != null)
        {
            revPyramidModeToggleSinglePlayer.isOn = ReversedPyramidMode;
        }

        if (revPyramidModeToggleMultiPlayer != null)
        {
            revPyramidModeToggleMultiPlayer.isOn = ReversedPyramidMode;
        }
    }

    public void SetNumberOfPlayers(float value)
    {
        SetNumberOfPlayers((int)value);
    }

    public void SetNumberOfPlayers(int value)
    {
        numberOfPlayers = Mathf.Clamp(value, 2, 10);
        ResizePlayerNamesArray();
        SyncSliderValues();
    }

    private void ResizePlayerNamesArray()
    {
        if (playerNames == null || playerNames.Length != numberOfPlayers)
        {
            string[] newArray = new string[numberOfPlayers];
            for (int i = 0; i < numberOfPlayers; i++)
            {
                if (playerNames != null && i < playerNames.Length)
                    newArray[i] = playerNames[i]; // Megőrzi a régi neveket
                else
                    newArray[i] = $"Player {i + 1}";
            }
            playerNames = newArray;
        }
    }

    private void SyncToggleStates()
    {
        if (revPyramidModeToggleSinglePlayer != null)
            revPyramidModeToggleSinglePlayer.isOn = reversedPyramidMode;

        if (revPyramidModeToggleMultiPlayer != null)
            revPyramidModeToggleMultiPlayer.isOn = reversedPyramidMode;
    }
    
    private void SyncSliderValues()
    {
        if (numberOfPlayersSliderSinglePlayer != null)
            numberOfPlayersSliderSinglePlayer.value = numberOfPlayers;
        
        if (numberOfPlayersSliderMultiPlayer != null)
            numberOfPlayersSliderMultiPlayer.value = numberOfPlayers;
    }

    #endregion
}
