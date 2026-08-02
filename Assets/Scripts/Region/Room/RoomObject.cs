using UnityEngine;

public class RoomObject : MonoBehaviour
{
    [SerializeField] Transform connector;

    public Transform Connector => connector;
}
