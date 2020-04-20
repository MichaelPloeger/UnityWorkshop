using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public GameObject target;
    public float speed;

    // Start is called before the first frame update
    void Start()
    {
        if(target == null)
        {
            target = GameObject.FindGameObjectWithTag("Player");
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        transform.position = Vector3.Slerp(transform.position, target.transform.position + (Vector3.up * 3) + Vector3.back, speed * Time.fixedDeltaTime) ;
    }
}
