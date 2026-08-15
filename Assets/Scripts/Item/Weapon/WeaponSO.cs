using System;
using UnityEngine;

[CreateAssetMenu(fileName = "WeaponSO", menuName = "Scriptable Objects/Items/WeaponSO")]
public class WeaponSO : ItemSO
{
    public Attack[] attacks;

    [Header("Stats")]
    public float range = 3f;
    public float attackRate = 1f;

    [Header("FX")]
    public AudioClip attackSound;
    public AudioClip contactSound;
    public Vector2 attackSoundPitchRange;

    public (float, float) GetDamageRange()
    {
        float minDmg = Mathf.Infinity;
        float maxDmg = Mathf.NegativeInfinity;

        foreach (Attack attack in attacks)
        {
            minDmg = Mathf.Min(attack.damage, minDmg);
            maxDmg = Math.Max(attack.damage, maxDmg);
        }

        return (minDmg, maxDmg);
    }
}
