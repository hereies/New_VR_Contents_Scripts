using UnityEngine;
using UnityEngine.InputSystem;

public class UpDownMove : MonoBehaviour
{
    [Header("Move Target")]
    public CharacterController characterController; // XROrigin에 보통 있음
    public Transform moveTransform;                  // CC 없을 때 fallback

    [Header("Input")]
    public InputActionProperty upDown;               // UpDown 액션(조이스틱Y/QE 등)

    [Header("Tuning")]
    public float speed = 2.0f;       // m/s
    public float deadzone = 0.15f;   // 스틱 드리프트 방지

    void Reset()
    {
        characterController = GetComponent<CharacterController>();
        moveTransform = transform;
    }

    void OnEnable()
    {
        upDown.action.Enable();
    }

    void OnDisable()
    {
        upDown.action.Disable();
    }

    void Update()
    {
        if (upDown == null || upDown.action == null) return;

        float v = upDown.action.ReadValue<float>(); // -1~+1 (아래 - / 위 +)
        if (Mathf.Abs(v) < deadzone) return;

        Vector3 delta = Vector3.up * (v * speed * Time.deltaTime);

        if (characterController != null && characterController.enabled)
            characterController.Move(delta);
        else if (moveTransform != null)
            moveTransform.position += delta;
    }
}
