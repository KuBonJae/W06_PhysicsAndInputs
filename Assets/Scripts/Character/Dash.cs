using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Dash : MonoBehaviour
{
    [SerializeField]
    int dashCount = 1;
    [SerializeField]
    int curDashCount = 1;
    [SerializeField]
    bool desiredDash = false;
    public bool currentDashing = false;
    public bool DashState = false;
    [SerializeField]
    public float maintainDashState = 0f;
    [SerializeField]
    float dashSpeed;
    [SerializeField]
    float inputDir;
    [SerializeField]
    float maxDashDistance = 6.0f;
    [SerializeField]
    float MinDashState = 0.18f;
    [SerializeField]
    float MaxDashState = 0.3f;
    [SerializeField]
    float accelMultiplier = 1f;
    [SerializeField]
    float gravMultiplier = 1f;

    private Rigidbody2D _body;
    private OnGround _ground;
    private Movement _movement;
    private Jump _jump;

    private float _dashDecceleration;
    public Vector2 _dashDecelVector;

    public Coroutine dashCoroutine;

    private PlayersInputs _playersInputs;

    private Climb _climb;

    bool isFirst = true;
    float time = 0;

    private void Awake()
    {
        _playersInputs = new PlayersInputs();
        _playersInputs.Enable();

        _body = GetComponent<Rigidbody2D>();
        _ground = GetComponent<OnGround>();
        _movement = GetComponent<Movement>();
        _jump = GetComponent<Jump>();
        _climb = GetComponent<Climb>();

        time = Time.time;
    }

    private void OnEnable()
    {
        _playersInputs.Player.Dash.started += OnDash;
        _playersInputs.Player.Dash.performed += OnDash;
        _playersInputs.Player.Dash.canceled += OnDash;
    }

    private void OnDisable()
    {
        _playersInputs.Player.Dash.started -= OnDash;
        _playersInputs.Player.Dash.performed -= OnDash;
        _playersInputs.Player.Dash.canceled -= OnDash;
    }

    public void OnDash(InputAction.CallbackContext context)
    {
        if (curDashCount > 0)
        {
            desiredDash = context.ReadValueAsButton();
            if(currentDashing)
            {
                desiredDash = false;
            }
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (desiredDash)
        {
            resetVelocity();
            calculateForce();
            return;
        }

        if (_ground.GetonGround() && curDashCount == 0)
        {
            curDashCount = dashCount; // dashcount reset
        }

        
    }

    private void Update()
    {
        float currentTime = 0;

        currentTime = Time.time;
        //if(Time.deltaTime == currentTime - time)
            //Debug.Log("update period : " + (currentTime - time).ToString());
            Debug.Log("update period : " + (Time.fixedDeltaTime).ToString());
        time = currentTime;
        
    }

    private void resetVelocity()
    {
        _body.velocity = Vector2.zero;
        _body.angularVelocity = 0f;
        _body.totalForce = Vector2.zero;
    }

    private void calculateForce()
    { 
        inputDir = _movement.direction;
        if (inputDir < 0.001f && inputDir > -0.001f) // no directional input
        {
            inputDir = _movement.lookDirection;
            if (_movement._pressDown || _movement._pressUp)
                inputDir = 0f;
        }

        Vector2 forceDirection = new Vector2(inputDir, 0f);
        
        //if(inputDir != 0)
        {
            desiredDash = false;
            dashSpeed = Mathf.Sqrt(-2 * Physics2D.gravity.y * gravMultiplier * maxDashDistance) * accelMultiplier;

            if(_movement._pressUp && _movement._pressDown)
            {
                // ignore pressing
            }
            else
            {
                if(_movement._pressUp)
                {
                    if(forceDirection.x == 0f)
                        forceDirection = new Vector2(0f, 1f);
                    else
                    // x^2 = x^2 * 1/2 + x^2 * 1/2;
                    forceDirection = new Vector2(forceDirection.x > 0f? Mathf.Sqrt(forceDirection.x * forceDirection.x * 0.5f) : (-1f) * Mathf.Sqrt(forceDirection.x * forceDirection.x * 0.5f)
                        , Mathf.Sqrt(forceDirection.x * forceDirection.x * 0.5f));
                }
                else if(_movement._pressDown)
                {
                    if(forceDirection.x == 0f)
                        forceDirection = new Vector2(0f, 1f);
                    else
                    forceDirection = new Vector2(forceDirection.x > 0f ? Mathf.Sqrt(forceDirection.x * forceDirection.x * 0.5f) : (-1f) * Mathf.Sqrt(forceDirection.x * forceDirection.x * 0.5f)
                        , (-1f) * Mathf.Sqrt(forceDirection.x * forceDirection.x * 0.5f));
                }
            }

            if(_ground.GetonGround() && !_movement._pressDown && !_movement._pressUp) // 바닥에서 슬라이딩 처럼 쓰는 대쉬
            {
                // 중력 건드리지 않음 -> 점프 연계용
            }
            else
                Physics2D.gravity = new Vector2(0f, 0f);

            //inputDir = inputDir * dashSpeed;
            //_dashDecceleration = (inputDir - (inputDir > 0 ? _movement.maxSpeed : (-1f) * _movement.maxSpeed)) / MinDashState;
            //_dashDecelVector = new Vector2((-1) * _dashDecceleration, 0f); // MaxDashState 가 끝나는 시점에 정확하게 MaxSpeed가 됨

            forceDirection *= dashSpeed;
            if (forceDirection.x == 0f)
                _dashDecelVector = new Vector2(0f, (forceDirection.y > 0 ? forceDirection.y - _movement.maxSpeed / 2f : forceDirection.y + _movement.maxSpeed / 2f) / MinDashState);
            else
            _dashDecelVector = new Vector2((forceDirection.x > 0? forceDirection.x - _movement.maxSpeed / 2f : forceDirection.x + _movement.maxSpeed / 2f) / MinDashState
                , (forceDirection.y > 0 ? forceDirection.y - _movement.maxSpeed / 2f : forceDirection.y + _movement.maxSpeed / 2f) / MinDashState);

            //Vector2 v = _body.velocity;
            //v.x = inputDir;
            //_body.velocity = v;
            _body.velocity = forceDirection;

            Debug.Log("Dash Body Velocity : " + _body.velocity.x.ToString() + " / " + _body.velocity.y.ToString());
            Debug.Log("Dash Body Decel_Velocity : " + _dashDecelVector.x.ToString() + " / " + _dashDecelVector.y.ToString());

            currentDashing = true;
            DashState = true;
            _jump.variablejumpHeight = false;
            curDashCount--;

            dashCoroutine = StartCoroutine(DashStateManaging());
        }
    }

    IEnumerator DashStateManaging()
    {
        float currentTime;
        float time = Time.time;
        while(currentDashing)
        {
            //maintainDashState += Time.deltaTime;
            //maintainDashState += 0.01f;

            currentTime = Time.time;
            maintainDashState += currentTime - time;
            
            if(_climb.GetonWall()) // 벽에 부딪히면, 바로 대쉬 종료
            {
                DashState = false;
                currentDashing = false;
                _jump.variablejumpHeight = true;
                Physics2D.gravity = new Vector2(0f, -9.81f);
                maintainDashState = 0f;
                _jump.dashJumping = false;
                yield return new WaitForSecondsRealtime(0.01f);
            }

            if (maintainDashState < MinDashState)
            {
                
            }
            else
            {
                DashState = false;

                if (maintainDashState < MaxDashState)
                {
                    //if (_jump.dashJumping)
                        _body.velocity -= _dashDecelVector * 0.01f;
                    //else
                    //    _body.velocity -= _dashDecelVector * 0.01f;
                }
                else
                {
                    currentDashing = false;
                    _jump.variablejumpHeight = true;
                    Physics2D.gravity = new Vector2(0f, -9.81f);
                    maintainDashState = 0f;
                    _jump.dashJumping = false;
                }
            }

            time = currentTime;

            yield return new WaitForSecondsRealtime(0.01f);
        }
    }
}
