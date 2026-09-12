using Unity.Netcode;
using UnityEngine;

public class RoomObject : MonoBehaviour
{
    [SerializeField] Transform connector;

    [Header("Enemies")]
    [SerializeField] Transform[] enemySpawnLocations;
    [SerializeField] RoomEnemySpawn[] enemySpawns;
    [SerializeField, Range(0f, 1f)] float enemySpawnChance = 0.7f;

    [Header("Props")]
    [SerializeField] Transform[] propLocation;
    [SerializeField] PropObject[] props;
    [SerializeField, Range(0f, 1f)] float propSpawnChance = 0.7f;

    public Transform Connector => connector;

    public void Initialize()
    {
        if (!NetworkManager.Singleton.IsServer)
            return;

        SpawnEnemies();
        SpawnProps();
    }

    void SpawnEnemies()
    {
        foreach (Transform location in enemySpawnLocations)
        {
            if (enemySpawns.Length == 0 || Random.value > enemySpawnChance)
                continue;

            RoomEnemySpawn prefab = enemySpawns[Random.Range(0, enemySpawns.Length)];
            RoomEnemySpawn spawn = Instantiate(prefab, location.position, location.rotation);

            spawn.GetComponent<NetworkObject>().Spawn();
        }
    }

    void SpawnProps()
    {
        foreach (Transform location in propLocation)
        {
            if (props.Length == 0 || Random.value > propSpawnChance)
                continue;

            PropObject prefab = props[Random.Range(0, props.Length)];
            PropObject prop = Instantiate(prefab, location.position, location.rotation);

            prop.GetComponent<NetworkObject>().Spawn();
        }
    }
}