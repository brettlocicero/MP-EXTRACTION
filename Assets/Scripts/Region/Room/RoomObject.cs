using System.Collections;
using UnityEngine;
using Unity.Netcode;

public class RoomObject : NetworkBehaviour
{
    [SerializeField] Transform[] enemyWaypoints;
    [SerializeField] GameObject[] enemyPrefabs;
    [SerializeField] Vector2Int enemyAmountRange;
    [SerializeField] float spawnInterval = 0.5f;
    [SerializeField] Transform connector;
    bool hasTriggered = false;

    public Transform Connector => connector;

    void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;
        if (hasTriggered) return;
        if (!other.CompareTag("Player")) return;

        hasTriggered = true;
        StartCoroutine(SpawnEnemiesRoutine());
    }

    IEnumerator SpawnEnemiesRoutine()
    {
        int enemyAmount = Random.Range(enemyAmountRange.x, enemyAmountRange.y);
        for (int i = 0; i < enemyAmount; i++)
        {
            Transform waypoint = enemyWaypoints[Random.Range(0, enemyWaypoints.Length)];
            GameObject prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
            GameObject enemy = Instantiate(prefab, waypoint.position, waypoint.rotation);
            enemy.GetComponent<NetworkObject>().Spawn();

            yield return new WaitForSeconds(spawnInterval);
        }
    }
}