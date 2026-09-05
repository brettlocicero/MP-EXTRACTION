using UnityEngine;

public class PlayerState : MonoBehaviour
{
    [SerializeField] PlayerController playerController;
    [SerializeField] Transform nameplateTransform;
    [SerializeField] string playerName = "Player";
    [Header("Health")]
    [SerializeField] int startingMaxHealth = 100;

    public string PlayerName => playerName;
    public int CurrentHealth { get; private set; }
    public int MaxHealth { get; private set; }
    public bool IsDead => CurrentHealth <= 0;

    void Awake()
    {
        MaxHealth = startingMaxHealth;
        CurrentHealth = startingMaxHealth;
    }

    void Start()
    {
        GameManager.Instance.RegisterPlayer(playerController);
        RefreshHealthUI();
    }

    public void Damage(int amount)
    {
        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        RefreshHealthUI();
    }

    public void Heal(int amount)
    {
        CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
        RefreshHealthUI();
    }

    public void SetMaxHealth(int amount, bool healToFull = false)
    {
        MaxHealth = amount;
        CurrentHealth = healToFull ? amount : Mathf.Min(CurrentHealth, amount);
        RefreshHealthUI();
    }

    void RefreshHealthUI() => UIManager.Instance.UpdateHealthBar(CurrentHealth, MaxHealth);
    public Transform GetNameplateTransform() => nameplateTransform;
}
