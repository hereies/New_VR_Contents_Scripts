using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using Unity.XR.CoreUtils;

public class TourModeController : MonoBehaviour
{
    void Start()
    {
        // 시작 시 무조건 정상 모드로 고정
        SetMode(TourMode.Normal);
    }

    public enum TourMode
    {
        Normal,      // 메인(중력ON, 일반이동ON)
        DetailView,  // 관람(중력OFF, 이동OFF) + 천장복귀 ON
        WideView,    // 관람(중력OFF, 이동OFF) + 천장복귀 ON
        FreeRoam     // 자유관람(중력OFF, FlyON, 이동ON, BoundsON) + 천장복귀 OFF
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

        [Header("FreeRoam Walls (optional)")]
        public GameObject freeRoamWallsRoot; // <- 추가
    }

    [Header("Groups (0=A, 1=B)")]
    public ViewGroup[] groups = new ViewGroup[2];

    [Header("References")]
    public XROrigin xrOrigin;
    public XROriginViewSwitcher viewSwitcher;

    [Header("Locomotion Providers")]
    public ActionBasedContinuousMoveProvider continuousMove;
    public LocomotionProvider[] disableInViewMode; // 관람 모드에서 끄고 싶은 것들(텔포/턴 등)

    [Header("Bounds (XROrigin에 1개만)")]
    public FreeRoamBounds freeRoamBounds;

    [Header("Optional: Vertical mover (관람에서만 위/아래)")]
    public Behaviour upDownMove; // RightStickVerticalMove 같은 것

    [Header("Vertical move (Keyboard X/Y)")]
    public Behaviour keyboardUpDownMove; // KeyboardUpDownMove.cs

    [Header("Optional Turn Provider (Right Stick assist)")]
    public ActionBasedContinuousTurnProvider continuousTurn;

    [Header("Tuning")]
    public float normalMoveSpeed = 1.5f;
    public float freeRoamMoveSpeed = 3.0f;
    public float turnSpeedDegPerSec = 90f;

    [Header("State (read-only)")]
    [SerializeField] private TourMode currentMode = TourMode.Normal;
    [SerializeField] private int activeGroupIndex = 0;

    void Reset()
    {
        xrOrigin = FindFirstObjectByType<XROrigin>();
        viewSwitcher = FindFirstObjectByType<XROriginViewSwitcher>();
        continuousMove = FindFirstObjectByType<ActionBasedContinuousMoveProvider>();
        freeRoamBounds = FindFirstObjectByType<FreeRoamBounds>();
    }

    // --------- 버튼 1~3 (Group A = 0) ---------
    public void Btn1_Detail_A() => EnterDetail(0);
    public void Btn2_Wide_A() => EnterWide(0);
    public void Btn3_Free_A() => EnterFreeRoam(0);

    // --------- 버튼 4~6 (Group B = 1) ---------
    public void Btn4_Detail_B() => EnterDetail(1);
    public void Btn5_Wide_B() => EnterWide(1);
    public void Btn6_Free_B() => EnterFreeRoam(1);

    // --------- 공통: 복귀(천장복귀 또는 UI 버튼) ---------
    public void ReturnToMain()
    {
        SetMode(TourMode.Normal);

        var g = GetGroup(activeGroupIndex);
        if (g == null) return;

        if (viewSwitcher != null)
            viewSwitcher.ReturnToMain(g.mainPoint);
    }

    // --------- 내부: 그룹 활성화 ---------
    private void ActivateGroup(int groupIndex)
    {
        activeGroupIndex = Mathf.Clamp(groupIndex, 0, groups.Length - 1);

        if (freeRoamBounds != null)
            freeRoamBounds.SetActiveArea(activeGroupIndex);

        // 벽은 SetMode에서 FreeRoam일 때만 켤 거라서, 여기선 일단 다 꺼두기
        for (int i = 0; i < groups.Length; i++)
        {
            var g = groups[i];
            if (g != null && g.freeRoamWallsRoot != null)
                g.freeRoamWallsRoot.SetActive(false);
        }
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

    private void SetMode(TourMode mode)
    {
        currentMode = mode;

        bool isViewMode = (mode == TourMode.DetailView || mode == TourMode.WideView);

        // 1) 관람 모드에서 텔포/턴/이동 관련 provider 끄기
        if (disableInViewMode != null)
        {
            foreach (var p in disableInViewMode)
            {
                if (p == null) continue;
                p.enabled = !isViewMode;
            }
        }

        // 2) Bounds는 자유관람에서만 ON
        if (freeRoamBounds != null)
            freeRoamBounds.enabled = (mode == TourMode.FreeRoam);

        // 2-1) FreeRoam 벽도 자유관람에서만 ON (끼임 방지 핵심)
        var g = GetGroup(activeGroupIndex);
        if (g != null && g.freeRoamWallsRoot != null)
            g.freeRoamWallsRoot.SetActive(mode == TourMode.FreeRoam);

        // 3) Continuous Move Provider 세팅 + 속도 튜닝
        if (continuousMove != null)
        {
            if (mode == TourMode.Normal)
            {
                continuousMove.enabled = true;
                continuousMove.enableFly = false;
                continuousMove.useGravity = true;

                // 버전에 따라 moveSpeed 필드명이 다를 수 있음.
                // 인스펙터에서 "Move Speed"가 있다면 아래 중 하나로 맞춰줘.
                // continuousMove.moveSpeed = normalMoveSpeed;
            }
            else if (isViewMode)
            {
                continuousMove.enabled = false;
                continuousMove.enableFly = false;
                continuousMove.useGravity = false;
            }
            else if (mode == TourMode.FreeRoam)
            {
                continuousMove.enabled = true;
                continuousMove.enableFly = true;
                continuousMove.useGravity = false;

                // continuousMove.moveSpeed = freeRoamMoveSpeed;
            }
        }

        // 3-1) 오른쪽 스틱 회전 보조(턴 속도 튜닝)
        if (continuousTurn != null)
        {
            continuousTurn.enabled = !isViewMode; // 관람에서 회전 막고 싶으면 이렇게
            continuousTurn.turnSpeed = turnSpeedDegPerSec;
        }

        // 4) 천장복귀는 관람 모드에서만
        if (viewSwitcher != null)
            viewSwitcher.enableLookUpReturn = isViewMode;

        // 5) 기존 upDownMove(입력 기반)는 이제 “안 쓰거나”, 관람에서만 쓰는 정책이면 그대로
        if (upDownMove != null)
            upDownMove.enabled = false; // <- 너 정책상 RightStick은 회전, 수직은 키보드니까 꺼버리는 게 안전

        // 6) 키보드 X/Y 상하 이동은 FreeRoam에서만 ON
        if (keyboardUpDownMove != null)
            keyboardUpDownMove.enabled = (mode == TourMode.FreeRoam);

    }

    private ViewGroup GetGroup(int idx)
    {
        if (groups == null || groups.Length == 0) return null;
        if (idx < 0 || idx >= groups.Length) return null;
        return groups[idx];
    }
}
