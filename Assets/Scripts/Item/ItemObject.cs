using UnityEngine;

public class ItemObject : MonoBehaviour
{
    [SerializeField] protected ItemSO item;

    [Header("Runtime")]
    [SerializeField] ItemInstance instance;

    [Header("Item References")]
    [SerializeField] Animator movementAnimator;

    [Header("Sprint Effect")]
    [SerializeField] Vector3 sprintPositionOffset;
    [SerializeField] Vector3 sprintRotationOffset;
    [SerializeField] float sprintTransitionSpeed = 8f;

    CharacterController playerCC;
    protected PlayerController playerController;
    protected Transform cameraTransform;

    float movementVelocity = 0f;
    float currentMovement = 0f;

    Vector3 baseLocalPosition;
    Quaternion baseLocalRotation;
    Vector3 currentSprintPosOffset;
    Quaternion currentSprintRotOffset = Quaternion.identity;

    public ItemSO ItemData => item;
    public ItemInstance Instance => instance;

    protected virtual void Start()
    {
        playerCC = GetComponentInParent<CharacterController>();
        playerController = GetComponentInParent<PlayerController>();
        cameraTransform = GetComponentInParent<Camera>().transform;

        baseLocalPosition = transform.localPosition;
        baseLocalRotation = transform.localRotation;

        CursorManager.LockCursor();
    }

    protected virtual void Update()
    {
        HandleMovementAnimation();
        HandleSprintEffect();
    }

    public void AssignInstance(ItemInstance instance)
    {
        this.instance = instance;
    }

    void HandleMovementAnimation()
    {
        if (playerController.IsMovementLocked()) return;

        float targetMovement = InputManager.Actions.Player.Move.ReadValue<Vector2>().magnitude;

        if (!playerCC.isGrounded)
            targetMovement = 0f;

        currentMovement = Mathf.SmoothDamp(currentMovement, targetMovement, ref movementVelocity, 0.1f);
        movementAnimator.SetFloat("Movement", currentMovement);
    }

    void HandleSprintEffect()
    {
        Vector3 targetPosOffset = playerController.IsSprinting() ? sprintPositionOffset : Vector3.zero;
        Quaternion targetRotOffset = playerController.IsSprinting() ? Quaternion.Euler(sprintRotationOffset) : Quaternion.identity;

        currentSprintPosOffset = Vector3.Lerp(currentSprintPosOffset, targetPosOffset, Time.deltaTime * sprintTransitionSpeed);
        currentSprintRotOffset = Quaternion.Slerp(currentSprintRotOffset, targetRotOffset, Time.deltaTime * sprintTransitionSpeed);

        transform.localPosition = baseLocalPosition + currentSprintPosOffset;
        transform.localRotation = baseLocalRotation * currentSprintRotOffset;
    }
}