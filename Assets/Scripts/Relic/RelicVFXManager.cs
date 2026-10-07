using Unity.Netcode;
using UnityEngine;

public class RelicVFXManager : NetworkBehaviour
{
    public static RelicVFXManager Instance;

    void Awake()
    {
        Instance = this;
    }

    public void PlayVFX(int relicId, Vector3 position)
    {
        PlayVFXRpc(relicId, position);
    }

    [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Everyone)]
    void PlayVFXRpc(int relicId, Vector3 position)
    {
        RelicDatabase.Instance.GetRelic(relicId).SpawnVFX(position);
    }
}