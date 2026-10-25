using Unity.Netcode;
using UnityEngine;

public class BreakableObject : PropObject
{
    [SerializeField] GameObject intact;
    [SerializeField] Rigidbody[] debris;
    [SerializeField] float maxHealth = 30f;
    [SerializeField] float breakForce = 50f;
    [SerializeField] float despawnDelay = 5f;

    [Header("Loot")]
    [SerializeField] ItemSO[] itemPool;
    [SerializeField] int dropCount = 1;
    [SerializeField] float lootForce = 6f;

    float health;
    bool broken;

    public override void OnNetworkSpawn()
    {
        health = maxHealth;
    }

    public void TakeDamage(float damage)
    {
        if (IsServer)
            ApplyDamage(damage);
        else
            TakeDamageRpc(damage);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    void TakeDamageRpc(float damage)
    {
        ApplyDamage(damage);
    }

    void ApplyDamage(float damage)
    {
        health -= damage;

        if (health <= 0f && !broken)
        {
            broken = true;
            BreakRpc();

            if (itemPool.Length > 0)
                SpawnLoot();

            Invoke(nameof(DespawnSelf), despawnDelay);
        }
    }

    void SpawnLoot()
    {
        for (int i = 0; i < dropCount; i++)
        {
            ItemSO item = itemPool[Random.Range(0, itemPool.Length)];
            LootItem drop = ItemDatabase.Instance.SpawnItemDrop(item, transform.position);

            Vector3 direction = new Vector3(Random.Range(-1f, 1f), Random.Range(0.5f, 1f), Random.Range(-1f, 1f)).normalized;
            drop.GetComponent<Rigidbody>().AddForce(direction * lootForce, ForceMode.Impulse);
        }
    }

    [Rpc(SendTo.Everyone)]
    void BreakRpc()
    {
        intact.SetActive(false);

        foreach (Rigidbody piece in debris)
        {
            piece.gameObject.SetActive(true);
            piece.AddExplosionForce(breakForce, transform.position, 5f);
        }

        GetComponent<Collider>().enabled = false;
    }

    void DespawnSelf()
    {
        NetworkObject.Despawn();
    }
}