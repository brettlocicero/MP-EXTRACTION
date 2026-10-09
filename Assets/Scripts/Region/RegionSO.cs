using Unity.Netcode;
using UnityEngine;

[CreateAssetMenu(fileName = "RegionSO", menuName = "Scriptable Objects/RegionSO")]
public class RegionSO : ScriptableObject
{
    [SerializeField] string regionName;

    [Header("")]
    [SerializeField] RoomObject entranceRoom;
    [SerializeField] RoomObject[] rooms;
    [SerializeField] int roomCount = 5;

    [Header("Atmosphere")]
    [SerializeField] Material skybox;
    [SerializeField] Color sunColor;
    [SerializeField] Color ambientSkyColor;
    [SerializeField] Color fogColor;

    public string RegionName => regionName;

    public void ApplyRegionAtmosphere()
    {
        RenderSettings.skybox = skybox;
        RenderSettings.sun.color = sunColor;
        RenderSettings.ambientSkyColor = ambientSkyColor;
        RenderSettings.fogColor = fogColor;
    }
    
    public RoomObject SpawnRoom(int roomIndex) 
    {
        RoomObject roomPrefab = rooms[Random.Range(0, rooms.Length)];
        if (roomIndex == 0) roomPrefab = entranceRoom;

        RoomObject roomObject = Instantiate(roomPrefab, Vector3.zero, Quaternion.identity);
        roomObject.NetworkObject.Spawn();
        roomObject.InitRoom();
        return roomObject;
    }
}