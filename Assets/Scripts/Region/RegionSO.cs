using UnityEngine;

[CreateAssetMenu(fileName = "RegionSO", menuName = "Scriptable Objects/RegionSO")]
public class RegionSO : ScriptableObject
{
    [SerializeField] string regionName;

    [Header("Atmosphere")]
    [SerializeField] Material skybox;
    [SerializeField] Color sunColor;
    [SerializeField] Color ambientSkyColor;
    [SerializeField] Color fogColor;

    public string RegionName => regionName;

    public void ApplyRegionAtmosphere()
    {
        RenderSettings.skybox = skybox;
        if (RenderSettings.sun != null)
            RenderSettings.sun.color = sunColor;
        RenderSettings.ambientSkyColor = ambientSkyColor;
        RenderSettings.fogColor = fogColor;
    }
}
