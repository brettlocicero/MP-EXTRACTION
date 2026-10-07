using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class EnemyWaveManager : NetworkBehaviour
{
    [Header("Enemies")]
    [SerializeField] GameObject[] enemyPrefabs;

    [Header("Wave Settings")]
    [SerializeField, Min(1)] int startingEnemyCount = 10;
    [SerializeField, Min(1)] int enemiesAddedPerWave = 5;
    [SerializeField, Min(0f)] float intermissionDuration = 30f;

    [Header("Spawn Settings")]
    [SerializeField, Min(0.1f)] float spawnInterval = 3f;
    [SerializeField, Min(1)] int maxConcurrentEnemies = 10;

    [Header("Placement")]
    [SerializeField, Min(5f)] float spawnRadius = 100f;
    [SerializeField] float raycastHeight = 100f;
    [SerializeField] float raycastDistance = 300f;
    [SerializeField] LayerMask groundMask;

    readonly List<EnemyAI> aliveEnemies = new();
    readonly List<EnemyAI> validEnemies = new();

    readonly NetworkVariable<int> currentWave = new(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
    );

    readonly NetworkVariable<int> enemiesRemaining = new(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
    );

    readonly NetworkVariable<double> nextWaveTime = new(
        0d, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
    );

    Vector3 arenaCenter;
    float spawnTimer;
    float spawnAngle;
    int enemiesToSpawn;
    bool spawning;

    public int CurrentWave => currentWave.Value;
    public int EnemiesRemaining => enemiesRemaining.Value;
    public bool IsIntermission => nextWaveTime.Value > 0d;
    public int IntermissionSeconds => IsSpawned && IsIntermission
        ? Mathf.Max(0, Mathf.CeilToInt((float)(nextWaveTime.Value - NetworkManager.ServerTime.Time)))
        : 0;

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
            return;

        currentWave.Value = 0;
        enemiesRemaining.Value = 0;
        nextWaveTime.Value = 0d;
    }

    public override void OnNetworkDespawn()
    {
        spawning = false;

        foreach (EnemyAI enemy in aliveEnemies)
        {
            if (enemy != null)
                enemy.OnEnemyKilled -= OnEnemyKilled;
        }

        aliveEnemies.Clear();
        validEnemies.Clear();
    }

    void Update()
    {
        if (IsSpawned && IsServer && spawning)
            TickWave();
    }

    void TickWave()
    {
        if (IsIntermission)
            TickIntermission();
        else
            TickCombat();
    }

    void TickIntermission()
    {
        if (NetworkManager.ServerTime.Time >= nextWaveTime.Value)
            StartNextWave();
    }

    void TickCombat()
    {
        RemoveDespawnedEnemies();

        if (enemiesRemaining.Value == 0)
        {
            nextWaveTime.Value = NetworkManager.ServerTime.Time + intermissionDuration;
            OfferRelics();
        }

        else if (CanSpawn())
        {
            spawnTimer -= Time.deltaTime;

            if (spawnTimer <= 0f)
                TrySpawnEnemy();
        }
    }

    void OfferRelics()
    {
        foreach (NetworkClient client in NetworkManager.ConnectedClientsList)
            client.PlayerObject.GetComponent<PlayerRelics>().OfferRelics(arenaCenter + Vector3.up);
    }

    bool CanSpawn()
    {
        return enemiesToSpawn > 0 && aliveEnemies.Count < maxConcurrentEnemies;
    }

    // Accounts for enemies despawned without going through their death event.
    void RemoveDespawnedEnemies()
    {
        for (int i = aliveEnemies.Count - 1; i >= 0; i--)
        {
            EnemyAI enemy = aliveEnemies[i];

            if (enemy != null && enemy.IsSpawned)
                continue;

            if (enemy != null)
                enemy.OnEnemyKilled -= OnEnemyKilled;

            aliveEnemies.RemoveAt(i);
            enemiesRemaining.Value--;
        }
    }

    public void StartSpawning(Vector3 center)
    {
        if (!IsSpawned || !IsServer || spawning)
            return;

        validEnemies.Clear();

        foreach (GameObject prefab in enemyPrefabs)
        {
            if (prefab != null && prefab.TryGetComponent(out EnemyAI enemy) && prefab.TryGetComponent<NetworkObject>(out _))
                validEnemies.Add(enemy);
        }

        if (validEnemies.Count == 0)
        {
            Debug.LogError("EnemySpawner: assign at least one enemy prefab with EnemyAI and NetworkObject.");
            return;
        }

        arenaCenter = center;
        currentWave.Value = 0;
        spawning = true;
        StartNextWave();
    }

    void StartNextWave()
    {
        currentWave.Value++;
        enemiesToSpawn = startingEnemyCount + (currentWave.Value - 1) * enemiesAddedPerWave;
        enemiesRemaining.Value = enemiesToSpawn;
        nextWaveTime.Value = 0d;
        spawnTimer = 0f;
        spawnAngle = Random.Range(0f, Mathf.PI * 2f);
    }

    void TrySpawnEnemy()
    {
        if (TryGetGroundPoint(out Vector3 spawnPos))
            SpawnEnemy(spawnPos);
    }

    // Raycasts down from above the ring position so the enemy lands on the ground.
    bool TryGetGroundPoint(out Vector3 groundPoint)
    {
        Vector3 offset = new Vector3(Mathf.Cos(spawnAngle), 0f, Mathf.Sin(spawnAngle)) * spawnRadius;
        Vector3 rayOrigin = arenaCenter + offset + Vector3.up * raycastHeight;

        // Spread successive spawns around the ring instead of clustering on one side.
        spawnAngle += 137.5f * Mathf.Deg2Rad;

        bool hitGround = Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, raycastDistance, groundMask);
        groundPoint = hit.point;
        groundPoint.y += 3f; // Add an offset in case the ground point is too low

        return hitGround;
    }

    void SpawnEnemy(Vector3 spawnPos)
    {
        Vector3 toCenter = arenaCenter - spawnPos;
        toCenter.y = 0f;

        EnemyAI enemy = Instantiate(PickEnemy(), spawnPos + Vector3.up * 0.1f, Quaternion.LookRotation(toCenter));

        enemy.NetworkObject.Spawn();
        enemy.OnEnemyKilled += OnEnemyKilled;
        aliveEnemies.Add(enemy);

        enemiesToSpawn--;
        spawnTimer = spawnInterval;
    }

    // Higher threat means a lower chance of being picked.
    EnemyAI PickEnemy()
    {
        float totalWeight = 0f;

        foreach (EnemyAI enemy in validEnemies)
            totalWeight += 1f / (enemy.Threat * enemy.Threat);

        float roll = Random.value * totalWeight;

        foreach (EnemyAI enemy in validEnemies)
        {
            roll -= 1f / (enemy.Threat * enemy.Threat);

            if (roll <= 0f)
                return enemy;
        }

        return validEnemies[0];
    }

    void OnEnemyKilled(EnemyAI enemy)
    {
        if (!IsServer || !aliveEnemies.Remove(enemy))
            return;

        enemy.OnEnemyKilled -= OnEnemyKilled;
        enemiesRemaining.Value--;
    }
}