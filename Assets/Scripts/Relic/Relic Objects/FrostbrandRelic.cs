using UnityEngine;

[CreateAssetMenu(fileName = "FrostbrandRelic", menuName = "Scriptable Objects/Relics/FrostbrandRelic")]
public class FrostbrandRelic : RelicSO
{
    [SerializeField] DebuffSO debuffToApply;

    protected override void ApplyEffect(WeaponContext weaponContext)
    {
        foreach (EnemyAI enemy in weaponContext.HitEnemies)
            enemy.ApplyDebuff(debuffToApply, weaponContext.SourceClientId);
    }
}