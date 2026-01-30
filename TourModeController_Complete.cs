using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using Unity.XR.CoreUtils;

public class TourModeController_Complete : MonoBehaviour
{
    public enum TourMode
    {
        Normal,      // 메인(중력ON, 이동ON)
        DetailView,  // 관람(중력OFF, 이동OFF) + 천장복귀 ON
        WideView,    // 관람(중력OFF, 이동OFF) + 천장복귀 ON
        FreeRoam     // 자유관람(중력OFF, Fly/이동ON) + Bounds/벽 ON
    }

    [System.Serializable]
    public class ViewGroup
    {
        public string name = "Group";

        [Header("Points")]
        public Transform mainPoint;
        public Transform detailPoint;
        public Transform widePoint;
        public Transform freeRoamStartPoint;

        [Header("FreeRoam Walls Root (optional)")]
        public GameObject freeRoamWallsRoot; // FreeRoam 전용 벽(콜라이더) 루트
    }

    [Header("Groups (0=A, 1=B)")]
    public ViewGroup[] groups = new ViewGroup[2];

    [Header("Start")]
    public int startGroupIndex = 0;
    public bool snapToStartMainPointOnPlay = true;

    [Header("References")]
    public XROrigin xrOrigin;
    public XROriginViewSwitcher viewSwitcher;

    [Header("Locomotion Providers")]
    public ActionBasedContinuousMoveProvider continuousMove;
    public LocomotionProvider[] disableInViewMode; // 관람에서 끌 provider(텔포/턴 등)

    [Header("Bounds (XROrigin에 1개만)")]
    public FreeRoamBounds freeRoamBounds;

    [Header("Optional: Vertical mover (관람에서만 위/아래) - 지금 정책이면 보통 OFF")]
    public Behaviour upDownMove;

    [Header("State (read-only)")]
    [SerializeField] private TourMode currentMode = TourMode.Normal;
    [SerializeField] private int activeGroupIndex = 0;

    [Header("Movement (gravity toggle)")]
    public SimpleVRCharacterMove_Stable mover;

    [Header("Gravity script (whatever is applying gravity now)")]
    public Behaviour gravityApplier; // 네가 실제로 중력 적용하는 스크립트

    [Header("FreeRoam vertical (X/Y buttons)")]
    public FreeRoamXYVerticalMove freeRoamVertical;

    [Header("Head Return UI")]
    public GameObject headReturnUI; // ReturnCanvas 또는 HeadUI

    void Reset()
    {
        xrOrigin = FindFirstObjectByType<XROrigin>();
        viewSwitcher = FindFirstObjectByType<XROriginViewSwitcher>();
        continuousMove = FindFirstObjectByType<ActionBasedContinuousMoveProvider>();
        freeRoamBounds = FindFirstObjectByType<FreeRoamBounds>();
        if (mover == null) mover = GetComponent<SimpleVRCharacterMove_Stable>();
    }

    void Start()
    {
        // 0) 시작 그룹 확정
        activeGroupIndex = Mathf.Clamp(startGroupIndex, 0, groups.Length - 1);

        // 1) 시작 시점에 "무조건" FreeRoam 관련(벽/Bounds) OFF
        DisableAllFreeRoamWalls();
        if (freeRoamBounds != null) freeRoamBounds.enabled = false;

        // 2) 현재 그룹 Area 인덱스 지정(FreeRoamBounds는 enable될 때만 clamp함)
        if (freeRoamBounds != null) freeRoamBounds.SetActiveArea(activeGroupIndex);

        // 3) 모드 Normal로 강제 초기화 (provider들 꼬임 방지)
        SetMode(TourMode.Normal);

        // 4) (선택) 시작 메인 포인트로 스냅해서 “이상한 메인으로 시작” 방지
        if (snapToStartMainPointOnPlay)
        {
            var g = GetGroup(activeGroupIndex);
            if (g != null && g.mainPoint != null && viewSwitcher != null)
                viewSwitcher.ReturnToMain(g.mainPoint);
        }
    }

    // --------- 버튼 1~3 (Group A = 0) ---------
    public void Btn1_Detail_A() => EnterDetail(0);
    public void Btn2_Wide_A() => EnterWide(0);
    public void Btn3_Free_A() => EnterFreeRoam(0);

    // --------- 버튼 4~6 (Group B = 1) ---------
    public void Btn4_Detail_B() => EnterDetail(1);
    public void Btn5_Wide_B() => EnterWide(1);
    public void Btn6_Free_B() => EnterFreeRoam(1);

    // --------- 공통: 복귀 ---------
    public void ReturnToMain()
    {
        SetMode(TourMode.Normal);

        var g = GetGroup(activeGroupIndex);
        if (g == null) return;

        if (viewSwitcher != null)
            viewSwitcher.ReturnToMain(g.mainPoint);
    }

    // --------- 모드 진입 ---------
    public void EnterDetail(int groupIndex)
    {
        ActivateGroup(groupIndex);
        SetMode(TourMode.DetailView);

        var g = GetGroup(activeGroupIndex);
        if (g == null || viewSwitcher == null) return;

        viewSwitcher.MoveToPoint(g.detailPoint, g.mainPoint, viewingState: true);
    }

    public void EnterWide(int groupIndex)
    {
        ActivateGroup(groupIndex);
        SetMode(TourMode.WideView);

        var g = GetGroup(activeGroupIndex);
        if (g == null || viewSwitcher == null) return;

        viewSwitcher.MoveToPoint(g.widePoint, g.mainPoint, viewingState: true);
    }

