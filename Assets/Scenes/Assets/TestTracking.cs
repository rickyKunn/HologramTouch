using UnityEngine;

public class ManualCircleTest : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CircleGestureDetector gestureDetector;

    [Header("Circle Settings")]
    [SerializeField] private float circleRadius = 0.3f;
    [SerializeField] private float circleSpeed = 1.0f;
    [SerializeField] private Vector3 circleCenter = new Vector3(0, 1, 2);

    [Header("Control")]
    [SerializeField] private KeyCode drawKey = KeyCode.Space;

    private float angle = 0f;
    private bool isDrawing = false;

    void Update()
    {
        // スペースキーを押している間、円を描く
        if (Input.GetKeyDown(drawKey))
        {
            isDrawing = true;
            angle = 0f;
            Debug.Log("🎨 Started drawing circle...");
        }

        if (Input.GetKeyUp(drawKey))
        {
            isDrawing = false;
            Debug.Log("✋ Stopped drawing");
        }

        if (isDrawing)
        {
            // 円周上の座標を計算
            float x = circleCenter.x + Mathf.Cos(angle) * circleRadius;
            float y = circleCenter.y + Mathf.Sin(angle) * circleRadius;
            float z = circleCenter.z;

            Vector3 position = new Vector3(x, y, z);

            // CircleGestureDetectorに座標を送信
            if (gestureDetector != null)
            {
                gestureDetector.fingertipPosition = position;
            }

            // 角度を進める
            angle += circleSpeed * Time.deltaTime;

            // 1周したら停止
            if (angle >= Mathf.PI * 2)
            {
                isDrawing = false;
                Debug.Log("⭕ Circle completed!");
            }
        }
    }

    void OnDrawGizmos()
    {
        // 描画予定の円を可視化
        Gizmos.color = Color.cyan;

        int segments = 32;
        for (int i = 0; i < segments; i++)
        {
            float angle1 = (i / (float)segments) * Mathf.PI * 2;
            float angle2 = ((i + 1) / (float)segments) * Mathf.PI * 2;

            Vector3 p1 = circleCenter + new Vector3(
                Mathf.Cos(angle1) * circleRadius,
                Mathf.Sin(angle1) * circleRadius,
                0
            );

            Vector3 p2 = circleCenter + new Vector3(
                Mathf.Cos(angle2) * circleRadius,
                Mathf.Sin(angle2) * circleRadius,
                0
            );

            Gizmos.DrawLine(p1, p2);
        }
    }
}