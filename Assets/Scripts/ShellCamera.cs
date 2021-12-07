using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class ShellCamera : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        XRDevice.DisableAutoXRCameraTracking(Camera.main, true);
        Camera.main.transform.position = new Vector3(0, 10, 0);
        Camera.main.transform.rotation = Quaternion.Euler(0, 0, 0);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
