using System.Collections;
using DG.Tweening;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class RegionGenerator : NetworkBehaviour
{
    public static RegionGenerator Instance;

    [SerializeField] GameObject hubObjects;
    [SerializeField] Transform regionRoot;
    [SerializeField] RegionSO[] availableRegions;
    [SerializeField] TextMeshProUGUI regionFloorText;
    [SerializeField] Vector3 playerSpawnPos;
    [SerializeField] RegionTransitionAnimator regionTransitionAnimator;
    [SerializeField] float transitionDuration = 2f;

    RegionSO currentRegion;
    System.Random regionRng;
    GameObject spawnedRegionInstance;

    readonly NetworkVariable<int> currentFloorIndex = new(
        -1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
    );

    readonly NetworkVariable<int> currentRegionIndex = new(
        -1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
    );

    public int CurrentFloorIndex => currentFloorIndex.Value;

    void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        currentFloorIndex.OnValueChanged += OnFloorIndexChanged;
        currentRegionIndex.OnValueChanged += OnRegionIndexChanged;

        UpdateFloorText(currentFloorIndex.Value);
        ApplyAtmosphere(currentRegionIndex.Value);
    }

    public override void OnNetworkDespawn()
    {
        currentFloorIndex.OnValueChanged -= OnFloorIndexChanged;
        currentRegionIndex.OnValueChanged -= OnRegionIndexChanged;
    }

    void OnFloorIndexChanged(int previous, int current)
    {
        UpdateFloorText(current);
    }

    void OnRegionIndexChanged(int previous, int current)
    {
        UpdateFloorText(currentFloorIndex.Value);
        ApplyAtmosphere(current);
    }

    // --- Generation flow ---

    public void GenerateRegion(RegionSO region, int regionSeed)
    {
        if (!IsServer)
            return;

        StartCoroutine(GenerateRegionRoutine(region, regionSeed));
    }

    IEnumerator GenerateRegionRoutine(RegionSO region, int regionSeed)
    {
        Debug.Log($"Generating region '{region.RegionName}' with seed {regionSeed}");

        int regionIndex = System.Array.IndexOf(availableRegions, region);

        PlayTransitionRpc();
        yield return new WaitForSeconds(transitionDuration);

        currentRegion = region;
        regionRng = new System.Random(regionSeed);
        currentFloorIndex.Value = 0;
        currentRegionIndex.Value = regionIndex;

        SpawnRegionBaseRpc(regionIndex);
        PostGenerationRpc();
        MoveAllPlayers();
    }

    public void GenerateNextFloor()
    {
        if (!IsServer)
            return;

        if (currentRegion == null)
            return;

        StartCoroutine(GenerateNextFloorRoutine());
    }

    IEnumerator GenerateNextFloorRoutine()
    {
        PlayTransitionRpc();
        yield return new WaitForSeconds(transitionDuration);

        currentFloorIndex.Value++;

        SpawnRegionBaseRpc(currentRegionIndex.Value);
        MoveAllPlayers();
    }

    // --- Region base spawn/clear (local per-client, non-networked) ---

    [Rpc(SendTo.Everyone)]
    void SpawnRegionBaseRpc(int regionIndex)
    {
        ClearInstancedRegion();

        Vector3 spawnPos = regionRoot != null ? regionRoot.position : Vector3.zero;
        spawnedRegionInstance = Instantiate(availableRegions[regionIndex].RegionBase, spawnPos, Quaternion.identity, regionRoot);
    }

    void ClearInstancedRegion()
    {
        if (spawnedRegionInstance != null)
            Destroy(spawnedRegionInstance);

        spawnedRegionInstance = null;
    }

    // --- RPCs ---

    [Rpc(SendTo.Everyone)]
    void PlayTransitionRpc()
    {
        Sequence seq = regionTransitionAnimator.PlayTransition();
    }

    [Rpc(SendTo.Everyone)]
    void PostGenerationRpc()
    {
        hubObjects.SetActive(false);
    }

    // --- Helpers ---

    void UpdateFloorText(int floorIndex)
    {
        if (currentRegionIndex.Value < 0)
        {
            regionFloorText.gameObject.SetActive(false);
            return;
        }

        regionFloorText.gameObject.SetActive(true);

        RegionSO region = availableRegions[currentRegionIndex.Value];
        regionFloorText.text = $"{region.RegionName} - {floorIndex + 1}";
    }

    void ApplyAtmosphere(int regionIndex)
    {
        if (regionIndex < 0)
            return;

        availableRegions[regionIndex].ApplyRegionAtmosphere();
    }

    void MoveAllPlayers()
    {
        foreach (NetworkClient client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject == null)
                continue;

            if (client.PlayerObject.TryGetComponent<PlayerController>(out var player))
            {
                player.Teleport(playerSpawnPos, Quaternion.identity);
            }
        }
    }
}