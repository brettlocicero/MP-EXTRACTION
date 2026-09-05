using System.Collections;
using DG.Tweening;
using UnityEngine;

public class RegionGenerator : MonoBehaviour
{
    public static RegionGenerator Instance;

    [SerializeField] GameObject hubObjects;
    [SerializeField] Transform regionRoot;
    [SerializeField] RegionSO[] availableRegions;
    [SerializeField] Vector3 playerSpawnPos;
    [SerializeField] RegionTransitionAnimator regionTransitionAnimator;
    [SerializeField] float transitionDuration = 2f;

    RegionSO currentRegion;
    GameObject spawnedRegionInstance;

    int currentRegionIndexValue = -1;
    int currentRegionIndex
    {
        get => currentRegionIndexValue;
        set
        {
            if (currentRegionIndexValue == value) return;
            int previous = currentRegionIndexValue;
            currentRegionIndexValue = value;
            OnRegionIndexChanged(previous, value);
        }
    }

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        ApplyAtmosphere(currentRegionIndex);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void OnRegionIndexChanged(int previous, int current)
    {
        ApplyAtmosphere(current);
    }

    // --- Generation flow ---

    public void GenerateRegion(RegionSO region, int regionSeed)
    {
        StartCoroutine(GenerateRegionRoutine(region, regionSeed));
    }

    IEnumerator GenerateRegionRoutine(RegionSO region, int regionSeed)
    {
        Debug.Log($"Generating region '{region.RegionName}' with seed {regionSeed}");

        int regionIndex = System.Array.IndexOf(availableRegions, region);

        PlayTransition();
        yield return new WaitForSeconds(transitionDuration);

        currentRegion = region;
        currentRegionIndex = regionIndex;

        SpawnRegionBase(regionIndex, regionSeed);
        PostGeneration();
    }

    // --- Region base spawn/clear (local) ---

    void SpawnRegionBase(int regionIndex, int seed)
    {
        ClearInstancedRegion();

        Vector3 spawnPos = regionRoot != null ? regionRoot.position : Vector3.zero;
        spawnedRegionInstance = Instantiate(availableRegions[regionIndex].RegionBase, spawnPos, Quaternion.identity, regionRoot);

        System.Random localRng = new System.Random(seed);
        availableRegions[regionIndex].SpawnLandmarks(localRng, regionRoot);
    }

    void ClearInstancedRegion()
    {
        if (spawnedRegionInstance != null)
            Destroy(spawnedRegionInstance);

        spawnedRegionInstance = null;
    }

    // --- Transition ---

    void PlayTransition()
    {
        Sequence seq = regionTransitionAnimator.PlayTransition();
    }

    void PostGeneration()
    {
        hubObjects.SetActive(false);
    }

    // --- Helpers ---

    void ApplyAtmosphere(int regionIndex)
    {
        if (regionIndex < 0)
            return;

        availableRegions[regionIndex].ApplyRegionAtmosphere();
    }

}
