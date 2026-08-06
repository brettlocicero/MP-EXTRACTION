using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RegionSO", menuName = "Scriptable Objects/RegionSO")]
public class RegionSO : ScriptableObject
{
    [SerializeField] string regionName;
    [SerializeField] int floorLength = 6;
    [SerializeField] int regionLength = 10;

    [Header("")]
    [SerializeField] RoomObject startRoom;
    [SerializeField] RoomObject finalRoom;
    [SerializeField] RoomObject[] rooms;

    [Header("Atmosphere")]
    [SerializeField] Material skybox;
    [SerializeField] Color sunColor;
    [SerializeField] Color ambientSkyColor;
    [SerializeField] Color fogColor;

    public string RegionName => regionName;
    public int FloorLength => floorLength;

    public RoomObject[] Rooms => rooms;
    public RoomObject StartRoom => startRoom;
    public RoomObject FinalRoom => finalRoom;

    public void ApplyRegionAtmosphere()
    {
        RenderSettings.skybox = skybox;
        RenderSettings.sun.color = sunColor;
        RenderSettings.ambientSkyColor = ambientSkyColor;
        RenderSettings.fogColor = fogColor;
    }
}