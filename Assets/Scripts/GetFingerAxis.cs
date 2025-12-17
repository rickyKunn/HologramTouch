using System;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;

public class FingerAxisManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI debugText;

    [SerializeField] private FingerKinematicsFilter filter = new FingerKinematicsFilter();
    [SerializeField] private VolumeMeter volumeMeter;
    private Transform finger_transform;
    private Vector3 currentAcceleration;
    private Vector3 prevPos, prevVel;

    public Vector3 CurrentPos => filter.smoothedPosition;
    public Vector3 CurrentVel => filter.smoothedVelocity;
    public Vector3 CurrentAcc => filter.smoothedAcceleration;
    private bool got_info;
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (!got_info) return;
        filter.Update(finger_transform.position, Time.deltaTime);

        volumeMeter.value = CurrentAcc;
        debugText.text = $"Pos:{CurrentPos}\nVel:{CurrentVel}\nAcc{CurrentAcc.x}";
    }
    public void GetFingerTransform(Transform _transform)
    {
        finger_transform = _transform;
        got_info = true;
    }

    public static Vector3 EstimateAccelerationWorld(
      Transform target,
      float dt,
      ref Vector3 prevPosition,
      ref Vector3 prevVelocity
      )
    {
        if (target == null) return Vector3.zero;
        if (dt <= Mathf.Epsilon) return Vector3.zero;

        Vector3 currentPos = target.position;

        // v(t) ≈ (x(t) - x(t-dt)) / dt
        Vector3 currentVel = (currentPos - prevPosition) / dt;

        // a(t) ≈ (v(t) - v(t-dt)) / dt
        Vector3 accel = (currentVel - prevVelocity) / dt;

        prevPosition = currentPos;
        prevVelocity = currentVel;

        return accel;
    }

}
