using UnityEngine;
using Unity.XR.CoreUtils;

public class XROriginViewSwitcher : MonoBehaviour
{
    [Header("XR References")]
    public XROrigin xrOrigin;
    public Transform xrCamera; // XROrigin 안의 Main Camera (HMD)

    [Header("Look-Up Return (Only in Viewing Mode)")]
    public bool enableLookUpReturn = true;
    public float lookUpAngleThreshold = 25f;
    public float lookUpHoldSeconds = 0.8f;

    [Header("State (read-only)")]
    [SerializeField] private bool isViewing = false;

    private float lookUpTimer = 0f;
    public Transform currentMainPoint = null;

    void Reset()
    {
        xrOrigin = FindFirstObjectByType<XROrigin>();
        if (xrOrigin != null && xrOrigin.Camera != null)
            xrCamera = xrOrigin.Camera.transform;
    }

    void Update()
    {
        if (!enableLookUpReturn || !isViewing) return;
        if (xrOrigin == null || xrCamera == null) return;
        if (currentMainPoint == null) return;

        bool lookingUp = Vector3.Angle(xrCamera.forward, Vector3.up) <= lookUpAngleThreshold;

        if (lookingUp)
        {
            lookUpTimer += Time.unscaledDeltaTime;
            if (lookUpTimer >= lookUpHoldSeconds)
            {
                ReturnToMain(currentMainPoint);
                lookUpTimer = 0f;
            }
        }
        else
        {
            lookUpTimer = 0f;
        }
    }

    // 관람/자유관람 포인트로 이동 (메인 포인트도 함께 주입)
    public void MoveToPoint(Transform target, Transform mainForReturn, bool viewingState)
    {
        if (xrOrigin == null || xrCamera == null || target == null) return;

        // ✅ 포즈를 복사해서 저장 (target이 나중에 움직여도 영향 최소화)
        Vector3 targetPos = target.position;
        Quaternion targetRot = target.rotation;

        currentMainPoint = mainForReturn;
        isViewing = viewingState;
        lookUpTimer = 0f;

        MoveOriginToPose(targetPos, targetRot);
    }

    private void MoveOriginToPose(Vector3 worldPos, Quaternion worldRot)
    {
        xrOrigin.MoveCameraToWorldLocation(worldPos);

        Vector3 currentForward = Vector3.ProjectOnPlane(xrCamera.forward, Vector3.up).normalized;
        Vector3 desiredForward = Vector3.ProjectOnPlane(worldRot * Vector3.forward, Vector3.up).normalized;

        if (currentForward.sqrMagnitude < 1e-6f || desiredForward.sqrMagnitude < 1e-6f) return;

        float deltaYaw = Vector3.SignedAngle(currentForward, desiredForward, Vector3.up);
        xrOrigin.RotateAroundCameraUsingOriginUp(deltaYaw);
    }


    // 버튼으로 복귀하거나, 관람 종료 처리할 때
    public void ReturnToMain(Transform main)
    {
        if (xrOrigin == null || xrCamera == null || main == null) return;

        currentMainPoint = main;
        isViewing = false;
        lookUpTimer = 0f;

        MoveOriginTo(main);
    }

    private void MoveOriginTo(Transform target)
    {
        // 1) 카메라가 target.position에 오도록 XROrigin 이동
        xrOrigin.MoveCameraToWorldLocation(target.position);

        // 2) 카메라 수평방향을 target 수평방향에 맞춰 yaw만 보정
        Vector3 currentForward = Vector3.ProjectOnPlane(xrCamera.forward, Vector3.up).normalized;
        Vector3 desiredForward = Vector3.ProjectOnPlane(target.forward, Vector3.up).normalized;

        if (currentForward.sqrMagnitude < 1e-6f || desiredForward.sqrMagnitude < 1e-6f)
            return;

        float deltaYaw = Vector3.SignedAngle(currentForward, desiredForward, Vector3.up);
        xrOrigin.RotateAroundCameraUsingOriginUp(deltaYaw);
    }
}
