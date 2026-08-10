using UnityEngine;

[System.Serializable]
public class LootDrop
{
    [Range(0f, 1f), SerializeField] float dropChance;
    [SerializeField] LootItem lootItem;

    public float DropChance => dropChance;
    public LootItem LootItem => lootItem;
    public bool RollDrop => Random.value <= DropChance;
}
