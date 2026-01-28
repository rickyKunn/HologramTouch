using UnityEngine;
using UnityEngine.VFX;
using System.Collections;

public class PortalVFXController : MonoBehaviour
{
    [Header("VFX Reference")]
    [SerializeField] private VisualEffect portalVFX;

    [Header("Portal Settings")]
    [SerializeField] private float portalDuration = 4.0f;
    [SerializeField] private float radiusMultiplier = 2.0f; // VFXのMajor Radiusスケール

    private Coroutine activePortal;

    void Awake()
    {
        if (portalVFX == null)
            portalVFX = GetComponent<VisualEffect>();
    }

    public void SpawnPortal(CircleGestureDetector.CircleData circle)
    {
        // 既存ポータル停止
        if (activePortal != null)
        {
            StopCoroutine(activePortal);
            portalVFX.Stop();
        }

        // VFXパラメータ設定
        portalVFX.SetVector3("SpawnPos", circle.center);
        portalVFX.SetFloat("SpawnRad", circle.radius * radiusMultiplier);

        Debug.Log($"🌀 Portal Spawned at {circle.center}, Radius={circle.radius:F3}m");

        // VFX再生
        portalVFX.Play();

        // 自動停止
        activePortal = StartCoroutine(AutoStopPortal());
    }

    private IEnumerator AutoStopPortal()
    {
        yield return new WaitForSeconds(portalDuration);

        portalVFX.Stop();
        Debug.Log("🛑 Portal stopped");
        activePortal = null;
    }

    // デバッグ用手動テスト
    [ContextMenu("Test Portal at Origin")]
    void TestPortal()
    {
        CircleGestureDetector.CircleData test = new CircleGestureDetector.CircleData
        {
            center = transform.position + Vector3.forward * 0.5f,
            radius = 0.3f,
            confidence = 1.0f
        };
        SpawnPortal(test);
    }
}