using UnityEngine;

public class HeadReturnUIFollower : MonoBehaviour
{
    [Header("Follow Target (HMD)")]
    public Transform head; // Main Camera transform

    [Header("Offset (head local space)")]
    public Vector3 localOffset = new Vector3(0f, 0.25f, 0.5f); // 위 + 앞

    [Header("Facing")]
    public bool faceHead = true;
    public bool yawOnly = true;

    void Reset()
    {
        var cam = Camera.main;
        if (cam != null) head = cam.transform;
    }

    void LateUpdate()
    {
        if (head == null) return;

        // 위치: 머리 기준 로컬 오프셋(위/앞)
        transform.position = head.TransformPoint(localOffset);

        if (!faceHead) return;

        // 항상 머리(카메라)를 바라보게
        Vector3 toHead = head.position - transform.position;
        if (yawOnly)
        {
            toHead = Vector3.ProjectOnPlane(toHead, Vector3.up);
            if (toHead.sqrMagnitude < 1e-6f) return;
        }

        transform.rotation = Quaternion.LookRotation(toHead.normalized, Vector3.up);
    }
}
