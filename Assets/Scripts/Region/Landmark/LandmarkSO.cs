using UnityEngine;

[CreateAssetMenu(fileName = "LandmarkSO", menuName = "Scriptable Objects/LandmarkSO")]
public class LandmarkSO : ScriptableObject
{
    [SerializeField] string landmarkName;
    [SerializeField] LandmarkObject landmarkObject;

    [Header("Category")]
    [SerializeField] LandmarkSize size = LandmarkSize.Small;
    [SerializeField] LandmarkRole role = LandmarkRole.Powerup;

    [Header("Placement")]
    [SerializeField] float footprintRadius = 5f;

    public string LandmarkName => landmarkName;
    public LandmarkObject LandmarkObject => landmarkObject;
    public LandmarkSize Size => size;
    public LandmarkRole Role => role;
    public float FootprintRadius => footprintRadius;
}