    public void EnterFreeRoam(int groupIndex)
    {
        ActivateGroup(groupIndex);
        SetMode(TourMode.FreeRoam);

        var g = GetGroup(activeGroupIndex);
        if (g == null || viewSwitcher == null) return;

        // 자유관람은 천장복귀 OFF
        viewSwitcher.MoveToPoint(g.freeRoamStartPoint, g.mainPoint, viewingState: false);
    }

    // --------- 내부 ---------
    private void ActivateGroup(int groupIndex)
    {
        activeGroupIndex = Mathf.Clamp(groupIndex, 0, groups.Length - 1);

        // 그룹에 맞는 Area 인덱스 설정(AreaA=0, AreaB=1)
        if (freeRoamBounds != null)
            freeRoamBounds.SetActiveArea(activeGroupIndex);

        // 그룹 바뀌면 우선 벽 다 꺼서 "끼임/갇힘" 방지
        DisableAllFreeRoamWalls();
    }

    private void SetMode(TourMode mode)
    {
        currentMode = mode;
        bool isViewMode = (mode == TourMode.DetailView || mode == TourMode.WideView);

        // 1) 관람 모드에서 provider 끄기
        if (disableInViewMode != null)
        {
            foreach (var p in disableInViewMode)
            {
                if (p == null) continue;
                p.enabled = !isViewMode;
            }
        }

        // 2) Bounds는 FreeRoam에서만 ON
        if (freeRoamBounds != null)
            freeRoamBounds.enabled = (mode == TourMode.FreeRoam);

        // 2-1) FreeRoam 벽도 FreeRoam에서만 ON
        var g = GetGroup(activeGroupIndex);
        if (g != null && g.freeRoamWallsRoot != null)
            g.freeRoamWallsRoot.SetActive(mode == TourMode.FreeRoam);

        // 3) Continuous Move Provider 세팅
        if (continuousMove != null)
        {
            if (mode == TourMode.Normal)
            {
                continuousMove.enabled = true;
                continuousMove.enableFly = false;
                continuousMove.useGravity = true;
            }
            else if (isViewMode)
            {
                continuousMove.enabled = false; // 관람은 이동 완전 차단
                continuousMove.enableFly = false;
                continuousMove.useGravity = false;
            }
            else if (mode == TourMode.FreeRoam)
            {
                continuousMove.enabled = true;
                continuousMove.enableFly = true;
                continuousMove.useGravity = false;
            }
        }

        // 4) 천장복귀는 관람 모드에서만
        if (viewSwitcher != null)
            viewSwitcher.enableLookUpReturn = isViewMode;

        // 5) 위/아래 이동 컴포넌트는 지금 정책상 기본 OFF (원하면 모드별로 다시 켜도 됨)
        if (upDownMove != null)
            upDownMove.enabled = false;

        // ===== Gravity policy by mode =====
        if (mover != null)
        {
            switch (mode)
            {
                case TourMode.Normal:
                    mover.useGravity = true;   // 메인: 중력 ON
                    break;

                case TourMode.DetailView:
                case TourMode.WideView:
                    mover.useGravity = false;  // 관람: 중력 OFF (공중 고정)
                    break;

                case TourMode.FreeRoam:
                    mover.useGravity = false;  // 자유관람: 무중력(원하면 true로 바꿔도 됨)
                    break;
            }
        }

        if (gravityApplier != null)
            gravityApplier.enabled = !isViewMode; // 관람이면 중력 OFF

        if (freeRoamVertical != null)
            freeRoamVertical.enabled = (mode == TourMode.FreeRoam);

        bool isViewing = (mode == TourMode.DetailView || mode == TourMode.WideView);

        if (headReturnUI != null)
            headReturnUI.SetActive(isViewing);

    }

    private void DisableAllFreeRoamWalls()
    {
        if (groups == null) return;
        for (int i = 0; i < groups.Length; i++)
        {
            var g = groups[i];
            if (g != null && g.freeRoamWallsRoot != null)
                g.freeRoamWallsRoot.SetActive(false);
        }
    }

    private ViewGroup GetGroup(int idx)
    {
        if (groups == null || groups.Length == 0) return null;
        if (idx < 0 || idx >= groups.Length) return null;
        return groups[idx];
    }

    private void MoveOriginToPose(Vector3 worldPos, Quaternion worldRot)
    {
        if (xrOrigin == null) return;

        // 카메라를 worldPos로 보낸다
        xrOrigin.MoveCameraToWorldLocation(worldPos);

        if (xrOrigin.Camera == null) return;
        var cam = xrOrigin.Camera.transform;

        // yaw만 맞춘다
        Vector3 currentForward = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized;
        Vector3 desiredForward = Vector3.ProjectOnPlane(worldRot * Vector3.forward, Vector3.up).normalized;
        if (currentForward.sqrMagnitude < 1e-6f || desiredForward.sqrMagnitude < 1e-6f) return;

        float deltaYaw = Vector3.SignedAngle(currentForward, desiredForward, Vector3.up);
        xrOrigin.RotateAroundCameraUsingOriginUp(deltaYaw);
    }

    private void TeleportTo(Transform target)
    {
        if (target == null) return;
        MoveOriginToPose(target.position, target.rotation);
    }

}
