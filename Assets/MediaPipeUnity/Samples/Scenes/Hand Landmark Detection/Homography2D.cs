using System;
using UnityEngine;

/// <summary>
/// 4点対応からホモグラフィ（画像→平面）を求めて、点を変換する。
/// 外部ライブラリ不要。ガウスジョルダンで 8x8 を解く。
/// </summary>
[Serializable]
public class Homography2D
{
    // H = [ h11 h12 h13
    //       h21 h22 h23
    //       h31 h32  1 ]
    // ※h33=1 に固定して 8未知数を解く
    [SerializeField] private double h11, h12, h13;
    [SerializeField] private double h21, h22, h23;
    [SerializeField] private double h31, h32;

    public bool IsValid { get; private set; }

    public void SetIdentity()
    {
        h11 = 1; h12 = 0; h13 = 0;
        h21 = 0; h22 = 1; h23 = 0;
        h31 = 0; h32 = 0;
        IsValid = true;
    }

    /// <summary>
    /// 画像座標→テーブル座標 へのHを、対応4点から推定する
    /// src: 画像(px)の4点、dst: テーブル(任意単位: mなど)の4点
    /// </summary>
    public bool SolveFrom4Points(Vector2[] src, Vector2[] dst)
    {
        if (src == null || dst == null || src.Length != 4 || dst.Length != 4)
        {
            IsValid = false;
            return false;
        }

        double[,] A = new double[8, 8];
        double[] b = new double[8];

        for (int i = 0; i < 4; i++)
        {
            double x = src[i].x;
            double y = src[i].y;
            double X = dst[i].x;
            double Y = dst[i].y;

            int r0 = 2 * i;
            A[r0, 0] = x;
            A[r0, 1] = y;
            A[r0, 2] = 1;
            A[r0, 3] = 0;
            A[r0, 4] = 0;
            A[r0, 5] = 0;
            A[r0, 6] = -X * x;
            A[r0, 7] = -X * y;
            b[r0] = X;

            int r1 = 2 * i + 1;
            A[r1, 0] = 0;
            A[r1, 1] = 0;
            A[r1, 2] = 0;
            A[r1, 3] = x;
            A[r1, 4] = y;
            A[r1, 5] = 1;
            A[r1, 6] = -Y * x;
            A[r1, 7] = -Y * y;
            b[r1] = Y;
        }

        if (!SolveLinearSystem8x8(A, b, out double[] xsol))
        {
            IsValid = false;
            return false;
        }

        h11 = xsol[0]; h12 = xsol[1]; h13 = xsol[2];
        h21 = xsol[3]; h22 = xsol[4]; h23 = xsol[5];
        h31 = xsol[6]; h32 = xsol[7];

        IsValid = true;
        return true;
    }

    /// <summary>
    /// 画像(px)の点をテーブル座標へ変換する
    /// </summary>
    public Vector2 Map(Vector2 p)
    {
        if (!IsValid) return Vector2.zero;

        double x = p.x;
        double y = p.y;

        double w = h31 * x + h32 * y + 1.0;
        if (Math.Abs(w) < 1e-9) return Vector2.zero;

        double X = (h11 * x + h12 * y + h13) / w;
        double Y = (h21 * x + h22 * y + h23) / w;

        return new Vector2((float)X, (float)Y);
    }

    private static bool SolveLinearSystem8x8(double[,] A, double[] b, out double[] x)
    {
        x = new double[8];

        double[,] M = new double[8, 9];
        for (int r = 0; r < 8; r++)
        {
            for (int c = 0; c < 8; c++) M[r, c] = A[r, c];
            M[r, 8] = b[r];
        }

        for (int col = 0; col < 8; col++)
        {
            int pivot = col;
            double max = Math.Abs(M[col, col]);
            for (int r = col + 1; r < 8; r++)
            {
                double v = Math.Abs(M[r, col]);
                if (v > max) { max = v; pivot = r; }
            }

            if (max < 1e-12) return false;

            if (pivot != col)
            {
                for (int c = col; c < 9; c++)
                {
                    double tmp = M[col, c];
                    M[col, c] = M[pivot, c];
                    M[pivot, c] = tmp;
                }
            }

            double div = M[col, col];
            for (int c = col; c < 9; c++) M[col, c] /= div;

            for (int r = 0; r < 8; r++)
            {
                if (r == col) continue;
                double factor = M[r, col];
                if (Math.Abs(factor) < 1e-12) continue;
                for (int c = col; c < 9; c++)
                {
                    M[r, c] -= factor * M[col, c];
                }
            }
        }

        for (int i = 0; i < 8; i++) x[i] = M[i, 8];
        return true;
    }
}
