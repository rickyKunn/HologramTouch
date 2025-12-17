using UnityEngine;
using System.Collections.Generic;
public class SendFingerAxis : MonoBehaviour
{
    private void Start()
    {
        print(this.gameObject.name);
        if (this.gameObject.name == "Point Annoation8" && transform.parent.gameObject.name == "Point List Annotation")
        {
            FindFirstObjectByType<FingerAxisManager>().GetFingerTransform(this.transform);
        }
    }

}



