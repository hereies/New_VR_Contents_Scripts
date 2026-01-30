using UnityEngine;
using UnityEngine.InputSystem;
using Unity.XR.CoreUtils;

[DisallowMultipleComponent]
public class XRCCLocomotion : MonoBehaviour
{
    [Header("References")]
    public XROrigin xrOrigin;
    public Transform hmd;
    public CharacterController cc;

    [Header("Move Settings")]
    public float normalMoveSpeed = 1.7f;
    public float freeRoamMoveSpeed = 3.0f;
    public bool useHmdDirection = true;
    public bool constrainToXZInNormal = true;

    [Header("Gravity (Normal)")]
    public bool useGravityInNormal = true;
    public float gravity = -20f;
    private float yVel;

    [Header("FreeRoam Vertical Keys")]
    public KeyCode upKey = KeyCode.X;
    public KeyCode downKey = KeyCode.Y;
    public float freeRoamVerticalSpeed = 2.0f;

    [Header("XR Input (OpenXR)")]
    public bool enableXRThumbstick = true;
    private InputAction moveAction;

    [Header("Keyboard Fallback")]
    public bool enableWASD = true;

    public enum MoveMode { Disabled, Normal, FreeRoam }
    [SerializeField] private MoveMode mode = MoveMode.Normal;

    public void SetModeNormal() { mode = MoveMode.Normal; }
    public void SetModeFreeRoam() { mode = MoveMode.FreeRoam; }
    public void SetModeDisabled() { mode = MoveMode.Disabled; }

    void Reset()
    {
        xrOrigin = FindFirstObjectByType<XROrigin>();
        cc = GetComponent<CharacterController>();
        if (xrOrigin != null && xrOrigin.Camera != null) hmd = xrOrigin.Camera.transform;
    }

    void Awake()
    {
        if (xrOrigin == null) xrOrigin = FindFirstObjectByType<XROrigin>();
        if (cc == null) cc = GetComponent<CharacterController>();
        if (hmd == null && xrOrigin != null && xrOrigin.Camera != null) hmd = xrOrigin.Camera.transform;

        // OpenXR Left Stick Vector2
        moveAction = new InputAction(
            name: "Move",
            type: InputActionType.Value,
            binding: "<XRController>{LeftHand}/thumbstick"
        );
    }

    void OnEnable()
    {
        if (enableXRThumbstick) moveAction?.Enable();
    }

    void OnDisable()
    {
        moveAction?.Disable();
    }

    void Update()
    {
        if (cc == null || !cc.enabled) return;
        if (mode == MoveMode.Disabled) return;

        // 1) Input (XR stick + WASD)
        Vector2 stick = Vector2.zero;
        if (enableXRThumbstick && moveAction != null) stick = moveAction.ReadValue<Vector2>();

        Vector2 wasd = Vector2.zero;
        if (enableWASD)
        {
            wasd.x = (Input.GetKey(KeyCode.D) ? 1f : 0f) - (Input.GetKey(KeyCode.A) ? 1f : 0f);
            wasd.y = (Input.GetKey(KeyCode.W) ? 1f : 0f) - (Input.GetKey(KeyCode.S) ? 1f : 0f);
        }

        Vector2 input = stick.sqrMagnitude >= wasd.sqrMagnitude ? stick : wasd;

        // 2) Direction
        Vector3 dir = new Vector3(input.x, 0f, input.y);

        Transform basis = (useHmdDirection && hmd != null) ? hmd : transform;
        dir = basis.TransformDirection(dir);

        float speed = (mode == MoveMode.FreeRoam) ? freeRoamMoveSpeed : normalMoveSpeed;

        // Normal에서는 수평 이동 고정 권장
        if (mode == MoveMode.Normal && constrainToXZInNormal)
            dir = Vector3.ProjectOnPlane(dir, Vector3.up).normalized * dir.magnitude;

        Vector3 delta = dir * (speed * Time.deltaTime);

        // 3) Vertical (Normal gravity vs FreeRoam keys)
        if (mode == MoveMode.Normal && useGravityInNormal)
        {
            if (cc.isGrounded && yVel < 0f) yVel = 0f;
            yVel += gravity * Time.deltaTime;
            delta.y = yVel * Time.deltaTime;
        }
        else if (mode == MoveMode.FreeRoam)
        {
            float v = 0f;
            if (Input.GetKey(upKey)) v += 1f;
            if (Input.GetKey(downKey)) v -= 1f;
            delta.y += v * freeRoamVerticalSpeed * Time.deltaTime;
            yVel = 0f; // FreeRoam에서는 중력 누적 금지
        }

        cc.Move(delta);
    }
}
