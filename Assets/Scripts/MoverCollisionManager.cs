using UnityEngine;

public class MoverCollisionManager : MonoBehaviour
{
    [SerializeField] GameObject Emitter = null;
    [SerializeField] GameObject SparkEmitter = null;
    [SerializeField] BluetoothSerialSender BLS;
    void Start()
    {
        BLS = FindAnyObjectByType<BluetoothSerialSender>();
    }

    // Update is called once per frame
    void Update()
    {

    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        var obj = other.gameObject;
        if (obj.tag == "FirstParticle")
        {
            if (Emitter == null) return;
            // Emitter.SetActive(true);
            var quat = GetXRotationFromRectPosition(this.GetComponent<RectTransform>());
            var newObj = Instantiate(SparkEmitter, new Vector3(0, 0, 0), quat);
            BLS.Write();
            Destroy(newObj, 3f);
        }
    }

    public static Quaternion GetXRotationFromRectPosition(RectTransform rect)
    {
        // 基準回転（固定）
        Quaternion baseRot = Quaternion.Euler(0f, -90f, 0f);

        if (rect == null) return baseRot;

        Vector2 p = rect.anchoredPosition;

        // atan2は [-180, 180] を返す（度）
        float deg = Mathf.Atan2(p.y, p.x) * Mathf.Rad2Deg;

        // 0..360 に正規化
        if (deg < 0f) deg += 360f;

        // x軸回転
        Quaternion xRot = Quaternion.AngleAxis(deg, Vector3.right);

        // 基準回転の上にx回転を適用（ローカル回転として足すイメージ）
        return baseRot * xRot;
    }
}
