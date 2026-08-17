using UnityEngine;

public class UiLookAtCamera : MonoBehaviour
{
    public Camera cameraObj;

    void LateUpdate()
    {
        transform.LookAt(transform.position + cameraObj.transform.rotation * Vector3.forward, cameraObj.transform.rotation * Vector3.up);
    }
}
