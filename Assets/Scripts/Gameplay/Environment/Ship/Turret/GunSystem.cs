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

    // 追加: ローカル多重要求防止
    private bool _localAccessPending = false;

    public override void Spawned()
    {
        IsInteractable = true; // 初期状態ではインタラクト可能
    }

    /// <summary>
    /// ネットワーク固定更新処理
    /// プレイヤーの入力を受け取り、砲塔の回転と射撃を制御
    /// </summary>
    public override void FixedUpdateNetwork()
    {
        // ネットワーク入力の取得に失敗した場合は処理を終了
        if (!GetInput(out PlayerNetworkInput input) || Runner.IsResimulation)
        {
            return;
        }

        // 押下エッジでのみ解除するように変更（多重解除防止）
        if (_currentPlayerStatus != null && !IsInteractable && input.InteractPressed.IsSet(MyButtons.Interact))
        {
            Debug.Log("操作終了");
            ReleseObject(_currentPlayerStatus);
            return;
        }

        // 砲塔の回転処理
        _gunEmplacementController.DoRotation(input);

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
        Debug.Log($"アクセス要求:{status}");

        // 追加: ローカル多重要求防止（同一押下内の連打・多重呼び出しをブロック）
        if (_localAccessPending) return;
        _localAccessPending = true;

        _currentPlayerStatus = status;

        // ネットワーク権限の有無で処理を分岐
        if (Object.HasStateAuthority)
        {
            Debug.Log("Host");
            // 権限がある場合は直接設定
            SetPlayerInputForce(player);
        }
        else
        {
            Debug.Log("Client");
            // 権限がない場合はRPCを使用してサーバーに要求
            RPC_RequestOperation(player);
        }
        _cinemachineCamera.Priority = 5;
        _currentPlayerStatus.RPC_SetControllerInteracting(true);
    }

    /// <summary>
    /// プレイヤーが砲塔の操作を終了する際の処理
    /// ネットワーク権限の解放とプレイヤー状態のリセットを行う
    /// </summary>
    /// <param name="status">プレイヤーの状態通知インターフェース</param>
    protected override void ReleseObject(INoticePlayerInteract status)
    {
        Debug.Log($"リリース要求:{_currentPlayerStatus}");

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
        _cinemachineCamera.Priority = 0;

        // 追加: 要求中フラグ解除
        _localAccessPending = false;
        _currentPlayerStatus = null;
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
        IsInteractable = false;
       
    }

    /// <summary>
    /// 入力権限を解放
    /// サーバー権限でのみ実行可能
    /// </summary>
    private void ReleasePlayerInputForce()
    {
        // NetworkObjectから入力権限を削除
        Object.RemoveInputAuthority();
        IsInteractable = true;
    }
}

