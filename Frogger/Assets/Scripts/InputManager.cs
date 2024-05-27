using System;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    public event Action<bool> OnJump;
    private PlayerControls _player;
    private bool _isJumping;
    
    private void OnEnable()
    {
        if (_player == null)
            _player = new PlayerControls();
        _player.Enable();
        _player.Player.Jump.performed += ctx => OnJump?.Invoke(true);
        _player.Player.Jump.canceled += ctx => OnJump?.Invoke(false);
    }
    
    private void OnDisable()
    {
        _player.Player.Jump.performed -= ctx => OnJump?.Invoke(true);
        _player.Player.Jump.canceled -= ctx => OnJump?.Invoke(false);       
        _player.Disable();
    }
}
