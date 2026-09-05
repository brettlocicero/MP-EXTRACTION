using UnityEngine;

[CreateAssetMenu(fileName = "IgniteDebuffSO", menuName = "Scriptable Objects/Debuffs/IgniteDebuffSO")]
public class IgniteDebuffSO : DebuffSO
{
    [SerializeField] int burnDamage;

    public override void Effect(EnemyAI target, ulong sourceClientId)
    {
        target.ApplyDebuffDamage(burnDamage, sourceClientId);
    }
}