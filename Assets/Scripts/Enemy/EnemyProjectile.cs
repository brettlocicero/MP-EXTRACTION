using Unity.Netcode;
using UnityEngine;

public class EnemyProjectile : NetworkBehaviour
{
    [SerializeField] float lifetime = 5f;

    float damage;
    bool hasHit;

    public void Init(float damage)
    {
        this.damage = damage;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
            Invoke(nameof(DespawnFromLifetime), lifetime);
    }

    void DespawnFromLifetime()
    {
        if (!hasHit)
            NetworkObject.Despawn();
    }

    void OnTriggerEnter(Collider other)
    {
        if (IsServer && !hasHit && other.CompareTag("Player") && other.TryGetComponent(out PlayerState player))
        {
            hasHit = true;
            player.Damage(Mathf.RoundToInt(damage));
            NetworkObject.Despawn();
        }
    }
}