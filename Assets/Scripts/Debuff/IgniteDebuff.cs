using UnityEngine;

public class IgniteDebuff : Debuff
{
    int burnDamage;

    public override string Id => "Ignite";

    public IgniteDebuff(GameObject debuffVFX, float tickInterval, int burnDamage, int ticks, ulong sourceClientId) : base(debuffVFX, tickInterval)
    {
        this.burnDamage = burnDamage;
        this.sourceClientId = sourceClientId;
        
        duration = ticks * tickInterval;
    }

    protected override void Effect(EnemyAI target)
    {
        target.ApplyDebuffDamage(burnDamage, sourceClientId);
    }
}