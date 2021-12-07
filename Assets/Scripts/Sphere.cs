using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sphere : MonoBehaviour
{
    public static Sphere Instance;

    [SerializeField] Renderer Renderer;

    public float distance;
    private float angle;
    private float radius;
    private Vector3 center;

    private float speed = 0.5f;
    private float angularPosition;

    public bool move;

    private void Awake() {
        Instance = this;
        move = false;
    }
    
    private void SetDefaultMeta() {
        SetMetaData(.6f, 0, 0);
    }

    // Start is called before the first frame update
    void Start()
    {
        SetDefaultMeta();
        transform.localPosition = GetPosition();
    }

    // Update is called once per frame
    void Update()
    { 

    }

    private Vector3 GetPosition() {
        Vector3 relativePos = new Vector3 {
            x = Mathf.Cos(angularPosition) * radius,
            y = Mathf.Sin(angularPosition) * radius,
            z = 0
        };
        return center + relativePos;
    }
    public float GetAngularPosition() {
        return angularPosition / Mathf.PI * 180;
    }

    public void StartMove(float distance, float angle, float speed) {
        Debug.Log("start move");
        if (!move) {
            SetMetaData(distance, angle, speed);
            move = true;
        }
    }

    public void PauseMove() {
        move = false;
    }

    public void StopMove() {
        move = false;
        SetDefaultMeta();
        transform.localPosition = GetPosition();
    }
    
    private void SetMetaData(float distance, float angle, float speed) {
        angularPosition = 0;
        this.speed = speed;
        this.distance = distance;
        this.angle = (angle / 180) * Mathf.PI;
        this.radius = Mathf.Tan(this.angle) * distance;
        this.center = new Vector3(0, 0, distance);
    }

    public void Hit() {
        Renderer.material.color = Color.green;
        if (move) {
            angularPosition += Time.deltaTime * speed;
            angularPosition = angularPosition >= 2 * Mathf.PI ? angularPosition - 2 * Mathf.PI : angularPosition;
            transform.localPosition = GetPosition();
        }
        //Debug.Log("hit");

    }
    public void UnHit() {
        Renderer.material.color = Color.white;
        //Debug.Log("unHit");
    }

}
