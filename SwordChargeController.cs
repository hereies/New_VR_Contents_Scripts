using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

[DisallowMultipleComponent]
public class SwordChargeController : MonoBehaviour
{
    [Header("Refs")]
    public XRGrabInteractable grab;

    [Header("Trigger Value Actions (float 0..1)")]
    public InputActionProperty leftTriggerValue;   // 예: XRI LeftHand Interaction/Activate Value
    public InputActionProperty rightTriggerValue;  // 예: XRI RightHand Interaction/Activate Value

    [Header("Charge")]
    public float holdSeconds = 3.0f;
    [Range(0.1f, 0.95f)] public float pressThreshold = 0.6f;

    [Header("Tip Glow (Optional)")]
    public Light tipLight;
    public ParticleSystem tipGlowParticle;

    [Header("State (read-only)")]
    [SerializeField] private bool charged = false;
    public bool IsCharged => charged;

    private enum Hand { None, Left, Right }
    private Hand holdingHand = Hand.None;

    private float holdTimer = 0f;

    void Reset()
    {
        grab = GetComponent<XRGrabInteractable>();
    }

    void Awake()
    {
        if (grab == null) grab = GetComponent<XRGrabInteractable>();

        if (grab != null)
        {
            grab.selectEntered.AddListener(OnSelectEntered);
            grab.selectExited.AddListener(OnSelectExited);
        }

        SetGlow(false);
    }

    void OnEnable()
    {
        // 액션 Enable (InputActionProperty는 직접 Enable해야 안정적)
        leftTriggerValue.action?.Enable();
        rightTriggerValue.action?.Enable();
    }

    void OnDisable()
    {
        leftTriggerValue.action?.Disable();
        rightTriggerValue.action?.Disable();
    }

    void OnDestroy()
    {
        if (grab != null)
        {
            grab.selectEntered.RemoveListener(OnSelectEntered);
            grab.selectExited.RemoveListener(OnSelectExited);
        }
    }

    void Update()
{
    if (grab == null || !grab.isSelected) return;
    if (holdingHand == Hand.None) return;

    float trigger = ReadTriggerValue(holdingHand);
    bool pressed = trigger >= pressThreshold;

    if (!charged)
    {
        if (pressed)
        {
            holdTimer += Time.deltaTime;
            if (holdTimer >= holdSeconds)
            {
                charged = true;
                SetGlow(true);
            }
        }
        else
        {
            holdTimer = 0f;
        }
    }
    else
    {
        // 기본 정책: 트리거 떼면 해제 (원하면 유지 정책으로 바꿔줄 수 있음)
        if (!pressed)
        {
            charged = false;
            holdTimer = 0f;
            SetGlow(false);
        }
    }
}

void OnSelectEntered(SelectEnterEventArgs args)
{
    holdingHand = DetectHand(args.interactorObject);
    holdTimer = 0f;
    charged = false;
    SetGlow(false);
}

void OnSelectExited(SelectExitEventArgs args)
{
    holdingHand = Hand.None;
    holdTimer = 0f;
    charged = false;
    SetGlow(false);
}

Hand DetectHand(IXRSelectInteractor interactor)
{
    // 가장 단순/안정: 이름 기반 (XRI 기본 프리팹은 보통 Left/Right가 이름에 들어감)
    // 더 정확하게 하고 싶으면, 아래에서 Left/Right Interactor Transform을 직접 지정하는 버전도 줄 수 있음.
    string n = interactor.transform.name.ToLowerInvariant();
    if (n.Contains("left")) return Hand.Left;
    if (n.Contains("right")) return Hand.Right;

    // 이름에 없으면, 부모 이름까지 훑기
    var p = interactor.transform.parent;
    while (p != null)
    {
        string pn = p.name.ToLowerInvariant();
        if (pn.Contains("left")) return Hand.Left;
        if (pn.Contains("right")) return Hand.Right;
        p = p.parent;
    }

    return Hand.None;
}

float ReadTriggerValue(Hand h)
{
    InputAction a = (h == Hand.Left) ? leftTriggerValue.action : rightTriggerValue.action;
    if (a == null) return 0f;

    // Activate Value는 보통 float(0..1)
    try { return a.ReadValue<float>(); }
    catch { return 0f; }
}

void SetGlow(bool on)
{
    if (tipLight != null) tipLight.enabled = on;

    if (tipGlowParticle != null)
    {
        if (on) tipGlowParticle.Play(true);
        else tipGlowParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }
}
}
