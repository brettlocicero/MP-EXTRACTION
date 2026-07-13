using Unity.Netcode;
using UnityEngine;

public class ConnectionManager : MonoBehaviour
{
    void Start()
    {
        // Existing callbacks
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        
        // Add this callback to detect when the server finishes starting up
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
                GameManager.Instance.StartGameSession();
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

    public void StartHost()
    {
        NetworkManager.Singleton.StartHost();
    }

    public void StartClient()
    {
        NetworkManager.Singleton.StartClient();
    }

    public void Stop()
    {
        NetworkManager.Singleton.Shutdown();
    }
}