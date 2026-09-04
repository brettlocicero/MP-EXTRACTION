public class WeaponContext
{
    public ulong SourceClientId { get; set; }
    public EnemyAI[] HitEnemies { get; set; }
    public float Damage { get; set; }
    public float StunTime { get; set; }
}