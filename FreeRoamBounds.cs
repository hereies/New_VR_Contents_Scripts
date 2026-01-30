using UnityEngine;

public class FreeRoamBounds : MonoBehaviour
{
    public enum BoundShape { Box, Sphere }

    [Header("Bounds")]
    public BoundShape shape = BoundShape.Box;
    public Collider[] boundsColliders;
    public int activeAreaIndex = 0;

    [Tooltip("경계 계산용이므로 Trigger 권장. 켜질 때 강제로 Trigger로 바꿈")]
    public bool forceTriggerOnEnable = true;

    [Header("Y Lock")]
    public bool lockYToBounds = true;

    public void SetActiveArea(int index)
    {
        activeAreaIndex = index;
    }

    void OnEnable()
    {
        if (!forceTriggerOnEnable || boundsColliders == null) return;
        for (int i = 0; i < boundsColliders.Length; i++)
        {
            if (boundsColliders[i] != null)
                boundsColliders[i].isTrigger = true;
        }
    }

    void LateUpdate()
    {
        Collider c = GetActiveCollider();
        if (c == null) return;

        Vector3 pos = transform.position;

        if (shape == BoundShape.Box)
        {
            Bounds b = c.bounds;
            pos.x = Mathf.Clamp(pos.x, b.min.x, b.max.x);
            pos.z = Mathf.Clamp(pos.z, b.min.z, b.max.z);
            if (lockYToBounds) pos.y = Mathf.Clamp(pos.y, b.min.y, b.max.y);
        }
        else
        {
            Vector3 center = c.bounds.center;
            float r = Mathf.Max(c.bounds.extents.x, c.bounds.extents.y, c.bounds.extents.z);
            Vector3 v = pos - center;
            if (v.sqrMagnitude > r * r) pos = center + v.normalized * r;
            if (lockYToBounds) pos.y = Mathf.Clamp(pos.y, c.bounds.min.y, c.bounds.max.y);
        }

        transform.position = pos;
    }

    private Collider GetActiveCollider()
    {
        if (boundsColliders == null || boundsColliders.Length == 0) return null;
        if (activeAreaIndex < 0 || activeAreaIndex >= boundsColliders.Length) return null;
        return boundsColliders[activeAreaIndex];
    }
}
