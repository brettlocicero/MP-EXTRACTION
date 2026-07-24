using Steamworks;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerState : NetworkBehaviour
{
    [SerializeField] PlayerController playerController;
    [SerializeField] Transform nameplateTransform;
    [SerializeField] string playerName = "Player";

    [Header("Health")]
    [SerializeField] int startingMaxHealth = 100;

    public NetworkVariable<FixedString64Bytes> PlayerName = new(
        "Player",
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<int> CurrentHealth = new(
        100,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<int> MaxHealth = new(
        100,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public override void OnNetworkSpawn()
    {
        PlayerName.OnValueChanged += OnPlayerNameNetworkChanged;
        CurrentHealth.OnValueChanged += OnHealthChanged;
        MaxHealth.OnValueChanged += OnMaxHealthChanged;

        UIManager.Instance.SpawnPlayerNameplate(this);

        if (IsServer)
        {
            MaxHealth.Value = startingMaxHealth;
            CurrentHealth.Value = startingMaxHealth;
        }

        if (IsOwner)
        {
            string resolvedName = GetSteamOrFallbackName();
            SetPlayerNameServerRpc(resolvedName);
            GameManager.Instance.RegisterPlayer(playerController);
            UIManager.Instance.UpdateHealthBar(CurrentHealth.Value, MaxHealth.Value);
        }

        playerName = PlayerName.Value.ToString();
    }

    public override void OnNetworkDespawn()
    {
        PlayerName.OnValueChanged -= OnPlayerNameNetworkChanged;
        CurrentHealth.OnValueChanged -= OnHealthChanged;
        MaxHealth.OnValueChanged -= OnMaxHealthChanged;
    }

    void OnPlayerNameNetworkChanged(FixedString64Bytes oldName, FixedString64Bytes newName)
    {
        playerName = newName.ToString();
    }

    void OnHealthChanged(int oldHealth, int newHealth)
    {
        if (!IsOwner)
            return;

        UIManager.Instance.UpdateHealthBar(newHealth, MaxHealth.Value);
    }

    void OnMaxHealthChanged(int oldHealth, int newHealth)
    {
        if (!IsOwner)
            return;

        UIManager.Instance.UpdateHealthBar(CurrentHealth.Value, newHealth);
    }

    string GetSteamOrFallbackName()
    {
        if (SteamClient.IsValid)
        {
            return SteamClient.Name;
        }

        return $"Player #{OwnerClientId}";
    }

    [ServerRpc]
    void SetPlayerNameServerRpc(string nameToSet)
    {
        PlayerName.Value = nameToSet;
        playerName = nameToSet;
    }

    public void Damage(int amount)
    {
        if (!IsServer)
            return;

        CurrentHealth.Value = Mathf.Max(0, CurrentHealth.Value - amount);
    }

    public void Heal(int amount)
    {
        if (!IsServer)
            return;

        CurrentHealth.Value = Mathf.Min(MaxHealth.Value, CurrentHealth.Value + amount);
    }

    public void SetMaxHealth(int amount, bool healToFull = false)
    {
        if (!IsServer)
            return;

        MaxHealth.Value = amount;

        if (healToFull)
            CurrentHealth.Value = amount;
        else
            CurrentHealth.Value = Mathf.Min(CurrentHealth.Value, amount);
    }

    public bool IsDead => CurrentHealth.Value <= 0;

    public Transform GetNameplateTransform()
    {
        return nameplateTransform;
    }
}