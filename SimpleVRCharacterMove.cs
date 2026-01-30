using UnityEngine;
using UnityEngine.InputSystem;
using Unity.XR.CoreUtils;
using JetBrains.Annotations;

[RequireComponent(typeof(CharacterController))]
public class SimpleVRCharacterMove : MonoBehaviour
{
    [Header("References")]
    public XROrigin xrOrigin;
    public Transform xrCamera; // HMD Camera Transform
    public CharacterController cc;

    [Header("Input Actions")]
    public InputActionProperty moveAction;   // Vector2 (Left Stick)
    public InputActionProperty turnAction;   // float or Vector2 X (optional)

    [Header("Tuning")]
    public float moveSpeed = 2.0f;           // m/s
    public float turnSpeedDegPerSec = 90f;   // deg/s
    public bool enableTurnAssist = true;

    void Reset()
    {
        xrOrigin = FindFirstObjectByType<XROrigin>();
        cc = GetComponent<CharacterController>();
        if (xrOrigin != null && xrOrigin.Camera != null)
            xrCamera = xrOrigin.Camera.transform;
    }

    void OnEnable()
    {
        moveAction.action?.Enable();
        turnAction.action?.Enable();
    }

    void OnDisable()
    {
        moveAction.action?.Disable();
        turnAction.action?.Disable();
    }

    void Update()
    {

        if (cc == null || !cc.enabled) return;
        if (xrCamera == null)
        {
            if (xrOrigin != null && xrOrigin.Camera != null) xrCamera = xrOrigin.Camera.transform;
            if (xrCamera == null) return;
        }

        // 1) 이동 (카메라 수평 방향 기준)
        Vector2 move = moveAction.action != null ? moveAction.action.ReadValue<Vector2>() : Vector2.zero;
        if (move.sqrMagnitude > 0.0001f)
        {
            Vector3 forward = Vector3.ProjectOnPlane(xrCamera.forward, Vector3.up).normalized;
            Vector3 right = Vector3.ProjectOnPlane(xrCamera.right, Vector3.up).normalized;

            Vector3 dir = forward * move.y + right * move.x;
            cc.Move(dir * (moveSpeed * Time.deltaTime));
        }

        // 2) 회전 보조 (원하면 오른쪽 스틱 X만 사용)
        if (enableTurnAssist && turnAction.action != null)
        {
            float yaw = 0f;

            // turnAction이 float이면 그대로 사용, Vector2면 x 사용
            if (turnAction.action.expectedControlType == "Vector2")
                yaw = turnAction.action.ReadValue<Vector2>().x;
            else
                yaw = turnAction.action.ReadValue<float>();

            if (Mathf.Abs(yaw) > 0.05f)
            {
                transform.Rotate(0f, yaw * turnSpeedDegPerSec * Time.deltaTime, 0f);
            }
        }
    }
}
