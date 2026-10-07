using UnityEngine;

[CreateAssetMenu(fileName = "EmberbrandRelic", menuName = "Scriptable Objects/Relics/EmberbrandRelic")]
public class EmberbrandRelic : RelicSO
{
    [SerializeField] DebuffSO debuffToApply;

    protected override void ApplyEffect(WeaponContext weaponContext)
    {
        foreach (EnemyAI enemy in weaponContext.HitEnemies)
            enemy.ApplyDebuff(debuffToApply, weaponContext.SourceClientId);
    }
}