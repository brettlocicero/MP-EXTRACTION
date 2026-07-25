using UnityEngine;

[CreateAssetMenu(fileName = "RegionSO", menuName = "Scriptable Objects/RegionSO")]
public class RegionSO : ScriptableObject
{
    [SerializeField] string regionName;
    [SerializeField] GameObject baseWorldObject;

    [Header("Atmosphere")]
    [SerializeField] Material skybox;
    [SerializeField] Color sunColor;
    [SerializeField] Color ambientSkyColor;

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
}