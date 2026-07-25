using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public class UnityLobbyManager : MonoBehaviour
{
    [SerializeField] ConnectionManager connectionManager;

    [SerializeField] string hostAddress = "127.0.0.1";
    [SerializeField] ushort port = 7777;

    public void CreateLobby()
    {
        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

        transport.SetConnectionData("0.0.0.0", port);

        Debug.Log($"Starting host on port {port}");
        connectionManager.StartHost();
    }

    public void JoinLobby()
    {
        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

        transport.SetConnectionData(hostAddress, port);
        Debug.Log($"Connecting to {hostAddress}:{port}");
        connectionManager.StartClient();
    }
}