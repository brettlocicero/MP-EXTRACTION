using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class RigidbodyEnemyMovement : EnemyMovement
{
    enum MoveMode { Idle, Seeking, Fleeing }

    [Header("Movement")]
    [SerializeField] float moveSpeed = 3.5f;
    [SerializeField] float rotationSpeed = 10f;
    [SerializeField] float stoppingDistance = 0.1f;
    [SerializeField] bool ignoreMovementRotation = false;

    [Header("Separation")]
    [SerializeField] float separationRadius = 1.5f;
    [SerializeField] float separationWeight = 1f;
    [SerializeField] LayerMask enemyLayerMask;

    Rigidbody rb;
    Vector3 focusPoint;
    MoveMode moveMode = MoveMode.Idle;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    public override void MoveTo(Vector3 destination)
    {
        focusPoint = destination;
        moveMode = MoveMode.Seeking;
    }

    public override void MoveAwayFrom(Vector3 position)
    {
        focusPoint = position;
        moveMode = MoveMode.Fleeing;
    }

    public override void Stop()
    {
        moveMode = MoveMode.Idle;
        rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
        rb.angularVelocity = Vector3.zero;
    }

    public override void Face(Vector3 position)
    {
        Vector3 direction = position - rb.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime));
        }
    }

    void FixedUpdate()
    {
        rb.angularVelocity = Vector3.zero;

        if (moveMode == MoveMode.Idle)
            return;

        Vector3 toFocus = focusPoint - rb.position;
        toFocus.y = 0f;

        if (moveMode == MoveMode.Seeking && toFocus.magnitude <= stoppingDistance)
        {
            Stop();
            return;
        }

        Vector3 seekDirection = GetSeekDirection(toFocus);
        Vector3 separationDirection = GetSeparationDirection();

        Vector3 moveDirection = (seekDirection + separationDirection * separationWeight).normalized;

        rb.linearVelocity = new Vector3(
            moveDirection.x * moveSpeed,
            rb.linearVelocity.y,
            moveDirection.z * moveSpeed
        );

        if (!ignoreMovementRotation)
        {
            Quaternion targetRotation = Quaternion.LookRotation(seekDirection);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime));
        }
    }

    Vector3 GetSeekDirection(Vector3 toFocus)
    {
        if (moveMode == MoveMode.Seeking)
            return toFocus.normalized;

        if (toFocus.sqrMagnitude > 0.001f)
            return -toFocus.normalized;

        return transform.right;
    }

    Vector3 GetSeparationDirection()
    {
        Collider[] neighbors = Physics.OverlapSphere(rb.position, separationRadius, enemyLayerMask);
        Vector3 push = Vector3.zero;

        foreach (Collider neighbor in neighbors)
        {
            if (neighbor.attachedRigidbody == rb)
                continue;

            Vector3 offset = rb.position - neighbor.attachedRigidbody.position;
            offset.y = 0f;

            float distance = offset.magnitude;

            if (distance > 0.001f)
            {
                float falloff = Mathf.Clamp01(1f - (distance / separationRadius));
                push += offset.normalized * falloff;
            }
        }

        return push;
    }
}