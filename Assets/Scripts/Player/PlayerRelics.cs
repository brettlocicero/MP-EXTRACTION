using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class PlayerRelics : NetworkBehaviour
{
    [SerializeField] int offerSize = 3;
    [SerializeField] RelicSO[] testRelics;
    [SerializeField] Image relicImagePrefab;

    readonly NetworkList<int> ownedRelicIds = new();
    readonly List<RelicSO> activeRelics = new();

    int[] pendingOffer = Array.Empty<int>();
    Transform relicUiRoot;

    public override void OnNetworkSpawn()
    {
        ownedRelicIds.OnListChanged += OnRelicsChanged;

        if (IsOwner)
        {
            // TODO: add better referencing
            relicUiRoot = GameObject.Find("Relic UI Root").transform;
            UpdateRelicUI();
        }

        // TODO: remove this --> testing
        if (IsServer)
        {
            foreach (RelicSO relic in testRelics)
                GrantRelic(relic.Id);
        }
    }

    public override void OnNetworkDespawn()
    {
        ownedRelicIds.OnListChanged -= OnRelicsChanged;

        foreach (RelicSO relic in activeRelics)
            Destroy(relic);

        activeRelics.Clear();
    }

    // Server only. The one way a relic is added to a player.
    public void GrantRelic(int relicId)
    {
        if (IsServer)
            ownedRelicIds.Add(relicId);
    }

    void OnRelicsChanged(NetworkListEvent<int> changeEvent)
    {
        if (IsServer && changeEvent.Type == NetworkListEvent<int>.EventType.Add)
            AddRuntimeRelic(changeEvent.Value);

        if (IsOwner)
            UpdateRelicUI();
    }

    void AddRuntimeRelic(int relicId)
    {
        // Clone the SO so each player gets their own state (hit counters, etc.)
        RelicSO relic = Instantiate(RelicDatabase.Instance.GetRelic(relicId));
        activeRelics.Add(relic);

        relic.Trigger(WeaponEvent.Passive, new WeaponContext { SourceClientId = OwnerClientId });
    }

    // Server only. Client hits reach here through EnemyAI on the server.
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

    // --- Offer flow ---

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

    // --- UI (owner only, built from the replicated list) ---

    void UpdateRelicUI()
    {
        foreach (Transform child in relicUiRoot)
            Destroy(child.gameObject);

        foreach (int id in ownedRelicIds)
        {
            Image relicUi = Instantiate(relicImagePrefab, relicUiRoot);
            relicUi.sprite = RelicDatabase.Instance.GetRelic(id).Icon;
        }
    }
}