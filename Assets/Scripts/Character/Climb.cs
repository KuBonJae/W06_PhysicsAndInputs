using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Climb : MonoBehaviour
{
    private bool onWall;
    private bool onGrap = false;
    public bool grabIsPressing = false;

    [Header("Collider Settings")]
    [SerializeField][Tooltip("Length of the wall-checking collider")] private float wallLength = 0.25f;
    [SerializeField] private float rayCircleRadius = 0.5f;
    // transform.position is always shows center of the mass.
    // if you want to check from back of your feet to front of your feet, you need to calculate the position by using characters width
    [SerializeField][Tooltip("Distance between the wall-checking colliders")] private Vector3 colliderOffset;

    [Header("Layer Masks")]
    [SerializeField][Tooltip("Which layers are read as the ground")] private LayerMask wallLayer;

    private PlayersInputs _playersInputs;

    private void Awake()
    {
        _playersInputs = new PlayersInputs();
        _playersInputs.Enable();
    }

    private void OnEnable()
    {
        _playersInputs.Player.Climb.started += OnGrabWall;
        _playersInputs.Player.Climb.performed += OnGrabWall;
        _playersInputs.Player.Climb.canceled += OnGrabWall;
    }

    private void OnDisable()
    {
        _playersInputs.Player.Climb.started -= OnGrabWall;
        _playersInputs.Player.Climb.performed -= OnGrabWall;
        _playersInputs.Player.Climb.canceled -= OnGrabWall;
    }

    // Update is called once per frame
    void Update()
    {
        int layer = ((int)wallLayer);
        //Check the ground using raycast, not the collider
        onWall = Physics2D.CircleCast(transform.position, rayCircleRadius, Vector2.left, wallLength, wallLayer)
            || Physics2D.CircleCast(transform.position, rayCircleRadius, Vector2.left, wallLength, wallLayer);

        // Physics2D.OverlapCircleNonAlloc() << 최적화에 도움 되는 방식
    }

    private void FixedUpdate()
    {
        //if (onWall && onGrap)
        //    Physics2D.gravity = Vector2.zero;
        //else
        //    Physics2D.gravity = new Vector2(0f, -9.81f);

        //GetComponent<Rigidbody2D>().AddForce(new Vector2(1f, 1f), ForceMode2D.Impulse);
    }

    private void OnDrawGizmos()
    {
        if (onWall)
            Gizmos.color = Color.green;
        else
            Gizmos.color = Color.red;

        Gizmos.DrawWireSphere(transform.position, rayCircleRadius);
    }

    public bool GetonWall()
    {
        return onWall;
    }

    public bool GetonGrab()
    {
        return onGrap; 
    }

    public void OnGrabWall(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            //if (onWall)
            //    Physics2D.gravity = Vector2.zero;
            //else
            //    Physics2D.gravity = new Vector2(0f, -9.81f);
            onGrap = true;
            grabIsPressing = true;
        }
        else if (context.canceled)
        {
            onGrap = false;

            Physics2D.gravity = new Vector2(0f, -9.81f);
        }
    }
}
