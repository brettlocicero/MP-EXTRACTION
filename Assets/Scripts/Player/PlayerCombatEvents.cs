using System;
using UnityEngine;

public class PlayerCombatEvents : MonoBehaviour
{
    public Action<EnemyAI, float> OnHit;
    public Action<EnemyAI> OnKill;
}