using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class RegionGenerator : NetworkBehaviour
{
    public static RegionGenerator Instance;

    [SerializeField] Transform regionRoot;
    [SerializeField] RegionSO[] availableRegions;
    [SerializeField] TextMeshProUGUI regionFloorText;
    [SerializeField] Vector3 playerSpawnPos;

    RegionSO currentRegion;
    System.Random regionRng;
    Vector3 cursor;
    readonly List<NetworkObject> spawnedRooms = new List<NetworkObject>();

    readonly NetworkVariable<int> currentRoomIndex = new NetworkVariable<int>(
        -1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
    );

    readonly NetworkVariable<int> currentFloorIndex = new NetworkVariable<int>(
        -1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
    );

    readonly NetworkVariable<int> currentRegionIndex = new NetworkVariable<int>(
        -1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
    );

    public int CurrentRoomIndex => currentRoomIndex.Value;
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

    public void GenerateRegion(RegionSO region, int regionSeed)
    {
        if (!IsServer)
            return;

        Debug.Log($"Generating region '{region.RegionName}' with seed {regionSeed}");

        int regionIndex = System.Array.IndexOf(availableRegions, region);

        ClearInstancedRegion();
        currentRegion = region;
        regionRng = new System.Random(regionSeed);
        cursor = regionRoot != null ? regionRoot.position : Vector3.zero;

        currentRoomIndex.Value = -1;
        currentFloorIndex.Value = 0;
        currentRegionIndex.Value = regionIndex;
        SpawnFloor(currentRegion.FloorLength);

        // After generation, run more commands
        MoveAllPlayers();
    }

    public void GenerateNextFloor()
    {
        if (!IsServer) return;
        if (currentRegion == null) return;

        ClearInstancedRegion();
        cursor = regionRoot != null ? regionRoot.position : Vector3.zero;

        currentFloorIndex.Value++;
        SpawnFloor(currentRegion.FloorLength);

        MoveAllPlayers();
    }

    void ClearInstancedRegion()
    {
        if (!IsServer) return;

        foreach (var room in spawnedRooms)
        {
            if (room != null && room.IsSpawned)
            {
                room.Despawn(true);
            }
        }

        spawnedRooms.Clear();
    }

    void SpawnFloor(int floorLength)
    {
        for (int i = 0; i < floorLength; i++)
        {
            currentRoomIndex.Value++;
            RoomObject roomDef = currentRegion.Rooms[regionRng.Next(currentRegion.Rooms.Length)];

            // If it is the first/last room of the floor, spawn a special room.
            if (i == 0)
                roomDef = currentRegion.StartRoom;
            else if (i == floorLength - 1)
                roomDef = currentRegion.FinalRoom;

            SpawnRoom(roomDef);
        }
    }

    void SpawnRoom(RoomObject roomDef)
    {
        RoomObject roomObj = Instantiate(roomDef, cursor, Quaternion.identity);

        NetworkObject netObj = roomObj.GetComponent<NetworkObject>();
        netObj.Spawn(true);
        spawnedRooms.Add(netObj);

        cursor = roomObj.Connector.position;
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