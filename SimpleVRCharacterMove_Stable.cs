using UnityEngine;
using UnityEngine.InputSystem;
using Unity.XR.CoreUtils;

[RequireComponent(typeof(CharacterController))]
public class SimpleVRCharacterMove_Stable : MonoBehaviour
{
    [Header("References")]
    public XROrigin xrOrigin;
    public Transform xrCamera;
    public CharacterController cc;

    [Header("Input Actions")]
    public InputActionProperty moveAction;   // Vector2 (Left Stick)
    public InputActionProperty turnAction;   // float or Vector2.x (Right Stick X)

    [Header("Move Tuning")]
    public float moveSpeed = 2.0f;           // m/s
    public float turnSpeedDegPerSec = 90f;
    public float deadzone = 0.15f;
    public bool moveRelativeToHeadYawOnly = true;
    public bool enableTurnAssist = true;

    [Header("Gravity")]
    public bool useGravity = true;
    public float gravity = -9.81f;           // m/s^2
    public float groundedStick = -1.0f;      // 바닥 붙임(작은 음수 유지)
    public float terminalVelocity = -25f;

    [Header("Ground Check (recommended ON in VR)")]
    public bool useSphereGroundCheck = true;
    public Transform groundCheck;            // 비우면 cc 바닥으로 자동
    public float groundCheckRadius = 0.2f;
    public float groundCheckDistance = 0.15f;
    public LayerMask groundMask = ~0;

    [Header("Debug")]
    public bool logGrounded = false;
    public float logInterval = 0.3f;

    float yVelocity;
    float logTimer;

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

        // ===== 0) Grounded 판단 =====
        bool grounded = IsGrounded();

        // ===== 1) 수평 이동 방향(dir) =====
        Vector2 move = (moveAction.action != null) ? moveAction.action.ReadValue<Vector2>() : Vector2.zero;
        if (move.magnitude < deadzone) move = Vector2.zero;

        Vector3 dir = Vector3.zero;
        if (move != Vector2.zero)
        {
            Vector3 forward, right;

            if (moveRelativeToHeadYawOnly)
            {
                Vector3 fwd = Vector3.ProjectOnPlane(xrCamera.forward, Vector3.up);
                if (fwd.sqrMagnitude < 1e-6f) fwd = transform.forward;
                forward = fwd.normalized;
                right = Vector3.Cross(Vector3.up, forward).normalized;
            }
            else
            {
                forward = Vector3.ProjectOnPlane(xrCamera.forward, Vector3.up).normalized;
                right = Vector3.ProjectOnPlane(xrCamera.right, Vector3.up).normalized;
            }

            dir = (forward * move.y + right * move.x);
        }

        // ===== 2) 중력(yVelocity) =====
        if (useGravity)
        {
            if (grounded && yVelocity < 0f)
                yVelocity = groundedStick; // 바닥에 붙여주기
            else
            {
                yVelocity += gravity * Time.deltaTime;
                if (yVelocity < terminalVelocity) yVelocity = terminalVelocity;
            }
        }
        else
        {
            // 중력 OFF면 수직 속도 0으로
            yVelocity = 0f;
        }

        // ===== 3) 최종 Move =====
        Vector3 motion = dir * moveSpeed;
        motion.y = yVelocity;

        cc.Move(motion * Time.deltaTime);

        // ===== 4) 회전 보조 =====
        if (enableTurnAssist && turnAction.action != null)
        {
            float yaw = (turnAction.action.expectedControlType == "Vector2")
                ? turnAction.action.ReadValue<Vector2>().x
                : turnAction.action.ReadValue<float>();

            if (Mathf.Abs(yaw) > 0.15f)
                transform.Rotate(0f, yaw * turnSpeedDegPerSec * Time.deltaTime, 0f);
        }

        // ===== 5) 디버그 =====
        if (logGrounded)
        {
            logTimer -= Time.deltaTime;
            if (logTimer <= 0f)
            {
                logTimer = logInterval;
                Debug.Log($"[Ground] grounded={grounded} yVel={yVelocity:F2} cc.isGrounded={cc.isGrounded} pos={transform.position}");
            }
        }
    }

    bool IsGrounded()
    {
        if (!useSphereGroundCheck)
            return cc.isGrounded;

        // groundCheck 없으면 CC 바닥 근처로 자동 계산
        Vector3 origin;
        if (groundCheck != null) origin = groundCheck.position;
        else
        {
            // CC 바닥 중앙 근처
            origin = transform.TransformPoint(cc.center);
            origin.y = transform.position.y + cc.center.y - (cc.height * 0.5f) + cc.radius + 0.02f;
        }

        return Physics.SphereCast(
            origin,
            groundCheckRadius,
            Vector3.down,
            out _,
            groundCheckDistance,
            groundMask,
            QueryTriggerInteraction.Ignore
        );
    }
}
