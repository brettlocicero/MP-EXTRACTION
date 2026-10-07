using UnityEngine;

public abstract class RelicSO : ScriptableObject
{
    [SerializeField] WeaponEvent weaponEvent;
    [SerializeField] int relicId;
    [SerializeField] string relicName = "Unnamed Relic";
    [SerializeField, TextArea] string description;
    [SerializeField] Sprite icon;
    [SerializeField] GameObject relicObject;
    [SerializeField] GameObject vfxPrefab;

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
    
    public void SpawnVFX(Vector3 position)
    {
        if (vfxPrefab == null) return;
    
        Instantiate(vfxPrefab, position, Quaternion.identity);
    }
}