using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class FreeRoamXYVerticalMove : MonoBehaviour
{
    [Header("Bindings (OpenXR)")]
    // X = Left primaryButton, Y = Left secondaryButton
    public InputActionProperty xUpAction;   // Button (float)
    public InputActionProperty yDownAction; // Button (float)

    [Header("Tuning")]
    public float verticalSpeed = 1.5f;
    public float deadzone = 0.5f;

    private CharacterController cc;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
    }

    void OnEnable()
    {
        xUpAction.action?.Enable();
        yDownAction.action?.Enable();
    }

    void OnDisable()
    {
        xUpAction.action?.Disable();
        yDownAction.action?.Disable();
    }

    void Update()
    {
        if (cc == null || !cc.enabled) return;

        float x = xUpAction.action != null ? xUpAction.action.ReadValue<float>() : 0f;
        float y = yDownAction.action != null ? yDownAction.action.ReadValue<float>() : 0f;

        float up = (x > deadzone) ? 1f : 0f;
        float down = (y > deadzone) ? 1f : 0f;

        float v = up - down;
        if (Mathf.Abs(v) < 0.01f) return;

        cc.Move(Vector3.up * (v * verticalSpeed * Time.deltaTime));
    }
}
