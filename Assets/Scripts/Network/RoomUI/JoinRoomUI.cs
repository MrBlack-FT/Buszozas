using UnityEngine;
using TMPro;
using System.Net;
using System.Collections.Generic;
using UnityEngine.UI;
using DG.Tweening;

public class JoinRoomUI : MonoBehaviour
{
    #region Változók
    [SerializeField] private UIActionTriggers uiActionTriggers;
    [SerializeField] private BuszNetworkDiscovery buszNetworkDiscovery;
    [SerializeField] private TMP_InputField playerNameInput;
    [SerializeField] private Button refreshButton;
    [SerializeField] private Transform serverListContainer; // ScrollView Content
    [SerializeField] private GameObject serverEntryPrefab;  // Egy szoba UI elem prefabja

    // A szobákat tárolni kell dictionary-ben, hogy ne legyen duplikáció.
    private Dictionary<long, GameObject> discoveredServers = new Dictionary<long, GameObject>();
    // A korábbi felfedezett szobákat külön dictionary-ben tároljuk a timeout kezeléséhez.
    private Dictionary<long, float> serverLastSeen = new Dictionary<long, float>();
    private float serverTimeout = 5f; // 5 másodperc után törlés

    #endregion

    #region Unity metódusok

    private void Start()
    {
        // Discovery feliratkozás
        var discovery = buszNetworkDiscovery;
        if (discovery != null)
        {
            discovery.OnServerFoundEvent += OnServerFound;
            discovery.StartDiscovery(); // Discovery indítása
        }
    }
    private void Update()
    {
        // Timeout ellenőrzés - töröljük a régi szobákat
        List<long> toRemove = new List<long>();
        foreach (var kvp in serverLastSeen)
        {
            if (Time.time - kvp.Value > serverTimeout)
            {
                toRemove.Add(kvp.Key);
            }
        }

        foreach (long serverId in toRemove)
        {
            if (discoveredServers.ContainsKey(serverId))
            {
                Destroy(discoveredServers[serverId]);
                discoveredServers.Remove(serverId);
            }
            serverLastSeen.Remove(serverId);
        }
    }

    #endregion

    #region Metódusok

    private void OnServerFound(ServerResponse response, IPEndPoint endpoint)
    {
        // Frissítjük a "last seen" időt.
        serverLastSeen[response.serverId] = Time.time;

        // Duplikáció szűrés - ha már létezik, akkor csak frissítjük az információkat.
        if (discoveredServers.ContainsKey(response.serverId))
        {
            var existingEntry = discoveredServers[response.serverId].GetComponent<ServerEntryUI>();
            existingEntry?.SetData(response, endpoint);
            return;
        }

        // Szoba hozzáadása a listához.
        GameObject entry = Instantiate(serverEntryPrefab, serverListContainer);
        discoveredServers[response.serverId] = entry;

        // Szoba adatok megjelenítése.
        var entryUI = entry.GetComponent<ServerEntryUI>();
        entryUI.SetData(response, endpoint);
        entryUI.OnJoinClicked = () => JoinServer(endpoint.Address.ToString());
    }

    private void JoinServer(string ipAddress)
    {
        string playerName = playerNameInput.text;

        if (string.IsNullOrWhiteSpace(playerName))
        {
            Debug.LogWarning("[JoinRoomUI -> JoinServer] Player name cannot be empty!");
            return;
        }

        // Játékos név mentése PlayerPrefs-be
        PlayerPrefs.SetString("playerName", playerName);
        PlayerPrefs.Save();

        var manager = FindFirstObjectByType<BuszNetworkManager>();
        manager.JoinRoom(ipAddress, playerName);

        ShowLobbyPanel();
    }

    private void ShowLobbyPanel()
    {
        DOVirtual.DelayedCall(0.25f, () =>
        {
            uiActionTriggers.AddCloseMultiplayerLobby();
            uiActionTriggers.AddOpenMultiplayerRoom();
            uiActionTriggers.RunSequence("CloseMultiplayerLobby - OpenMultiplayerRoom");
        });
    }

    public void OnRefreshButtonClicked()
    {
        // Felfedezés újraindítása
        var discovery = buszNetworkDiscovery;
        if (discovery != null)
        {
            discovery.StopDiscovery();
            discovery.StartDiscovery();
        }
    }

    #endregion
}