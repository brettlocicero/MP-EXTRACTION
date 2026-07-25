using Netcode.Transports.Facepunch;
using Steamworks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public class NetworkBootstrapper : MonoBehaviour
{
    [Header("Bootstrapper Settings")]
    [SerializeField] TransportMode transportMode = TransportMode.Unity;
    [SerializeField] uint steamAppId = 480;

    [Header("Transport References")]
    [SerializeField] UnityTransport unityTransport;
    [SerializeField] FacepunchTransport steamTransport;

    void Start()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("[Bootstrapper] NetworkManager.Singleton was not found!");
            return;
        }

        if (transportMode == TransportMode.Steam)
        {
            InitializeSteam();
        }

        ConfigureTransport();
    }

    void InitializeSteam()
    {
        if (SteamClient.IsValid)
            return;

        try
        {
            SteamClient.Init(steamAppId);
            Debug.Log($"[Bootstrapper] Steam initialized successfully. AppID: {SteamClient.AppId} ({SteamClient.Name})");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Bootstrapper] Failed to initialize Steam: {e.Message}");
            Debug.LogWarning("[Bootstrapper] Falling back to Unity Transport.");
            transportMode = TransportMode.Unity;
        }
    }

    void ConfigureTransport()
    {
        switch (transportMode)
        {
            case TransportMode.Unity:
                if (unityTransport == null)
                {
                    Debug.LogError("[Bootstrapper] UnityTransport reference is missing.");
                    return;
                }

                NetworkManager.Singleton.NetworkConfig.NetworkTransport = unityTransport;
                Debug.Log("[Bootstrapper] Using Unity Transport.");
                break;

            case TransportMode.Steam:
                if (!SteamClient.IsValid)
                {
                    Debug.LogWarning("[Bootstrapper] Steam is not initialized. Falling back to Unity Transport.");

                    if (unityTransport == null)
                    {
                        Debug.LogError("[Bootstrapper] UnityTransport reference is missing.");
                        return;
                    }

                    NetworkManager.Singleton.NetworkConfig.NetworkTransport = unityTransport;
                    transportMode = TransportMode.Unity;
                    break;
                }

                if (steamTransport == null)
                {
                    Debug.LogError("[Bootstrapper] FacepunchTransport reference is missing.");
                    return;
                }

                NetworkManager.Singleton.NetworkConfig.NetworkTransport = steamTransport;
                Debug.Log("[Bootstrapper] Using Facepunch Steam Transport.");
                break;
        }

        LogNetworkConfig();
    }

    void LogNetworkConfig()
    {
        var config = NetworkManager.Singleton.NetworkConfig;

        Debug.Log(
            $"[Bootstrapper] NetworkConfig\n" +
            $"Transport: {config.NetworkTransport.GetType().Name}\n" +
            $"ProtocolVersion: {config.ProtocolVersion}\n" +
            $"TickRate: {config.TickRate}\n" +
            $"SceneManagement: {config.EnableSceneManagement}\n" +
            $"NetworkPrefabs: {config.Prefabs.Prefabs.Count}"
        );
    }

    void OnApplicationQuit()
    {
        if (SteamClient.IsValid)
        {
            SteamClient.Shutdown();
            Debug.Log("[Bootstrapper] Steam shut down.");
        }
    }
}