using Unity.Netcode;
using UnityEngine;

public class PlayerProjectile : NetworkBehaviour
{
    Attack attack;
    ulong ownerClientId;
    bool hasHit;

    public void Init(Attack attack)
    {
        this.attack = attack;
    }

    void OnTriggerEnter(Collider other)
    {
        if (IsServer && !hasHit && other.CompareTag("Enemy") && other.TryGetComponent(out EnemyAI enemy))
        {
            hasHit = true;
            enemy.TakeDamage(attack.damage, attack.stunTime, attack.direction);
            NetworkObject.Despawn();
        }
    }
}