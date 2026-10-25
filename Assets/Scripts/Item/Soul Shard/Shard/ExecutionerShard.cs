using UnityEngine;

[CreateAssetMenu(fileName = "ExecutionerShard", menuName = "Scriptable Objects/Soul Shards/ExecutionerShard")]
public class ExecutionerShard : SoulShardSO
{
    [SerializeField, Range(0f, 1f)] float healthThreshold = 0.3f;
    [SerializeField] float bonusDamageMultiplier = 0.5f;

    protected override void ApplyEffect(WeaponContext weaponContext)
    {
        foreach (EnemyAI enemy in weaponContext.HitEnemies)
        {
            if (enemy && enemy.HealthPercent <= healthThreshold)
                enemy.TakeDamage(weaponContext.Damage * bonusDamageMultiplier, 0f, AttackDirection.Middle);
        }
    }
}