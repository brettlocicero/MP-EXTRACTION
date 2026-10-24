public struct WeaponStats
{
    public float damageMultiplier;
    public float range;
    public float attackRate;

    public WeaponStats(WeaponSO weapon)
    {
        damageMultiplier = 1f;
        range = weapon.range;
        attackRate = weapon.attackRate;
    }
}