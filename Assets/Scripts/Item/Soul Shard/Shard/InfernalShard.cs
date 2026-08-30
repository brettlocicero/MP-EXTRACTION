using UnityEngine;

[CreateAssetMenu(fileName = "InfernalShard", menuName = "Scriptable Objects/Soul Shards/InfernalShard")]
public class InfernalShard : SoulShardSO
{
    [SerializeField] int burnDamage = 3;
    [SerializeField] int ticks = 3;

    protected override void ApplyEffect(WeaponContext weaponContext)
    {
        foreach (EnemyAI enemy in weaponContext.HitEnemies)
        {
            enemy.TakeDamage(burnDamage, 0f, AttackDirection.None);
        }
    }
}
