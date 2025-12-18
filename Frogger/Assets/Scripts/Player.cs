using System;
using System.Numerics;
using DG.Tweening;
using Unity.Jobs;
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
    //[SerializeField] private float slipSpeed = 7f;
    [SerializeField] private PhysicsMaterial2D groundMat;
    [SerializeField] private PhysicsMaterial2D slipMat;
    [SerializeField] private Transform visual;
    [SerializeField] private Slider sensSlider;
    [SerializeField] private LayerMask slipperyLayerMask;
    [SerializeField] private LayerMask slimeLayerMask;
    [SerializeField] private LayerMask tongueStickLayerMask;
    [Header("Controller Settings")]
    [SerializeField, Range(0.001f, 0.1f)] private float sensitivity;
    [SerializeField, Range(0f, 1f)] private float deadzone = 0.4f;
    [Header("Swinging")]
    [SerializeField] private GameObject targetSwingObject;
    [SerializeField] private float radius;
    [SerializeField] private float swingSpeedMult = 1.0005f;
    [SerializeField] private Transform tongueEnd;
    [SerializeField] private Transform tongueStart;
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
    private Tween _tongueExtendTween;
    private bool _isCharging;
    private float _chargeTime;
    private bool _isGrounded;
    private bool _isTouchingLeftWall;
    private bool _isTouchingRightWall;
    private bool _isTouchingUpWall;
    private int _direction = 1;
    private bool _canSwing;
    private int _currentlyActiveSwingPoint;
    private bool _isSticking;
    private float _stickingSurfaceAngle;
    private bool _isUpsideDown;
    private Vector2 _swingDirection;
    private bool _falseSwing;
    private bool _isOnSlippery;
    private bool _isOnSlimey;
    private bool _isTongueOut;

    private void Awake()
    {
        _inputManager = GetComponent<InputManager>();
        _spriteAnimator = GetComponentInChildren<SpriteAnimator>();
        _distanceJoint = GetComponent<DistanceJoint2D>();
        _raySensor = GetComponent<RaySensor>();
        _rb = GetComponent<Rigidbody2D>();
        _inputManager.OnJump += OnJump;
        _inputManager.OnSwing += OnSwing;
        RetractTongue();
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

        if (_isOnSlippery)
        {
            _rb.gravityScale = 1.5f;
            _rb.sharedMaterial = slipMat;
        }
        else
        {
            _rb.sharedMaterial = groundMat;
        }

        if (_isSticking)
        {
            _isGrounded = false;
        }

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
        else if (_isSticking && Mathf.Abs(_stickingSurfaceAngle) >= 90f)
        {
            if (_stickingSurfaceAngle < 0)
                _direction = 1;
            else
                _direction = -1;
        }

        if (!_isOnSlimey && !_isOnSlippery && _isSticking)
        {
            _rb.gravityScale = 0f;
        }

        if (!_isGrounded && !_isTouchingRightWall && !_isTouchingLeftWall && !_isTouchingUpWall)
            UnStickToWall();
        if(_spriteAnimator.GetCurrentAnimation() == "Idle" && _isTongueOut)
            _spriteAnimator.SwitchAnimation("Idle Mouth", true);
        if(_spriteAnimator.GetCurrentAnimation() == "Idle Mouth" && !_isTongueOut)
            _spriteAnimator.SwitchAnimation("Idle", true);
        if(_spriteAnimator.GetCurrentAnimation() == "Jump" && _isTongueOut)
            _spriteAnimator.SwitchAnimation("Jump Mouth", true);
        if(_spriteAnimator.GetCurrentAnimation() == "Jump Mouth" && !_isTongueOut)
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
        _falseSwing = _isTongueOut && !_distanceJoint.enabled;
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
        
        if(_isTongueOut)
            Swing();
        else
        {
            tongueEnd.position = tongueStart.position;
        }
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
        if(_isTongueOut || _isTongueOut)
            return;
        var info = GetSwingPointInfo();
        if (_canSwing)
        {
            targetSwingObject.transform.position = info.point;
            var isSlime = ((1 << info.transform.gameObject.layer) & slimeLayerMask) != 0;
            if (isSlime)
            {
                ExtendTongue(targetSwingObject.transform.position, () => RetractTongue());
            }
            else
            {
                _distanceJoint.enabled = true;
                _rb.drag = 0.25f;
                Invoke(nameof(StopSwing), 5f);
                ExtendTongue(targetSwingObject.transform.position);
            }
        }
        else
        {
            targetSwingObject.transform.position = transform.position + (Vector3) _swingDirection * tongueLength;
            ExtendTongue(targetSwingObject.transform.position, () => RetractTongue());
        }
    }
    
    private void Swing()
    {
        _rb.velocity *= swingSpeedMult;
        if (!_tongueExtendTween.active)
            tongueEnd.position = targetSwingObject.transform.position;
        tongueStart.LookAt(targetSwingObject.transform);
    }
    
    private void StopSwing()
    {
        CancelInvoke(nameof(StopSwing));
        RetractTongue();
        _distanceJoint.enabled = false;
        _rb.drag = 0;
    }

    private void ExtendTongue(Vector3 to, TweenCallback onComplete = null)
    {
        _isTongueOut = true;
        _tongueExtendTween = tongueEnd.DOMove(to, 0.3f).OnComplete(onComplete);
    }

    private void RetractTongue(TweenCallback onComplete = null)
    {
        tongueEnd.DOMove(tongueStart.position, 0.3f).OnComplete(() =>
        {
            _isTongueOut = false;
            onComplete?.Invoke();
        });
    }
    
    private bool CanSwing()
    {
        var hit = _raySensor.CastHit(tongueLength, 0f, 0f, tongueStickLayerMask, _swingDirection);
        return hit.collider != null;
    }

    private RaycastHit2D GetSwingPointInfo()
    {
        var hit = _raySensor.CastHit(tongueLength, 0f, 0f, tongueStickLayerMask, _swingDirection);
        return hit;
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

        if (!_isGrounded && !_isTouchingRightWall && !_isTouchingLeftWall && !_isTouchingUpWall)
        {
            _spriteAnimator.SwitchAnimation("Idle");
            _isCharging = false;
            _chargeTime = 0f;
        }
    }
    
    private void OnJump(bool startedJumping)
    {
        if(!_isGrounded && !_isTouchingRightWall && !_isTouchingLeftWall && !_isTouchingUpWall)
            return;
        if(_isTongueOut)
            return;
        if(startedJumping)
            StartCharge();
        else
            StopCharge();
    }

    private void StartCharge()
    {
        _isCharging = true;
        _chargeTime = 0f;
        _spriteAnimator.SwitchAnimation("Charge");
    }

    private void StopCharge()
    {
        if(!_isCharging)
            return;
        _isCharging = false;
        _spriteAnimator.SwitchAnimation("Jump");
        var jumpForceY = Mathf.Lerp(jumpHeightMin, jumpHeightMax, _chargeTime / timeUntilMax) * 10000;
        var jumpForceX = Mathf.Lerp(moveSpeedMin, moveSpeedMax, _chargeTime / timeUntilMax) * _direction * 10000;
        if (_isOnSlimey)
        {
            jumpForceY /= 1.5f;
            jumpForceX /= 1.5f;
        }
        
        if(!_isUpsideDown)
            _rb.AddForceY(jumpForceY);
        else
            _rb.AddForceY(-jumpForceY);
        _rb.AddForceX(jumpForceX);
        _chargeTime = 0f;
    }
    #endregion
    
    private void GroundCheck() 
    {
        var grounded = _raySensor.Cast(groundRayLength, groundRayOffset, groundRayXOffset, groundSideRayOffset, groundLayerMask, Vector3.down);
        var slipperyHit = _raySensor.Cast(groundRayLength, groundRayOffset, groundRayXOffset, groundSideRayOffset, slipperyLayerMask, Vector3.down);
        _isOnSlimey = _raySensor.Cast(groundRayLength, groundRayOffset, groundRayXOffset, groundSideRayOffset, slimeLayerMask, Vector3.down);
        if (grounded && !_isGrounded && !_isSticking)
            _spriteAnimator.SwitchAnimation("Land");
        if (slipperyHit && !_isOnSlippery)
            _rb.velocityX = Mathf.Max(Mathf.Abs(_rb.velocityX), 6f) * Mathf.Sign(_rb.velocityX);
        
        _isOnSlippery = slipperyHit;
        _isGrounded = grounded;
    }

    private void WallCheck()
    {
        Debug.Log("Performing Slip check...");
        var leftWall = _raySensor.CastAll(wallRayLength, wallRayOffset, wallRayXOffset, wallSideRayOffset, wallLayerMask, Vector3.left);
        if (leftWall)
        {
            var slipperyHit = _raySensor.CastHit(wallRayLength, wallRayOffset, wallRayXOffset, slipperyLayerMask, Vector3.left);
            _isOnSlippery = slipperyHit.transform != null;

            _isOnSlimey = _raySensor.Cast(wallRayLength, wallRayOffset, wallRayXOffset, wallSideRayOffset, slimeLayerMask, Vector3.left);
            
            if (!_isTouchingLeftWall)
            {
                var hit = _raySensor.CastHit(wallRayLength, wallRayOffset, wallRayXOffset, wallLayerMask, Vector3.left);
                StickToWall(-90f, hit.point, _isOnSlippery);
            }
        }
        _isTouchingLeftWall = leftWall;
        
        var rightWall = _raySensor.CastAll(wallRayLength, wallRayOffset, wallRayXOffset, wallSideRayOffset, wallLayerMask, Vector3.right);
        if (rightWall)
        {
            var slipperyHit = _raySensor.CastHit(wallRayLength, wallRayOffset, wallRayXOffset, slipperyLayerMask, Vector3.right);
            _isOnSlippery = slipperyHit.transform != null;
            
            _isOnSlimey = _raySensor.Cast(wallRayLength, wallRayOffset, wallRayXOffset, wallSideRayOffset, slimeLayerMask, Vector3.right);
            
            if (!_isTouchingRightWall)
            {
                var hit = _raySensor.CastHit(wallRayLength, wallRayOffset, wallRayXOffset, wallLayerMask, Vector3.right);
                StickToWall(90f, hit.point, _isOnSlippery);
            }
        }
        _isTouchingRightWall = rightWall;
        
        var upWall = _raySensor.CastAll(wallRayLength, wallRayOffset, wallRayXOffset, wallSideRayOffset, wallLayerMask, Vector3.up);
        if (upWall)
        {
            var slipperyHit = _raySensor.CastHit(wallRayLength, wallRayOffset, wallRayXOffset, slipperyLayerMask, Vector3.up);
            _isOnSlippery = slipperyHit.transform != null;
            
            _isOnSlimey = _raySensor.Cast(wallRayLength, wallRayOffset, wallRayXOffset, wallSideRayOffset, slimeLayerMask, Vector3.up);
            
            if (!_isTouchingUpWall)
            {
                var hit = _raySensor.CastHit(wallRayLength, wallRayOffset, wallRayXOffset, wallLayerMask, Vector3.up);
                StickToWall(180f, hit.point, _isOnSlippery);
            }
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
        if (!_isOnSlimey)
            _rb.gravityScale = 0;
        else
            _rb.gravityScale = 0.1f;    
    
        visual.rotation = Quaternion.Euler(new Vector3(0f, 0f, surfaceAngle));
        transform.position = snapPoint;
        if (!slippery)
        {
            _rb.velocity = Vector2.zero;
        }

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
