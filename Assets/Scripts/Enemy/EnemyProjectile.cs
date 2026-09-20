using Unity.Netcode;
using UnityEngine;

public class EnemyProjectile : NetworkBehaviour
{
    float damage;
    bool hasHit;

    public void Init(float damage)
    {
        this.damage = damage;
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