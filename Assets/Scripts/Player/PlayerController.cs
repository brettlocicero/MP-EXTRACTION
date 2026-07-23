using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] Transform cameraTransform;
    public Camera camera;
    [SerializeField] Transform clientObjects;
    [SerializeField] Transform serverObjects;

    [Header("Movement")]
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float gravity = -20f;
    [SerializeField] float jumpHeight = 1.5f;

    [Header("Look")]
    [SerializeField] float mouseSensitivity = 0.15f;

    CharacterController controller;

    Vector2 moveInput;
    Vector2 lookInput;

    float verticalVelocity;
    float cameraPitch;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            controller.enabled = false;
            clientObjects.gameObject.SetActive(false);
            serverObjects.gameObject.SetActive(true);
            return;
        }

        clientObjects.gameObject.SetActive(true);
        serverObjects.gameObject.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (!IsOwner)
            return;

        ReadInput();
        Look();
        Move();
    }

    void ReadInput()
    {
        moveInput = InputManager.Actions.Player.Move.ReadValue<Vector2>();
        lookInput = InputManager.Actions.Player.Look.ReadValue<Vector2>();
    }

    void Move()
    {
        if (controller.isGrounded && verticalVelocity < 0)
        {
            verticalVelocity = -2f;
        }

        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
        controller.Move(move * moveSpeed * Time.deltaTime);

        if (InputManager.Actions.Player.Jump.WasPressedThisFrame() && controller.isGrounded)
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        verticalVelocity += gravity * Time.deltaTime;
        controller.Move(Time.deltaTime * verticalVelocity * Vector3.up);
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
}