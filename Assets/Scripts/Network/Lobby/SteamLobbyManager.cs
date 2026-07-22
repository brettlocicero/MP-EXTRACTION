using Netcode.Transports.Facepunch;
using Steamworks;
using Steamworks.Data;
using Unity.Netcode;
using UnityEngine;

public class SteamLobbyManager : MonoBehaviour
{
    Lobby currentLobby;

    void OnEnable()
    {
        SteamFriends.OnGameLobbyJoinRequested += OnGameLobbyJoinRequested;
    }

    void OnDisable()
    {
        SteamFriends.OnGameLobbyJoinRequested -= OnGameLobbyJoinRequested;
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
        currentLobby.SetJoinable(true);
        currentLobby.SetData("HostId", SteamClient.SteamId.ToString());

        if (NetworkManager.Singleton.NetworkConfig.NetworkTransport is FacepunchTransport transport)
        {
            transport.targetSteamId = SteamClient.SteamId;
        }
        
        else
        {
            Debug.LogError("The active transport is NOT FacepunchTransport!");
            return;
        }

        NetworkManager.Singleton.StartHost();
        Debug.Log($"Created Steam lobby {currentLobby.Id}");

        SteamFriends.OpenGameInviteOverlay(currentLobby.Id);
    }

    async void OnGameLobbyJoinRequested(Lobby lobby, SteamId steamId)
    {
        Debug.Log($"Steam Overlay 'Join Game' requested for Lobby: {lobby.Id}");
        await JoinLobby(lobby.Id);
    }

    public async System.Threading.Tasks.Task JoinLobby(SteamId lobbyId)
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
            Debug.LogError("Lobby has no HostId meta-data!");
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

    public void LeaveLobby()
    {
        currentLobby.Leave();
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }
    }
}