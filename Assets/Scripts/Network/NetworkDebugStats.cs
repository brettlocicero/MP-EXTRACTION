using System.Text;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class NetworkDebugStats : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] TextMeshProUGUI debugText;

    [Header("Settings")]
    [SerializeField] float refreshRate = 0.25f;

    float timer;
    readonly StringBuilder builder = new();

    void Update()
    {
        timer += Time.unscaledDeltaTime;

        if (timer < refreshRate)
            return;

        timer = 0f;
        UpdateText();
    }

    void UpdateText()
    {
        if (debugText == null)
            return;

        builder.Clear();
        AppendFrameStats();

        bool networkRunning = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;

        if (networkRunning)
            AppendNetworkStats(NetworkManager.Singleton);
        else
            builder.AppendLine("\nNetwork: Not Running");

        debugText.text = builder.ToString();
    }

    void AppendFrameStats()
    {
        float fps = 1f / Mathf.Max(Time.unscaledDeltaTime, 0.0001f);
        float frameMs = Time.unscaledDeltaTime * 1000f;

        builder.AppendLine($"FPS: {fps:F0}");
        builder.AppendLine($"Frame: {frameMs:F1} ms");
    }

    void AppendNetworkStats(NetworkManager networkManager)
    {
        builder.AppendLine();
        builder.AppendLine($"Transport: {networkManager.NetworkConfig.NetworkTransport.GetType().Name}");
        builder.AppendLine($"Is Server: {networkManager.IsServer}");
        builder.AppendLine($"Is Client: {networkManager.IsClient}");
        builder.AppendLine($"Is Host: {networkManager.IsHost}");
        builder.AppendLine($"Players: {networkManager.ConnectedClients.Count}");

        if (networkManager.SpawnManager != null)
            builder.AppendLine($"Objects: {networkManager.SpawnManager.SpawnedObjects.Count}");

        if (networkManager.NetworkTickSystem != null)
        {
            builder.AppendLine($"Tick: {networkManager.NetworkTickSystem.LocalTime.Tick}");
            builder.AppendLine($"Tick Rate: {networkManager.NetworkTickSystem.TickRate}");
        }

        AppendRttStats(networkManager);
    }

    void AppendRttStats(NetworkManager networkManager)
    {
        ulong rtt = networkManager.NetworkConfig.NetworkTransport.GetCurrentRtt(NetworkManager.ServerClientId);
        builder.AppendLine($"RTT: {rtt} ms");
    }
}