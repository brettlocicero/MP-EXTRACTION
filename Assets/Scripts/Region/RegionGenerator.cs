using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class RegionGenerator : NetworkBehaviour
{
    public static RegionGenerator Instance;

    [SerializeField] Transform regionRoot;
    [SerializeField] RegionSO[] availableRegions;

    RegionSO currentRegion;
    readonly List<NetworkObject> spawnedRooms = new List<NetworkObject>();

    readonly NetworkVariable<int> currentRoomIndex = new NetworkVariable<int>(
        -1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
    );

    public int CurrentRoomIndex => currentRoomIndex.Value;

    void Awake()
    {
        Instance = this;
    }

    public void GenerateRegion(RegionSO region, int regionSeed)
    {
        if (!IsServer)
            return;

        Debug.Log($"Generating region '{region.RegionName}' with seed {regionSeed}");

        int regionIndex = System.Array.IndexOf(availableRegions, region);
        var rng = new System.Random(regionSeed);

        ClearInstancedRegion();
        currentRegion = region;

        ApplyAtmosphereClientRpc(regionIndex);
        currentRoomIndex.Value = -1;
        SpawnFloor(rng);
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

    void SpawnFloor(System.Random rng)
    {
        Vector3 cursor = regionRoot != null ? regionRoot.position : Vector3.zero;
        for (int i = 0; i < currentRegion.FloorLength; i++)
        {
            currentRoomIndex.Value = i;
            RoomObject roomDef = currentRegion.Rooms[rng.Next(currentRegion.Rooms.Length)];
            SpawnRoom(roomDef, ref cursor);
        }
    }

    void SpawnRoom(RoomObject roomDef, ref Vector3 cursor)
    {
        RoomObject roomObj = Instantiate(roomDef, cursor, Quaternion.identity);

        NetworkObject netObj = roomObj.GetComponent<NetworkObject>();
        netObj.Spawn(true);
        spawnedRooms.Add(netObj);

        cursor = roomObj.Connector.position;
    }

    [ClientRpc]
    void ApplyAtmosphereClientRpc(int regionIndex)
    {
        availableRegions[regionIndex].ApplyRegionAtmosphere();
    }
}