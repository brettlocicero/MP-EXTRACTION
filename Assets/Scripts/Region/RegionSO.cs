using System;
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
    [SerializeField] int extractionPointCount = 2;
    [SerializeField] LandmarkSO[] landmarks;
    [SerializeField] float placementRadius = 400f;
    [SerializeField] float minDistanceFromOrigin = 60f;
    [SerializeField] float minLandmarkBuffer = 5f;
    [SerializeField] int maxPlacementAttempts = 30;
    [SerializeField] float raycastHeight = 100f;
    [SerializeField] float raycastDistance = 200f;
    [SerializeField] LayerMask groundMask;

    [Header("Footprint Validation")]
    [SerializeField] int footprintSampleCount = 8;
    [SerializeField] float maxFootprintHeightVariance = 1.5f;

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

        List<LandmarkSO> placementOrder = BuildPlacementOrder(rng);
        MinDistanceSampler sampler = new MinDistanceSampler(minLandmarkBuffer);
        Vector2 origin2D = new Vector2(regionRoot.position.x, regionRoot.position.z);

        foreach (LandmarkSO landmark in placementOrder)
        {
            bool IsValidCandidate(Vector2 candidate2D) =>
                IsWithinRegionBounds(candidate2D, origin2D) &&
                sampler.IsSpaceAvailable(candidate2D, landmark.FootprintRadius) &&
                TryGetStableGroundPoint(To3D(candidate2D, regionRoot), landmark.FootprintRadius, out _);

            if (!TryFindPosition(rng, origin2D, IsValidCandidate, out Vector2 point2D))
            {
                Debug.LogWarning($"[RegionSO] Could not find space for landmark '{landmark.LandmarkName}'.");
                continue;
            }

            sampler.AddAcceptedPoint(point2D, landmark.FootprintRadius);
            PlaceLandmarkInstance(regionRoot, rng, landmark, point2D);
        }
    }

    bool TryFindPosition(System.Random rng, Vector2 origin2D, Func<Vector2, bool> isValidCandidate, out Vector2 result)
    {
        for (int attempt = 0; attempt < maxPlacementAttempts; attempt++)
        {
            float angle = (float)(rng.NextDouble() * Mathf.PI * 2.0);
            float distance = minDistanceFromOrigin + (float)(rng.NextDouble() * (placementRadius - minDistanceFromOrigin));
            Vector2 candidate = origin2D + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;

            if (isValidCandidate(candidate))
            {
                result = candidate;
                return true;
            }
        }

        result = Vector2.zero;
        return false;
    }

    List<LandmarkSO> BuildPlacementOrder(System.Random rng)
    {
        List<LandmarkSO> order = new();

        AddRandomOfRole(rng, order, LandmarkRole.BossArena, 1);
        AddRandomOfRole(rng, order, LandmarkRole.Extraction, extractionPointCount);

        LandmarkSO[] pool = FilterByRole(LandmarkRole.Powerup, LandmarkRole.Combat, LandmarkRole.KeyItem);
        int poolAmount = Mathf.Max(0, landmarkAmount - order.Count);

        for (int i = 0; i < poolAmount && pool.Length > 0; i++)
            order.Add(pool[rng.Next(pool.Length)]);

        // Largest footprints first, so small landmarks fill the gaps left around them.
        order.Sort((a, b) => b.FootprintRadius.CompareTo(a.FootprintRadius));

        return order;
    }

    void AddRandomOfRole(System.Random rng, List<LandmarkSO> order, LandmarkRole role, int count)
    {
        LandmarkSO[] candidates = FilterByRole(role);

        if (candidates.Length == 0)
            return;

        for (int i = 0; i < count; i++)
            order.Add(candidates[rng.Next(candidates.Length)]);
    }

    LandmarkSO[] FilterByRole(params LandmarkRole[] roles)
    {
        List<LandmarkSO> filtered = new();

        foreach (LandmarkSO landmark in landmarks)
        {
            if (Array.IndexOf(roles, landmark.Role) >= 0)
                filtered.Add(landmark);
        }

        return filtered.ToArray();
    }

    bool TryFindIndependentPosition(System.Random rng, Vector2 origin2D, float radius, Func<Vector2, bool> isValidCandidate, MinDistanceSampler sampler, out Vector2 result)
    {
        for (int attempt = 0; attempt < maxPlacementAttempts; attempt++)
        {
            float angle = (float)(rng.NextDouble() * Mathf.PI * 2.0);
            float distance = minDistanceFromOrigin + (float)(rng.NextDouble() * (placementRadius - minDistanceFromOrigin));
            Vector2 candidate = origin2D + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;

            if (sampler.IsSpaceAvailable(candidate, radius) && isValidCandidate(candidate))
            {
                sampler.AddAcceptedPoint(candidate, radius);
                result = candidate;
                return true;
            }
        }

        result = Vector2.zero;
        return false;
    }

    void PlaceLandmarkInstance(Transform regionRoot, System.Random rng, LandmarkSO landmark, Vector2 point2D)
    {
        TryGetStableGroundPoint(To3D(point2D, regionRoot), landmark.FootprintRadius, out Vector3 groundPoint);

        float yRotation = (float)(rng.NextDouble() * 360.0);
        Quaternion rotation = Quaternion.Euler(0f, yRotation, 0f);

        Instantiate(landmark.LandmarkObject, groundPoint, rotation, regionRoot);
    }

    bool IsWithinRegionBounds(Vector2 candidate2D, Vector2 origin2D)
    {
        float distance = Vector2.Distance(candidate2D, origin2D);
        return distance >= minDistanceFromOrigin && distance <= placementRadius;
    }

    Vector3 To3D(Vector2 xz, Transform regionRoot)
    {
        return new Vector3(xz.x, regionRoot.position.y, xz.y);
    }

    bool TryGetStableGroundPoint(Vector3 candidateXZ, float footprintRadius, out Vector3 groundPoint)
    {
        if (!TryRaycastGround(candidateXZ, out groundPoint))
            return false;

        for (int i = 0; i < footprintSampleCount; i++)
        {
            float angle = i * Mathf.PI * 2f / footprintSampleCount;
            Vector3 sampleXZ = candidateXZ + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * footprintRadius;

            if (!TryRaycastGround(sampleXZ, out Vector3 samplePoint))
                return false;

            if (Mathf.Abs(samplePoint.y - groundPoint.y) > maxFootprintHeightVariance)
                return false;
        }

        return true;
    }

    bool TryRaycastGround(Vector3 xzPosition, out Vector3 groundPoint)
    {
        Vector3 rayOrigin = new Vector3(xzPosition.x, xzPosition.y + raycastHeight, xzPosition.z);

        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, raycastDistance, groundMask))
        {
            groundPoint = hit.point;
            return true;
        }

        groundPoint = Vector3.zero;
        return false;
    }
}