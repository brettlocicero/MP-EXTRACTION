using Unity.Netcode;
using UnityEngine;

[CreateAssetMenu(fileName = "VampiricShard", menuName = "Scriptable Objects/Soul Shards/VampiricShard")]
public class VampiricShard : SoulShardSO
{
    [SerializeField, Range(0f, 1f)] float healPercent = 0.05f;

    protected override void ApplyEffect(WeaponContext weaponContext)
    {
        PlayerState player = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<PlayerState>();
        float totalDamage = weaponContext.Damage * weaponContext.HitEnemies.Length;

        player.HealRpc(Mathf.CeilToInt(totalDamage * healPercent));
    }
}