using UnityEngine;

public abstract class DebuffSO : ScriptableObject
{
    public string debuffId;
    [SerializeField] GameObject debuffVFX;
    [SerializeField] float tickInterval = 1f;
    [SerializeField] int ticks = 5;

    public float TickInterval => tickInterval;
    public float Duration => tickInterval * ticks;

    public abstract void Effect(EnemyAI target, ulong sourceClientId);
}