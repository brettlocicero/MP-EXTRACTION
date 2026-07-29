using System;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class PlayerCurrency : NetworkBehaviour
{
    public NetworkVariable<int> Souls = new(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
    );

    TextMeshProUGUI soulsText;

    void Start()
    {
        soulsText = UIManager.Instance.SoulsText;
    }

    public override void OnNetworkSpawn()
    {
        Souls.OnValueChanged += UpdateSoulsTextUI;

        if (IsOwner)
        {
            soulsText = UIManager.Instance.SoulsText;
            soulsText.text = Souls.Value.ToString();
            var data = SaveManager.Load();
            SubmitLoadedSoulsServerRpc(data.souls);
        }
    }

    public override void OnNetworkDespawn()
    {
        Souls.OnValueChanged -= UpdateSoulsTextUI;
    }

    [ServerRpc]
    void SubmitLoadedSoulsServerRpc(int loadedSouls) => Souls.Value = loadedSouls;

    public void AddSouls(int amount)
    {
        if (!IsServer) return;
        Souls.Value += amount;
        PersistCurrentSouls();
    }

    public bool SpendSouls(int amount)
    {
        if (!IsServer || Souls.Value < amount) return false;
        Souls.Value -= amount;

        return true;
    }

    public void ClearSoulsOnRunLoss()
    {
        if (!IsServer) return;
        Souls.Value = 0;
        PersistToOwner();
    }

    public void PersistCurrentSouls()
    {
        if (!IsServer) return;
        PersistToOwner();
    }

    void PersistToOwner()
    {
        var rpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { OwnerClientId } }
        };

        PersistSoulsClientRpc(Souls.Value, rpcParams);
    }

    [ClientRpc]
    void PersistSoulsClientRpc(int soulsValue, ClientRpcParams rpcParams = default)
    {
        SaveManager.Save(new PlayerSaveData { souls = soulsValue });
    }

    void UpdateSoulsTextUI(int previousValue, int newValue)
    {
        if (!IsOwner)
            return;

        if (soulsText)
            soulsText.text = newValue.ToString();
    }
}