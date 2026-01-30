using UnityEngine;

[DisallowMultipleComponent]
public class SwordSwingVFXSpawner_Dual : MonoBehaviour
{
    [Header("Refs")]
    public SwordSwingDetector detector;
    public SwordChargeController charge;

    [Header("VFX Prefabs (root objects)")]
    public GameObject normalVfxPrefab;   // Tip 기준
    public GameObject chargedVfxPrefab;  // Player 기준(고정형)

    [Header("Spawn Points")]
    public Transform tipSpawnPoint;      // 보통 Tip
    public Transform playerCenter;       // 보통 XROrigin 또는 Main Camera

    [Header("Charged Spawn Mode")]
    public bool chargedFollowPlayer = false; // false=월드 고정(A안), true=플레이어 따라옴(B안)
    public bool chargedUsePlayerYawOnly = true; // true면 카메라 pitch/roll 무시(편안함)
    public Vector3 chargedOffset = new Vector3(0f, 0f, 0.6f); // 플레이어 앞쪽으로 살짝

    [Header("Lifetime")]
    public float fallbackLifetime = 2.0f;

    void Reset()
    {
        detector = GetComponent<SwordSwingDetector>();
        charge = GetComponent<SwordChargeController>();
    }

    void OnEnable()
    {
        if (detector == null) detector = GetComponent<SwordSwingDetector>();
        if (charge == null) charge = GetComponent<SwordChargeController>();
        if (detector != null) detector.Swing += OnSwing;
    }

    void OnDisable()
    {
        if (detector != null) detector.Swing -= OnSwing;
    }

    void OnSwing(float speed)
    {
        bool isCharged = (charge != null && charge.IsCharged);

        if (!isCharged)
            SpawnAtTip();
        else
            SpawnAtPlayer();
    }

    void SpawnAtTip()
    {
        if (normalVfxPrefab == null) return;
        Transform sp = tipSpawnPoint != null ? tipSpawnPoint : transform;

        GameObject vfx = Instantiate(normalVfxPrefab, sp.position, sp.rotation);
        PlayAll(vfx);
        Destroy(vfx, EstimateLifetime(vfx));
    }

    void SpawnAtPlayer()
    {
        if (chargedVfxPrefab == null) return;
        if (playerCenter == null) playerCenter = Camera.main != null ? Camera.main.transform : null;
        if (playerCenter == null) return;

        Vector3 pos = playerCenter.TransformPoint(chargedOffset);

        Quaternion rot;
        if (chargedUsePlayerYawOnly)
        {
            Vector3 fwd = Vector3.ProjectOnPlane(playerCenter.forward, Vector3.up).normalized;
            if (fwd.sqrMagnitude < 1e-6f) fwd = Vector3.forward;
            rot = Quaternion.LookRotation(fwd, Vector3.up);
        }
        else
        {
            rot = playerCenter.rotation;
        }

        GameObject vfx = Instantiate(chargedVfxPrefab, pos, rot);

        if (chargedFollowPlayer)
            vfx.transform.SetParent(playerCenter, worldPositionStays: true);

        PlayAll(vfx);
        Destroy(vfx, EstimateLifetime(vfx));
    }

    void PlayAll(GameObject root)
    {
        var systems = root.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var ps in systems) ps.Play(true);
    }

    float EstimateLifetime(GameObject root)
    {
        var systems = root.GetComponentsInChildren<ParticleSystem>(true);
        if (systems == null || systems.Length == 0) return fallbackLifetime;

        float max = 0f;
        foreach (var ps in systems)
        {
            var main = ps.main;
            if (main.loop) return fallbackLifetime;

            float startLifetimeMax = 0f;
            var sl = main.startLifetime;
            if (sl.mode == ParticleSystemCurveMode.Constant) startLifetimeMax = sl.constant;
            else if (sl.mode == ParticleSystemCurveMode.TwoConstants) startLifetimeMax = sl.constantMax;
            else startLifetimeMax = sl.constantMax;

            float t = main.duration + startLifetimeMax;
            if (t > max) max = t;
        }
        return Mathf.Max(0.1f, max + 0.1f);
    }
}
