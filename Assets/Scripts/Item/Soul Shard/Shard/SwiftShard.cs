using UnityEngine;

[CreateAssetMenu(fileName = "SwiftShard", menuName = "Scriptable Objects/Soul Shards/SwiftShard")]
public class SwiftShard : SoulShardSO
{
    // attackRate is a cooldown, so lower is faster
    [SerializeField, Range(0.1f, 1f)] float attackRateMultiplier = 0.85f;

    public override void ModifyStats(ref WeaponStats stats)
    {
        stats.attackRate *= attackRateMultiplier;
    }
}