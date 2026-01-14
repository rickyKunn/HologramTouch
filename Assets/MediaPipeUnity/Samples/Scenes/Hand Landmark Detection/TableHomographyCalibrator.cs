using UnityEngine;

/// <summary>
/// 画面を4回クリックして、画像→テーブル のホモグラフィを作る。
/// クリック順：左下 → 右下 → 右上 → 左上
/// </summary>
public class TableHomographyCalibrator : MonoBehaviour
{
    [Header("Table size (meters)")]
    public float tableWidthMeters = 0.60f;
    public float tableHeightMeters = 0.40f;

    [Header("Calibration")]
    public Vector2[] imageCornersPx = new Vector2[4];   // クリックで埋まる
    public Homography2D H = new Homography2D();

    private int _clickCount = 0;

    private const string KEY_H = "HOMOGRAPHY_8";

    private void Start()
    {
        Load();
    }

    private void Update()
    {
        // 左クリックで4点取得
        if (Input.GetMouseButtonDown(0))
        {
            if (_clickCount < 4)
            {
                imageCornersPx[_clickCount] = Input.mousePosition;
                Debug.Log($"Corner[{_clickCount}] = {imageCornersPx[_clickCount]}");
                _clickCount++;

                if (_clickCount == 4)
                {
                    Solve();
                }
            }
        }

        // Rでやり直し
        if (Input.GetKeyDown(KeyCode.R))
        {
            _clickCount = 0;
            Debug.Log("Reset calibration clicks.");
        }

        // Sで保存
        if (Input.GetKeyDown(KeyCode.S))
        {
            Save();
            Debug.Log("Saved homography.");
        }
    }

    public void Solve()
    {
        // テーブル座標の4点（m）
        // クリック順：左下 → 右下 → 右上 → 左上 に対応させる
        Vector2[] tableCorners =
        {
            new Vector2(0f, 0f),
            new Vector2(tableWidthMeters, 0f),
            new Vector2(tableWidthMeters, tableHeightMeters),
            new Vector2(0f, tableHeightMeters)
        };

        bool ok = H.SolveFrom4Points(imageCornersPx, tableCorners);
        Debug.Log($"Homography solved: {ok}, valid={H.IsValid}");
    }

    public bool IsReady()
    {
        return H != null && H.IsValid;
    }

    private void Save()
    {
        // 8パラメータをJSONっぽく保存（簡易）
        // ※Homography2Dの中身はprivateなので、ここでは再計算用に画像4点を保存しておく方式にする
        // 固定台ならこれで十分（起動時にSolveし直す）
        PlayerPrefs.SetInt(KEY_H + "_HAS", 1);
        for (int i = 0; i < 4; i++)
        {
            PlayerPrefs.SetFloat(KEY_H + $"_X{i}", imageCornersPx[i].x);
            PlayerPrefs.SetFloat(KEY_H + $"_Y{i}", imageCornersPx[i].y);
        }
        PlayerPrefs.SetFloat(KEY_H + "_W", tableWidthMeters);
        PlayerPrefs.SetFloat(KEY_H + "_H", tableHeightMeters);
        PlayerPrefs.Save();
    }

    private void Load()
    {
        if (PlayerPrefs.GetInt(KEY_H + "_HAS", 0) == 0) return;

        tableWidthMeters = PlayerPrefs.GetFloat(KEY_H + "_W", tableWidthMeters);
        tableHeightMeters = PlayerPrefs.GetFloat(KEY_H + "_H", tableHeightMeters);

        for (int i = 0; i < 4; i++)
        {
            float x = PlayerPrefs.GetFloat(KEY_H + $"_X{i}", 0f);
            float y = PlayerPrefs.GetFloat(KEY_H + $"_Y{i}", 0f);
            imageCornersPx[i] = new Vector2(x, y);
        }

        _clickCount = 4;
        Solve();
        Debug.Log("Loaded homography from PlayerPrefs.");
    }
}
