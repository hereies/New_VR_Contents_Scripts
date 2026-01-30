using UnityEngine;
using UnityEngine.InputSystem;

public class MoveInputProbe : MonoBehaviour
{
    public InputActionProperty move; // Vector2
    public float interval = 0.5f;
    float t;

    void OnEnable() => move.action?.Enable();
    void OnDisable() => move.action?.Disable();

    void Update()
    {
        t += Time.deltaTime;
        if (t < interval) return;
        t = 0f;

        Vector2 v = Vector2.zero;
        try { if (move.action != null) v = move.action.ReadValue<Vector2>(); } catch { }
        Debug.Log($"[MoveInputProbe] Move = {v}");
    }
}
