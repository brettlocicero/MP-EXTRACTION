using UnityEngine;

[CreateAssetMenu(fileName = "InfernalShard", menuName = "Scriptable Objects/Soul Shards/InfernalShard")]
public class InfernalShard : SoulShardSO
{
    [SerializeField] int burnDamage = 3;
    [SerializeField] int ticks = 3;
    [SerializeField] float tickInterval = 1f;

    protected override void ApplyEffect(WeaponContext weaponContext)
    {
        foreach (EnemyAI enemy in weaponContext.HitEnemies)
        {
            enemy.AddDebuff(
                new IgniteDebuff
                (
                    vfx, 
                    tickInterval, 
                    burnDamage, 
                    ticks, 
                    weaponContext.SourceClientId
                )
            );
        }
    }
}
