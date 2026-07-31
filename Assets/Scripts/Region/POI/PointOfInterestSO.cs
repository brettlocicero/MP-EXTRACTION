using UnityEngine;

[CreateAssetMenu(fileName = "POI SO", menuName = "Scriptable Objects/POI SO")]
public class PointOfInterestSO : ScriptableObject
{
    [SerializeField] string poiName;
    [SerializeField] GameObject obj;
    [SerializeField] float footprintRadius = 50f;
    [SerializeField] bool useRandomRotation = true;

    public string POI_Name => poiName;
    public GameObject Object => obj;
    public float FootprintRadius => footprintRadius;
    public bool UseRandomRotation => useRandomRotation;
}
