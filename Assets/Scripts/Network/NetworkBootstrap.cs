using Netcode.Transports.Facepunch;
using Steamworks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.UI;

public class NetworkBootstrap : MonoBehaviour
{
    [SerializeField] NetworkManager networkManager;

    [Header("Settings")]
    [SerializeField] TransportMode transportMode = TransportMode.Unity;
    [SerializeField] Button createLobbyButton;
    [SerializeField] Button joinLobbyButton;

    [Header("Transports")]
    [SerializeField] UnityTransport unityTransport;
    [SerializeField] FacepunchTransport steamTransport;
    bool ownsSteamClient;

    void Awake()
    {
        if (ConfigureTransport() && transportMode == TransportMode.Steam)
        {
            InitializeSteam();
        }
    }

    void Update()
    {
        // Lobby callbacks must run before a host/client exists. Once Netcode starts,
        // FacepunchTransport takes over callback processing in OnEarlyUpdate.
        if (transportMode == TransportMode.Steam && SteamClient.IsValid && (networkManager == null || !networkManager.IsListening))
        {
            SteamClient.RunCallbacks();
        }
    }

    bool ConfigureTransport()
    {
        if (networkManager == null)
        {
            Debug.LogError("[NetworkBootstrap] Assign a NetworkManager in the inspector.");
            return false;
        }

        switch (transportMode)
        {
            case TransportMode.Unity:
                if (unityTransport == null)
                {
                    Debug.LogError("[NetworkBootstrap] Assign a UnityTransport in the inspector.");
                    return false;
                }

                createLobbyButton.onClick.AddListener(
                    transform.GetComponentInChildren<UnityLobbyManager>().CreateLobby
                );

                joinLobbyButton.gameObject.SetActive(true);

                networkManager.NetworkConfig.NetworkTransport = unityTransport;
                Debug.Log("[NetworkBootstrap] Using Unity Transport.");
                return true;

            case TransportMode.Steam:
                if (steamTransport == null)
                {
                    Debug.LogError("[NetworkBootstrap] Assign a FacepunchTransport in the inspector.");
                    return false;
                }

                createLobbyButton.onClick.AddListener(
                    transform.GetComponentInChildren<SteamLobbyManager>().CreateLobby
                );

                joinLobbyButton.gameObject.SetActive(false);

                networkManager.NetworkConfig.NetworkTransport = steamTransport;
                Debug.Log("[NetworkBootstrap] Using Facepunch Transport.");
                return true;
        }

        return false;
    }

    void InitializeSteam()
    {
        if (SteamClient.IsValid)
        {
            return;
        }

        try
        {
            SteamClient.Init(steamTransport.SteamAppId, false);
            ownsSteamClient = SteamClient.IsValid;
            Debug.Log($"[NetworkBootstrap] Steam initialized for App ID {steamTransport.SteamAppId}.");
        }

        catch (System.Exception exception)
        {
            Debug.LogException(exception);
        }
    }

    void OnApplicationQuit()
    {
        if (ownsSteamClient && SteamClient.IsValid)
        {
            SteamClient.Shutdown();
        }
    }
}
