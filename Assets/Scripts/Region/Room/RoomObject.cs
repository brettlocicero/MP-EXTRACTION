using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class RoomObject : NetworkBehaviour
{
    [SerializeField] EnemyAI[] enemyPool;
    [SerializeField] Transform[] enemySpawnLocations;
    [SerializeField] int baseEnemyCount = 10;
    [SerializeField] Transform relicSpawnPoint;

    readonly List<EnemyAI> aliveEnemies = new();

    public void InitRoom()
    {
        for (int i = 0; i < baseEnemyCount; i++)
            SpawnEnemy();
    }

    void SpawnEnemy()
    {
        EnemyAI prefab = enemyPool[Random.Range(0, enemyPool.Length)];
        Transform location = enemySpawnLocations[Random.Range(0, enemySpawnLocations.Length)];

        EnemyAI enemy = Instantiate(prefab, location.position, location.rotation);
        enemy.NetworkObject.Spawn();
        enemy.OnEnemyKilled += OnEnemyKilled;
        aliveEnemies.Add(enemy);
    }

    void OnEnemyKilled(EnemyAI enemy)
    {
        enemy.OnEnemyKilled -= OnEnemyKilled;
        aliveEnemies.Remove(enemy);

        if (aliveEnemies.Count == 0)
            ClearRoom();
    }

    void ClearRoom()
    {
        foreach (NetworkClient client in NetworkManager.ConnectedClientsList)
            client.PlayerObject.GetComponent<PlayerRelics>().OfferRelics(relicSpawnPoint.position);
    }

    public override void OnNetworkDespawn()
    {
        foreach (EnemyAI enemy in aliveEnemies)
        {
            if (enemy != null)
            {
                enemy.OnEnemyKilled -= OnEnemyKilled;

                if (enemy.IsSpawned)
                    enemy.NetworkObject.Despawn();
            }
        }

        aliveEnemies.Clear();
    }
}