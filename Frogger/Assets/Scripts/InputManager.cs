using System;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    public event Action<bool> OnJump;
    public event Action<bool> OnSwing;
    private PlayerControls _player;
    private bool _isJumping;
    
    private void OnEnable()
    {
        if (_player == null)
            _player = new PlayerControls();
        _player.Enable();
        _player.Player.Jump.performed += ctx => OnJump?.Invoke(true);
        _player.Player.Jump.canceled += ctx => OnJump?.Invoke(false);
        _player.Player.Swing.performed += ctx => OnSwing?.Invoke(true);
        _player.Player.Swing.canceled += ctx => OnSwing?.Invoke(false);
    }
    
    private void OnDisable()
    {
        _player.Player.Jump.performed -= ctx => OnJump?.Invoke(true);
        _player.Player.Jump.canceled -= ctx => OnJump?.Invoke(false); 
        _player.Player.Swing.performed += ctx => OnSwing?.Invoke(true);
        _player.Player.Swing.canceled += ctx => OnSwing?.Invoke(false);
        _player.Disable();
    }
}
