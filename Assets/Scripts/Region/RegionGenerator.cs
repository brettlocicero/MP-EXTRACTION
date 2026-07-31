using UnityEngine;

public class RegionGenerator : MonoBehaviour
{
    public static RegionGenerator Instance;
    void Awake()
    {
        Instance = this;
    }

    GameObject instancedRegion;

    public void GenerateRegion(RegionSO region, int regionSeed)
    {
        Random.InitState(regionSeed);
        ClearInstancedRegion();

        Debug.Log($"Spawning region with {regionSeed}");

        // Create based region root object.
        instancedRegion = new GameObject($"Region: {region.RegionName}");
        GameObject baseWorldObj = region.GenerateBaseWorld();
        baseWorldObj.transform.SetParent(instancedRegion.transform);

        // Build the environment for the region.
        region.ApplyRegionAtmosphere();
        region.GeneratePointsOfInterest();
    }

    void ClearInstancedRegion()
    {
        if (instancedRegion)
        {
            Destroy(instancedRegion);
        }
    }
}
