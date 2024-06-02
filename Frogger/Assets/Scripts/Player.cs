using System.Numerics;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Color = UnityEngine.Color;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;


public class Player : MonoBehaviour
{
    [SerializeField] private bool useController = false;
    [SerializeField] private float jumpHeightMin = 7.5f;
    [SerializeField] private float jumpHeightMax = 20f;    
    [SerializeField] private float moveSpeedMin = 7.5f;
    [SerializeField] private float moveSpeedMax = 20f;
    [SerializeField] private float timeUntilMax = 1f;
    [SerializeField] private Transform visual;
    [SerializeField] private Slider sensSlider;
    [SerializeField] private LayerMask slipperyLayerMask;
    [Header("Controller Settings")]
    [SerializeField, Range(0.001f, 0.1f)] private float sensitivity;
    [SerializeField, Range(0f, 1f)] private float deadzone = 0.4f;
    [Header("Swinging")]
    [SerializeField] private GameObject targetSwingObject;
    [SerializeField] private Transform swingPoint;
    [SerializeField] private float radius;
    [SerializeField] private float swingSpeedMult = 1.0005f;
    [SerializeField] private LineRenderer swingLine;
    [SerializeField] private float tongueLength = 10f;
    [SerializeField] private Transform dot;
    [Header("Ground Check")]
    [SerializeField] private LayerMask groundLayerMask;
    [Range(0.1f, 5.0f), SerializeField] private float groundRayLength = 0.25f;
    [Range(0.1f, 5.0f), SerializeField] private float groundRayOffset = 0.5f;
    [Range(-5.0f, 5.0f), SerializeField] private float groundRayXOffset = 0.5f;
    [Range(0.01f, 1f), SerializeField] private float groundSideRayOffset = 0.5f;
    [Header("Wall Check")]
    [SerializeField] private LayerMask wallLayerMask;
    [Range(0.1f, 5.0f), SerializeField] private float wallRayLength = 0.25f;
    [Range(0.1f, 5.0f), SerializeField] private float wallRayOffset = 0.5f;
    [Range(-5.0f, 5.0f), SerializeField] private float wallRayXOffset = 0.5f;
    [Range(0.01f, 0.5f), SerializeField] private float wallSideRayOffset = 0.5f;
    
    private InputManager _inputManager;
    private SpriteAnimator _spriteAnimator;
    private DistanceJoint2D _distanceJoint;
    private RaySensor _raySensor;
    private Rigidbody2D _rb;
    private bool _isCharging;
    private float _chargeTime;
    private bool _isGrounded;
    private bool _isTouchingLeftWall;
    private bool _isTouchingRightWall;
    private bool _isTouchingUpWall;
    private int _direction = 1;
    private bool _canSwing;
    private int _currentlyActiveSwingPoint;
    private bool _isSwinging;
    private bool _isSticking;
    private float _stickingSurfaceAngle;
    private bool _isUpsideDown;
    private Vector2 _swingDirection;
    private bool _falseSwing;

    private void Awake()
    {
        _inputManager = GetComponent<InputManager>();
        _spriteAnimator = GetComponentInChildren<SpriteAnimator>();
        _distanceJoint = GetComponent<DistanceJoint2D>();
        _raySensor = GetComponent<RaySensor>();
        _rb = GetComponent<Rigidbody2D>();
        _inputManager.OnJump += OnJump;
        _inputManager.OnSwing += OnSwing;
    }

    private void Update()
    {
        sensitivity = sensSlider.value;
        EventSystem.current.SetSelectedGameObject(null);

        if (Keyboard.current.rKey.wasPressedThisFrame)
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        
        GroundCheck();
        WallCheck();
        
        UpdateSwinging();
        UpdateCharging();

        _isUpsideDown = _isSticking && Mathf.Abs(_stickingSurfaceAngle) > 90f;

        if (!_isSticking || _isUpsideDown)
        {
            if (useController)
            {
                if (Gamepad.current.leftStick.value.x != 0)
                {
                    if (Gamepad.current.leftStick.value.x < -deadzone)
                    {
                        _spriteAnimator.FlipX(!_isUpsideDown);
                        _direction = -1;
                    }
                    else if (Gamepad.current.leftStick.value.x > deadzone)
                    {
                        _spriteAnimator.FlipX(_isUpsideDown);
                        _direction = 1;
                    }
                }
            }
            else
            {
                if (Camera.main.ScreenToWorldPoint(Mouse.current.position.value).x < transform.position.x)
                {
                    _spriteAnimator.FlipX(!_isUpsideDown);
                    _direction = -1;
                }
                else
                {
                    _spriteAnimator.FlipX(_isUpsideDown);
                    _direction = 1;
                }
            }
        }
        else if (_isSticking && !(Mathf.Abs(_stickingSurfaceAngle) < 90f))
        {
            if (_stickingSurfaceAngle < 0)
                _direction = 1;
            else
                _direction = -1;
        }

        if (!_isGrounded && !_isTouchingRightWall && !_isTouchingLeftWall && !_isTouchingUpWall)
            UnStickToWall();
        if(_spriteAnimator.GetCurrentAnimation() == "Idle" && _isSwinging)
            _spriteAnimator.SwitchAnimation("Idle Mouth", true);
        if(_spriteAnimator.GetCurrentAnimation() == "Idle Mouth" && !_isSwinging)
            _spriteAnimator.SwitchAnimation("Idle", true);
        if(_spriteAnimator.GetCurrentAnimation() == "Jump" && _isSwinging)
            _spriteAnimator.SwitchAnimation("Jump Mouth", true);
        if(_spriteAnimator.GetCurrentAnimation() == "Jump Mouth" && !_isSwinging)
            _spriteAnimator.SwitchAnimation("Jump", true);
    }
    
