using UnityEngine;
using System.Collections;

public class LookAtCameraBehaviour : MonoBehaviour
{

    private void Update()
    {
        transform.LookAt(new Vector3(Camera.current.transform.position.x, transform.position.y, Camera.current.transform.position.z));
    }
}


