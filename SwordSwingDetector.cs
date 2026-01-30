using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[DisallowMultipleComponent]
public class SwordSwingDetector : MonoBehaviour
{
    [Header("Refs")]
    public XRGrabInteractable grab;
    public Rigidbody rb;
    public Transform tip; // 칼끝

    [Header("Swing Thresholds")]
    public float swingSpeedThreshold = 3.0f; // m/s
    public float minHoldTime = 0.05f;        // seconds
    public float cooldown = 0.15f;           // seconds

    [Header("Debug")]
    public bool debugLog = true;

    public System.Action<float> Swing; // speed

    float holdTimer;
    float cooldownTimer;

    void Reset()
    {
        grab = GetComponent<XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();
    }

    void Awake()
    {
        if (grab == null) grab = GetComponent<XRGrabInteractable>();
        if (rb == null) rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (cooldownTimer > 0f) cooldownTimer -= Time.deltaTime;

        if (grab == null || rb == null || !grab.isSelected)
        {
            holdTimer = 0f;
            return;
        }

        float speed = GetTipSpeed();
        if (speed >= swingSpeedThreshold)
        {
            holdTimer += Time.deltaTime;
            if (holdTimer >= minHoldTime && cooldownTimer <= 0f)
            {
                holdTimer = 0f;
                cooldownTimer = cooldown;

                if (debugLog) Debug.Log($"[Swing] speed={speed:F2} m/s", this);
                Swing?.Invoke(speed);
            }
        }
        else
        {
            holdTimer = 0f;
        }
    }

    float GetTipSpeed()
    {
        // tip이 있으면 회전까지 포함된 칼끝 속도를 정확히 구함
        Vector3 p = tip != null ? tip.position : transform.position;
        return rb.GetPointVelocity(p).magnitude;
    }
}
