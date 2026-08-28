using Netcode.Transports.Facepunch;
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
    void Awake()
    {
        if (ConfigureTransport() && transportMode == TransportMode.Steam)
        {
            steamTransport.EnsureSteamReady();
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

    void OnApplicationQuit()
    {
        if (transportMode == TransportMode.Steam && steamTransport != null)
        {
            steamTransport.ShutdownSteamClient();
        }
    }
}
