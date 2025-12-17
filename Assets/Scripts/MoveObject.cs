using UnityEngine;

public class MoveObject : MonoBehaviour
{
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

    }
    private void OnTriggerEnter(Collider other)
    {
        print(other.gameObject.name);
        if (other.gameObject.name == "Point Annoation8")
        {
            rigidbody.useGravity = true;
            rigidbody.AddForce(new Vector3(-1000f, 0, 0));
        }
    }
}
