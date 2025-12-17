using UnityEngine;

public class MoveObject : MonoBehaviour
{
    [SerializeField] private FingerAxisManager fingerAxisManager;
    private Transform transform;
    private Rigidbody rigidbody;
    private bool move = false;
    private void Awake()
    {
        transform = this.GetComponent<Transform>();
        rigidbody = this.GetComponent<Rigidbody>();
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            rigidbody.useGravity = false;
            transform.position = Vector3.zero;
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        print(other.gameObject.name);
        if (other.gameObject.name == "Point Annoation8")
        {
            Vector3 power = fingerAxisManager.CurrentAcc;
            rigidbody.useGravity = true;
            rigidbody.AddForce(power);
        }
    }
}
