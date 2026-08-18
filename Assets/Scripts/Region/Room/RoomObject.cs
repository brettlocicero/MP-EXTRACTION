using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class RoomObject : NetworkBehaviour
{
    [SerializeField] RoomEnemySpawner enemySpawner;
    [SerializeField] Transform connector;
    [SerializeField] GameObject barrierObject;

    bool hasTriggered = false;
    bool spawningComplete = false;
    int enemiesAlive = 0;

    NetworkVariable<bool> barrierUp = new(
        true, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server
    );

    NetworkVariable<bool> combatActive = new(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    NetworkVariable<int> killCount = new(
        0,
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

        combatActive.OnValueChanged += OnCombatActiveChanged;
        killCount.OnValueChanged += OnKillCountChanged;

        if (combatActive.Value)
        {
            CombatUIManager.Instance.NotifyCombatStarted(enemySpawner.SpawnDuration);
            CombatUIManager.Instance.UpdateKillCount(killCount.Value);
        }
    }

    public override void OnNetworkDespawn()
    {
        if (barrierObject)
        {
            barrierUp.OnValueChanged -= OnBarrierChanged;
        }

        combatActive.OnValueChanged -= OnCombatActiveChanged;
        killCount.OnValueChanged -= OnKillCountChanged;

        if (combatActive.Value)
        {
            CombatUIManager.Instance.NotifyCombatEnded();
        }
    }

    void OnBarrierChanged(bool previous, bool current)
    {
        if (barrierObject)
        {
            barrierObject.SetActive(current);
        }
    }

    void OnCombatActiveChanged(bool previous, bool current)
    {
        if (current)
        {
            CombatUIManager.Instance.NotifyCombatStarted(enemySpawner.SpawnDuration);
        }
        else
        {
            CombatUIManager.Instance.NotifyCombatEnded();
        }
    }

    void OnKillCountChanged(int previous, int current)
    {
        CombatUIManager.Instance.UpdateKillCount(current);
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
        combatActive.Value = true;

        enemySpawner.OnEnemySpawned += RegisterEnemy;
        enemySpawner.OnSpawningComplete += OnSpawningComplete;
        enemySpawner.StartEnemySpawning();
    }

    void OnSpawningComplete()
    {
        spawningComplete = true;
        enemySpawner.OnSpawningComplete -= OnSpawningComplete;

        if (enemiesAlive <= 0)
        {
            ClearRoom();
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
        enemySpawner.NotifyEnemyDied();
        killCount.Value++;

        if (spawningComplete && enemiesAlive <= 0)
        {
            ClearRoom();
        }
    }

    void ClearRoom()
    {
        barrierUp.Value = false;
        combatActive.Value = false;
    }
}