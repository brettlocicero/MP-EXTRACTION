using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] Transform cameraTransform;
    [SerializeField] Transform inventoryCameraTransform;
    public Camera camera;
    [SerializeField] Transform clientObjects;
    [SerializeField] GameObject serverObjects;
    [SerializeField] Animator modelAnimator;

    [Header("Movement")]
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float sprintSpeed = 8f;
    [SerializeField] float gravity = -30f;
    [SerializeField] float jumpHeight = 1.5f;

    [Header("Ground Stick")]
    [SerializeField] float groundStickVelocity = 8f;
    [SerializeField] float minAirborneTimeForLandSound = 0.15f;

    [Header("Look")]
    [SerializeField] float mouseSensitivity = 0.15f;

    [Header("Head Bob")]
    [SerializeField] float bobFrequency = 10f;
    [SerializeField] float bobAmount = 0.05f;
    [SerializeField] float bobSideAmount = 0.03f;
    [SerializeField] float bobSmoothSpeed = 8f;

    [Header("Head Tilt")]
    [SerializeField] float maxTilt = 5f;
    [SerializeField] float tiltSmoothSpeed = 8f;
    [SerializeField] CinemachineCamera cinemachineCam;

    [Header("Audio")]
    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioClip[] footstepSounds;
    [SerializeField] AudioClip[] sprintFootstepSounds;
    [SerializeField] AudioClip jumpSound;
    [SerializeField] AudioClip landSound;
    [SerializeField] float landSoundVelocityThreshold = -4f;
    [SerializeField] float footstepInterval = 0.45f;
    [SerializeField] float sprintFootstepInterval = 0.3f;

    CharacterController controller;

    Vector2 moveInput;
    Vector2 lookInput;

    float verticalVelocity;
    float cameraPitch;

    float footstepTimer;
    float airborneTimer;

    bool wasGrounded;
    bool isJumping;
    bool isSprinting;

    Vector3 cameraDefaultPosition;
    float bobTimer;
    float currentTilt;
    bool isSensLocked = false;
    bool isMovementLocked = false;

    void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (cameraTransform != null)
            cameraDefaultPosition = cameraTransform.localPosition;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            controller.enabled = false;
            clientObjects.gameObject.SetActive(false);
            inventoryCameraTransform.gameObject.SetActive(false);
            return;
        }

        clientObjects.gameObject.SetActive(true);
        SetLayerRecursively(serverObjects, LayerMask.NameToLayer("LocalHidden"));

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (!IsOwner)
            return;

        ReadInput();
        
        HandleHeadBob();
        HandleHeadTilt();

        Look();
        Move();
    }

    void ReadInput()
    {
        moveInput = InputManager.Actions.Player.Move.ReadValue<Vector2>();
        lookInput = InputManager.Actions.Player.Look.ReadValue<Vector2>();

        isSprinting = InputManager.Actions.Player.Sprint.IsPressed() && !isMovementLocked && moveInput.y > 0.1f;

        if (isMovementLocked) moveInput = Vector2.zero;
        if (isSensLocked) lookInput = Vector2.zero;
    }

    void HandleHeadBob()
    {
        bool moving = moveInput.magnitude > 0.1f && controller.isGrounded;

        if (moving)
        {
            bobTimer += Time.deltaTime * bobFrequency;

            float bobY = Mathf.Sin(bobTimer) * bobAmount;
            float bobX = Mathf.Cos(bobTimer * 0.5f) * bobSideAmount;

            Vector3 targetPosition = cameraDefaultPosition + new Vector3(bobX, bobY, 0);

            cameraTransform.localPosition = Vector3.Lerp(cameraTransform.localPosition, targetPosition, Time.deltaTime * bobSmoothSpeed);
        }

        else
        {
            bobTimer = 0;
            cameraTransform.localPosition = Vector3.Lerp(cameraTransform.localPosition, cameraDefaultPosition, Time.deltaTime * bobSmoothSpeed);
        }
    }

    void HandleHeadTilt()
    {
        if (cinemachineCam == null)
            return;

        float targetTilt = -moveInput.x * maxTilt;

        currentTilt = Mathf.Lerp(currentTilt, targetTilt, Time.deltaTime * tiltSmoothSpeed);
        cinemachineCam.Lens.Dutch = currentTilt;
    }

    void Move()
    {
        bool grounded = controller.isGrounded;
        float fallVelocity = verticalVelocity;

        if (grounded)
            verticalVelocity = -groundStickVelocity;

        float currentSpeed = isSprinting ? sprintSpeed : moveSpeed;
        Vector3 horizontalMove = transform.right * moveInput.x + transform.forward * moveInput.y;

        HandleJump(grounded);

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 fullMove = horizontalMove * currentSpeed + Vector3.up * verticalVelocity;
        controller.Move(fullMove * Time.deltaTime);

        HandleGroundSounds(grounded, fallVelocity);

        modelAnimator.SetFloat("Movement", moveInput.magnitude, 0.1f, Time.deltaTime);

        wasGrounded = grounded;
    }

    void HandleJump(bool grounded)
    {
        if (!grounded || isMovementLocked)
            return;

        if (InputManager.Actions.Player.Jump.WasPressedThisFrame())
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);

            isJumping = true;

            if (jumpSound)
                audioSource.PlayOneShot(jumpSound);
        }
    }

    void HandleGroundSounds(bool grounded, float fallVelocity)
    {
        if (!grounded)
            airborneTimer += Time.deltaTime;

        // Landing
        if (!wasGrounded && grounded)
        {
            isJumping = false;

            if (landSound && fallVelocity < landSoundVelocityThreshold && airborneTimer >= minAirborneTimeForLandSound)
                audioSource.PlayOneShot(landSound);

            airborneTimer = 0f;
        }

        // Footsteps
        if (grounded && moveInput.magnitude > 0.1f)
        {
            footstepTimer -= Time.deltaTime;

            if (footstepTimer <= 0)
            {
                PlayFootstep();
                footstepTimer = isSprinting ? sprintFootstepInterval : footstepInterval;
            }
        }

        else
        {
            footstepTimer = 0;
        }
    }

    void PlayFootstep()
    {
        AudioClip[] clips = isSprinting ? sprintFootstepSounds : footstepSounds;

        if (clips.Length == 0)
            return;

        AudioClip clip = clips[Random.Range(0, clips.Length)];

        audioSource.PlayOneShot(clip);
    }

    void Look()
    {
        float mouseX = lookInput.x * mouseSensitivity;
        float mouseY = lookInput.y * mouseSensitivity;

        cameraPitch -= mouseY;
        cameraPitch = Mathf.Clamp(cameraPitch, -90f, 90f);

        cameraTransform.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    public void PlayAttackAnimation()
    {
        modelAnimator.SetTrigger("Attack");
    }

    void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;

        foreach (Transform child in obj.transform)
            SetLayerRecursively(child.gameObject, layer);
    }

    public void Teleport(Vector3 position, Quaternion rotation)
    {
        if (!IsServer)
            return;

        ClientRpcParams rpcParams = new()
        {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { OwnerClientId } }
        };

        TeleportClientRpc(position, rotation, rpcParams);
    }

    [ClientRpc]
    void TeleportClientRpc(Vector3 position, Quaternion rotation, ClientRpcParams rpcParams = default)
    {
        controller.enabled = false;
        transform.position = position;
        transform.rotation = rotation;
        controller.enabled = true;

        verticalVelocity = 0f;
    }

    public void LockSensitivity()
    {
        isSensLocked = true;
    }

    public void UnlockSensitivity()
    {
        isSensLocked = false;
    }

    public void LockMovement()
    {
        isMovementLocked = true;
    }

    public void UnlockMovement()
    {
        isMovementLocked = false;
    }

    public bool IsSensitivityLocked()
    {
        return isSensLocked;
    }

    public bool IsMovementLocked()
    {
        return isMovementLocked;
    }

    public bool IsSprinting()
    {
        return isSprinting;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void LaunchProjectileRpc(int weaponId, int attackIndex, Vector3 spawnPos, Quaternion spawnRot, Vector3 forwardVec)
    {
        WeaponSO weapon = ItemDatabase.Instance.GetItem(weaponId) as WeaponSO;
        Attack attack = weapon.attacks[attackIndex];

        PlayerProjectile projObj = Instantiate(attack.projectile, spawnPos, spawnRot);
        projObj.GetComponent<NetworkObject>().Spawn();
        projObj.GetComponent<Rigidbody>().AddForce(forwardVec * attack.projectileForce, ForceMode.Impulse);
        projObj.Init(weapon.attacks[attackIndex]);
    }

    public void SetInventoryCameraActive(bool flag)
    {
        inventoryCameraTransform.gameObject.SetActive(flag);
    }
}