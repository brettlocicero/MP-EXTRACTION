using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class RegionGenerator : NetworkBehaviour
{
    public static RegionGenerator Instance;

    [SerializeField] GameObject hubObjects;
    [SerializeField] Transform regionRoot;
    [SerializeField] RegionSO[] availableRegions;
    [SerializeField] Vector3 playerSpawnPos = new Vector3(0f, 2f, 0f);
    [SerializeField] RegionTransitionAnimator regionTransitionAnimator;
    [SerializeField] float transitionDuration = 2f;

    [Header("Arena")]
    [SerializeField] EnemySpawner enemySpawner;
    [SerializeField] Material groundMaterial;
    [SerializeField, Min(20f)] float arenaSize = 120f;
    [SerializeField, Min(2f)] float playerSpawnSpacing = 3f;

    readonly Dictionary<ulong, int> playerSpawnSlots = new();
    GameObject spawnedRegionInstance;
    bool generating;

    readonly NetworkVariable<int> currentRegionIndex = new(
        -1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
    );

    Vector3 ArenaCenter => regionRoot != null ? regionRoot.position : Vector3.zero;

    void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
            currentRegionIndex.Value = -1;

        currentRegionIndex.OnValueChanged += OnRegionIndexChanged;

        if (IsServer)
            NetworkManager.OnClientDisconnectCallback += OnClientDisconnected;

        ApplyRegion(currentRegionIndex.Value);
    }

    public override void OnNetworkDespawn()
    {
        currentRegionIndex.OnValueChanged -= OnRegionIndexChanged;
        NetworkManager.OnClientDisconnectCallback -= OnClientDisconnected;
        StopAllCoroutines();
        playerSpawnSlots.Clear();
        generating = false;
        ClearInstancedRegion();

        if (hubObjects != null)
            hubObjects.SetActive(true);
    }

    void OnRegionIndexChanged(int previous, int current)
    {
        ApplyRegion(current);
    }

    public void GenerateRegion(RegionSO region, int regionSeed)
    {
        if (!IsServer || generating || currentRegionIndex.Value >= 0)
            return;

        int regionIndex = System.Array.IndexOf(availableRegions, region);

        if (regionIndex < 0 || enemySpawner == null)
        {
            Debug.LogError("RegionGenerator: assign the region and arena enemy spawner.");
            return;
        }

        generating = true;
        StartCoroutine(GenerateRegionRoutine(regionIndex));
    }

    IEnumerator GenerateRegionRoutine(int regionIndex)
    {
        PlayTransitionRpc();
        yield return new WaitForSeconds(transitionDuration);

        // Persistent state also builds the same arena for clients joining mid-wave.
        currentRegionIndex.Value = regionIndex;

        float remainingTransition = regionTransitionAnimator != null
            ? Mathf.Max(0f, regionTransitionAnimator.PlayDuration - transitionDuration)
            : 0f;
        yield return new WaitForSeconds(remainingTransition);

        while (!AllPlayersPlaced() || !enemySpawner.IsSpawned)
            yield return null;

        enemySpawner.StartSpawning(ArenaCenter);
        generating = false;
    }

    void ApplyRegion(int regionIndex)
    {
        if (regionIndex < 0 || regionIndex >= availableRegions.Length || spawnedRegionInstance != null)
            return;

        RegionSO region = availableRegions[regionIndex];
        region.ApplyRegionAtmosphere();
        spawnedRegionInstance = region.SpawnRegionBase(ArenaCenter, regionRoot);

        if (hubObjects != null)
            hubObjects.SetActive(false);

        Physics.SyncTransforms();

        if (IsClient)
            StartCoroutine(PlaceLocalPlayerRoutine());
    }

    IEnumerator PlaceLocalPlayerRoutine()
    {
        // The owner's player object may spawn after the scene objects during a late join.
        yield return null;

        while (NetworkManager.LocalClient.PlayerObject == null || !NetworkManager.LocalClient.PlayerObject.IsSpawned)
            yield return null;

        ArenaReadyServerRpc();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    void ArenaReadyServerRpc(RpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;

        if (currentRegionIndex.Value < 0 || playerSpawnSlots.ContainsKey(clientId))
            return;

        if (!NetworkManager.ConnectedClients.TryGetValue(clientId, out NetworkClient client) ||
            client.PlayerObject == null || !client.PlayerObject.TryGetComponent<PlayerController>(out var player))
            return;

        int slot = 0;

        while (playerSpawnSlots.ContainsValue(slot))
            slot++;

        playerSpawnSlots.Add(clientId, slot);

        float angle = slot * 137.5f * Mathf.Deg2Rad;
        float radius = Mathf.Sqrt(slot) * playerSpawnSpacing;
        Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
        player.Teleport(ArenaCenter + playerSpawnPos + offset, Quaternion.identity);
    }

    bool AllPlayersPlaced()
    {
        if (NetworkManager.ConnectedClientsList.Count == 0)
            return false;

        foreach (NetworkClient client in NetworkManager.ConnectedClientsList)
        {
            if (!playerSpawnSlots.ContainsKey(client.ClientId))
                return false;
        }

        return true;
    }

    void OnClientDisconnected(ulong clientId)
    {
        playerSpawnSlots.Remove(clientId);
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
        if (regionTransitionAnimator != null)
            regionTransitionAnimator.PlayTransition();
    }
}
