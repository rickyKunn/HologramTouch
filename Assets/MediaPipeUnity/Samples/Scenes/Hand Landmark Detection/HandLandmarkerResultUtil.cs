using System;
using UnityEngine;
using Mediapipe.Tasks.Vision.HandLandmarker;
using Mediapipe.Tasks.Components.Containers;

public static class HandLandmarkerResultUtil
{
    // MediaPipe Hands: INDEX_FINGER_TIP = 8
    public const int INDEX_FINGER_TIP = 8;

    // ★追加：入力を左右反転して推論している場合、handedness が逆になることがある
    // その場合は true にすると「現実の右手」を拾えるように Left/Right 判定を反転する
    public static bool InvertHandedness = true;

    public static bool TryGetRightIndexTip(
      in HandLandmarkerResult result,
      out Vector3 tipNormalized,
      out Vector3 tipWorld,
      out float handednessScore
    )
    {
        tipNormalized = default;
        tipWorld = default;
        handednessScore = 0f;

        var handedness = result.handedness;
        var handLandmarks = result.handLandmarks;
        var handWorldLandmarks = result.handWorldLandmarks;

        if (handedness == null || handLandmarks == null) return false;

        int n = Math.Min(handedness.Count, handLandmarks.Count);
        for (int i = 0; i < n; i++)
        {
            if (!IsDesiredHand(handedness[i], out float score)) continue;

            // normalized landmarks（多くのコンテナは landmarks(小文字)）
            var lnList = handLandmarks[i].landmarks;
            if (lnList == null || lnList.Count <= INDEX_FINGER_TIP) continue;

            var ln = lnList[INDEX_FINGER_TIP];
            tipNormalized = new Vector3(ln.x, ln.y, ln.z);

            // world landmarks（あれば）
            if (handWorldLandmarks != null && i < handWorldLandmarks.Count)
            {
                var lwList = handWorldLandmarks[i].landmarks;
                if (lwList != null && lwList.Count > INDEX_FINGER_TIP)
                {
                    var lw = lwList[INDEX_FINGER_TIP];
                    tipWorld = new Vector3(lw.x, lw.y, lw.z);
                }
            }

            handednessScore = score;
            return true;
        }

        return false;
    }

    private static bool IsDesiredHand(Classifications cls, out float score)
    {
        score = 0f;

        var cats = cls.categories;
        if (cats == null || cats.Count == 0) return false;

        // Category が struct の場合、null 初期化できない
        var top = cats[0];
        for (int k = 1; k < cats.Count; k++)
        {
            var c = cats[k];
            if (c.score > top.score) top = c;
        }

        score = top.score;

        // ★ここがポイント：反転しているなら Right/Left を入れ替える
        var desired = InvertHandedness ? "Left" : "Right";
        return string.Equals(top.categoryName, desired, StringComparison.OrdinalIgnoreCase);
    }
}