    #region Swing

    private void OnSwing(bool startedSwinging)
    {
        if(_falseSwing || _isCharging)
            return;
        if(startedSwinging)
            StartSwing();
        else
            StopSwing();
    }
    
    private void UpdateSwinging()
    {
        _canSwing = CanSwing();
        _falseSwing = _isSwinging && !_distanceJoint.enabled;
        if (useController)
        {
            var horizontal = Gamepad.current.leftStick.x.ReadValue();
            var vertical = Gamepad.current.leftStick.y.ReadValue();
            var newDirection = new Vector2(horizontal, vertical);
            _swingDirection = Vector2.Lerp(_swingDirection, newDirection, sensitivity).normalized;
        }
        else
        {
            var pos = Camera.main.ScreenToWorldPoint(Input.mousePosition).IgnoreZ();
            _swingDirection = (pos - transform.position).normalized;
        }
        
        if(_isSwinging)
            Swing();
        if (useController)
        {
            dot.gameObject.SetActive(true);
            
            var hit = _raySensor.CastHit(tongueLength, 0f, 0f, groundLayerMask, _swingDirection);
            if(_canSwing)
                dot.position = hit.point;
            else
                dot.position = transform.position + (Vector3) _swingDirection * tongueLength;
        }
        else
            dot.gameObject.SetActive(false);
    }
    
    private void StartSwing()
    {
        var swingPointInfo = GetSwingPointInfo();
        swingLine.SetPosition(0, swingPoint.position);
        swingLine.SetPosition(1, swingPoint.position);
        if (_canSwing)
        {
            targetSwingObject.transform.position = swingPointInfo.Item2;
            _distanceJoint.enabled = true;
            _rb.drag = 0.25f;
            Invoke(nameof(StopSwing), 5f);
            swingLine.DoSetPosition(1, targetSwingObject.transform.position, 0.3f).OnComplete(() =>
            {
                _isSwinging = true; 
            });
        }
        else
        {
            targetSwingObject.transform.position = transform.position + swingPointInfo.Item1 * tongueLength;
            swingLine.DoSetPosition(1, targetSwingObject.transform.position, 0.3f).OnComplete(() =>
            {
                swingLine.DoSetPosition(1, swingPoint.position, 0.3f).OnComplete(() =>
                {
                    _isSwinging = false;
                    swingLine.enabled = false;
                });
            });
            _isSwinging = true; 
        }
        swingLine.enabled = true;
    }
    
    private void Swing()
    {
        swingLine.enabled = true;
        _rb.velocity *= swingSpeedMult;
        swingLine.SetPosition(0, swingPoint.position);
    }
    
    private void StopSwing()
    {
        CancelInvoke(nameof(StopSwing));
        swingLine.DoSetPosition(1, swingPoint.position, 0.3f).OnUpdate(() =>
        {
            
        }).OnComplete(() =>
        {
            swingLine.enabled = false;
        });
        _isSwinging = false;
        _distanceJoint.enabled = false;
        _rb.drag = 0;
    }
    
    private bool CanSwing()
    {
        var hit = _raySensor.CastHit(tongueLength, 0f, 0f, groundLayerMask, _swingDirection);
        return hit.collider != null;
    }

    private (Vector3, Vector3) GetSwingPointInfo()
    {
        var hit = _raySensor.CastHit(tongueLength, 0f, 0f, groundLayerMask, _swingDirection);
        return (_swingDirection, hit.point);
    }

    #endregion

    #region Jumping
    private void UpdateCharging()
    {
        if (_isCharging && _chargeTime < timeUntilMax)
        {
            _chargeTime += Time.deltaTime;
            _chargeTime = Mathf.Clamp(_chargeTime, 0f, timeUntilMax);
        }
    }
    
    private void OnJump(bool startedJumping)
    {
        if(!_isGrounded && !_isTouchingRightWall && !_isTouchingLeftWall && !_isTouchingUpWall)
            return;
        if(_isSwinging)
            return;
        if(startedJumping)
            StartCharge();
        else
            StopCharge();
    }

    public void StartCharge()
    {
        _isCharging = true;
        _chargeTime = 0f;
        _spriteAnimator.SwitchAnimation("Charge");
    }
    
