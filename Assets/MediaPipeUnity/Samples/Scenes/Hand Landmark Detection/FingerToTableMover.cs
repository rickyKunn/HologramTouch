using UnityEngine;
using Mediapipe.Tasks.Vision.HandLandmarker;

/// <summary>
/// 右手人差し指先（Normalized）を、ホモグラフィでテーブル座標へ変換し、Unityワールドに配置する。
/// </summary>
public class FingerToTableMover : MonoBehaviour
{
    [Header("Inputs")]
    [SerializeField] private Mediapipe.Unity.Sample.HandLandmarkDetection.HandLandmarkerRunner runner;
    [SerializeField] private TableHomographyCalibrator calibrator;

    [Header("Image size (must match ImageSource)")]
    [SerializeField] private int cameraWidth = 1280;
    [SerializeField] private int cameraHeight = 720;

    [Header("Table frame in Unity")]
    [SerializeField] private Transform tableOrigin; // テーブル左下のTransform
    [SerializeField] private Transform tableFrame;  // テーブルの向きを決めるTransform（任意）
    [SerializeField] private Transform fingerProxy; // 指先プロキシ（Sphereなど）

    // 最新の指先（必要値だけ保持）
    private bool _hasTip;
    private Vector3 _tipN; // normalized 0..1

    private void OnEnable()
    {
        if (runner != null) runner.OnHandLandmarkerResult += OnResult;
    }

    private void OnDisable()
    {
        if (runner != null) runner.OnHandLandmarkerResult -= OnResult;
    }

    private void OnResult(HandLandmarkerResult result)
    {
        if (HandLandmarkerResultUtil.TryGetRightIndexTip(result, out var tipN, out _, out _))
        {
            _hasTip = true;
            _tipN = tipN; // コピーして保持
        }
        else
        {
            _hasTip = false;
        }
    }

    private void Update()
    {
        if (fingerProxy == null || tableOrigin == null || calibrator == null) return;
        if (!_hasTip) return;
        if (!calibrator.IsReady()) return;

        // normalized -> pixel
        float px = _tipN.x * cameraWidth;
        float py = (1f - _tipN.y) * cameraHeight; // y反転
        Vector2 tipPx = new Vector2(px, py);

        // pixel -> table (meters)
        Vector2 tipTable = calibrator.H.Map(tipPx);

        // table -> unity world
        // tableFrame があればその right/forward を使う（テーブルの向きが分かりやすい）
        Vector3 axisX = (tableFrame != null) ? tableFrame.right : Vector3.right;
        Vector3 axisY = (tableFrame != null) ? tableFrame.forward : Vector3.forward;

        Vector3 worldPos = tableOrigin.position + axisX * tipTable.x + axisY * tipTable.y;

        // ★親子スケールの影響を避けたいなら position（ワールド）で動かす
        fingerProxy.position = worldPos;
    }
}
