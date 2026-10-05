using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class PlayerRelics : NetworkBehaviour
{
    [SerializeField] int offerSize = 3;
    [SerializeField] RelicSO testRelic;

    readonly NetworkList<int> ownedRelicIds = new();
    readonly List<RelicSO> activeRelics = new();

    int[] pendingOffer = Array.Empty<int>();

    public override void OnNetworkSpawn()
    {
        ownedRelicIds.OnListChanged += OnRelicsChanged;

        if (IsServer && testRelic != null)
            GrantRelic(testRelic.Id);
    }

    public override void OnNetworkDespawn()
    {
        ownedRelicIds.OnListChanged -= OnRelicsChanged;

        foreach (RelicSO relic in activeRelics)
            Destroy(relic);

        activeRelics.Clear();
    }

    public void GrantRelic(int relicId)
    {
        if (IsServer)
            ownedRelicIds.Add(relicId);
    }

    void OnRelicsChanged(NetworkListEvent<int> changeEvent)
    {
        if (IsServer && changeEvent.Type == NetworkListEvent<int>.EventType.Add)
            AddRuntimeRelic(changeEvent.Value);
    }

    void AddRuntimeRelic(int relicId)
    {
        RelicSO relic = Instantiate(RelicDatabase.Instance.GetRelic(relicId));
        activeRelics.Add(relic);

        relic.Trigger(WeaponEvent.Passive, new WeaponContext { SourceClientId = OwnerClientId });
    }

    public void Trigger(WeaponEvent weaponEvent, WeaponContext weaponContext)
    {
        if (!IsServer)
        {
            return;
        }

        foreach (RelicSO relic in activeRelics)
            relic.Trigger(weaponEvent, weaponContext);
    }

    [ServerRpc]
    public void NotifyAttackServerRpc()
    {
        Trigger(WeaponEvent.OnAttack, new WeaponContext { SourceClientId = OwnerClientId });
    }

    public void OfferRelics()
    {
        if (!IsServer)
            return;

        pendingOffer = RelicDatabase.Instance.GetRandomOffer(offerSize, GetOwnedIds());

        if (pendingOffer.Length > 0)
            OfferRelicsRpc(pendingOffer, RpcTarget.Single(OwnerClientId, RpcTargetUse.Temp));
    }

    List<int> GetOwnedIds()
    {
        List<int> ids = new();

        foreach (int id in ownedRelicIds)
            ids.Add(id);

        return ids;
    }

    [Rpc(SendTo.SpecifiedInParams)]
    void OfferRelicsRpc(int[] offeredIds, RpcParams rpcParams)
    {
        // RelicChoiceUI.Instance.Show(offeredIds, this);
    }

    public void Choose(int relicId)
    {
        ChooseRelicServerRpc(relicId);
    }

    [ServerRpc]
    void ChooseRelicServerRpc(int relicId)
    {
        if (!pendingOffer.Contains(relicId))
            return;

        GrantRelic(relicId);
        pendingOffer = Array.Empty<int>();
    }
}