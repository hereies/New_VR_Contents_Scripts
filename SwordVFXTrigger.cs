using UnityEngine;

[DisallowMultipleComponent]
public class SwordVFXTrigger : MonoBehaviour
{
    [Header("Refs")]
    public SwordSwingDetector detector;

    [Header("Optional VFX (¾ø¾îµµ µÊ)")]
    public ParticleSystem swingParticle; // Ä® ÁÖº¯/Ä®³¡¿¡ ºÙ¿©µµ µÊ
    public AudioSource audioSource;
    public AudioClip swingSfx;

    [Header("Tuning")]
    public bool scaleWithSpeed = false;
    public float maxSpeedForScale = 8f;
    public float minParticleScale = 0.8f;
    public float maxParticleScale = 1.5f;

    void Reset()
    {
        detector = GetComponent<SwordSwingDetector>();
    }

    void OnEnable()
    {
        if (detector == null) detector = GetComponent<SwordSwingDetector>();
        if (detector != null) detector.Swing += OnSwing;
    }

    void OnDisable()
    {
        if (detector != null) detector.Swing -= OnSwing;
    }

    void OnSwing(float speed)
    {
        // VFX
        if (swingParticle != null)
        {
            if (scaleWithSpeed)
            {
                float t = Mathf.Clamp01(speed / Mathf.Max(0.001f, maxSpeedForScale));
                float s = Mathf.Lerp(minParticleScale, maxParticleScale, t);
                swingParticle.transform.localScale = Vector3.one * s;
            }
            swingParticle.Play(true);
        }

        // SFX
        if (audioSource != null && swingSfx != null)
            audioSource.PlayOneShot(swingSfx);
    }
}
