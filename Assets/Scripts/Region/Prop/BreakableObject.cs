using Unity.Netcode;
using UnityEngine;

public class BreakableObject : PropObject
{
    [SerializeField] GameObject intact;
    [SerializeField] Rigidbody[] debris;
    [SerializeField] float maxHealth = 30f;
    [SerializeField] float breakForce = 50f;
    [SerializeField] float despawnDelay = 5f;

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
            Invoke(nameof(DespawnSelf), despawnDelay);
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