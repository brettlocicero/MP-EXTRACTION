using UnityEngine;

public class Sway : MonoBehaviour 
{
    [SerializeField] float amount = 0.02f;
    [SerializeField] float maxAmount = 0.03f;
    [SerializeField] float smooth = 3f;
    [SerializeField] float walkTiltScale = 10f;

    Quaternion def;

    void Start()
    {
        def = transform.localRotation;
    }

    void Update()
    {
        Vector2 lookInput = InputManager.Actions.Player.Look.ReadValue<Vector2>() / 10f;
        Vector2 moveInput = InputManager.Actions.Player.Move.ReadValue<Vector2>();

        float factorX = Mathf.Clamp(lookInput.y * amount, -maxAmount, maxAmount);
        float factorY = Mathf.Clamp(-lookInput.x * amount, -maxAmount, maxAmount);
        float factorZ = 0f;

        Vector3 tilt = new(moveInput.y / 2f, 0f, -moveInput.x * walkTiltScale);

        Vector3 targetEuler = def.eulerAngles + new Vector3(factorX + tilt.x, factorY + tilt.y, factorZ + tilt.z);
        Quaternion targetRotation = Quaternion.Euler(targetEuler);

        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, Time.deltaTime * smooth);
    }
}
