using System;
using Fusion;
using R3;
using UnityEngine;

public enum MyButtons
{
    Jump = 0,
    Interact = 1,
    Run = 2
}

public struct PlayerNetworkInput : INetworkInput
{
    /// <summary>移動入力（WASD, 左スティック）</summary>
    public Vector2 MovementInput;

    /// <summary>カメラ回転入力（マウス, 右スティック）</summary>
    public Vector2 LookInput;

    /// <summary>攻撃入力（左クリック, RTトリガー）</summary>
    public bool AttackPressed;

    /// <summary>インタラクト入力（E, Xボタン）</summary>
    public bool InteractPressed;

    /// <summary>走る入力（Shift, 左スティッククリック）</summary>
    public NetworkButtons RunPressed;

    /// <summary>
    /// ジャンプ入力（スペース, Aボタン）
    /// </summary>
    public NetworkButtons JumpPressed;

}
