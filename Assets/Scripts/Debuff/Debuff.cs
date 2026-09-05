using UnityEngine;

public abstract class Debuff
{
    public float tickInterval = 1f;
    public float duration;
    public ulong sourceClientId;

    float elapsedTime;
    float tickTimer;

    public int InstanceId { get; set; }
    public abstract string Id { get; }

    public bool Expired => elapsedTime >= duration;

    GameObject debuffVFX;

    public Debuff(GameObject debuffVFX, float tickInterval)
    {
        this.debuffVFX = debuffVFX;
        this.tickInterval = tickInterval;
    }

    public void Tick(EnemyAI target, float deltaTime)
    {
        elapsedTime += deltaTime;
        tickTimer += deltaTime;

        while (tickTimer >= tickInterval && elapsedTime - tickTimer + tickInterval <= duration)
        {
            tickTimer -= tickInterval;
            Effect(target);
        }
    }

    protected abstract void Effect(EnemyAI target);

    public void Destroy()
    {
        
    }
}