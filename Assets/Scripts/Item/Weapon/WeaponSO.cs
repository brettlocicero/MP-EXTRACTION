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
}
