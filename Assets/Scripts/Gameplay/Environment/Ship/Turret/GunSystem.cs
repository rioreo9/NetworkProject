using Fusion;

using Unity.Cinemachine;

using UnityEngine;

/// 峯廻制作
/// 艦砲システム - プレイヤーが操縦可能な砲塔システム
/// </summary>
public class GunSystem : BaseInteractControlObject
{

    [Header("Gun System Components")]

    // 砲塔制御コンポーネント
    [SerializeField, Required]
    private GunEmplacementController _gunEmplacementController = default;

    // 砲撃制御コンポーネント
    [SerializeField, Required]
    private GunFireControl _gunFireControl = default;

    // カメラ制御用Cinemachineコンポーネント
    [SerializeField, Required]
    private CinemachineCamera _cinemachineCamera = default;


    // 現在操作しているプレイヤーの状態を保持する変数
    private INoticePlayerInteract _currentPlayerStatus = null;

    /// <summary>
    /// ネットワーク固定更新処理
    /// プレイヤーの入力を受け取り、砲塔の回転と射撃を制御
    /// </summary>
    public override void FixedUpdateNetwork()
    {
        // ネットワーク入力の取得に失敗した場合は処理を終了
        if (!GetInput(out PlayerNetworkInput input))
        {
            return;
        }

        // インタラクトボタンが押された場合は操作を終了
        if (input.InteractPressed.IsSet(MyButtons.Interact))
        {
            ReleseObject(_currentPlayerStatus);
            return;
        }

        // 砲塔の回転処理
        //_gunEmplacementController.DoRotation(input);

        // 射撃処理 - 攻撃ボタンの状態に基づいて発砲
        _gunFireControl.Fire(_gunEmplacementController.transform, input.AttackPressed.IsSet(MyButtons.Attack));
    }

    /// <summary>
    /// プレイヤーが砲塔の操作を開始する際の処理
    /// ネットワーク権限の管理とプレイヤー状態の設定を行う
    /// </summary>
    /// <param name="player">操作するプレイヤーの参照</param>
    /// <param name="status">プレイヤーの状態通知インターフェース</param>
    public override void AccesObject(PlayerRef player, INoticePlayerInteract status)
    {
        // ネットワーク権限の有無で処理を分岐
        if (Object.HasStateAuthority)
        {
            // 権限がある場合は直接設定
            SetPlayerInputForce(player);
        }
        else
        {
            // 権限がない場合はRPCを使用してサーバーに要求
            RPC_RequestOperation(player);
        }

        // プレイヤーに操作中状態を通知
        status.RPC_SetControllerInteracting(true);
        _currentPlayerStatus = status;
        _cinemachineCamera.Priority = 5;
    }

    /// <summary>
    /// プレイヤーが砲塔の操作を終了する際の処理
    /// ネットワーク権限の解放とプレイヤー状態のリセットを行う
    /// </summary>
    /// <param name="status">プレイヤーの状態通知インターフェース</param>
    protected override void ReleseObject(INoticePlayerInteract status)
    {
        // ネットワーク権限の有無で処理を分岐
        if (Object.HasStateAuthority)
        {
            // 権限がある場合は直接解除
            ReleasePlayerInputForce();
        }
        else
        {
            // 権限がない場合はRPCを使用してサーバーに要求
            RPC_RequestRelease();
        }

        // プレイヤーに操作終了状態を通知
        status.RPC_SetControllerInteracting(false);
        _currentPlayerStatus = null;
        _cinemachineCamera.Priority = 0;
    }

    /// <summary>
    /// サーバーに操縦開始を要求するRPC
    /// クライアントから呼び出され、サーバーで実行される
    /// </summary>
    /// <param name="player">操作権限を付与するプレイヤー</param>
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestOperation(PlayerRef player)
    {
        SetPlayerInputForce(player);
    }

    /// <summary>
    /// サーバーに操縦終了を要求するRPC
    /// クライアントから呼び出され、サーバーで実行される
    /// </summary>
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestRelease()
    {
        ReleasePlayerInputForce();
    }

    /// <summary>
    /// 指定プレイヤーに入力権限を設定
    /// サーバー権限でのみ実行可能
    /// </summary>
    /// <param name="player">権限を付与するプレイヤー</param>
    private void SetPlayerInputForce(PlayerRef player)
    {
        // NetworkObjectに入力権限を割り当て
        Object.AssignInputAuthority(player);
    }

    /// <summary>
    /// 入力権限を解放
    /// サーバー権限でのみ実行可能
    /// </summary>
    private void ReleasePlayerInputForce()
    {
        // NetworkObjectから入力権限を削除
        Object.RemoveInputAuthority();
    }
}

