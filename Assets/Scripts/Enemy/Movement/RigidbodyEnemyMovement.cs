using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class RigidbodyEnemyMovement : EnemyMovement
{
    [Header("Movement")]
    [SerializeField] float moveSpeed = 3.5f;
    [SerializeField] float rotationSpeed = 10f;
    [SerializeField] float stoppingDistance = 0.1f;

    [Header("Separation")]
    [SerializeField] float separationRadius = 1.5f;
    [SerializeField] float separationWeight = 1f;
    [SerializeField] LayerMask enemyLayerMask;

    Rigidbody rb;
    Vector3 destination;
    bool isMoving;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    public override void MoveTo(Vector3 destination)
    {
        this.destination = destination;
        isMoving = true;
    }

    public override void Stop()
    {
        isMoving = false;
        rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
        rb.angularVelocity = Vector3.zero;
    }

    public override void Face(Vector3 position)
    {
        Vector3 direction = position - rb.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.001f)
            rb.MoveRotation(Quaternion.LookRotation(direction));
    }

    void FixedUpdate()
    {
        rb.angularVelocity = Vector3.zero;

        if (!isMoving)
            return;

        Vector3 toDestination = destination - rb.position;
        toDestination.y = 0f;

        if (toDestination.magnitude <= stoppingDistance)
        {
            Stop();
            return;
        }

        Vector3 seekDirection = toDestination.normalized;
        Vector3 separationDirection = GetSeparationDirection();

        Vector3 moveDirection = (seekDirection + separationDirection * separationWeight).normalized;

        rb.linearVelocity = new Vector3(
            moveDirection.x * moveSpeed,
            rb.linearVelocity.y,
            moveDirection.z * moveSpeed
        );

        Quaternion targetRotation = Quaternion.LookRotation(seekDirection);
        rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime));
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