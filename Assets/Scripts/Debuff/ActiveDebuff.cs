public class ActiveDebuff
{
    public DebuffSO source;
    public ulong sourceClientId;
    public int instanceId;

    float elapsedTime;
    float tickTimer;

    public bool Expired => elapsedTime >= source.Duration;

    public ActiveDebuff(DebuffSO source, ulong sourceClientId, int instanceId)
    {
        this.source = source;
        this.sourceClientId = sourceClientId;
        this.instanceId = instanceId;
    }

    public void Tick(EnemyAI target, float deltaTime)
    {
        elapsedTime += deltaTime;
        tickTimer += deltaTime;

        while (tickTimer >= source.TickInterval && elapsedTime - tickTimer + source.TickInterval <= source.Duration)
        {
            tickTimer -= source.TickInterval;
            source.Effect(target, sourceClientId);
        }
    }
}