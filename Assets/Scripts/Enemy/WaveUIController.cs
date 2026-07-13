using UnityEngine;
using TMPro;
using Unity.Netcode;

public class WaveUIController : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] EnemySpawner spawner;

    [Header("UI Elements")]
    [SerializeField] TextMeshProUGUI waveText;
    [SerializeField] TextMeshProUGUI timerText;

    void Start()
    {
        if (spawner == null)
        {
            spawner = FindAnyObjectByType<EnemySpawner>();
        }

        // Wait until Netcode initializes this machine's network session before binding listeners
        NetworkManager.Singleton.OnClientConnectedCallback += InitializeUIListeners;
    }

    void InitializeUIListeners(ulong clientId)
    {
        // Prevent duplicate bindings
        NetworkManager.Singleton.OnClientConnectedCallback -= InitializeUIListeners;

        // Subscribing to the NetworkVariable changes allows all players to instantly sync UI changes
        spawner.currentWave.OnValueChanged += UpdateWaveUI;
        spawner.waveCountdown.OnValueChanged += UpdateTimerUI;
        spawner.isIntermission.OnValueChanged += ToggleTimerVisibility;

        // Run initial configuration draw
        UpdateWaveUI(0, spawner.currentWave.Value);
        ToggleTimerVisibility(false, spawner.isIntermission.Value);
    }

    void OnDestroy()
    {
        // Standard clean up rules to avoid memory leaks if scenes change
        if (spawner != null)
        {
            spawner.currentWave.OnValueChanged -= UpdateWaveUI;
            spawner.waveCountdown.OnValueChanged -= UpdateTimerUI;
            spawner.isIntermission.OnValueChanged -= ToggleTimerVisibility;
        }
    }

    void UpdateWaveUI(int previous, int current)
    {
        waveText.text = $"WAVE: {current}";
    }

    void UpdateTimerUI(float previous, float current)
    {
        // Rounding to int keeps it clean instead of dumping raw floats down to decimal strings
        timerText.text = $"Next Wave In: {Mathf.CeilToInt(current)}s";
    }

    void ToggleTimerVisibility(bool previous, bool isIntermissionActive)
    {
        // Hide the timer display block entirely when active combat rounds are in progress
        timerText.gameObject.SetActive(isIntermissionActive);
    }
}