using UnityEngine;

[CreateAssetMenu(fileName = "InfernalShard", menuName = "Scriptable Objects/Soul Shards/InfernalShard")]
public class InfernalShard : SoulShardSO
{
    [SerializeField] DebuffSO debuffToApply;

    protected override void ApplyEffect(WeaponContext weaponContext)
    {
        foreach (EnemyAI enemy in weaponContext.HitEnemies)
        {
            enemy.AddDebuff(debuffToApply);
        }
    }
}