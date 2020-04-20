using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Character2D : MonoBehaviour
{
    public float speed = 0.1f;
    public float player_height = 2.0f;

    public GameObject left_leg;
    public GameObject left_leg_target;

    public GameObject right_leg;
    public GameObject right_leg_target;

    public GameObject left_arm_target;

    public LayerMask footLayers;

    private Camera main_cam;

    //private Rigidbody2D rigidbody;
    // Start is called before the first frame update
    void Start()
    {
        //rigidbody = gameObject.GetComponent<Rigidbody2D>();
        main_cam = Camera.main;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        //rigidbody.velocity = (new Vector2(Input.GetAxis("Horizontal") * force * Time.fixedDeltaTime, rigidbody.velocity.y - 5));
        transform.Translate(Input.GetAxis("Horizontal") * speed, 0, 0);
        UpdateFeetPositions();
        UpdateArmPositions();
    }

    /// <summary>
    /// Calculate the position for the targets of the feet.
    /// </summary>
    void UpdateFeetPositions()
    {
        //Ray2D left = new Ray2D();
        //Ray2D right = new Ray2D();

        RaycastHit2D hitLeft = Physics2D.Raycast(left_leg.transform.position, Vector2.down, 5,footLayers);
        RaycastHit2D hitRight = Physics2D.Raycast(right_leg.transform.position, Vector2.down, 5,footLayers);

        if (hitLeft.collider != null && hitRight.collider!= null){
            float height = hitLeft.point.y;
            if(height > hitRight.point.y)
            {
                height = hitRight.point.y;
            }
            gameObject.transform.position = new Vector3(transform.position.x,height + player_height,transform.position.z);

            left_leg_target.transform.position = hitLeft.point;
            right_leg_target.transform.position = hitRight.point;
        }

    }

    void UpdateArmPositions()
    {
        left_arm_target.transform.position = main_cam.ScreenToWorldPoint(Input.mousePosition);
        //Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        print(Input.mousePosition);
            
    }

    

    private void OnDrawGizmos()
    {
        
        RaycastHit2D hitLeft = Physics2D.Raycast(left_leg.transform.position, Vector2.down, 5,footLayers);
        RaycastHit2D hitRight = Physics2D.Raycast(right_leg.transform.position, Vector2.down, 5, footLayers);
        if (hitLeft.collider != null && hitRight != null)
        {
            Gizmos.DrawLine(left_leg.transform.position, hitLeft.point);
            Gizmos.DrawLine(right_leg.transform.position, hitRight.point);

        }
        else
        {
            Gizmos.DrawRay(left_leg.transform.position, Vector2.down * (left_leg.transform.position.y - hitLeft.point.y));
        }
    }
}
