using System;
using Unity.Netcode;
using UnityEngine;

public class DecayManager : NetworkBehaviour
{
    public static DecayManager Instance;

    [Header("Timing")]
    [SerializeField] float decayDurationMinutes = 15f;

    [Header("Visuals")]
    [SerializeField] Color maxAmbientColor = Color.red;

    public event Action OnDecayMaxed;

    NetworkVariable<double> decayStartTime = new(
        -1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
    );

    Color startAmbientColor;
    bool hasTriggeredMax;

    public float Progress { get; private set; }

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        if (decayStartTime.Value < 0)
            return;

        double elapsed = NetworkManager.Singleton.ServerTime.Time - decayStartTime.Value;
        float durationSeconds = decayDurationMinutes * 60f;

        Progress = Mathf.Clamp01((float)(elapsed / durationSeconds));

        RenderSettings.ambientSkyColor = Color.Lerp(startAmbientColor, maxAmbientColor, Progress);

        if (IsServer && !hasTriggeredMax && Progress >= 1f)
        {
            hasTriggeredMax = true;
            OnDecayMaxed?.Invoke();
        }
    }

    public void StartDecay()
    {
        if (!IsServer)
            return;

        startAmbientColor = RenderSettings.ambientSkyColor;
        decayStartTime.Value = NetworkManager.Singleton.ServerTime.Time;
    }
}