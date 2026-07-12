using UnityEngine;

public class ItemObject : MonoBehaviour
{
    [SerializeField] protected ItemSO item;

    [Header("Item References")]
    [SerializeField] Animator movementAnimator;

    CharacterController playerCC;

    float movementVelocity = 0f;
    float currentMovement = 0f;

    protected virtual void Start()
    {
        playerCC = GetComponentInParent<CharacterController>();
    }

    protected virtual void Update()
    {
        HandleMovementAnimation();
    }

    void HandleMovementAnimation()
    {
        float targetMovement = InputManager.Actions.Player.Move.ReadValue<Vector2>().magnitude;

        if (!playerCC.isGrounded)
            targetMovement = 0f;

        currentMovement = Mathf.SmoothDamp(currentMovement, targetMovement, ref movementVelocity, 0.1f);
        movementAnimator.SetFloat("Movement", currentMovement);
    }
}
