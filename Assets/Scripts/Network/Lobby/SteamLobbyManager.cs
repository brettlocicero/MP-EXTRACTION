using Netcode.Transports.Facepunch;
using Steamworks;
using Steamworks.Data;
using Unity.Netcode;
using UnityEngine;

public class SteamLobbyManager : MonoBehaviour
{
    [SerializeField] ConnectionManager connectionManager;
    Lobby currentLobby;

    void Awake()
    {
        connectionManager = connectionManager != null ? connectionManager : FindAnyObjectByType<ConnectionManager>();
    }

    void OnEnable()
    {
        SteamFriends.OnGameLobbyJoinRequested += OnGameLobbyJoinRequested;
    }

    void OnDisable()
    {
        SteamFriends.OnGameLobbyJoinRequested -= OnGameLobbyJoinRequested;
    }

    public async void CreateLobby()
    {
        if (!TryGetSteamTransport(out FacepunchTransport transport))
        {
            return;
        }

        if (!SteamClient.IsValid)
        {
            Debug.LogError("Steam is not ready. Select Steam in NetworkBootstrap and make sure the Facepunch transport can initialize.");
            return;
        }

        if (NetworkManager.Singleton.IsListening)
        {
            Debug.LogWarning("Network is already running.");
            return;
        }

        transport.targetSteamId = SteamClient.SteamId;

        if (!connectionManager.StartHost())
        {
            Debug.LogError("Failed to start host.");
            return;
        }

        Lobby? lobbyResult;
        try
        {
            lobbyResult = await SteamMatchmaking.CreateLobbyAsync(4);
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception);
            connectionManager.Stop();
            return;
        }

        if (!lobbyResult.HasValue)
        {
            Debug.LogError("Failed to create Steam lobby.");

            NetworkManager.Singleton.Shutdown();
            return;
        }

        currentLobby = lobbyResult.Value;

        currentLobby.SetPublic();
        currentLobby.SetJoinable(true);
        currentLobby.SetData("HostId", SteamClient.SteamId.ToString());

        Debug.Log($"Created Steam Lobby: {currentLobby.Id}");
        // SteamFriends.OpenGameInviteOverlay(currentLobby.Id);
    }

    async void OnGameLobbyJoinRequested(Lobby lobby, SteamId steamId)
    {
        if (NetworkManager.Singleton.IsListening)
        {
            Debug.Log("Ignoring Steam join request because already in a game.");
            return;
        }

        Debug.Log($"Joining Steam Lobby: {lobby.Id}");
        await JoinLobby(lobby.Id);
    }

    public async System.Threading.Tasks.Task JoinLobby(SteamId lobbyId)
    {
        if (!TryGetSteamTransport(out FacepunchTransport transport))
        {
            return;
        }

        if (!SteamClient.IsValid)
        {
            Debug.LogError("Steam is not ready. Select Steam in NetworkBootstrap and make sure the Facepunch transport can initialize.");
            return;
        }

        if (NetworkManager.Singleton.IsListening)
        {
            connectionManager.Stop();
        }

        Lobby? lobbyResult;
        try
        {
            lobbyResult = await SteamMatchmaking.JoinLobbyAsync(lobbyId);
        }

        catch (System.Exception exception)
        {
            Debug.LogException(exception);
            return;
        }

        if (!lobbyResult.HasValue)
        {
            Debug.LogError("Failed to join Steam lobby.");
            return;
        }

        currentLobby = lobbyResult.Value;
        string hostIdString = currentLobby.GetData("HostId");

        if (string.IsNullOrEmpty(hostIdString))
        {
            Debug.LogError("Lobby has no HostId.");
            return;
        }

        if (!ulong.TryParse(hostIdString, out ulong hostIdValue))
        {
            Debug.LogError("Lobby HostId is invalid.");
            return;
        }

        SteamId hostId = hostIdValue;
        transport.targetSteamId = hostId;
        Debug.Log($"Connecting to host SteamID: {hostId}");

        if (!connectionManager.StartClient())
        {
            Debug.LogError("Failed to start client.");
            return;
        }

        Debug.Log($"Client started for lobby {currentLobby.Id}");
    }

    public void LeaveLobby()
    {
        if (currentLobby.Id != 0)
        {
            currentLobby.Leave();
            currentLobby = default;
        }

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            connectionManager.Stop();
        }

        Debug.Log("Left Steam lobby.");
    }

    bool TryGetSteamTransport(out FacepunchTransport transport)
    {
        transport = null;

        if (connectionManager == null)
        {
            Debug.LogError("SteamLobbyManager requires a ConnectionManager.");
            return false;
        }

        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("NetworkManager.Singleton was not found.");
            return false;
        }

        transport = NetworkManager.Singleton.NetworkConfig.NetworkTransport as FacepunchTransport;
        if (transport == null)
        {
            Debug.LogError("Steam lobbies require TransportMode.Steam (FacepunchTransport).");
            return false;
        }

        return true;
    }
}