    public void StopCharge()
    {
        if(!_isCharging)
            return;
        _isCharging = false;
        _spriteAnimator.SwitchAnimation("Jump");
        if(!_isUpsideDown)
            _rb.AddForceY(Mathf.Lerp(jumpHeightMin, jumpHeightMax, _chargeTime / timeUntilMax) * 10000);
        else
            _rb.AddForceY(Mathf.Lerp(jumpHeightMin, jumpHeightMax, _chargeTime / timeUntilMax) * -10000);
        _rb.AddForceX(Mathf.Lerp(moveSpeedMin, moveSpeedMax, _chargeTime / timeUntilMax) * _direction * 10000);
        _chargeTime = 0f;
    }
    #endregion
    
    private void GroundCheck() 
    {
        var grounded = _raySensor.Cast(groundRayLength, groundRayOffset, groundRayXOffset, groundSideRayOffset, groundLayerMask, Vector3.down);
        if (grounded && !_isGrounded)
            _spriteAnimator.SwitchAnimation("Land");
        _isGrounded = grounded;
    }

    private void WallCheck()
    {
        var leftWall = _raySensor.CastAll(wallRayLength, wallRayOffset, wallRayXOffset, wallSideRayOffset, wallLayerMask, Vector3.left);
        if (leftWall && !_isTouchingLeftWall)
        {
            var hit = _raySensor.CastHit(wallRayLength, wallRayOffset, wallRayXOffset, wallLayerMask, Vector3.left);
            var slipperyHit = _raySensor.CastHit(wallRayLength, wallRayOffset, wallRayXOffset, slipperyLayerMask, Vector3.left);
            StickToWall(-90f, hit.point, slipperyHit.collider != null);
        }
        _isTouchingLeftWall = leftWall;
        var rightWall = _raySensor.CastAll(wallRayLength, wallRayOffset, wallRayXOffset, wallSideRayOffset, wallLayerMask, Vector3.right);
        if (rightWall && !_isTouchingRightWall)
        {
            var hit = _raySensor.CastHit(wallRayLength, wallRayOffset, wallRayXOffset, wallLayerMask, Vector3.right);
            var slipperyHit = _raySensor.CastHit(wallRayLength, wallRayOffset, wallRayXOffset, slipperyLayerMask, Vector3.right);
            StickToWall(90f, hit.point, slipperyHit.collider != null);
        }
        _isTouchingRightWall = rightWall;
        var upWall = _raySensor.CastAll(wallRayLength, wallRayOffset, wallRayXOffset, wallSideRayOffset, wallLayerMask, Vector3.up);
        if (upWall && !_isTouchingUpWall)
        {
            var hit = _raySensor.CastHit(wallRayLength, wallRayOffset, wallRayXOffset, wallLayerMask, Vector3.up);
            var slipperyHit = _raySensor.CastHit(wallRayLength, wallRayOffset, wallRayXOffset, slipperyLayerMask, Vector3.up);
            StickToWall(180f, hit.point, slipperyHit.collider != null);
        }
        _isTouchingUpWall = upWall;
    }

    private void StickToWall(float surfaceAngle, Vector2 snapPoint, bool slippery)
    {
        if(_falseSwing)
            StopSwing();
        if(_isSticking)
            return;
        _spriteAnimator.SwitchAnimation("Land");
        if(!slippery)
            _rb.gravityScale = 0;
        visual.rotation = Quaternion.Euler(new Vector3(0f, 0f, surfaceAngle));
        transform.position = snapPoint;
        _rb.velocity = Vector2.zero;
        _stickingSurfaceAngle = surfaceAngle;
        _isSticking = true;
    }

    private void UnStickToWall()
    {
        _rb.gravityScale = 1;
        visual.rotation = Quaternion.Euler(Vector3.zero);
        _isSticking = false;
    }
    
    private void OnDrawGizmos() 
    {
        if (!_raySensor)
            _raySensor = GetComponent<RaySensor>();
        
        _raySensor.CastGizmos(Color.cyan, groundRayLength, groundRayOffset, groundRayXOffset, groundSideRayOffset, Vector3.down);
        _raySensor.CastGizmos(Color.red, wallRayLength, wallRayOffset, wallRayXOffset, wallSideRayOffset, Vector3.right);
        _raySensor.CastGizmos(Color.red, wallRayLength, wallRayOffset, wallRayXOffset, wallSideRayOffset, Vector3.left);
        _raySensor.CastGizmos(Color.red, wallRayLength, wallRayOffset, wallRayXOffset, wallSideRayOffset, Vector3.up);
        var hit = _raySensor.CastHit(tongueLength, 0f, 0f, groundLayerMask, _swingDirection);
        if(CanSwing())
            _raySensor.CastGizmo(Color.green, hit.distance, 0f, 0f, _swingDirection);
        else
            _raySensor.CastGizmo(Color.green, tongueLength, 0f, 0f, _swingDirection);
    }
}
