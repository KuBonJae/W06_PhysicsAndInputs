using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class SuperDash : MonoBehaviour
{
    PlayersInputs inputActions;
    Movement _movement;
    Rigidbody2D _rigidbody;

    private void Awake()
    {
        inputActions = new PlayersInputs();
        inputActions.Enable();
        _movement = GetComponent<Movement>();
        _rigidbody = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()
    {
        inputActions.Player.SuperDash.started += OnSuperDash;
        inputActions.Player.SuperDash.performed += OnSuperDash;
        inputActions.Player.SuperDash.canceled += OnSuperDash;
    }

    private void OnDisable()
    {
        inputActions.Player.SuperDash.started -= OnSuperDash;
        inputActions.Player.SuperDash.performed -= OnSuperDash;
        inputActions.Player.SuperDash.canceled -= OnSuperDash;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnSuperDash(InputAction.CallbackContext context)
    {
        Debug.Log("Super Dash!");
        float directionX = _movement.direction;
        if(directionX > 0)
        {
            Vector2 DashSpeed = new Vector2(30f, 15f);
            _rigidbody.velocity += DashSpeed;
        }
        else if(directionX < 0)
        {
            Vector2 DashSpeed = new Vector2(-30f, 15f);
            _rigidbody.velocity += DashSpeed;
        }
    }
}
