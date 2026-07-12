using Netcode.Transports.Facepunch;
using Steamworks;
using Steamworks.Data;
using Unity.Netcode;
using UnityEngine;

public class SteamLobbyManager : MonoBehaviour
{
    [SerializeField] ConnectionManager connectionManager;

    private Lobby currentLobby;

    void Start()
    {
        // CRITICAL: Subscribe to Steam's overlay and invite events
        SteamMatchmaking.OnLobbyInvite += OnLobbyInviteReceived;
        SteamMatchmaking.OnLobbyGameCreated += OnSteamGameJoinAction;
    }

    void OnDestroy()
    {
        // Always unsubscribe when the object is destroyed to prevent memory leaks
        SteamMatchmaking.OnLobbyInvite -= OnLobbyInviteReceived;
        SteamMatchmaking.OnLobbyGameCreated -= OnSteamGameJoinAction;
    }

    void Update()
    {
        if (SteamClient.IsValid)
        {
            SteamClient.RunCallbacks();
        }
    }

    public async void CreateLobby()
    {
        var lobbyResult = await SteamMatchmaking.CreateLobbyAsync(4);

        if (!lobbyResult.HasValue)
        {
            Debug.LogError("Failed to create Steam lobby: Result was null.");
            return;
        }

        currentLobby = lobbyResult.Value;
        currentLobby.SetPublic();
        
        // This makes the lobby searchable to friends
        currentLobby.SetJoinable(true); 
        currentLobby.SetData("HostId", SteamClient.SteamId.ToString());

        Debug.Log($"Created Steam lobby {currentLobby.Id}");

        NetworkManager.Singleton.StartHost();
        
        // Opens overlay to invite friends
        SteamFriends.OpenGameInviteOverlay(currentLobby.Id);
    }

    // 1. This triggers if a friend accepts an invite via Steam Chat
    private void OnLobbyInviteReceived(Friend friend, Lobby lobby)
    {
        Debug.Log($"Received lobby invite from {friend.Name}. Joining lobby {lobby.Id}...");
        JoinLobby(lobby.Id);
    }

    // 2. This triggers when Steam processes the "Join Game" overlay action
    private void OnSteamGameJoinAction(Lobby lobby, uint ip, ushort port, SteamId steamId)
    {
        Debug.Log($"Steam Overlay 'Join Game' clicked! Target Lobby: {lobby.Id}");
        JoinLobby(lobby.Id);
    }

    public async void JoinLobby(SteamId lobbyId)
    {
        var lobbyResult = await SteamMatchmaking.JoinLobbyAsync(lobbyId);

        if (!lobbyResult.HasValue)
        {
            Debug.LogError("Failed to join Steam lobby: Result was null.");
            return;
        }

        currentLobby = lobbyResult.Value;

        string hostIdString = currentLobby.GetData("HostId");
        if (string.IsNullOrEmpty(hostIdString))
        {
            Debug.LogError("Lobby has no HostId meta-data");
            return;
        }

        SteamId hostId = ulong.Parse(hostIdString);

        if (NetworkManager.Singleton.NetworkConfig.NetworkTransport is FacepunchTransport transport)
        {
            transport.targetSteamId = hostId;
            Debug.Log($"Transport target set to Host SteamID: {hostId}");
        }
        else
        {
            Debug.LogError("The active transport is NOT FacepunchTransport!");
            return;
        }

        NetworkManager.Singleton.StartClient();
        Debug.Log($"Netcode Client started for Steam lobby {currentLobby.Id}");
    }
}