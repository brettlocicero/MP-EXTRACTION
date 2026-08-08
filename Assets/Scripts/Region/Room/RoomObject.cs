using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class RoomObject : NetworkBehaviour
{
    [SerializeField] Transform[] enemyWaypoints;
    [SerializeField] GameObject[] enemyPrefabs;
    [SerializeField] Vector2Int enemyAmountRange;
    [SerializeField] float spawnInterval = 0.5f;
    [SerializeField] Transform connector;
    [SerializeField] GameObject barrierObject;

    bool hasTriggered = false;
    int enemiesAlive = 0;

    NetworkVariable<bool> barrierUp = new(
        true, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server
    );

    public Transform Connector => connector;

    public override void OnNetworkSpawn()
    {
        if (barrierObject)
        {
            barrierUp.OnValueChanged += OnBarrierChanged;
            barrierObject.SetActive(barrierUp.Value);
        }
    }

    public override void OnNetworkDespawn()
    {
        if (barrierObject)
        {
            barrierUp.OnValueChanged -= OnBarrierChanged;
        }
    }

    void OnBarrierChanged(bool previous, bool current)
    {
        if (barrierObject)
        {
            barrierObject.SetActive(current);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        NetworkObject playerNetworkObject = other.GetComponent<NetworkObject>();
        if (playerNetworkObject == null) return;
        if (!playerNetworkObject.IsOwner) return;

        TriggerRoomServerRpc();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    void TriggerRoomServerRpc()
    {
        if (hasTriggered) return;

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

            RegisterEnemy(enemy);

            yield return new WaitForSeconds(spawnInterval);
        }
    }

    void RegisterEnemy(GameObject enemy)
    {
        enemiesAlive++;

        EnemyAI enemyAI = enemy.GetComponent<EnemyAI>();
        enemyAI.OnEnemyKilled += OnEnemyKilled;
    }

    void OnEnemyKilled(EnemyAI enemy)
    {
        enemy.OnEnemyKilled -= OnEnemyKilled;
        enemiesAlive--;

        if (enemiesAlive <= 0)
        {
            barrierUp.Value = false;
        }
    }
}