using UnityEngine;

public class OrbitCamera : MonoBehaviour
{
    public Transform target; // 전시물 중심 (바라볼 대상)
    public float sensitivity = 120.0f; // (도/초) 느낌으로 사용
    public float distance = 10.0f;

    // 회전 각도 제한 (바닥 뚫기 방지)
    public float minVerticalAngle = 10f;
    public float maxVerticalAngle = 80f;

    [Header("Look-Up Return (Auto Return To Main)")]
    public bool enableLookUpReturn = true;
    public CameraSwitcher switcher;   // CameraSwitcher 연결
    public float lookUpAngleThreshold = 25f; // forward와 Vector3.up의 각도가 이 값 이하이면 "위를 본다"
    public float lookUpHoldSeconds = 0.8f;   // 위를 이 시간 이상 유지하면 복귀
    public bool requireNoInputWhileHolding = true; // 드래그 중엔 카운트 안 하게

    float rotX;
    float rotY;
    float lookUpTimer = 0f;

    void Start()
    {
        Vector3 angles = transform.eulerAngles;
        rotX = angles.y;
        rotY = angles.x;
    }

    void OnEnable()
    {
        lookUpTimer = 0f;
    }

    void LateUpdate()
    {
        float dt = Time.unscaledDeltaTime;

        bool isInteracting = Input.GetMouseButton(0);

        // 회전
        if (isInteracting)
        {
            rotX += Input.GetAxis("Mouse X") * sensitivity * dt;
            rotY -= Input.GetAxis("Mouse Y") * sensitivity * dt;
            rotY = Mathf.Clamp(rotY, minVerticalAngle, maxVerticalAngle);
        }

        Quaternion rotation = Quaternion.Euler(rotY, rotX, 0);
        Vector3 position = rotation * new Vector3(0f, 0f, -distance) + (target ? target.position : Vector3.zero);

        transform.SetPositionAndRotation(position, rotation);

        // 1) "위를 보면 자동 복귀"
        if (enableLookUpReturn && switcher != null)
        {
            bool lookingUp = Vector3.Angle(transform.forward, Vector3.up) <= lookUpAngleThreshold;

            if (lookingUp && (!requireNoInputWhileHolding || !isInteracting))
            {
                lookUpTimer += dt;
                if (lookUpTimer >= lookUpHoldSeconds)
                {
                    switcher.ReturnToMain();
                }
            }
            else
            {
                lookUpTimer = 0f;
            }
        }
    }
}
