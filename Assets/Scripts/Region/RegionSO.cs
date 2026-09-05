using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RegionSO", menuName = "Scriptable Objects/RegionSO")]
public class RegionSO : ScriptableObject
{
    [SerializeField] string regionName;
    [SerializeField] int regionLength = 10;

    [Header("")]
    [SerializeField] GameObject regionBase;

    [Header("Landmarks")]
    [SerializeField] int landmarkAmount = 15;
    [SerializeField] LandmarkSO[] landmarks;
    [SerializeField] float placementRadius = 40f;
    [SerializeField] float minDistanceFromOrigin = 10f;
    [SerializeField] float minDistanceBetweenLandmarks = 8f;
    [SerializeField] int maxPlacementAttempts = 30;
    [SerializeField] float raycastHeight = 100f;
    [SerializeField] float raycastDistance = 200f;
    [SerializeField] LayerMask groundMask;

    [Header("Atmosphere")]
    [SerializeField] Material skybox;
    [SerializeField] Color sunColor;
    [SerializeField] Color ambientSkyColor;
    [SerializeField] Color fogColor;

    public string RegionName => regionName;
    public GameObject RegionBase => regionBase;

    public void ApplyRegionAtmosphere()
    {
        RenderSettings.skybox = skybox;
        RenderSettings.sun.color = sunColor;
        RenderSettings.ambientSkyColor = ambientSkyColor;
        RenderSettings.fogColor = fogColor;
    }

    public void SpawnLandmarks(System.Random rng, Transform regionRoot)
    {
        if (landmarks.Length == 0)
            return;

        List<Vector3> placedPositions = new();

        for (int i = 0; i < landmarkAmount; i++)
        {
            if (!TryFindValidPosition(rng, regionRoot.position, placedPositions, out Vector3 samplePos))
                continue;

            Vector3 rayOrigin = new Vector3(samplePos.x, regionRoot.position.y + raycastHeight, samplePos.z);

            if (!Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, raycastDistance, groundMask))
                continue;

            placedPositions.Add(samplePos);

            LandmarkSO landmark = landmarks[rng.Next(landmarks.Length)];
            float yRotation = (float)(rng.NextDouble() * 360.0);
            Quaternion rotation = Quaternion.Euler(0f, yRotation, 0f);

            Instantiate(landmark.LandmarkObject, hit.point, rotation, regionRoot);
        }
    }

    bool TryFindValidPosition(System.Random rng, Vector3 origin, List<Vector3> existingPositions, out Vector3 result)
    {
        for (int attempt = 0; attempt < maxPlacementAttempts; attempt++)
        {
            float angle = (float)(rng.NextDouble() * Mathf.PI * 2f);
            float distance = minDistanceFromOrigin + (float)(rng.NextDouble() * (placementRadius - minDistanceFromOrigin));
            Vector3 offset = new Vector3(Mathf.Cos(angle) * distance, 0f, Mathf.Sin(angle) * distance);
            Vector3 candidate = origin + offset;

            if (IsFarEnoughFromOthers(candidate, existingPositions))
            {
                result = candidate;
                return true;
            }
        }

        result = Vector3.zero;
        return false;
    }

    bool IsFarEnoughFromOthers(Vector3 candidate, List<Vector3> existingPositions)
    {
        foreach (Vector3 placed in existingPositions)
        {
            if (Vector3.Distance(candidate, placed) < minDistanceBetweenLandmarks)
                return false;
        }

        return true;
    }
}