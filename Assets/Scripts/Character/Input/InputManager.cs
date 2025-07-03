using System;
using UnityEngine;
using UnityEngine.InputSystem;
using R3;

public class InputManager : MonoBehaviour, GameInput.IPlayerActions
{

    private GameInput _gameInput;
    private GameInput.PlayerActions _playerActions;

    private Vector2 _currentMovementInput = Vector2.zero;
    private bool _jumpPressed = false;
    private bool _interactPressed = false;

    private PlayerNetworkInput _networkInput = new PlayerNetworkInput();
    public PlayerNetworkInput NetworkInput => _networkInput;

    private void Awake()
    {
        _gameInput = new GameInput();
        _playerActions = _gameInput.Player;

        _playerActions.SetCallbacks(this);
    }

    private void OnEnable()
    {
        _gameInput?.Enable();
    }
    private void OnDisable()
    {
        _gameInput?.Disable();
    }

    private void OnDestroy()
    {
        if (_playerActions.enabled)
        {
            _playerActions.RemoveCallbacks(this);
        }
        _gameInput?.Dispose();
    }

    /// <summary>
    /// ネットワーク入力構造体を更新し、変更を通知
    /// </summary>
    public void UpdateNetWorkInput()
    {
        // 既存の構造体のフィールドを直接更新
        _networkInput.MovementInput = _currentMovementInput; // 移動方向（正規化済み）
        _networkInput.JumpPressed.Set(MyButtons.Jump, _jumpPressed); // ジャンプボタンが押されたか
        _networkInput.InteractPressed.Set(MyButtons.Interact, _interactPressed); // インタラクトボタンが押されたか
    }

    /// <summary>
    /// ボタン系入力をリセット（1フレームのみ有効にするため）
    /// </summary>
    public void ResetButtonInputs()
    {
        _jumpPressed = false;
        _interactPressed = false;
    }

    #region Input Action Callbacks

    /// <summary>移動入力コールバック（WASD、左スティック）</summary>
    public void OnMove(InputAction.CallbackContext context)
    {
        _currentMovementInput = context.ReadValue<Vector2>();
    }

    /// <summary>ジャンプ入力コールバック（スペース、Aボタン）</summary>
    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            _jumpPressed = true;
        }
    }

    /// <summary>インタラクト入力コールバック（F、Xボタン）</summary>
    public void OnInteract(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            _interactPressed = true;
        }
    }

    #endregion

    /// <summary>
    /// 外部からネットワーク入力を取得するためのメソッド
    /// Photon Fusionから呼び出される
    /// </summary>
    /// <returns>現在の入力状態</returns>
    public PlayerNetworkInput GetPlayerInput()
    {
        return _networkInput;
    }
}
