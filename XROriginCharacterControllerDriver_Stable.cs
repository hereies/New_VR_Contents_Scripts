using UnityEngine;
using Unity.XR.CoreUtils;

[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterController))]
public class XROriginCharacterControllerDriver_Stable : MonoBehaviour
{
    [Header("References")]
    public XROrigin xrOrigin;
    public CharacterController cc;
    public Transform hmd; // xrOrigin.Camera.transform

    [Header("Capsule (recommended defaults)")]
    public float minHeight = 1.0f;
    public float maxHeight = 2.2f;
    public float radius = 0.25f;
    public float skinWidth = 0.10f;

    [Header("Centering")]
    public bool centerOnHmdXZ = true;
    public float centerOffsetY = -0.05f;     // 바닥에 살짝 여유
    public float centerMaxDistance = 0.25f;  // 너무 크면 벽에 박힘

    [Header("Stability")]
    public bool smoothCapsule = true;
    public float capsuleLerpSpeed = 20f;

    [Header("Overlap Fix (anti-stuck)")]
    public bool resolveOverlaps = true;
    public LayerMask collisionMask = ~0; // 기본: 전부
    public int maxResolveIterations = 8;
    public float resolveStepUp = 0.03f; // 한번에 올리는 높이 (m)

    void Reset()
    {
        xrOrigin = FindFirstObjectByType<XROrigin>();
        cc = GetComponent<CharacterController>();
        if (xrOrigin != null && xrOrigin.Camera != null)
            hmd = xrOrigin.Camera.transform;
    }

    void Awake()
    {
        if (xrOrigin == null) xrOrigin = FindFirstObjectByType<XROrigin>();
        if (cc == null) cc = GetComponent<CharacterController>();
        if (hmd == null && xrOrigin != null && xrOrigin.Camera != null) hmd = xrOrigin.Camera.transform;

        ApplyCapsuleImmediate();
    }

    void LateUpdate()
    {
        if (xrOrigin == null || cc == null || hmd == null) return;
        if (!cc.enabled) return;

        // 1) 목표 캡슐 계산
        float targetHeight = Mathf.Clamp(hmd.localPosition.y, minHeight, maxHeight);

        Vector3 targetCenter = cc.center;

        if (centerOnHmdXZ)
        {
            targetCenter.x = Mathf.Clamp(hmd.localPosition.x, -centerMaxDistance, centerMaxDistance);
            targetCenter.z = Mathf.Clamp(hmd.localPosition.z, -centerMaxDistance, centerMaxDistance);
        }

        targetCenter.y = (targetHeight * 0.5f) + centerOffsetY;

        // 2) 적용 (스무딩 옵션)
        if (smoothCapsule)
        {
            cc.height = Mathf.Lerp(cc.height, targetHeight, Time.deltaTime * capsuleLerpSpeed);
            cc.center = Vector3.Lerp(cc.center, targetCenter, Time.deltaTime * capsuleLerpSpeed);
        }
        else
        {
            cc.height = targetHeight;
            cc.center = targetCenter;
        }

        cc.radius = radius;
        cc.skinWidth = skinWidth;

        // 3) 겹침 상태면 위로 살짝 들어올려서 stuck 방지
        if (resolveOverlaps)
            ResolveOverlapByDepenetration();
    }

    void ApplyCapsuleImmediate()
    {
        if (cc == null) return;
        cc.radius = radius;
        cc.skinWidth = skinWidth;
    }

    void ResolveOverlapByDepenetration()
    {
        if (!IsOverlappingCapsule(out Collider first)) return;

        for (int i = 0; i < maxResolveIterations; i++)
        {
            GetCapsuleWorld(out var p1, out var p2, out var r);
            float queryR = Mathf.Max(0.01f, r - cc.skinWidth - 0.02f);

            var cols = Physics.OverlapCapsule(p1, p2, queryR, collisionMask, QueryTriggerInteraction.Ignore);
            Vector3 total = Vector3.zero;
            int count = 0;

            foreach (var c in cols)
            {
                if (c == null) continue;
                if (c.transform == transform) continue;
                if (c.transform.IsChildOf(transform)) continue;

                // 침투 벡터 계산
                if (Physics.ComputePenetration(
                        cc, transform.position, transform.rotation,
                        c, c.transform.position, c.transform.rotation,
                        out Vector3 dir, out float dist))
                {
                    total += dir * (dist + 0.001f);
                    count++;
                }
            }

            if (count == 0) return;

            // 필요한 만큼만 밀어냄 (위로만 올리지 않음)
            transform.position += total;

            // 탈출했는지 체크
            if (!IsOverlappingCapsule(out _)) return;
        }
    }


    bool IsOverlappingCapsule(out Collider hit)
    {
        hit = null;

        GetCapsuleWorld(out Vector3 p1, out Vector3 p2, out float r);

        // 약간 줄여서(스킨보다) 과민 반응 방지
        float queryR = Mathf.Max(0.01f, r - cc.skinWidth - 0.02f);

        var cols = Physics.OverlapCapsule(p1, p2, queryR, collisionMask, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < cols.Length; i++)
        {
            var c = cols[i];
            if (c == null) continue;

            // 자기 자신/자식 충돌 제외
            if (c.transform == transform) continue;
            if (c.transform.IsChildOf(transform)) continue;

            hit = c;
            return true;
        }
        return false;
    }

    void GetCapsuleWorld(out Vector3 p1, out Vector3 p2, out float r)
    {
        // Unity CharacterController 캡슐을 월드 캡슐로 근사
        Vector3 centerWorld = transform.TransformPoint(cc.center);
        r = cc.radius;

        float half = Mathf.Max(0f, (cc.height * 0.5f) - r);
        Vector3 up = transform.up;

        p1 = centerWorld + up * half;
        p2 = centerWorld - up * half;
    }
}
