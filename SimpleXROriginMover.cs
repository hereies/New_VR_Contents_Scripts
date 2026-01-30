using UnityEngine;
using UnityEngine.InputSystem;
using Unity.XR.CoreUtils;

[DisallowMultipleComponent]
public class SimpleXROriginMover : MonoBehaviour
{
    [Header("References")]
    public XROrigin xrOrigin;
    public Transform viewCamera;              // HMD 카메라(메인카메라)
    public CharacterController cc;

    [Header("Move")]
    public float speed = 2.0f;                // 수평 이동 속도
    public bool useCameraDirection = true;    // 카메라 기준 이동
    public bool constrainToXZ = true;         // 수평면 이동(걷기)

    [Header("Gravity / Jump (optional)")]
    public bool useGravity = true;
    public float gravity = -20f;
    public float jumpPower = 5f;
    private float yVelocity = 0f;

    [Header("Vertical Keys (optional)")]
    public bool allowVerticalKeys = true;
    public KeyCode upKey = KeyCode.X;         // 상승
    public KeyCode downKey = KeyCode.Y;       // 하강
    public float verticalSpeed = 2.0f;

    [Header("Inputs (OpenXR)")]
    public bool enableXRThumbstick = true;    // Quest 왼손 스틱
    private InputAction moveAction;           // Vector2

    [Header("Debug")]
    public bool logInput = false;

    void Reset()
    {
        xrOrigin = FindFirstObjectByType<XROrigin>();
        if (xrOrigin != null && xrOrigin.Camera != null)
            viewCamera = xrOrigin.Camera.transform;

        cc = GetComponent<CharacterController>();
    }

    void Awake()
    {
        if (xrOrigin == null) xrOrigin = FindFirstObjectByType<XROrigin>();
        if (viewCamera == null)
        {
            if (xrOrigin != null && xrOrigin.Camera != null) viewCamera = xrOrigin.Camera.transform;
            else if (Camera.main != null) viewCamera = Camera.main.transform;
        }

        if (cc == null) cc = GetComponent<CharacterController>();

        // Quest/OpenXR 왼손 스틱(Vector2)
        moveAction = new InputAction(
            name: "Move",
            type: InputActionType.Value,
            binding: "<XRController>{LeftHand}/thumbstick"
        );
    }

    void OnEnable()
    {
        if (enableXRThumbstick && moveAction != null)
            moveAction.Enable();
    }

    void OnDisable()
    {
        if (moveAction != null)
            moveAction.Disable();
    }

    void Update()
    {
        if (cc == null) return;
        if (!cc.enabled) return;

        // 1) 입력 받기: XR 스틱 + 키보드 WASD(대체)
        Vector2 stick = Vector2.zero;
        if (enableXRThumbstick && moveAction != null)
            stick = moveAction.ReadValue<Vector2>();

        Vector2 wasd = new Vector2(
            (Input.GetKey(KeyCode.D) ? 1f : 0f) - (Input.GetKey(KeyCode.A) ? 1f : 0f),
            (Input.GetKey(KeyCode.W) ? 1f : 0f) - (Input.GetKey(KeyCode.S) ? 1f : 0f)
        );

        // 둘 중 더 큰 입력 사용(동시에 들어오면 우선순위)
        Vector2 input = stick.sqrMagnitude >= wasd.sqrMagnitude ? stick : wasd;

        // 2) 방향 만들기 (h,v -> Vector3)
        Vector3 dir = new Vector3(input.x, 0f, input.y);

        // 2.0 카메라 기준 방향으로 변환
        if (useCameraDirection && viewCamera != null)
        {
            dir = viewCamera.TransformDirection(dir);
        }

        // 2.1 수평면 고정(카메라 피치로 위/아래 섞이는 걸 방지)
        if (constrainToXZ)
        {
            dir = Vector3.ProjectOnPlane(dir, Vector3.up);
        }

        // 3) 중력/점프/상하키 처리
        float vertical = 0f;

        if (allowVerticalKeys)
        {
            if (Input.GetKey(upKey)) vertical += 1f;
            if (Input.GetKey(downKey)) vertical -= 1f;
        }

        // (A) 중력 사용 모드: 점프/중력은 yVelocity로
        if (useGravity)
        {
            yVelocity += gravity * Time.deltaTime;

            if (cc.isGrounded && yVelocity < 0f)
                yVelocity = 0f;

            // Space 점프(디버그). Quest 점프는 나중에 버튼 액션으로 연결 가능
            if (Input.GetKeyDown(KeyCode.Space) && cc.isGrounded)
                yVelocity = jumpPower;

            dir.y = yVelocity;
        }
        else
        {
            // (B) 무중력: X/Y 키로 상하 이동
            dir.y = vertical * verticalSpeed;
        }

        // 4) 이동
        Vector3 delta = dir * speed * Time.deltaTime;
        cc.Move(delta);

        if (logInput && input.sqrMagnitude > 0.01f)
            Debug.Log($"[XRPlayerMove] input={input} dir={dir} grounded={cc.isGrounded}");
    }
}
