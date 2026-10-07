using UnityEngine;

public class RelicPickup : MonoBehaviour, IInteractable
{
    [SerializeField] Transform relicSpawnPoint;

    int relicId;
    PlayerRelics owner;

    public void Init(int relicId, PlayerRelics owner)
    {
        this.relicId = relicId;
        this.owner = owner;

        RelicSO relic = RelicDatabase.Instance.GetRelic(relicId);
        gameObject.name = relic.name;
        
        Instantiate(relic.RelicObject, relicSpawnPoint);
    }

    public void Interact()
    {
        owner.Choose(relicId);
    }
}