using System.Collections.Generic;
using UnityEngine;

public class CameraSwitcher : MonoBehaviour
{
    // 0~N번: 고정 카메라들, 마지막: 자유 카메라
    public List<GameObject> cameras;

    [Header("Defaults")]
    public int mainIndex = 0;

    [Tooltip("카메라를 켤 때 원래 저장된 위치/회전으로 리셋할지")]
    public bool resetOnSwitch = false;

    struct Pose
    {
        public Vector3 pos;
        public Quaternion rot;
        public Pose(Vector3 p, Quaternion r) { pos = p; rot = r; }
    }

    private readonly Dictionary<GameObject, Pose> _initialPose = new();

    void Awake()
    {
        // 카메라 원래 자리 저장
        _initialPose.Clear();
        foreach (var cam in cameras)
        {
            if (cam == null) continue;
            var t = cam.transform;
            _initialPose[cam] = new Pose(t.position, t.rotation);
        }
    }

    void Start()
    {
        SwitchCamera(mainIndex);
    }

    // 버튼에 연결할 함수
    public void SwitchCamera(int index)
    {
        if (cameras == null || cameras.Count == 0) return;
        index = Mathf.Clamp(index, 0, cameras.Count - 1);

        for (int i = 0; i < cameras.Count; i++)
        {
            bool on = (i == index);
            if (cameras[i] != null)
            {
                cameras[i].SetActive(on);
                if (on && resetOnSwitch) ResetCameraPose(i);
            }
        }
    }

    public void ReturnToMain()
    {
        SwitchCamera(mainIndex);
        ResetCameraPose(mainIndex);
    }

    public void ResetCameraPose(int index)
    {
        if (cameras == null || index < 0 || index >= cameras.Count) return;
        var cam = cameras[index];
        if (cam == null) return;

        if (_initialPose.TryGetValue(cam, out var p))
        {
            cam.transform.SetPositionAndRotation(p.pos, p.rot);
        }
    }
}
