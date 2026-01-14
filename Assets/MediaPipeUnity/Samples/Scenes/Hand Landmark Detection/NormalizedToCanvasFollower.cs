using UnityEngine;
using UnityEngine.UI;

public class NormalizedToCanvasFollower : MonoBehaviour
{
    [Header("Reference (座標を合わせる基準Rect)")]
    [SerializeField] private RectTransform referenceRect; // Main Canvas全体 or 映像を貼ってるRawImageのRectなど

    [Header("Target (動かすUI)")]
    [SerializeField] private RectTransform targetRect;    // nullならこのRectTransform

    [Header("Options")]
    [SerializeField] private bool clamp01 = true;         // 0..1にクランプするか
    [SerializeField] private bool updateEveryFrame = true;// 毎フレーム反映するか（結果が疎でも追従し続ける）

    [Header("Mirror (左右反転)")]
    // ★追加：物理特性上、画面を反転する必要がある場合に使う
    // チェックONのときだけX方向を左右反転する（ミラー）
    [SerializeField] private bool mirrorX = false;

    // referenceRect内のどこを基準線にミラーするか（0=左端, 0.5=中央, 1=右端）
    [SerializeField, Range(0f, 1f)] private float mirrorCenterX01 = 0.5f;

    [Header("Linear transform strength (線形変換の強度)")]
    // ★ここが要望：X/Yの変換強度（=感度）をインスペクタで変更
    // 1 = 通常、0 = pivotに固定、2 = 2倍の感度
    [SerializeField, Range(0f, 3f)] private float strengthX = 1f;
    [SerializeField, Range(0f, 3f)] private float strengthY = 1f;

    // どこを中心に強度をかけるか（referenceRectの中での位置）
    // (0,0)=左下, (1,1)=右上, (0.5,0.5)=中央
    [SerializeField] private Vector2 pivotInReferenceRect01 = new Vector2(0.5f, 0.5f);

    // 軸反転したいとき用（必要なら使う）
    [SerializeField] private bool invertX = false;
    [SerializeField] private bool invertY = false;

    [Header("Smoothing (座標補完)")]
    [SerializeField] private bool enableSmoothing = true;     // 補完を使う
    [SerializeField] private float smoothTime = 0.05f;        // 小さいほど追従が速い（0.03〜0.12あたりから）
    [SerializeField] private float maxSpeed = 5000f;          // 念のため上限
    [SerializeField] private float noUpdateHoldSeconds = 0.2f;// 結果が来ない時間が長いときの扱い用（任意）

    // 最新の正規化座標（左上原点、x右、y下）
    private Vector2 _latestNormalized;
    private bool _hasValue;

    // ★補完用：現在位置と速度（SmoothDampが内部で使う）
    private Vector2 _currentAnchoredPos;
    private Vector2 _anchoredVel;

    // ★「最後に新しい値が来た時刻」
    private float _lastUpdateTime;

    private Canvas _canvas;
    private Camera _uiCamera;

    private void Awake()
    {
        if (targetRect == null)
        {
            targetRect = GetComponent<RectTransform>();
        }

        if (referenceRect == null)
        {
            // とりあえず「自分の親」を基準にする（Main Canvas直下に置いてるなら親=CanvasでOK）
            referenceRect = targetRect != null ? targetRect.parent as RectTransform : null;
        }

        _canvas = referenceRect != null ? referenceRect.GetComponentInParent<Canvas>() : null;

        // Canvasのレンダーモードによってカメラ指定が必要/不要
        // Screen Space - Overlay：null
        // Screen Space - Camera / World Space：canvas.worldCamera（未設定ならCamera.main）
        if (_canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            _uiCamera = _canvas.worldCamera != null ? _canvas.worldCamera : Camera.main;
        }
        else
        {
            _uiCamera = null;
        }

        // 初期値
        if (targetRect != null)
        {
            _currentAnchoredPos = targetRect.anchoredPosition;
        }
        _lastUpdateTime = Time.unscaledTime;
    }

    private void Update()
    {
        if (!updateEveryFrame) return;
        ApplyIfNeeded();
    }

    /// <summary>
    /// MediaPipeの正規化座標を渡す（左上原点、x右、y下、0..1）
    /// </summary>
    public void SetNormalizedPosition(Vector2 normalizedTopLeft)
    {
        if (clamp01)
        {
            normalizedTopLeft.x = Mathf.Clamp01(normalizedTopLeft.x);
            normalizedTopLeft.y = Mathf.Clamp01(normalizedTopLeft.y);
        }

        _latestNormalized = normalizedTopLeft;
        _hasValue = true;

        // ★新しい値が来た時刻を更新
        _lastUpdateTime = Time.unscaledTime;

        if (!updateEveryFrame)
        {
            ApplyIfNeeded();
        }
    }

