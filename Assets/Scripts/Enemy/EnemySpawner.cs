using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Enemies")]
    public GameObject[] enemyPrefabs;

    [Header("Spawn Settings")]
    public float spawnInterval = 3f;
    public int maxConcurrentEnemies = 10;

    [Header("Placement")]
    public float minSpawnDistance = 50f;
    public float maxSpawnDistance = 75f;
    public float raycastHeight = 100f;
    public float raycastDistance = 200f;
    public LayerMask groundMask;

    List<NetworkObject> aliveEnemies = new List<NetworkObject>();
    float spawnTimer;
    bool spawning;

    void Update()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
            return;

        if (!spawning)
            return;

        spawnTimer += Time.deltaTime;

        if (spawnTimer < spawnInterval)
            return;

        spawnTimer = 0f;
        aliveEnemies.RemoveAll(enemy => enemy == null);

        if (aliveEnemies.Count >= maxConcurrentEnemies)
            return;

        TrySpawnEnemy();
    }

    public void StartSpawning()
    {
        spawning = true;
    }

    void TrySpawnEnemy()
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0)
            return;

        Transform playerTransform = GetRandomPlayerTransform();

        if (playerTransform == null)
            return;

        if (!TryGetGroundPoint(playerTransform.position, out Vector3 spawnPos))
            return;

        GameObject enemyPrefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
        GameObject instance = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);

        if (instance.TryGetComponent(out NetworkObject networkObject))
        {
            networkObject.Spawn();
            aliveEnemies.Add(networkObject);
        }

        else
        {
            Debug.LogWarning("EnemySpawner: enemyPrefab has no NetworkObject component.");
        }
    }

    Transform GetRandomPlayerTransform()
    {
        List<Transform> players = new List<Transform>();

        foreach (NetworkClient client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject != null)
                players.Add(client.PlayerObject.transform);
        }

        if (players.Count == 0)
            return null;

        return players[Random.Range(0, players.Count)];
    }

    bool TryGetGroundPoint(Vector3 playerPos, out Vector3 groundPoint)
    {
        float angle = Random.Range(0f, Mathf.PI * 2f);
        float distance = Random.Range(minSpawnDistance, maxSpawnDistance);

        Vector3 offset = new Vector3(Mathf.Cos(angle) * distance, 0f, Mathf.Sin(angle) * distance);
        Vector3 samplePos = playerPos + offset;
        Vector3 rayOrigin = new Vector3(samplePos.x, playerPos.y + raycastHeight, samplePos.z);

        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, raycastDistance, groundMask))
        {
            groundPoint = hit.point;
            return true;
        }

        groundPoint = Vector3.zero;
        return false;
    }
}