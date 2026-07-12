using UnityEngine;

[CreateAssetMenu(fileName = "WeaponSO", menuName = "Scriptable Objects/Items/WeaponSO")]
public class WeaponSO : ItemSO
{
    public Attack[] attacks;

    [Header("Stats")]
    public float attackRate = 1f;
}
