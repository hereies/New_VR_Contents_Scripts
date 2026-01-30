using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

public class XRIInputProbe : MonoBehaviour
{
    [Header("Pose Actions (Vector3/Quaternion)")]
    public InputActionProperty leftPosition;   // XRI LeftHand/Position
    public InputActionProperty leftRotation;   // XRI LeftHand/Rotation
    public InputActionProperty rightPosition;  // XRI RightHand/Position
    public InputActionProperty rightRotation;  // XRI RightHand/Rotation

    [Header("Input Actions (Vector2/float)")]
    public InputActionProperty leftMove;       // XRI LeftHand Locomotion/Move
    public InputActionProperty rightMove;      // 보통 없고 Left만 Move일 때가 많음(프로젝트 설정 따라 다름)
    public InputActionProperty leftSelectValue;// XRI LeftHand Interaction/Select Value
    public InputActionProperty leftActivateValue;// XRI LeftHand Interaction/Activate Value
    public InputActionProperty rightSelectValue;
    public InputActionProperty rightActivateValue;

    public float logInterval = 1.0f;
    float t;

    void OnEnable()
    {
        EnableAll(true);
    }

    void OnDisable()
    {
        EnableAll(false);
    }

    void EnableAll(bool on)
    {
        void Set(InputActionProperty p)
        {
            if (p.action == null) return;
            if (on) p.action.Enable();
            else p.action.Disable();
        }

        Set(leftPosition); Set(leftRotation);
        Set(rightPosition); Set(rightRotation);
        Set(leftMove); Set(rightMove);
        Set(leftSelectValue); Set(leftActivateValue);
        Set(rightSelectValue); Set(rightActivateValue);
    }

    void Update()
    {
        t += Time.deltaTime;
        if (t < logInterval) return;
        t = 0f;

        Vector3 lp = SafeReadVec3(leftPosition);
        Quaternion lr = SafeReadQuat(leftRotation);
        Vector3 rp = SafeReadVec3(rightPosition);
        Quaternion rr = SafeReadQuat(rightRotation);

        Vector2 lm = SafeReadVec2(leftMove);
        float lsv = SafeReadFloat(leftSelectValue);
        float lav = SafeReadFloat(leftActivateValue);
        float rsv = SafeReadFloat(rightSelectValue);
        float rav = SafeReadFloat(rightActivateValue);

        Debug.Log(
            $"[XRIProbe] Lpos:{lp} LrotY:{lr.eulerAngles.y:F1} | Rpos:{rp} RrotY:{rr.eulerAngles.y:F1} | " +
            $"Move:{lm} | LSel:{lsv:F2} LAct:{lav:F2} | RSel:{rsv:F2} RAct:{rav:F2}"
        );
    }

    Vector3 SafeReadVec3(InputActionProperty p) { try { return p.action?.ReadValue<Vector3>() ?? Vector3.zero; } catch { return Vector3.zero; } }
    Quaternion SafeReadQuat(InputActionProperty p) { try { return p.action?.ReadValue<Quaternion>() ?? Quaternion.identity; } catch { return Quaternion.identity; } }
    Vector2 SafeReadVec2(InputActionProperty p) { try { return p.action?.ReadValue<Vector2>() ?? Vector2.zero; } catch { return Vector2.zero; } }
    float SafeReadFloat(InputActionProperty p) { try { return p.action?.ReadValue<float>() ?? 0f; } catch { return 0f; } }
}
