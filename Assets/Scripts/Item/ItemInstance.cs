using System;
using System.Collections.Generic;

[Serializable]
public class ItemInstance
{
    static ulong nextInstanceId = 0;

    public ulong instanceId;
    public int baseItemId;
    public string customName;

    public List<SoulShardSO> soulShards = new();

    public ItemInstance() { }

    public ItemInstance(int baseItemId)
    {
        this.baseItemId = baseItemId;
        instanceId = nextInstanceId++;
    }

    public void AddSoulShard(SoulShardSO soulShard)
    {
        soulShards.Add(soulShard);
    }
}