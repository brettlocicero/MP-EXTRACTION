using Unity.Netcode;
using UnityEngine;

public class ConnectionManager : MonoBehaviour
{
    void Start()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("[ConnectionManager] NetworkManager.Singleton was not found.");
            enabled = false;
            return;
        }

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        
        NetworkManager.Singleton.OnServerStarted += OnServerStarted;
    }

    void OnDestroy()
    {
        // Clean up callbacks when the object is destroyed to prevent memory leaks
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
            NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
        }
    }

    void OnServerStarted()
    {
        Debug.Log("[ConnectionManager] Server is successfully running.");

        // Check authority using the Singleton directly
        if (NetworkManager.Singleton.IsServer)
        {
            if (GameManager.Instance != null)
            {
                Debug.Log("[ConnectionManager] Starting game manager...");
                // GameManager.Instance.StartGameSession();
            }
            
            else
            {
                Debug.LogError("[ConnectionManager] Found no GameManager instance in the scene!");
            }
        }
    }

    void OnClientConnected(ulong clientId)
    {
        Debug.Log($"Connected: {clientId}");
    }

    void OnClientDisconnected(ulong clientId)
    {
        Debug.Log($"Disconnected: {clientId}");
    }

    public bool StartHost()
    {
        if (NetworkManager.Singleton == null || NetworkManager.Singleton.IsListening)
        {
            return false;
        }

        return NetworkManager.Singleton.StartHost();
    }

    public bool StartClient()
    {
        if (NetworkManager.Singleton == null || NetworkManager.Singleton.IsListening)
        {
            return false;
        }

        return NetworkManager.Singleton.StartClient();
    }

    public void Stop()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.Shutdown();
        }
    }
}
