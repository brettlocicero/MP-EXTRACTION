using System.Collections;
using DG.Tweening;
using Unity.Netcode;
using UnityEngine;

public class RegionGenerator : NetworkBehaviour
{
    public static RegionGenerator Instance;

    [SerializeField] GameObject hubObjects;
    [SerializeField] Transform regionRoot;
    [SerializeField] RegionSO[] availableRegions;
    [SerializeField] Vector3 playerSpawnPos;
    [SerializeField] RegionTransitionAnimator regionTransitionAnimator;
    [SerializeField] float transitionDuration = 2f;

    RegionSO currentRegion;
    GameObject spawnedRegionInstance;

    readonly NetworkVariable<int> currentRegionIndex = new(
        -1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
    );

    void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        currentRegionIndex.OnValueChanged += OnRegionIndexChanged;

        ApplyAtmosphere(currentRegionIndex.Value);
    }

    public override void OnNetworkDespawn()
    {
        currentRegionIndex.OnValueChanged -= OnRegionIndexChanged;
    }

    void OnRegionIndexChanged(int previous, int current)
    {
        ApplyAtmosphere(current);
    }

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
        currentRegionIndex.Value = regionIndex;

        GenerateRegionRpc(regionIndex);
        PostGenerationRpc();
        MoveAllPlayers();
    }


    [Rpc(SendTo.Everyone)]
    void GenerateRegionRpc(int regionIndex)
    {
        ClearInstancedRegion();

        Vector3 spawnPos = regionRoot != null ? regionRoot.position : Vector3.zero;
        // spawnedRegionInstance = Instantiate(availableRegions[regionIndex].RegionBase, spawnPos, Quaternion.identity, regionRoot);

        availableRegions[regionIndex].SpawnRooms(regionRoot);
    }

    void ClearInstancedRegion()
    {
        if (spawnedRegionInstance != null)
            Destroy(spawnedRegionInstance);

        spawnedRegionInstance = null;
    }

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