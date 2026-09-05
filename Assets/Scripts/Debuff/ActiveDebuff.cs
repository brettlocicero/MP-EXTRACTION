public class ActiveDebuff
{
    public DebuffSO source;
    public int instanceId;

    float elapsedTime;
    float tickTimer;

    public bool Expired => elapsedTime >= source.Duration;

    public ActiveDebuff(DebuffSO source, int instanceId)
    {
        this.source = source;
        this.instanceId = instanceId;
    }

    public void Tick(EnemyAI target, float deltaTime)
    {
        elapsedTime += deltaTime;
        tickTimer += deltaTime;

        while (tickTimer >= source.TickInterval && elapsedTime - tickTimer + source.TickInterval <= source.Duration)
        {
            tickTimer -= source.TickInterval;
            source.Effect(target);
        }
    }
}