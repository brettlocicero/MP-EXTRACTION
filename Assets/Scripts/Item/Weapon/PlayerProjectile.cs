using UnityEngine;

public class PlayerProjectile : MonoBehaviour
{
    Attack attack;
    bool hasHit;

    public void Init(Attack attack)
    {
        this.attack = attack;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!hasHit && other.CompareTag("Enemy") && other.TryGetComponent(out EnemyAI enemy))
        {
            hasHit = true;
            Vector3 hitPoint = other.ClosestPoint(transform.position);
            enemy.TakeDamage(attack.damage, attack.stunTime, attack.direction, hitPoint);
            Destroy(gameObject);
        }    
    }
}