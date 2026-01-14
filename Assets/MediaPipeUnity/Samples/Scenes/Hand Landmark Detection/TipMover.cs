using UnityEngine;
using Mediapipe.Tasks.Vision.HandLandmarker;

public class TipMover : MonoBehaviour
{
    [SerializeField] private Mediapipe.Unity.Sample.HandLandmarkDetection.HandLandmarkerRunner runner;
    [SerializeField] private NormalizedToCanvasFollower follower = null;

    private bool _hasTip;
    private Vector3 _tipN; // normalized 

    private void OnEnable()
    {
        if (runner != null) runner.OnHandLandmarkerResult += OnResult;
    }

    private void OnDisable()
    {
        if (runner != null) runner.OnHandLandmarkerResult -= OnResult;
    }

    private void OnResult(HandLandmarkerResult result)
    {
        if (HandLandmarkerResultUtil.TryGetRightIndexTip(result, out var tipN, out _, out _))
        {
            _hasTip = true;
            _tipN = tipN;
        }
        else
        {
            _hasTip = false;
        }
    }

    private void Update()
    {
        if (!_hasTip) return;
        follower.SetNormalizedPosition(new Vector2(_tipN.x, _tipN.y));
    }
}
