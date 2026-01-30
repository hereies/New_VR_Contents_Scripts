using UnityEngine;
using Unity.XR.CoreUtils;

[DisallowMultipleComponent]
public class XROriginCharacterControllerDriver : MonoBehaviour
{
    [Header("References")]
    public XROrigin xrOrigin;
    public CharacterController cc;
    public Transform hmd; // xrOrigin.Camera.transform

    [Header("Capsule")]
    public float minHeight = 1.0f;
    public float maxHeight = 2.2f;
    public float radius = 0.2f;
    public float skinWidth = 0.02f;

    [Header("Centering")]
    public bool centerOnHmdXZ = true;
    public float centerOffsetY = 0.0f;
    public float centerMaxDistance = 0.5f;

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

        if (cc != null)
        {
            cc.radius = radius;
            cc.skinWidth = skinWidth;
        }
    }

    void LateUpdate()
    {
        if (xrOrigin == null || cc == null || hmd == null) return;
        if (!cc.enabled) return;

        float targetHeight = Mathf.Clamp(hmd.localPosition.y, minHeight, maxHeight);
        cc.height = targetHeight;

        Vector3 center = cc.center;

        if (centerOnHmdXZ)
        {
            float x = Mathf.Clamp(hmd.localPosition.x, -centerMaxDistance, centerMaxDistance);
            float z = Mathf.Clamp(hmd.localPosition.z, -centerMaxDistance, centerMaxDistance);
            center.x = x;
            center.z = z;
        }

        center.y = (cc.height * 0.5f) + centerOffsetY;
        cc.center = center;
    }
}
