using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sphere : MonoBehaviour
{

    private const float movingLimit = 5f;
    private const float origin = 1f;
    private const float upLimit = origin + movingLimit;
    private const float downLimit = origin - movingLimit;

    private float speed = 0.5f;

    private bool movingUp = true;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
         if (movingUp) {
            if (transform.position.y < upLimit) {
                transform.Translate(Vector3.up*Time.deltaTime, Space.World);
            } else {
                movingUp = false;
                transform.Translate(Vector3.down*Time.deltaTime, Space.World);
            }
         } else {
            if (transform.position.y > downLimit) {
                transform.Translate(Vector3.down*Time.deltaTime, Space.World);
            } else {
                movingUp = true;
                transform.Translate(Vector3.up*Time.deltaTime, Space.World);
            }
        }
    }
}
