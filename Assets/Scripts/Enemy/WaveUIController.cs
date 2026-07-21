using UnityEngine;
using TMPro;
using Unity.Netcode;

public class WaveUIController : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] EnemySpawner spawner;

    [Header("UI")]
    [SerializeField] TextMeshProUGUI malevolenceText;
    [SerializeField] TextMeshProUGUI countdownText;

    void Start()
    {
        if (spawner == null)
            spawner = FindAnyObjectByType<EnemySpawner>();

        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("[MalevolenceUI] NetworkManager not found.");
            return;
        }

        NetworkManager.Singleton.OnClientConnectedCallback += InitializeUIListeners;
        spawner.malevolence.OnValueChanged += UpdateMalevolenceUI;
        spawner.malevolenceCountdown.OnValueChanged += UpdateCountdownUI;

        UpdateMalevolenceUI(spawner.malevolence.Value, spawner.malevolence.Value);
        UpdateCountdownUI(spawner.malevolenceCountdown.Value, spawner.malevolenceCountdown.Value);
    }

    void InitializeUIListeners(ulong clientId)
    {
        // Only initialize for the local client
        if (clientId != NetworkManager.Singleton.LocalClientId)
            return;

        NetworkManager.Singleton.OnClientConnectedCallback -= InitializeUIListeners;

        // In case the spawner wasn't found during Start()
        if (spawner == null)
            spawner = FindAnyObjectByType<EnemySpawner>();

        if (spawner == null)
        {
            Debug.LogError("[MalevolenceUI] EnemySpawner not found.");
            return;
        }

        spawner.malevolence.OnValueChanged += UpdateMalevolenceUI;

        // Draw initial value
        UpdateMalevolenceUI(spawner.malevolence.Value, spawner.malevolence.Value);
    }

    void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientConnectedCallback -= InitializeUIListeners;

        if (spawner != null)
        {
            spawner.malevolence.OnValueChanged -= UpdateMalevolenceUI;
            spawner.malevolence.OnValueChanged -= UpdateMalevolenceUI;
            spawner.malevolenceCountdown.OnValueChanged -= UpdateCountdownUI;
        }
    }

    void UpdateCountdownUI(float previous, float current)
    {
        if (spawner.malevolence.Value >= 5)
        {
            countdownText.text = "MAX MALEVOLENCE";
            return;
        }

        int minutes = Mathf.FloorToInt(current / 60f);
        int seconds = Mathf.FloorToInt(current % 60f);

        countdownText.text = $"Next Increase: {minutes:00}:{seconds:00}";
    }

    void UpdateMalevolenceUI(int previous, int current)
    {
        malevolenceText.text = $"MALEVOLENCE {ToRoman(current)}";
    }

    string ToRoman(int value)
    {
        return value switch
        {
            1 => "I",
            2 => "II",
            3 => "III",
            4 => "IV",
            5 => "V",
            _ => value.ToString()
        };
    }
}