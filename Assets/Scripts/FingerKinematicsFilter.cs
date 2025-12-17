using UnityEngine;

[System.Serializable]
public class FingerKinematicsFilter
{
    [Header("Smoothing (seconds)")]
    [Tooltip("Position smoothing time constant (seconds). Smaller = more responsive, larger = smoother.")]
    public float positionSmoothTime = 0.05f;

    [Tooltip("Velocity smoothing time constant (seconds).")]
    public float velocitySmoothTime = 0.08f;

    [Tooltip("Acceleration smoothing time constant (seconds).")]
    public float accelerationSmoothTime = 0.12f;

    [Header("Safety")]
    [Tooltip("If dt is larger than this, treat it as a reset-ish frame to avoid spikes.")]
    public float maxDt = 0.1f;

    [Header("Deadzone (per-axis)")]
    [Tooltip("速度の最低値")]
    public float velocityAxisDeadzone = 1.0f;

    [Tooltip("加速度の最低値")]
    public float accelerationAxisDeadzone = 1.0f;

    public bool initialized { get; private set; }

    public Vector3 rawPosition { get; private set; }
    public Vector3 smoothedPosition { get; private set; }
    public Vector3 smoothedVelocity { get; private set; }
    public Vector3 smoothedAcceleration { get; private set; }

    // 1 - exp(-dt/tau) を Lerp係数として使う（フレームレート依存を減らす）
    private static float AlphaFromTimeConstant(float dt, float smoothTime)
    {
        if (smoothTime <= Mathf.Epsilon) return 1f;
        return 1f - Mathf.Exp(-dt / smoothTime);
    }

    // 各軸の絶対値が threshold 以下なら 0 にする
    private static Vector3 ApplyAxisDeadzone(Vector3 v, float threshold)
    {
        if (threshold <= 0f) return v;

        if (Mathf.Abs(v.x) <= threshold) v.x = 0f;
        if (Mathf.Abs(v.y) <= threshold) v.y = 0f;
        if (Mathf.Abs(v.z) <= threshold) v.z = 0f;

        return v;
    }

    public void Reset(Vector3 currentRawPosition)
    {
        initialized = true;

        rawPosition = currentRawPosition;
        smoothedPosition = currentRawPosition;
        smoothedVelocity = Vector3.zero;
        smoothedAcceleration = Vector3.zero;

        _prevSmoothedPosition = smoothedPosition;
        _prevSmoothedVelocity = smoothedVelocity;
    }

    // rawPosition を入れると、平滑化した位置・速度・加速度が更新される
    public void Update(Vector3 currentRawPosition, float dt)
    {
        if (dt <= Mathf.Epsilon) return;

        // スパイク対策（タブ復帰などで dt が急にデカい時）
        if (dt > maxDt)
        {
            Reset(currentRawPosition);
            return;
        }

        rawPosition = currentRawPosition;

        if (!initialized)
        {
            Reset(currentRawPosition);
            return;
        }

        // ---- 1) 位置をLerpで平滑化 ----
        float aPos = AlphaFromTimeConstant(dt, positionSmoothTime);
        smoothedPosition = Vector3.Lerp(smoothedPosition, rawPosition, aPos);

        // ---- 2) 平滑化位置から速度を差分 ----
        // v(t) ≈ (x_s(t) - x_s(t-dt)) / dt
        Vector3 newVelocity = (smoothedPosition - _prevSmoothedPosition) / dt;

        // 速度もLerpで平滑化
        float aVel = AlphaFromTimeConstant(dt, velocitySmoothTime);
        smoothedVelocity = Vector3.Lerp(smoothedVelocity, newVelocity, aVel);

        // 速度の各軸で「1以下は切り捨て（0に）」を適用
        smoothedVelocity = ApplyAxisDeadzone(smoothedVelocity, velocityAxisDeadzone);

        // ---- 3) 平滑化速度から加速度を差分 ----
        // a(t) ≈ (v_s(t) - v_s(t-dt)) / dt
        Vector3 newAcceleration = (smoothedVelocity - _prevSmoothedVelocity) / dt;

        // 加速度もLerpで平滑化
        float aAcc = AlphaFromTimeConstant(dt, accelerationSmoothTime);
        smoothedAcceleration = Vector3.Lerp(smoothedAcceleration, newAcceleration, aAcc);

        // 加速度の各軸でも「1以下は切り捨て（0に）」を適用（不要なら accelerationAxisDeadzone=0 に）
        smoothedAcceleration = ApplyAxisDeadzone(smoothedAcceleration, accelerationAxisDeadzone);

        // 前回値更新
        _prevSmoothedPosition = smoothedPosition;
        _prevSmoothedVelocity = smoothedVelocity;
    }

    private Vector3 _prevSmoothedPosition;
    private Vector3 _prevSmoothedVelocity;
}
