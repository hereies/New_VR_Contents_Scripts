using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

public class KeyboardVerticalMove : MonoBehaviour
{
    [Header("References")]
    public CharacterController characterController; // 있으면 이걸로 이동(권장)
    public Transform moveTransform;                  // 없으면 이 Transform을 직접 이동 (보통 XROrigin)

    [Header("Input")]
    public InputActionProperty upDownAction;         // FlyUpDown 액션 연결

    [Header("Settings")]
    public float verticalSpeed = 2.0f;               // m/s
    public bool useUnscaledTime = false;             // UI/시간정지 고려하면 true도 가능

    void Reset()
    {
        characterController = GetComponent<CharacterController>();
        moveTransform = transform;
    }

    void OnEnable()
    {
        if (upDownAction != null && upDownAction.action != null)
            upDownAction.action.Enable();
    }

    void OnDisable()
    {
        if (upDownAction != null && upDownAction.action != null)
            upDownAction.action.Disable();
    }

    void Update()
    {
        if (upDownAction == null || upDownAction.action == null) return;

        float v = upDownAction.action.ReadValue<float>(); // -1~+1
        if (Mathf.Abs(v) < 0.001f) return;

        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        Vector3 delta = Vector3.up * (v * verticalSpeed * dt);

        // CharacterController가 있으면 Move로(충돌/스텝/슬로프 등 안정적)
        if (characterController != null && characterController.enabled)
        {
            characterController.Move(delta);
        }
        else if (moveTransform != null)
        {
            moveTransform.position += delta;
        }
    }
}
