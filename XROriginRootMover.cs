using UnityEngine;
using UnityEngine.InputSystem;
using Unity.XR.CoreUtils;

public class XROriginRootMover : MonoBehaviour
{
    public XROrigin xrOrigin;
    public InputActionProperty moveAction;
    public float speed = 2.0f;

    Transform cam;
    Vector3 afterUpdatePos;

    void OnEnable()
    {
        moveAction.action?.Enable();
        if (xrOrigin != null && xrOrigin.Camera != null) cam = xrOrigin.Camera.transform;
    }

    void Update()
    {
        Debug.Log($"CAMERA WORLD POS = {Camera.main.transform.position}");

        if (xrOrigin == null) return;
        if (cam == null && xrOrigin.Camera != null) cam = xrOrigin.Camera.transform;
        if (cam == null) return;

        Vector2 move = moveAction.action != null ? moveAction.action.ReadValue<Vector2>() : Vector2.zero;

        Vector3 before = xrOrigin.transform.position;

        if (move.sqrMagnitude >= 1e-4f)
        {
            Vector3 fwd = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized;
            Vector3 right = Vector3.ProjectOnPlane(cam.right, Vector3.up).normalized;
            Vector3 dir = fwd * move.y + right * move.x;

            xrOrigin.transform.position = before + dir * (speed * Time.deltaTime);
        }

        afterUpdatePos = xrOrigin.transform.position;

        Debug.Log($"[RootMover] move={move} before={before} afterUpdate={afterUpdatePos}");
    }

    void LateUpdate()
    {
        Vector3 late = xrOrigin.transform.position;

        // Update에서 바뀌었는데 LateUpdate에서 다시 바뀌면 "덮어쓰기" 확정
        if ((late - afterUpdatePos).sqrMagnitude > 1e-8f)
            Debug.Log($"[RootMover] OVERRIDDEN late={late} (was {afterUpdatePos})");
    }
}
