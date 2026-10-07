using UnityEngine;

public abstract class RelicSO : ScriptableObject
{
    [SerializeField] int relicId;
    [SerializeField] string relicName = "Unnamed Relic";
    [SerializeField, TextArea] string description;
    [SerializeField] Sprite icon;
    [SerializeField] GameObject relicObject;

    [Header("Relic Settings")]
    [SerializeField] WeaponEvent weaponEvent;

    public int Id => relicId;
    public string RelicName => relicName;
    public string Description => description;
    public Sprite Icon => icon;
    public GameObject RelicObject => relicObject;

    public void Trigger(WeaponEvent weaponEvent, WeaponContext weaponContext)
    {
        if (this.weaponEvent == weaponEvent)
            ApplyEffect(weaponContext);
    }

    protected abstract void ApplyEffect(WeaponContext weaponContext);
}