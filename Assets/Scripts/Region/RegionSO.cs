using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RegionSO", menuName = "Scriptable Objects/RegionSO")]
public class RegionSO : ScriptableObject
{
    [SerializeField] string regionName;
    [SerializeField] GameObject baseWorldObject;
    [SerializeField] float radius = 1000f;

    [Header("Atmosphere")]
    [SerializeField] Material skybox;
    [SerializeField] Color sunColor;
    [SerializeField] Color ambientSkyColor;

    [Header("Points of Interest")]
    [SerializeField] PointOfInterestSO[] pointsOfInterest;
    [SerializeField] int poiSpawnAmount = 8;
    [SerializeField] float edgeMargin = 50f;
    [SerializeField] int maxPlacementAttempts = 30;
    [SerializeField] LayerMask groundLayerMask;

    public string RegionName => regionName;
    public GameObject BaseWorldObject => baseWorldObject;

    public void ApplyRegionAtmosphere()
    {
        RenderSettings.skybox = skybox;
        RenderSettings.sun.color = sunColor;
        RenderSettings.ambientSkyColor = ambientSkyColor;
    }

    public GameObject GenerateBaseWorld()
    {
        GameObject baseWorldObj = Instantiate(BaseWorldObject, Vector3.zero, Quaternion.identity);
        return baseWorldObj;
    }

    public void GeneratePointsOfInterest()
    {
        List<(Vector3 pos, float radius)> placed = new List<(Vector3, float)>();
        foreach (PointOfInterestSO poi in pointsOfInterest)
        {
            for (int i = 0; i < poiSpawnAmount; i++)
            {
                if (TryGetValidPosition(placed, poi.FootprintRadius, out Vector3 pos))
                {
                    placed.Add((pos, poi.FootprintRadius));
                    GameObject poiObject = Instantiate(poi.Object, pos, Quaternion.identity);
                    
                    if (poi.UseRandomRotation) 
                        OrientPOI(poiObject);
                }
            }
        }
    }

    void OrientPOI(GameObject poiObject)
    {
        float yRot = Random.Range(0f, 360f);
        poiObject.transform.localEulerAngles = new Vector3(0f, yRot, 0f);
    }

    bool TryGetValidPosition(List<(Vector3 pos, float radius)> existing, float footprintRadius, out Vector3 result)
    {
        for (int attempt = 0; attempt < maxPlacementAttempts; attempt++)
        {
            Vector2 circlePoint = Random.insideUnitCircle * (radius - edgeMargin);
            Vector3 candidate = new Vector3(circlePoint.x, 0f, circlePoint.y);

            if (TryProjectToGround(ref candidate))
            {
                bool valid = true;
                foreach (var (pos, otherRadius) in existing)
                {
                    if (Vector3.Distance(candidate, pos) < footprintRadius + otherRadius)
                    {
                        valid = false;
                        break;
                    }
                }

                if (valid)
                {
                    result = candidate;
                    return true;
                }
            }
        }

        result = Vector3.zero;
        return false;
    }

    bool TryProjectToGround(ref Vector3 point)
    {
        Vector3 rayOrigin = new Vector3(point.x, 5000f, point.z);
        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 10000f, groundLayerMask))
        {
            point.y = hit.point.y;
            return true;
        }

        return false;
    }
}