using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RelicDatabase : MonoBehaviour
{
    public static RelicDatabase Instance;

    [SerializeField] RelicSO[] relics;

    void Awake()
    {
        Instance = this;
    }

    public RelicSO GetRelic(int id)
    {
        RelicSO relic = relics.FirstOrDefault(r => r.Id == id);

        if (relic == null)
            Debug.LogError($"Relic ID {id} not found in the database!");

        return relic;
    }

    public int[] GetRandomOffer(int count, IEnumerable<int> ownedIds)
    {
        return relics
            .Select(relic => relic.Id)
            .Except(ownedIds)
            .OrderBy(id => Random.value)
            .Take(count)
            .ToArray();
    }
}