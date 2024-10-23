using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class Jump : MonoBehaviour
{
    [Header("Components")]
    [HideInInspector] public Rigidbody2D _body;
    private OnGround _ground;
    [HideInInspector] public Vector2 velocity;
    private Dash _dash;
    private Movement _movement;


    [Header("Jumping Stats")]
    [SerializeField, Range(2f, 5.5f)][Tooltip("Maximum jump height")] public float jumpHeight = 7.3f;
    [SerializeField, Range(0.2f, 1.25f)][Tooltip("How long it takes to reach that height before coming back down")] public float timeToJumpApex;
    [SerializeField, Range(0f, 5f)][Tooltip("Gravity multiplier to apply when going up")] public float upwardMovementMultiplier = 1f;
    [SerializeField, Range(1f, 10f)][Tooltip("Gravity multiplier to apply when coming down")] public float downwardMovementMultiplier = 6.17f;
    [SerializeField, Range(0, 1)][Tooltip("How many times can you jump in the air?")] public int maxAirJumps = 0;

    [Header("Options")]
    [Tooltip("Should the character drop when you let go of jump?")] public bool variablejumpHeight;
    [SerializeField, Range(1f, 10f)][Tooltip("Gravity multiplier when you let go of jump")] public float jumpCutOff;
    [SerializeField][Tooltip("The fastest speed the character can fall")] public float speedLimit;
    [SerializeField, Range(0f, 0.3f)][Tooltip("How long should coyote time last?")] public float coyoteTime = 0.15f;
    [SerializeField, Range(0f, 0.3f)][Tooltip("How far from ground should we cache your jump?")] public float jumpBuffer = 0.15f;

    [Header("Calculations")]
    public float jumpSpeed;
    private float defaultGravityScale;
    public float gravMultiplier;

    [Header("Current State")]
    public bool canJumpAgain = false;
    private bool desiredJump;
    private float jumpBufferCounter;
    private float coyoteTimeCounter = 0;
    public bool pressingJump;
    public bool onGround;
    private bool currentlyJumping;
    public bool dashJumping;

    private PlayersInputs _playersInputs;

    private float minHeight = -2f;
    private float maxHeight = -2f;

    private void Awake()
    {
        _playersInputs = new PlayersInputs();
        _playersInputs.Enable();

        _body = GetComponent<Rigidbody2D>();
        _ground = GetComponent<OnGround>();
        _dash = GetComponent<Dash>();
        _movement= GetComponent<Movement>();
        defaultGravityScale = 1f;
    }

    private void OnEnable()
    {
        _playersInputs.Player.Jump.started += OnJump;
        _playersInputs.Player.Jump.performed += OnJump;
        _playersInputs.Player.Jump.canceled += OnJump;
    }

    private void OnDisable()
    {
        _playersInputs.Player.Jump.started -= OnJump;
        _playersInputs.Player.Jump.performed -= OnJump;
        _playersInputs.Player.Jump.canceled -= OnJump;
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if(MoveLimiter.instance.CharacterMove)
        {
            if (context.started)
            {
                desiredJump = true; // when we press btn, tell the script we desired a jump
                pressingJump = true;
            }

            if (context.canceled)
                pressingJump = false;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Physics2D.gravity.y == 0f)
            return;

        SetPhysics();

        onGround = _ground.GetonGround();

        if(jumpBuffer > 0f) // if we have jumpBuffer
        {
            if(desiredJump) // player want to jump
            {
                jumpBufferCounter += Time.deltaTime; // if not on the ground, this parameter will be increase

                if(jumpBufferCounter > jumpBuffer) // if its value is larger then jumpBuffer constant, ignore jumping
                {
                    desiredJump = false;
                    jumpBufferCounter = 0f;
                }
            }
        }

        if(!currentlyJumping && !onGround) // get off from the edge of the ground
        {
            coyoteTimeCounter += Time.deltaTime;
        }
        else
        {
            coyoteTimeCounter = 0f;
        }
    }

    private void SetPhysics()
    {
        // v = v_0 - g*t
        // vt = s = v_0 * t - 1/2 * g * t^2 (integration)
        // initial velocity of y is zero -> s (distance) = -1/2 * g * t^2;
        // g (gravity) = -2 * s / (t^2);
        Vector2 newGravity = new Vector2(0, -2 * (jumpHeight) / ((timeToJumpApex) * (timeToJumpApex)));
        // already 9.8m/s^2 gravity is adjusting to character, so we just calculate multiplier so that just multiply it to gravity constant
        _body.gravityScale = (newGravity.y / Physics2D.gravity.y) * gravMultiplier;
    }

    private void FixedUpdate()
    {
        velocity = _body.velocity;

        if (desiredJump && onGround)
        {
            calculateGravity();
            SetPhysics();

            DoAJump();
            _body.velocity = velocity;

            return;
        }

        calculateGravity();
    }

    private void DoAJump()
    {
        // onground -> can jump / in coyote time -> can jump / can doublejump -> can jump
        if(onGround || (coyoteTimeCounter  > 0.03f && coyoteTimeCounter < coyoteTime) || canJumpAgain)
        {
            desiredJump = false;
            jumpBufferCounter = 0f;
            coyoteTimeCounter = 0f;

            canJumpAgain = (maxAirJumps == 1 && canJumpAgain == false);

            //if (_body.gravityScale > 15f)
            //{
            //    desiredJump = true;
            //    return;
            //}

            // v = v0 - gt -> v^2 = v0^2 - 2 v0 gt + gt ^ 2
            //      = v0 ^ 2 - 2 * g * (v0 * t - 1/2 * g * t^2)
            //      = v0 ^ 2 - 2 * g * s
            // when max height, velocity is zero -> v0 ^ 2 = 2 * g * s
            // gravity is minus, so check the sign of root
            jumpSpeed = Mathf.Sqrt(-2 * Physics2D.gravity.y * _body.gravityScale * jumpHeight);

            if(velocity.y > 0) // adjust velocity to make strength always same
            {
                jumpSpeed = Mathf.Max(jumpSpeed - velocity.y, 0);
            } 
            else if(velocity.y < 0)
            {
                jumpSpeed += Mathf.Abs(velocity.y);
            }

            if (_dash.DashState)
            {
                dashJumping = true;
                velocity.x *= 1.5f; // 속도 배로 곱해줌 
            }
            //if(_movement._pressDown)
            int LastInput = CircularInputBuffer.instance.Head == 0 ? CircularInputBuffer.instance.GetBuffer().Length - 1 : CircularInputBuffer.instance.Head - 1;
            int LastInputBefore = LastInput == 0 ? CircularInputBuffer.instance.GetBuffer().Length - 1 : LastInput - 1;
            if (CircularInputBuffer.instance.GetBuffer()[LastInputBefore] != null && CircularInputBuffer.instance.GetBuffer()[LastInputBefore].Item1 == new Vector2(0f, -1f)) // 아래 키 입력
            {
                if (CircularInputBuffer.instance.GetBuffer()[LastInput].Item1 == new Vector2(1f, 0) ||
                    CircularInputBuffer.instance.GetBuffer()[LastInput].Item1 == new Vector2(-1f, 0))
                {
                    velocity.x *= 5f; // 속도 배로 곱해줌 
                }
            }
            Debug.Log("Jump Speed Plus : " + jumpSpeed.ToString());

            velocity.y += jumpSpeed;
            currentlyJumping = true;

            // juice script initiatedwa

            //
        }

        if (jumpBuffer == 0) // no jumpbuffer, then turn off desiredjump immediately
            desiredJump = false;
    }

    private void calculateGravity()
    {
        // 위를 향하는 속도의 벡터 + 현재 대쉬 상태 아님
        if (_body.velocity.y > 0.01f)
        {
            if(onGround) // 
            {
                gravMultiplier = defaultGravityScale;
            }
            else
            {
                if (variablejumpHeight) // using feature that when player release the button, character starts to fall
                {
                    if (pressingJump || (pressingJump && currentlyJumping))
                        gravMultiplier = upwardMovementMultiplier;
                    else
                    {
                        if (dashJumping) // 대쉬 중에 점프했다면
                            gravMultiplier = upwardMovementMultiplier;
                        else
                            gravMultiplier = jumpCutOff;
                    }
                }
                else
                    gravMultiplier = upwardMovementMultiplier;
            }
        }
        else if(_body.velocity.y < -0.01f)
        {
            if(onGround)
            {
                gravMultiplier = defaultGravityScale;
            }
            else
            {
                if (dashJumping) // 대쉬 중에 점프했다면
                    gravMultiplier = upwardMovementMultiplier; // 빠른 낙하 방지 (일단은)
                else
                    gravMultiplier = downwardMovementMultiplier;
            }
        }
        else // not moving vertically
        {
            if (onGround)
                currentlyJumping = false;

            gravMultiplier = defaultGravityScale;
        }

        _body.velocity = new Vector2(_body.velocity.x, Mathf.Clamp(_body.velocity.y, -speedLimit, 100));

        if (_body.velocity.y > -0.001f && _body.velocity.y < 0.001f)
        {
            Debug.Log("current y pos : " + _body.transform.position.y.ToString());
            if(minHeight > _body.transform.position.y)
                minHeight = _body.transform.position.y;
            if(maxHeight < _body.transform.position.y)
                maxHeight = _body.transform.position.y;

            GameObject.Find("Canvas").transform.Find("DeltaHeight").GetComponent<TextMeshProUGUI>().text = "Delta Height : " + (maxHeight - minHeight).ToString();
        }
    }

    public void OnValueChanged(Single s)
    {
        GameObject.Find("Canvas").transform.Find("JumpSlider").transform.Find("JumpHeight").GetComponent<TextMeshProUGUI>().text = "Jump Height : " + jumpHeight.ToString();
        jumpHeight = s;
    }
}
