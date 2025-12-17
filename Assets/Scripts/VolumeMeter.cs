using UnityEngine;
using UnityEngine.UI;

public class VolumeMeter : MonoBehaviour
{
    [Header("Input")]
    [Tooltip("Vector3 を入力")]
    public Vector3 value;

    [Header("UI")]
    [Tooltip("白いImageのRectTransformを指定（左アンカー推奨）")]
    public RectTransform bar;

    [Tooltip("バーの最大幅(px)。親の幅に合わせたいなら 0 にして Start() で自動取得に変更してもOK")]
    public float maxWidth = 300f;

    [Header("Scaling")]
    [Tooltip("value.magnitude がこの値のときバーが100%になる")]
    public float fullScale = 10f;

    [Header("Smoothing (optional)")]
    [Tooltip("0ならスムージング無し。0.05〜0.2くらいが使いやすい")]
    public float smoothTime = 0.08f;

    private float _currentWidth;
    private float _widthVel;

    private void Reset()
    {
        maxWidth = 300f;
        fullScale = 10f;
        smoothTime = 0.08f;
    }

    private void Update()
    {
        if (bar == null) return;

        float mag = value.magnitude;

        float level01 = (fullScale <= Mathf.Epsilon) ? 0f : Mathf.Clamp01(mag / fullScale);
        float targetWidth = level01 * maxWidth;

        if (smoothTime > 0f)
        {
            _currentWidth = Mathf.SmoothDamp(_currentWidth, targetWidth, ref _widthVel, smoothTime);
        }
        else
        {
            _currentWidth = targetWidth;
        }

        // 左端固定で幅だけ変える（anchor/pivotが左固定になってる前提）
        Vector2 size = bar.sizeDelta;
        size.x = _currentWidth;
        bar.sizeDelta = size;
    }
}
