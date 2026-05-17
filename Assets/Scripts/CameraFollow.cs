using UnityEngine;
using UnityEngine.InputSystem;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Position Settings")]
    public Vector3 offset = new Vector3(0f, 8f, -6f);
    public float followSpeed = 10f;

    [Header("Rotation Settings")]
    public float rotationSpeed = 0.3f;
    public float minVerticalAngle = -20f;
    public float maxVerticalAngle = 60f;

    [Header("Look Settings")]
    public float lookAtHeight = 0f;

    private float yaw = 0f;   // горизонталь
    private float pitch = 20f; // вертикаль (начальный угол сверху)

    void LateUpdate()
    {
        if (target == null) return;

        // Вращение только при зажатой ПКМ
        if (Mouse.current.rightButton.isPressed)
        {
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();
            yaw += mouseDelta.x * rotationSpeed;
            pitch -= mouseDelta.y * rotationSpeed;
            pitch = Mathf.Clamp(pitch, minVerticalAngle, maxVerticalAngle);
        }

        Vector3 lookTarget = target.position + Vector3.up * lookAtHeight;

        // Применяем углы поворота к offset
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 desiredPosition = lookTarget + rotation * offset;

        transform.position = Vector3.Lerp(
            transform.position,
            desiredPosition,
            followSpeed * Time.deltaTime
        );

        transform.LookAt(lookTarget);
    }

    public Vector3 Forward => new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;
    public Vector3 Right => new Vector3(transform.right.x, 0f, transform.right.z).normalized;
}