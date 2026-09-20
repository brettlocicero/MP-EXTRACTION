using UnityEngine;

public abstract class EnemyMovement : MonoBehaviour
{
    public abstract void MoveTo(Vector3 destination);
    public abstract void MoveAwayFrom(Vector3 position);
    public abstract void Stop();
    public abstract void Face(Vector3 position);
}