using System;
using Unity.Netcode;

[Serializable]
public class ItemInstance : INetworkSerializable
{
    static ulong nextInstanceId = 0;

    public ulong instanceId;
    public int baseItemId;
    public string customName;

    public ItemInstance() { }

    public ItemInstance(int baseItemId)
    {
        this.baseItemId = baseItemId;
        instanceId = nextInstanceId++;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref instanceId);
        serializer.SerializeValue(ref baseItemId);
        serializer.SerializeValue(ref customName);
    }
}