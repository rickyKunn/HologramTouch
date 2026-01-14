using UnityEngine;
using Mediapipe.Tasks.Vision.HandLandmarker;

public class RightIndexFingerTipReceiver : MonoBehaviour
{
    [SerializeField] private Mediapipe.Unity.Sample.HandLandmarkDetection.HandLandmarkerRunner _runner;

    // 例：このTransformを動かしたい（球など）
    [SerializeField] private Transform _target;

    private void OnEnable()
    {
        if (_runner != null)
        {
            _runner.OnHandLandmarkerResult += OnResult;
        }
    }

    private void OnDisable()
    {
        if (_runner != null)
        {
            _runner.OnHandLandmarkerResult -= OnResult;
        }
    }

    private void OnResult(HandLandmarkerResult result)
    {
        // ★ここで “右手” と “人差し指先” を抜く
        // MediaPipeの仕様上、Index finger tip は 8。:contentReference[oaicite:3]{index=3}
        // handedness は "Left"/"Right"。ただしミラー入力前提の注意あり。:contentReference[oaicite:4]{index=4}

        // ※以下の「handedness / handLandmarks」の実際の参照方法は、
        //   あなたのプロジェクトの HandLandmarkerResult の定義に合わせて修正してください。
        //   Tasksとしては handedness と landmarks を保持する設計です。:contentReference[oaicite:5]{index=5}

        // 疑似コード例：
        // var handedness = result.handedness;   // or result.handedness()
        // var hands = result.handLandmarks;    // or result.landmarks()
        //
        // for (int i = 0; i < hands.Count; i++)
        // {
        //   var label = handedness[i][0].categoryName; // "Right" など（実メンバ名は要確認）
        //   if (label != "Right") continue;
        //
        //   var tip = hands[i][8]; // INDEX_FINGER_TIP
        //   var nx = tip.x; var ny = tip.y; var nz = tip.z; // x,yは[0,1]の正規化 :contentReference[oaicite:6]{index=6}
        //
        //   // ここであなたの「現実→Unity座標変換（定関数）」に通して _target を動かす
        // }

        // とりあえず“やる場所”の提示が主目的なので、ここでは空実装にしてあります。
        // もし HandLandmarkerResult の実際のメンバ名（補完で出るやつ）を貼ってくれれば、
        // その型にピッタリ合う形でコンパイル通る版を書けます。
    }
}
