using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveSphere : MonoBehaviour
{
    public float zMovement = 1f;
    public float yMovement = 3f;
    private float movedInZ = 0f;
    private float movedInY = 0f;

    Vector3 startPostion;
    private void Start()
    {
        startPostion = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 movement = new Vector3();
         if (movedInY < yMovement)
        {
            movement.y = -0.01f;
            movedInY += 0.01f;
        }
        else if (movedInZ < zMovement)
        {
            movement.z = -0.01f;
            movedInZ+= 0.01f;
        }
        if(movedInY >= yMovement && movedInZ >= zMovement)
        {
            transform.position = startPostion;
            movedInZ = 0f;
            movedInY = 0f;
        }
        transform.Translate(movement);
    }
}
