using UnityEngine;

[CreateAssetMenu(fileName = "LandmarkSO", menuName = "Scriptable Objects/LandmarkSO")]
public class LandmarkSO : ScriptableObject
{
    [SerializeField] string landmarkName;
    [SerializeField] LandmarkObject landmarkObject;

    public string LandmarkName => landmarkName;
    public LandmarkObject LandmarkObject => landmarkObject;
}