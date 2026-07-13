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

    [Header("Transport Component References")]
    [SerializeField] UnityTransport unityTransport;
    [SerializeField] FacepunchTransport steamTransport;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        if (transportMode == TransportMode.Steam)
        {
            InitializeSteam();
        }
    }

    void Start()
    {
        ConfigureTransport();
    }

    void InitializeSteam()
    {
        if (SteamClient.IsValid) return;

        try
        {
            SteamClient.Init(steamAppId);
            Debug.Log($"[Bootstrapper] Steam successfully initialized. AppID: {SteamClient.AppId} ({SteamClient.Name})");
        }
        
        catch (System.Exception e)
        {
            Debug.LogError($"[Bootstrapper] Steam failed to initialize! Is Steam desktop app running? Error: {e.Message}");
            Debug.LogWarning("[Bootstrapper] Falling back to Unity Transport due to Steam initialization failure.");
            transportMode = TransportMode.Unity;
        }
    }

    void ConfigureTransport()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("[Bootstrapper] NetworkManager.Singleton is missing from the scene!");
            return;
        }

        switch (transportMode)
        {
            case TransportMode.Unity:
                if (unityTransport == null)
                {
                    Debug.LogError("[Bootstrapper] UnityTransport reference is missing in Inspector!");
                    return;
                }
                NetworkManager.Singleton.NetworkConfig.NetworkTransport = unityTransport;
                Debug.Log("[Bootstrapper] Active network transport set to: UnityTransport.");
                break;

            case TransportMode.Steam:
                if (!SteamClient.IsValid)
                {
                    Debug.LogError("[Bootstrapper] FacepunchTransport selected, but Steam is not initialized! Swapping to UnityTransport.");
                    NetworkManager.Singleton.NetworkConfig.NetworkTransport = unityTransport;
                    return;
                }

                if (steamTransport == null)
                {
                    Debug.LogError("[Bootstrapper] FacepunchTransport reference is missing in Inspector!");
                    return;
                }
                
                NetworkManager.Singleton.NetworkConfig.NetworkTransport = steamTransport;
                Debug.Log("[Bootstrapper] Active network transport set to: FacepunchTransport (Steam).");
                break;
        }
    }

    void OnApplicationQuit()
    {
        if (SteamClient.IsValid)
        {
            SteamClient.Shutdown();
            Debug.Log("[Bootstrapper] Steam Client shut down cleanly.");
        }
    }
}