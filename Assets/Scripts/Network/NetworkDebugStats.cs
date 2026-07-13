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
        
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening) return;

        float fps = 1f / Mathf.Max(Time.unscaledDeltaTime, 0.0001f);
        float frameMs = Time.unscaledDeltaTime * 1000f;

        builder.AppendLine($"FPS: {fps:F0}");
        builder.AppendLine($"Frame: {frameMs:F1} ms");

        if (NetworkManager.Singleton == null)
        {
            builder.AppendLine();
            builder.AppendLine("Network: Not Running");

            debugText.text = builder.ToString();
            return;
        }

        builder.AppendLine();

        builder.AppendLine($"Is Server: {NetworkManager.Singleton.IsServer}");
        builder.AppendLine($"Is Client: {NetworkManager.Singleton.IsClient}");
        builder.AppendLine($"Is Host: {NetworkManager.Singleton.IsHost}");

        builder.AppendLine($"Players: {NetworkManager.Singleton.ConnectedClients.Count}");

        if (NetworkManager.Singleton.SpawnManager != null)
            builder.AppendLine($"Objects: {NetworkManager.Singleton.SpawnManager.SpawnedObjects.Count}");

        if (NetworkManager.Singleton.NetworkTickSystem != null)
        {
            builder.AppendLine($"Tick: {NetworkManager.Singleton.NetworkTickSystem.LocalTime.Tick}");
            builder.AppendLine($"Tick Rate: {NetworkManager.Singleton.NetworkTickSystem.TickRate}");
        }

        ulong rtt = NetworkManager.Singleton.NetworkConfig.NetworkTransport.GetCurrentRtt(NetworkManager.ServerClientId);

        builder.AppendLine($"RTT: {rtt} ms");

        debugText.text = builder.ToString();
    }
}