    private void ApplyIfNeeded()
    {
        if (!_hasValue) return;
        if (referenceRect == null || targetRect == null) return;

        // --- 1) 正規化(左上原点) -> Screenピクセル座標へ ---
        // MediaPipe: yは下向き（上=0, 下=1）
        // Unity Screen: yは上向き（下=0, 上=Screen.height）
        // なので y を反転して Screen座標にする
        float screenX = _latestNormalized.x * Screen.width;
        float screenY = (1f - _latestNormalized.y) * Screen.height;
        Vector2 screenPoint = new Vector2(screenX, screenY);

        // --- 2) Screenピクセル -> referenceRectのローカル座標へ ---
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(referenceRect, screenPoint, _uiCamera, out Vector2 localPoint))
        {
            Vector2 pivotLocal = GetPivotLocal(referenceRect, pivotInReferenceRect01);

            // p は referenceRect ローカル座標系の点
            Vector2 p = localPoint;

            // ★追加：左右反転（チェックONの時だけ）
            // referenceRect内の mirrorCenterX01 の縦線を基準にミラーする
            if (mirrorX)
            {
                float mirrorCenterLocalX = GetLocalXAt01(referenceRect, mirrorCenterX01);
                p.x = (2f * mirrorCenterLocalX) - p.x; // x' = 2c - x
            }

            // 既存：軸反転（必要なら使う）
            if (invertX) p.x = pivotLocal.x - (p.x - pivotLocal.x);
            if (invertY) p.y = pivotLocal.y - (p.y - pivotLocal.y);

            // (B) 強度（感度）を適用： p' = c + (p - c) * strength
            Vector2 pStrength;
            pStrength.x = pivotLocal.x + (p.x - pivotLocal.x) * strengthX;
            pStrength.y = pivotLocal.y + (p.y - pivotLocal.y) * strengthY;

            // これが最終的な「目標位置」
            Vector2 targetPos = pStrength;

            // 結果がしばらく来てない場合、動きを止めたい/保持したい場合（任意）
            float dtNoUpdate = Time.unscaledTime - _lastUpdateTime;
            if (dtNoUpdate > noUpdateHoldSeconds)
            {
                targetPos = _currentAnchoredPos;
            }

            // --- 3) ターゲットを移動（補完あり/なし） ---
            if (targetRect.parent == referenceRect)
            {
                if (enableSmoothing)
                {
                    _currentAnchoredPos = Vector2.SmoothDamp(
                        _currentAnchoredPos,
                        targetPos,
                        ref _anchoredVel,
                        smoothTime,
                        maxSpeed,
                        Time.unscaledDeltaTime
                    );
                    targetRect.anchoredPosition = _currentAnchoredPos;
                }
                else
                {
                    _currentAnchoredPos = targetPos;
                    targetRect.anchoredPosition = targetPos;
                }
            }
            else
            {
                // 親が違う場合は、referenceRectのローカル点 -> ワールドへ
                Vector3 worldPos = referenceRect.TransformPoint(targetPos);

                if (enableSmoothing)
                {
                    float k = 1f - Mathf.Exp(-25f * Time.unscaledDeltaTime);
                    targetRect.position = Vector3.Lerp(targetRect.position, worldPos, k);
                }
                else
                {
                    targetRect.position = worldPos;
                }
            }
        }
    }

    /// <summary>
    /// referenceRect内の(0..1)指定点を、referenceRectローカル座標に変換する
    /// pivotIn01: (0,0)=左下, (1,1)=右上
    /// </summary>
    private static Vector2 GetPivotLocal(RectTransform rectTransform, Vector2 pivotIn01)
    {
        pivotIn01.x = Mathf.Clamp01(pivotIn01.x);
        pivotIn01.y = Mathf.Clamp01(pivotIn01.y);

        Rect r = rectTransform.rect;
        float x = Mathf.Lerp(r.xMin, r.xMax, pivotIn01.x);
        float y = Mathf.Lerp(r.yMin, r.yMax, pivotIn01.y);
        return new Vector2(x, y);
    }

    /// <summary>
    /// referenceRectのローカル座標で、xの01位置（0=左端, 1=右端）を返す
    /// </summary>
    private static float GetLocalXAt01(RectTransform rectTransform, float x01)
    {
        x01 = Mathf.Clamp01(x01);
        Rect r = rectTransform.rect;
        return Mathf.Lerp(r.xMin, r.xMax, x01);
    }
}
