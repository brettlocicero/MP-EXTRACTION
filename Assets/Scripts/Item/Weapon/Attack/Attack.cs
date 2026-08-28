using UnityEngine;

[System.Serializable]
public class Attack
{
    public string attackName;
    public AnimationClip animationClip;
    public AttackDirection direction;
    public PlayerProjectile projectile;
    public float projectileForce = 50f;
    public bool useHitbox = true;

    [Header("")]
    public float attackDelay = 0.2f;
    public float damage = 10f;
    public float stunTime = 1f;
    public float camShakeIntensity = 3f;
    public float camShakeDuration = 0.2f;
}
