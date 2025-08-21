using System;
using Fusion;
using R3;
using UnityEngine;

public enum MyButtons
{
    Jump = 0,
    Interact = 1,
    Run = 2,
    Attack = 3,
    Drop = 4,
}

public struct PlayerNetworkInput : INetworkInput
{
    /// <summary>移動入力（WASD, 左スティック）</summary>
    public Vector2 MoveInput;

    /// <summary>移動方向（正規化済み）</summary>
    public Vector3 MoveDirection;

    //// <summary>カメラの方向（Y軸回転のみを考慮）</summary>
    public Vector3 CameraForwardDirection;

    ///// <summary>カメラ回転入力（マウス, 右スティック）</summary>
    public Vector2 LookInput;

    ///// <summary>攻撃入力（左クリック, RTトリガー）</summary>
    public NetworkButtons AttackPressed;

    ///// <summary>インタラクト入力（E, Xボタン）</summary>
    public NetworkButtons InteractPressed;

    ///// <summary>ドロップ入力（Q, Bボタン）</summary>
    public NetworkButtons DropPressed;

    ///// <summary>走る入力（Shift, 左スティッククリック）</summary>
    public NetworkButtons RunPressed;

    ///// <summary>
    ///// ジャンプ入力（スペース, Aボタン）
    ///// </summary>
    public NetworkButtons JumpPressed;
}
