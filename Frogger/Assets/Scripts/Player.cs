using System;
using UnityEngine;
using UnityEngine.InputSystem;


public class Player : MonoBehaviour
{
    [SerializeField] private float jumpHeightMin = 7.5f;
    [SerializeField] private float jumpHeightMax = 20f;
    [SerializeField] private float timeUntilMax = 1f;
    [SerializeField] private float moveSpeed = 10f;
    private InputManager _inputManager;
    private SpriteAnimator _spriteAnimator;
    private Rigidbody2D _rb;
    private bool _isCharging;
    private float _chargeTime;
    private bool _wasGrounded;
    private bool _isGrounded;
    private int _direction = 1;
    
    private void Awake()
    {
        _inputManager = GetComponent<InputManager>();
        _spriteAnimator = GetComponent<SpriteAnimator>();
        _rb = GetComponent<Rigidbody2D>();
        _inputManager.OnJump += OnJump;
    }

    private void Update()
    {
        _isGrounded = _rb.IsTouchingLayers();
        if (_isGrounded && !_wasGrounded)
            _spriteAnimator.SwitchAnimation("Land");
        _wasGrounded = _isGrounded;
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
        _rb.AddForceY(Mathf.Lerp(jumpHeightMin, jumpHeightMax, _chargeTime / timeUntilMax) * 100, ForceMode2D.Impulse);
        _rb.AddForceX(Mathf.Lerp(jumpHeightMin, jumpHeightMax, _chargeTime / timeUntilMax) * moveSpeed * _direction * 100, ForceMode2D.Impulse);
        _chargeTime = 0f;
    }
}
