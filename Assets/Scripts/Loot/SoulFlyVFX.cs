using UnityEngine;

public class SoulsFlyVFX : MonoBehaviour
{
    [SerializeField] float flySpeed = 6f;
    [SerializeField] float arriveDistance = 0.3f;

    Transform targetPlayer;

    public void SetTarget(Transform player) => targetPlayer = player;

    void FixedUpdate()
    {
        if (targetPlayer == null)
            return;

        transform.position = Vector3.MoveTowards(transform.position, targetPlayer.transform.position, flySpeed * Time.deltaTime);
        if (Vector3.Distance(transform.position, targetPlayer.transform.position) <= arriveDistance)
            Destroy(gameObject);
    }
}