using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour, GameInput.IPlayerActions
{
    private GameInput _gameInput;
    private GameInput.PlayerActions _playerActions;

    private Vector2 _currentMovementInput;
    private bool _jumpPressed;
    private bool _interactPressed;

    private void Awake()
    {
        GameInput inputActions = new GameInput();
        _playerActions = inputActions.Player;

        _playerActions.SetCallbacks(this);
    }

    private void OnEnable()
    {
        _gameInput.Enable();
    }
    private void OnDisable()
    {
        _gameInput.Disable();
    }

    private void OnDestroy()
    {
        if (_playerActions.enabled)
        {
            _playerActions.RemoveCallbacks(this);
        }
        _gameInput?.Dispose();
    }

    public PlayerInputManager GetPlayerInput
}
