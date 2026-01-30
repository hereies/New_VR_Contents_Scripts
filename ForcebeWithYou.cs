using UnityEngine;
using UnityEngine.InputSystem;
using Unity.XR.CoreUtils;

public class ForcebeWithYou : MonoBehaviour
{
    public XROrigin origin;
    public InputActionProperty move; // Vector2
    public float speed = 2f;

    void OnEnable() => move.action?.Enable();
    void OnDisable() => move.action?.Disable();

    void Update()
    {
        if (origin == null || origin.Camera == null) return;

        var cam = origin.Camera.transform;
        Vector2 v = move.action.ReadValue<Vector2>();
        if (v.sqrMagnitude < 1e-4f) return;

        Vector3 fwd = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized;
        Vector3 right = Vector3.ProjectOnPlane(cam.right, Vector3.up).normalized;
        Vector3 delta = (fwd * v.y + right * v.x) * (speed * Time.deltaTime);

        // 카메라가 "가야 할 월드 위치"로 강제 이동
        origin.MoveCameraToWorldLocation(cam.position + delta);
    }
}
