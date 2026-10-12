using UnityEngine;

public abstract class EnemyAttack : MonoBehaviour
{
    [Header("Attack")]
    [SerializeField] protected float range = 2f;
    [SerializeField] protected float minRange = 0f;
    [SerializeField] protected float cooldown = 1f;
    [SerializeField] protected bool attacksWhileMoving = false;
    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioClip attackSound;

    protected float nextAttackTime;

    public float Range => range;
    public float MinRange => minRange;
    public bool AttacksWhileMoving => attacksWhileMoving;
    public bool IsAttacking { get; protected set; }
    public bool IsReady => Time.time >= nextAttackTime;
    public AudioClip AttackSound => attackSound;

    public abstract void Execute(PlayerState target);
    public abstract void Cancel();

    protected void PlayAttackSound()
    {
        if (!audioSource) return;

        audioSource.pitch = Random.Range(0.9f, 1.1f);
        if (attackSound) audioSource.PlayOneShot(attackSound);
    }
}