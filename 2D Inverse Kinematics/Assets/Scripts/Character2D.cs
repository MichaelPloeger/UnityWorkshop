using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Character2D : MonoBehaviour
{
    public float speed = 0.1f;
    public float player_height = 2.0f;
    

    public GameObject left_leg;
    public GameObject left_leg_target;
    public GameObject right_leg_target;

    public GameObject right_leg;

    public GameObject left_arm_target;
    public GameObject right_arm_target;

    public LayerMask footLayers;

    private Camera main_cam;

    //private Rigidbody2D rigidbody;
    // Start is called before the first frame update
    void Start()
    {
        main_cam = Camera.main;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if((transform.position.x > -80 && Input.GetAxis("Horizontal") < 0) || (transform.position.x < 15 && Input.GetAxis("Horizontal") > 0))
        transform.Translate(Input.GetAxis("Horizontal") * speed, 0, 0);
        UpdateFeetPositions();
        UpdateArmPositions();
    }

    /// <summary>
    /// Calculate the position for the targets of the feet.
    /// </summary>
    void UpdateFeetPositions()
    {
        RaycastHit2D hitLeft = Physics2D.Raycast(left_leg.transform.position, Vector2.down, 5,footLayers); //left leg ground hit
        RaycastHit2D hitRight = Physics2D.Raycast(right_leg.transform.position, Vector2.down, 5,footLayers); // right leg ground hit

        if (hitLeft.collider != null && hitRight.collider!= null){
            float height = hitLeft.point.y; // set the height of the character from the left leg.

            if(height > hitRight.point.y) // check if right leg is lower then the left leg.
            {
                height = hitRight.point.y; // set the height of the character from the right leg.
            }


            gameObject.transform.position = new Vector3(transform.position.x,height + player_height,transform.position.z); //set the height of the character to a new vector3 from the lowest leg 

            left_leg_target.transform.position = hitLeft.point; // set the target of the left leg to the ground hit position from the raycast.
            right_leg_target.transform.position = hitRight.point; // set the target of the right leg to the ground hit position from the raycast.
        }

    }

    /// <summary>
    /// Update the arm positions towards the mouse pointer
    /// </summary>
    void UpdateArmPositions()
    {
        left_arm_target.transform.position = main_cam.ScreenToWorldPoint(Input.mousePosition);
        right_arm_target.transform.position = main_cam.ScreenToWorldPoint(Input.mousePosition);
        //Implement Right Arm
    }

    

    private void OnDrawGizmos()
    {
        
        RaycastHit2D hitLeft = Physics2D.Raycast(left_leg.transform.position, Vector2.down, 5,footLayers);
        RaycastHit2D hitRight = Physics2D.Raycast(right_leg.transform.position, Vector2.down, 5, footLayers);
        if (hitLeft.collider != null && hitRight.collider != null)
        {
            Gizmos.DrawLine(left_leg.transform.position, hitLeft.point);
            Gizmos.DrawLine(right_leg.transform.position, hitRight.point);

        }
    }
}
