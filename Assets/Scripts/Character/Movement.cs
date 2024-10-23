using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class Movement : MonoBehaviour
{
    [Header("Movement Stats")]
    [SerializeField, Range(0f, 20f)][Tooltip("Maximum movement speed")] public float maxSpeed = 10f;
    [SerializeField, Range(0f, 100f)][Tooltip("How fast to reach max speed")] public float maxAcceleration = 52f;
    [SerializeField, Range(0f, 100f)][Tooltip("How fast to stop after letting go")] public float maxDecceleration = 52f;
    [SerializeField, Range(0f, 100f)][Tooltip("How fast to stop when changing direction")] public float maxTurnSpeed = 80f;
    [SerializeField, Range(0f, 100f)][Tooltip("How fast to reach max speed when in mid-air")] public float maxAirAcceleration;
    [SerializeField, Range(0f, 100f)][Tooltip("How fast to stop in mid-air when no direction is used")] public float maxAirDeceleration;
    [SerializeField, Range(0f, 100f)][Tooltip("How fast to stop when changing direction when in mid-air")] public float maxAirTurnSpeed = 80f;
    [SerializeField][Tooltip("Friction to apply against movement on stick")] private float friction = 0f;

    //[Header("Options")]
    //[Tooltip("When false, the charcter will skip acceleration and deceleration and instantly move and stop")] public bool useAcceleration;

    [Header("Calculations")]
    public float direction;
    private Vector2 desiredVelocity;
    public Vector2 velocity;
    private float maxSpeedChange;
    private float acceleration;
    private float deceleration;
    private float turnSpeed;

    [Header("Current State")]
    public bool onGround;
    public bool pressingKey;
    public bool onWall;
    public bool onGrab;

    //private PlayerInputs _input;
    private Rigidbody2D _body;
    private OnGround _ground;
    private Dash _dash;
    private Climb _climb;
    private GameObject Canvas;

    //public PlayerInput _playerInput;
    private PlayersInputs _playersInputs;
    public bool _pressUp = false;
    public bool _pressDown = false;
    public float lookDirection = 1f;
    private void Awake()
    {
        _playersInputs = new PlayersInputs();
        _playersInputs.Enable();
    }

    private void OnEnable()
    {
        //_playersInputs.Player.Move.started += OnMovement;    
        _playersInputs.Player.Move.performed += OnMovement;    
        _playersInputs.Player.Move.canceled += OnMovement;

        //_playersInputs.Player.TriggerUp.started += OnPressUp;
        _playersInputs.Player.TriggerUp.performed += OnPressUp;
        _playersInputs.Player.TriggerUp.canceled += OnPressUp;

        //_playersInputs.Player.TriggerDown.started += OnPressDown;
        _playersInputs.Player.TriggerDown.performed += OnPressDown;
        _playersInputs.Player.TriggerDown.canceled += OnPressDown;
    }

    private void OnDisable()
    {
        //_playersInputs.Player.Move.started -= OnMovement;
        _playersInputs.Player.Move.performed -= OnMovement;
        _playersInputs.Player.Move.canceled -= OnMovement;

        //_playersInputs.Player.TriggerUp.started -= OnPressUp;
        _playersInputs.Player.TriggerUp.performed -= OnPressUp;
        _playersInputs.Player.TriggerUp.canceled -= OnPressUp;

        //_playersInputs.Player.TriggerDown.started -= OnPressDown;
        _playersInputs.Player.TriggerDown.performed -= OnPressDown;
        _playersInputs.Player.TriggerDown.canceled -= OnPressDown;
    }

    // Start is called before the first frame update
    void Start()
    {
        _ground = GetComponent<OnGround>();
        _body = GetComponent<Rigidbody2D>();
        _dash = GetComponent<Dash>();
        _climb = GetComponent<Climb>();

        Canvas = GameObject.Find("Canvas");
        //_playerInput = GetComponent<PlayerInput>();

        //StartCoroutine(BufferCheck());
    }

    IEnumerator BufferCheck()
    {
        while(true)
        {
            yield return new WaitForSeconds(Time.fixedDeltaTime);

            if (CircularInputBuffer.instance.BufferIsEmpty())
                continue;

            if (Time.time - CircularInputBuffer.instance.GetBuffer()[CircularInputBuffer.instance.Tail].Item2 > 0.5f)
                CircularInputBuffer.instance.Tail = CircularInputBuffer.instance.Tail == CircularInputBuffer.instance.GetBuffer().Length - 1 ? 0 : CircularInputBuffer.instance.Tail++;
        }
        
    }

    public void OnMovement(InputAction.CallbackContext context)
    {
        if (MoveLimiter.instance.CharacterMove) // if player can move?
        { 
            direction = context.ReadValue<float>();

            CircularInputBuffer.instance.InputToBuffer(new Vector2(direction, 0f), Time.time);

            if (direction > 0f)
                lookDirection = 1f;
            else if (direction < 0f)
                lookDirection = -1f;
            else 
            {
                // if input 0 << no input << remain previous input -1 or 1
            }

            Debug.Log("Movement : " + direction.ToString());

            if(context.canceled)
                direction = 0f;
        }
    }

    public void OnPressUp(InputAction.CallbackContext context)
    {
        if (context.started || context.performed)
        {
            _pressUp = true;
            CircularInputBuffer.instance.InputToBuffer(new Vector2(0f, 1f), Time.time);
        }
        else if(context.canceled)
            _pressUp = false;
    }

    public void OnPressDown(InputAction.CallbackContext context)
    {
        if (context.started || context.performed)
        {
            _pressDown = true;
            CircularInputBuffer.instance.InputToBuffer(new Vector2(0f, -1f), Time.time);
        }
        else if (context.canceled)
            _pressDown = false;
    }

    // Update is called once per frame
    void Update()
    {
        TextMeshProUGUI bufferText = Canvas.transform.Find("InputBuffer").GetComponent<TextMeshProUGUI>();
        bufferText.text = "";
        for (int i = CircularInputBuffer.instance.Tail; ; i++)
        {
            if (i > CircularInputBuffer.instance.GetBuffer().Length - 1)
                i = 0;

            if (i == CircularInputBuffer.instance.Head)
                break;

            bufferText.text += "(" + CircularInputBuffer.instance.GetBuffer()[i].Item1.x.ToString() + ", " + CircularInputBuffer.instance.GetBuffer()[i].Item1.y.ToString() + ")";
        }

        if (_dash.currentDashing)
            return;

        if (!MoveLimiter.instance.CharacterMove)
        direction = 0f; // cant move, then speed is 0

        if (direction != 0f)
        {
            // capsule sprite disappear when we flip the sprite
            transform.localScale = new Vector3(direction > 0f ? 0.5f : -0.5f, 0.5f, 0.5f); // flip the image
            pressingKey = true;
        }
        else
            pressingKey = false;

        desiredVelocity = new Vector2(direction, 0f) * Mathf.Max(maxSpeed - friction, 0f); // friction is too high, then can't move
    }

    private void FixedUpdate()
    {
        if (_dash.currentDashing)
            return;

        // check character is ground
        onGround = _ground.GetonGround();

        // check character want to grab the wall
        onWall = _climb.GetonWall();
        onGrab = _climb.GetonGrab();
        if(onWall && onGrab)
        {
            Physics2D.gravity = Vector2.zero;
            StartCoroutine("RollBackGravity");
            MoveAlongTheWall();
        }
        else
        {
            // get character's velocity
            velocity = _body.velocity;

            // 정지는 안된다!
            // if (_pressDown)
            // {
            //     _body.velocity = Vector2.zero;
            //     return; // 아래 키 누르고 있으면 정지
            // }

            // calculate movement depending on "instant movement" boolean
            //if (useAcceleration)
            runWithAcceleration();
        }
        
    }

    IEnumerator RollBackGravity()
    {
        while (onWall && onGrab)
        {
            yield return new WaitForSecondsRealtime(0.01f); // 계속 살아 있다가
        }

        // 둘 중 하나라도 풀리면 바로 중력 원복
        Physics2D.gravity = new Vector2(0f, -9.81f);
    }
    
    private void MoveAlongTheWall()
    {
        _body.velocity = Vector2.zero;
        Debug.Log("Make velocity zero : " + _body.velocity.x.ToString() + " " + _body.velocity.y.ToString());
        if (_pressUp && _pressDown)
            return;
        else
        {
            if (_pressUp)
            {
                //transform.position += new Vector3(0f, Time.unscaledDeltaTime * maxSpeed * 0.5f, 0f); // deltaTime 이어도 어차피 fixedUpdate 이므로 강제로 unscaled로 바뀔듯
                transform.position += new Vector3(0f, 0.02f * maxSpeed * 0.5f, 0f); // deltaTime 이어도 어차피 fixedUpdate 이므로 강제로 unscaled로 바뀔듯
            }
            else if (_pressDown)
            {
                //transform.position -= new Vector3(0f, Time.unscaledDeltaTime * maxSpeed * 0.5f, 0f); //  여기도 일단 unscaled로 변경
                transform.position -= new Vector3(0f, 0.02f * maxSpeed * 0.5f, 0f); //  여기도 일단 unscaled로 변경
            }
        }
    }

    private void runWithAcceleration()
    {
        // check the values depending on "onGround"
        acceleration = onGround ? maxAcceleration : maxAirAcceleration;
        deceleration = onGround ? maxDecceleration : maxAirDeceleration;
        turnSpeed = onGround ? maxTurnSpeed : maxAirTurnSpeed;

        // is player pressing button?
        if(pressingKey)
        {
            // sign of input direction is diff => player want to move other direction
            if (Mathf.Sign(direction) != Mathf.Sign(velocity.x))
            {
                //maxSpeedChange = turnSpeed * Time.deltaTime;
                //maxSpeedChange = turnSpeed * Time.unscaledDeltaTime;
                maxSpeedChange = turnSpeed * 0.02f;
            }
            else
            {
                //maxSpeedChange = acceleration * Time.deltaTime;
                //maxSpeedChange = acceleration * Time.unscaledDeltaTime;
                maxSpeedChange = acceleration * 0.02f;
            }
        }
        else // not pressing btn -> character must to be stopped
        {
            //maxSpeedChange = deceleration * Time.deltaTime;
            //maxSpeedChange = deceleration * Time.unscaledDeltaTime;
            maxSpeedChange = deceleration * 0.02f;
        }

        // change the velocity of x 
        float deltaX = Mathf.Abs(velocity.x - Mathf.MoveTowards(velocity.x, desiredVelocity.x, maxSpeedChange));
        //if (_dash.currentDashing)
        //{
        //    if (velocity.y > 0f)
        //    {
        //        velocity.y -= deltaX;
        //    }
        //    else if (velocity.y < 0f)
        //    {
        //        velocity.y += deltaX;
        //    }
        //}
        velocity.x = Mathf.MoveTowards(velocity.x, desiredVelocity.x, maxSpeedChange);


        _body.velocity = velocity;
    }
}
