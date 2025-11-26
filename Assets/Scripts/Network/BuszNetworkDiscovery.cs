using UnityEngine;
using Mirror;
using Mirror.Discovery;
using System.Net;


/// LAN-on automatikusan felderíti a létrehozott szobákat.
/// Szobák listáját szolgáltatja a Join UI-nak.

public class BuszNetworkDiscovery : NetworkDiscoveryBase<ServerRequest, ServerResponse>
{
    #region Server (Host)

    
    /// Host válaszol a discovery request-ekre
    
    protected override ServerResponse ProcessRequest(ServerRequest request, IPEndPoint endpoint)
    {
        // Host válasza, amit a client-ek látni fognak
        return new ServerResponse
        {
            serverId = ServerId,
            uri = transport.ServerUri(),
            busName = NetworkRoomData.Instance != null ? NetworkRoomData.Instance.busName : "Busz",
            currentPlayers = NetworkRoomData.Instance != null ? NetworkRoomData.Instance.playerNames.Count : 0,
            maxPlayers = NetworkRoomData.Instance != null ? NetworkRoomData.Instance.maxPlayers : 4
        };
    }

    #endregion

    #region Client

    
    /// Client megtalál egy host-ot
    
    protected override void ProcessResponse(ServerResponse response, IPEndPoint endpoint)
    {
        // Szoba hozzáadása a listához
        Debug.Log($"[Discovery] Found server: {response.busName} ({response.currentPlayers}/{response.maxPlayers}) at {endpoint.Address}");

        // Event küldése a UI-nak
        OnServerFoundEvent?.Invoke(response, endpoint);
    }

    #endregion

    #region Events

    
    /// Event amikor találunk egy szervert (UI feliratkozhat rá)
    
    public System.Action<ServerResponse, IPEndPoint> OnServerFoundEvent;

    #endregion
}

#region Server Response Data


/// Host-tól kapott válasz adatai

[System.Serializable]
public struct ServerResponse : NetworkMessage
{
    public long serverId;
    public System.Uri uri;
    
    // Custom adatok a Buszozas játékhoz
    public string busName;
    public int currentPlayers;
    public int maxPlayers;
}


/// Client discovery request-je (lehet üres)

[System.Serializable]
public struct ServerRequest : NetworkMessage
{
    // Jelenleg nincs extra adat
}

#endregion
