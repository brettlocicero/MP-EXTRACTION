using UnityEngine;

[CreateAssetMenu(fileName = "RegionSO", menuName = "Scriptable Objects/RegionSO")]
public class RegionSO : ScriptableObject
{
    [SerializeField] string regionName;

    [Header("")]
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

    public void SpawnRooms(Transform regionRoot, int seed)
    {
        System.Random rng = new(seed);

        Vector3 cursorPosition = regionRoot.position;
        Quaternion cursorRotation = regionRoot.rotation;

        for (int i = 0; i < roomCount; i++)
        {
            RoomObject roomObject = rooms[rng.Next(rooms.Length)];
            RoomObject roomInstance = Instantiate(roomObject, cursorPosition, cursorRotation, regionRoot);
            roomInstance.Initialize();

            if (roomInstance.Connector == null)
            {
                Debug.LogWarning($"[RegionSO] Room prefab '{roomObject.name}' is missing a RoomObject/Connector, stopping room chain early.");
                break;
            }

            roomInstance.Connector.GetPositionAndRotation(out cursorPosition, out cursorRotation);
        }
    }
}
