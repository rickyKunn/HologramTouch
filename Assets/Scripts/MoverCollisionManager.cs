using UnityEngine;

public class MoverCollisionManager : MonoBehaviour
{
    [SerializeField] GameObject Emitter = null;

    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    private void OnTriggerEnter(Collider collision)
    {
        print("3d");

    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        var obj = other.gameObject;
        if (obj.tag == "FirstParticle")
        {
            if (Emitter == null) return;
            Emitter.SetActive(true);
        }
    }
}
