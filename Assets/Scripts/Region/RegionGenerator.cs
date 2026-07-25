using UnityEngine;

public class RegionGenerator : MonoBehaviour
{
    public static RegionGenerator Instance;
    void Awake()
    {
        Instance = this;
    }

    GameObject instancedRegion;

    public void GenerateRegion(RegionSO region)
    {
        ClearInstancedRegion();

        // TODO: Add generation extra logic here.
        instancedRegion = new GameObject($"Region: {region.RegionName}");
        GameObject baseWorldObj = region.GenerateBaseWorld();
        baseWorldObj.transform.SetParent(instancedRegion.transform);

        region.ApplyRegionAtmosphere();
    }

    void ClearInstancedRegion()
    {
        if (instancedRegion)
        {
            Destroy(instancedRegion);
        }
    }
}
