using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;


public class Player : MonoBehaviour
{
    [SerializeField] private float jumpHeightMin = 7.5f;
    [SerializeField] private float jumpHeightMax = 20f;    
    [SerializeField] private float moveSpeedMin = 7.5f;
    [SerializeField] private float moveSpeedMax = 20f;
    [SerializeField] private float timeUntilMax = 1f;
    [Header("Swinging")]
    [SerializeField] private List<GameObject> targetSwingObjects;
    [SerializeField] private float radius;
    [SerializeField] private float swingSpeedMult = 1.0005f;
    [SerializeField] private LineRenderer swingLine;
    [Header("Ground Check")]
    [SerializeField] private LayerMask groundLayerMask;
    [Range(0.1f, 5.0f), SerializeField] private float groundRayLength = 0.25f;
    [Range(0.1f, 5.0f), SerializeField] private float groundRayOffset = 0.5f;
    [Range(-5.0f, 5.0f), SerializeField] private float groundRayXOffset = 0.5f;
    [Range(0.01f, 0.5f), SerializeField] private float groundSideRayOffset = 0.5f;

    
    private InputManager _inputManager;
    private SpriteAnimator _spriteAnimator;
    private DistanceJoint2D _distanceJoint;
    private RaySensor _raySensor;
    private Rigidbody2D _rb;
    private bool _isCharging;
    private float _chargeTime;
    private bool _wasGrounded;
    private bool _isGrounded;
    private int _direction = 1;
    private bool _canSwing;
    private int _currentlyActiveSwingPoint;
    private bool _isSwinging;

    private void Awake()
    {
        _inputManager = GetComponent<InputManager>();
        _spriteAnimator = GetComponent<SpriteAnimator>();
        _distanceJoint = GetComponent<DistanceJoint2D>();
        _raySensor = GetComponent<RaySensor>();
        _rb = GetComponent<Rigidbody2D>();
        _inputManager.OnJump += OnJump;
    }

    private void Update()
    {
        GroundCheck();
        
        for (int i = 0; i < targetSwingObjects.Count; i++)
        {
            if (Vector2.Distance(targetSwingObjects[i].transform.position, transform.position) <= 20f)
            {
                _currentlyActiveSwingPoint = i;
                break;
            }
        }
        
        _canSwing = InRange(targetSwingObjects[_currentlyActiveSwingPoint].transform.position);
        
        if(Input.GetMouseButtonDown(0) && _canSwing)
            StartSwing();
        
        if(Input.GetMouseButton(0) && _canSwing)
            Swing();
        
        if(Input.GetMouseButtonUp(0) && _isSwinging)
            StopSwing();
        
        if (_isCharging && _chargeTime < timeUntilMax)
        {
            _chargeTime += Time.deltaTime;
            _chargeTime = Mathf.Clamp(_chargeTime, 0f, timeUntilMax);
        }

        if (Camera.main.ScreenToWorldPoint(Mouse.current.position.value).x < transform.position.x)
        {
            _spriteAnimator.FlipX(true);
            _direction = -1;
        }
        else
        {
            _spriteAnimator.FlipX(false);
            _direction = 1;
        }
    }
    
    private void StartSwing()
    {
        _isSwinging = true; 
        swingLine.enabled = true;
        _distanceJoint.enabled = true;
        _rb.drag = 0.25f;
        swingLine.SetPosition(1, transform.position);
        swingLine.DoSetPosition(1, targetSwingObjects[_currentlyActiveSwingPoint].transform.position, 0.2f);
        Invoke(nameof(StopSwing), 5f);
    }
    
    private void Swing()
    {
        _rb.velocity *= swingSpeedMult;
        swingLine.SetPosition(0, transform.position);
    }
    
    private void StopSwing()
    {
        CancelInvoke(nameof(StopSwing));
        _isSwinging = false;
        swingLine.enabled = false;
        _distanceJoint.enabled = false;
        _rb.drag = 0;
    }

    private bool InRange(Vector2 target)
    {
        return radius >= Vector2.Distance(transform.position, target) || _isSwinging;
    }


    private void OnJump(bool startedJumping)
    {
        if(!_isGrounded)
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
        _rb.AddForceY(Mathf.Lerp(jumpHeightMin, jumpHeightMax, _chargeTime / timeUntilMax) * 10000, ForceMode2D.Force);
        _rb.AddForceX(Mathf.Lerp(moveSpeedMin, moveSpeedMax, _chargeTime / timeUntilMax) * _direction * 10000, ForceMode2D.Force);
        _chargeTime = 0f;
    }
    
    private void GroundCheck() 
    {
        var grounded = _raySensor.Cast(groundRayLength, groundRayOffset, groundRayXOffset, groundSideRayOffset, groundLayerMask, Vector3.down);
        _isGrounded = grounded;
        if (_isGrounded && !_wasGrounded)
            _spriteAnimator.SwitchAnimation("Land");
        _wasGrounded = _isGrounded;
    }
    private void OnDrawGizmos() 
    {
        if (!_raySensor)
            _raySensor = GetComponent<RaySensor>();
        
        _raySensor.CastGizmos(Color.cyan, groundRayLength, groundRayOffset, groundRayXOffset, groundSideRayOffset, Vector3.down);
    }

}
