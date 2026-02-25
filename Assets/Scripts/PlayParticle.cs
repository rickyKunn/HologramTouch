using NUnit.Framework.Constraints;
using UnityEngine;

public class PlayParticle : MonoBehaviour
{

    [SerializeField] GameObject Emitter = null;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    void OnTriggerEnter(Collider other)
    {
        var obj = other.gameObject;
        if (obj.tag == "FirstParticle")
        {
            if (Emitter == null) return;
            Emitter.SetActive(true);
        }
    }
}
