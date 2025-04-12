using UnityEngine;

public class FaceCamera : MonoBehaviour
{
    void LateUpdate()
    {
        transform.rotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position);
    }
}
