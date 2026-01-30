using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[DisallowMultipleComponent]
public class SwordGripSetupHelper : MonoBehaviour
{
    public XRGrabInteractable grab;
    public Rigidbody rb;

    [Header("Auto Rigidbody Defaults")]
    public float mass = 2f;

    void Reset()
    {
        grab = GetComponent<XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();
    }

    void Awake()
    {
        if (grab == null) grab = GetComponent<XRGrabInteractable>();
        if (rb == null) rb = GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.mass = mass;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }

        if (grab != null && grab.attachTransform == null)
        {
            Debug.LogWarning("[Sword] XRGrabInteractable.AttachTransform(GripPoint)가 비어있음. 손잡이 잡는 위치가 이상할 수 있어요.", this);
        }
    }
}
