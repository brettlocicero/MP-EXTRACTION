using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class PlayerRelics : NetworkBehaviour
{
    [Header("Offer")]
    [SerializeField] int offerSize = 3;
    [SerializeField] RelicPickup pickupPrefab;
    [SerializeField] float pickupSpacing = 2f;

    [Header("UI")]
    [SerializeField] Image relicImagePrefab;

    [Header("Testing")]
    [SerializeField] RelicSO[] testRelics;

    readonly NetworkList<int> ownedRelicIds = new();
    readonly List<RelicSO> activeRelics = new();
    readonly List<RelicPickup> spawnedPickups = new();

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
        // if (IsServer)
        // {
        //     foreach (RelicSO relic in testRelics)
        //         GrantRelic(relic.Id);
        // }
    }

    public override void OnNetworkDespawn()
    {
        ownedRelicIds.OnListChanged -= OnRelicsChanged;

        foreach (RelicSO relic in activeRelics)
            Destroy(relic);

        activeRelics.Clear();
        ClearPickups();
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
        if (IsServer)
        {
            foreach (RelicSO relic in activeRelics)
                relic.Trigger(weaponEvent, weaponContext);
        }
    }

    [ServerRpc]
    public void NotifyAttackServerRpc()
    {
        Trigger(WeaponEvent.OnAttack, new WeaponContext { SourceClientId = OwnerClientId });
    }

    // --- Offer flow ---

    // Server only. Rolls this player's offer and sends it to them alone.
    public void OfferRelics(Vector3 center)
    {
        if (!IsServer)
            return;

        pendingOffer = RelicDatabase.Instance.GetRandomOffer(offerSize, GetOwnedIds());

        if (pendingOffer.Length > 0)
            OfferRelicsRpc(pendingOffer, center, RpcTarget.Single(OwnerClientId, RpcTargetUse.Temp));
    }

    List<int> GetOwnedIds()
    {
        List<int> ids = new();

        foreach (int id in ownedRelicIds)
            ids.Add(id);

        return ids;
    }

    // Runs on the owning client. Pickups are local and never networked.
    [Rpc(SendTo.SpecifiedInParams)]
    void OfferRelicsRpc(int[] offeredIds, Vector3 center, RpcParams rpcParams)
    {
        ClearPickups();

        for (int i = 0; i < offeredIds.Length; i++)
        {
            float offset = (i - (offeredIds.Length - 1) / 2f) * pickupSpacing;
            RelicPickup pickup = Instantiate(pickupPrefab, center + Vector3.right * offset + Vector3.up * -25f, Quaternion.identity);

            pickup.Init(offeredIds[i], this);
            spawnedPickups.Add(pickup);
        }
    }

    public void Choose(int relicId)
    {
        ChooseRelicServerRpc(relicId);
        ClearPickups();
    }

    [ServerRpc]
    void ChooseRelicServerRpc(int relicId)
    {
        if (pendingOffer.Contains(relicId))
        {
            GrantRelic(relicId);
            pendingOffer = Array.Empty<int>();
        }
    }

    void ClearPickups()
    {
        foreach (RelicPickup pickup in spawnedPickups)
            Destroy(pickup.gameObject);

        spawnedPickups.Clear();
    }

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