using UnityEngine;

public class PositionWatchdog : MonoBehaviour
{
    Vector3 last;

    void Start() => last = transform.position;

    void LateUpdate()
    {
        if ((transform.position - last).sqrMagnitude > 1e-8f)
            Debug.Log($"[WATCH] {name} moved -> {transform.position}");

        last = transform.position;
    }
}
