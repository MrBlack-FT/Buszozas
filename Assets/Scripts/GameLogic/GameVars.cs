using UnityEngine;
using System.Linq;

public class GameVars : MonoBehaviour
{
    #region Singleton
    public static GameVars Instance { get; private set; }
    #endregion

    #region Változók
    [SerializeField] private Debugger debugger;

    private string busName = "SINGLEPLAYER";
    private int numberOfPlayers = 10;
    private bool reversedPyramidMode;
    private string[] playerNames = new string[10] {"", "", "", "", "", "", "", "", "", ""};
    #endregion

    #region Getterek és Setterek

    public string BusName { get => busName; set => busName = value; }
    public bool ReversedPyramidMode { get => reversedPyramidMode; set => reversedPyramidMode = value; }
    public int NumberOfPlayersInGame
    {
        get => numberOfPlayers;
        set
        {
            numberOfPlayers = Mathf.Clamp(value, 2, 10);
            ResizePlayerNamesArray();
        }
    }

    #endregion

    #region Unity metódusok

    void Awake()
    {
        // Singleton + DontDestroyOnLoad
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(transform.root.gameObject);
        }
        else
        {
            // Ha már létezik egy példány, akkor az újat törölni kell.
            Destroy(gameObject);
            return;
        }

        if (debugger == null)
        {
            debugger = Resources.FindObjectsOfTypeAll<Debugger>().FirstOrDefault();
        }
    }

    void Update()
    {
        if (debugger != null && playerNames != null)
        {
            debugger.UpdatePersistentLog("NumberOfPlayersInGame", NumberOfPlayersInGame.ToString());
            debugger.UpdatePersistentLog("ReversedPyramidMode", debugger.ColoredString(ReversedPyramidMode.ToString(), ReversedPyramidMode ? Color.green : Color.red));
            debugger.UpdatePersistentLog("PlayerNames", "");
            debugger.UpdatePersistentLog("", string.Join("\n", playerNames));
        }
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
            playerNames[index] = name;
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
    // MULTIPLAYER

    public void ToggleReversedPyramidMode()
    {
        ReversedPyramidMode = !ReversedPyramidMode;
    }

    public void SetReversedPyramidMode(bool value)
    {
        ReversedPyramidMode = value;
    }

    public void SetNumberOfPlayers(float value)
    {
        SetNumberOfPlayers((int)value);
    }

    public void SetNumberOfPlayers(int value)
    {
        numberOfPlayers = Mathf.Clamp(value, 2, 10);
        ResizePlayerNamesArray();
    }

    private void ResizePlayerNamesArray()
    {
        if (playerNames == null || playerNames.Length != numberOfPlayers)
        {
            string[] newArray = new string[numberOfPlayers];
            for (int i = 0; i < numberOfPlayers; i++)
            {
                if (playerNames != null && i < playerNames.Length)
                {
                    // Korábbi nevek megtartása
                    newArray[i] = playerNames[i];
                }
                else
                {
                    //newArray[i] = $"Player {i + 1}";
                    newArray[i] = "";
                }
            }
            playerNames = newArray;
        }
    }

    public void ResetToDefaults()
    {
        BusName = "SINGLEPLAYER";
        NumberOfPlayersInGame = 10;
        ReversedPyramidMode = false;
        playerNames = new string[10] {"", "", "", "", "", "", "", "", "", ""};
    }

    public bool ValidateAndStartSinglePlayerGame()
    {
        for (int i = 0; i < NumberOfPlayersInGame; i++)
        {
            if (string.IsNullOrWhiteSpace(playerNames[i]))
            {
                return false;
            }
        }
        return true;
    }

    public bool ValidateAndStartMultiPlayerGame()
    {
        if (string.IsNullOrWhiteSpace(busName))
        {
            return false;
        }
        return true;
    }

    #endregion
}
