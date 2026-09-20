using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Enemies")]
    public GameObject[] enemyPrefabs;

    [Header("Spawn Settings")]
    [Min(0.1f)] public float spawnInterval = 2f;
    [Range(1, 25)] public int maxConcurrentEnemies = 25;

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

        if (spawnTimer < Mathf.Max(0.1f, spawnInterval))
            return;

        spawnTimer = 0f;
        aliveEnemies.RemoveAll(enemy => enemy == null || !enemy.IsSpawned);

        if (aliveEnemies.Count >= Mathf.Clamp(maxConcurrentEnemies, 1, 25))
            return;

        TrySpawnEnemy();
    }

    public void StartSpawning()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
            return;

        spawnTimer = 0f;
        spawning = true;
    }

    public void StopSpawning()
    {
        spawning = false;
        spawnTimer = 0f;
        foreach (NetworkObject enemy in aliveEnemies)
        {
            if (enemy != null && enemy.TryGetComponent(out EnemyAI ai))
                ai.OnEnemyKilled -= OnEnemyKilled;
        }
        aliveEnemies.Clear();
    }

    void OnEnemyKilled(EnemyAI enemy)
    {
        if (!aliveEnemies.Remove(enemy.NetworkObject))
            return;

        enemy.OnEnemyKilled -= OnEnemyKilled;
        GameManager.Instance.RegisterEnemyKill();
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
        if (enemyPrefab == null || !enemyPrefab.TryGetComponent<NetworkObject>(out _) ||
            !enemyPrefab.TryGetComponent<EnemyAI>(out _))
        {
            Debug.LogWarning("EnemySpawner: enemy prefab needs NetworkObject and EnemyAI components.");
            return;
        }

        GameObject instance = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);

        // Place the bottom of the enemy's collider on the arena floor.
        if (instance.TryGetComponent(out Collider body))
        {
            instance.transform.position += Vector3.up * (spawnPos.y - body.bounds.min.y + 0.05f);
        }

        NetworkObject networkObject = instance.GetComponent<NetworkObject>();
        instance.GetComponent<EnemyAI>().OnEnemyKilled += OnEnemyKilled;
        networkObject.Spawn();
        aliveEnemies.Add(networkObject);
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

        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, raycastDistance, groundMask, QueryTriggerInteraction.Ignore))
        {
            groundPoint = hit.point;
            return true;
        }

        groundPoint = Vector3.zero;
        return false;
    }
}
