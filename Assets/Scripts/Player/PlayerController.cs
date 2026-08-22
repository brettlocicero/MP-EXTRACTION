using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] Transform cameraTransform;
    public Camera camera;
    [SerializeField] Transform clientObjects;
    [SerializeField] GameObject serverObjects;
    [SerializeField] Animator modelAnimator;

    [Header("Movement")]
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float gravity = -30f;
    [SerializeField] float jumpHeight = 1.5f;

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
    [SerializeField] AudioClip jumpSound;
    [SerializeField] AudioClip landSound;
    [SerializeField] float footstepInterval = 0.45f;

    CharacterController controller;

    Vector2 moveInput;
    Vector2 lookInput;

    float verticalVelocity;
    float cameraPitch;

    float footstepTimer;

    bool wasGrounded;
    bool isJumping;

    Vector3 cameraDefaultPosition;
    float bobTimer;
    float currentTilt;
    float cachedSensitivity = 0;

    void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (cameraTransform != null)
            cameraDefaultPosition = cameraTransform.localPosition;
    }

    void Start()
    {
        cachedSensitivity = mouseSensitivity;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            controller.enabled = false;
            clientObjects.gameObject.SetActive(false);
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

        if (grounded && verticalVelocity < 0)
        {
            verticalVelocity = -2f;
        }

        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
        controller.Move(move * moveSpeed * Time.deltaTime);

        HandleJump(grounded);

        verticalVelocity += gravity * Time.deltaTime;
        controller.Move(Vector3.up * verticalVelocity * Time.deltaTime);

        HandleGroundSounds(grounded);

        modelAnimator.SetFloat("Movement", moveInput.magnitude, 0.1f, Time.deltaTime);

        wasGrounded = grounded;
    }

    void HandleJump(bool grounded)
    {
        if (!grounded)
            return;

        if (InputManager.Actions.Player.Jump.WasPressedThisFrame())
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);

            isJumping = true;

            if (jumpSound)
                audioSource.PlayOneShot(jumpSound);
        }
    }

    void HandleGroundSounds(bool grounded)
    {
        // Landing
        if (!wasGrounded && grounded)
        {
            isJumping = false;

            if (landSound)
                audioSource.PlayOneShot(landSound);
        }

        // Footsteps
        if (grounded && moveInput.magnitude > 0.1f)
        {
            footstepTimer -= Time.deltaTime;

            if (footstepTimer <= 0)
            {
                PlayFootstep();
                footstepTimer = footstepInterval;
            }
        }
        else
        {
            footstepTimer = 0;
        }
    }

    void PlayFootstep()
    {
        if (footstepSounds.Length == 0)
            return;

        AudioClip clip = footstepSounds[Random.Range(0, footstepSounds.Length)];

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
        if (mouseSensitivity > 0f)
            cachedSensitivity = mouseSensitivity;

        mouseSensitivity = 0f;
    }

    public void UnlockSensitivity()
    {
        mouseSensitivity = cachedSensitivity;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void LaunchProjectileRpc(int weaponId, int attackIndex, Vector3 spawnPos, Quaternion spawnRot, Vector3 forwardVec)
    {
        WeaponSO weapon = ItemDatabase.Instance.GetItem(weaponId) as WeaponSO;
        Attack attack = weapon.attacks[attackIndex];

        Rigidbody projObj = Instantiate(attack.projectile, spawnPos, spawnRot);
        projObj.GetComponent<NetworkObject>().Spawn();
        projObj.AddForce(forwardVec * attack.projectileForce, ForceMode.Impulse);
    }
}