using UnityEngine;

/// <summary>
/// NormalizedToCanvasFollowerの2D座標を3D空間に変換して
/// CircleGestureDetectorに渡すブリッジスクリプト
/// </summary>
public class UITo3DBridge : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform fingertipUI; // 指を表示しているUI要素
    [SerializeField] private CircleGestureDetector gestureDetector;

    [Header("3D Conversion Settings")]
    [SerializeField] private float depth = 2.0f; // カメラからの距離
    [SerializeField] private Camera targetCamera; // nullならCamera.main

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;

    private Vector3 lastWorldPosition;

    void Start()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    void Update()
    {
        if (fingertipUI == null || gestureDetector == null || targetCamera == null)
            return;

        // UI座標（RectTransform）からScreen座標を取得
        Vector3 screenPos = RectTransformUtility.WorldToScreenPoint(targetCamera, fingertipUI.position);

        // Screen座標から3D World座標に変換
        screenPos.z = depth; // カメラからの距離を設定
        Vector3 worldPos = targetCamera.ScreenToWorldPoint(screenPos);

        // CircleGestureDetectorに渡す
        gestureDetector.fingertipPosition = worldPos;
        lastWorldPosition = worldPos;
    }

    void OnGUI()
    {
        if (!showDebugInfo) return;

        GUI.Label(new Rect(10, 70, 400, 20),
            $"3D Position: ({lastWorldPosition.x:F2}, {lastWorldPosition.y:F2}, {lastWorldPosition.z:F2})");

        if (gestureDetector != null)
        {
            GUI.Label(new Rect(10, 90, 400, 20),
                $"Gesture Detector Active: {gestureDetector.enabled}");
        }
    }

    // Gizmosで3D位置を可視化
    void OnDrawGizmos()
    {
        if (gestureDetector == null) return;

        // 現在の指位置
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(lastWorldPosition, 0.05f);

        // カメラから指への線
        if (targetCamera != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(targetCamera.transform.position, lastWorldPosition);
        }
    }
}