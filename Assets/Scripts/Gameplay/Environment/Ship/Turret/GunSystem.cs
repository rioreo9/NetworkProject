using Fusion;

using UnityEngine;

public class GunSystem : BaseInteractControlObject
{
    [SerializeField, Required]
    private GunEmplacementController _gunEmplacementController = default;

    [SerializeField, Required]
    private GunFireControl _gunFireControl = default;

    public override void FixedUpdateNetwork()
    {
        if (!GetInput(out PlayerNetworkInput input)) return;

        _gunEmplacementController.DoRotation(input);

        // 修正: AttackPressed.IsPressed → AttackPressed.IsSet(0) など、ボタンが押されているか判定
        _gunFireControl.Fire(_gunEmplacementController.transform, input.AttackPressed.IsSet(MyButtons.Attack));
    }

    /// <summary>
    /// プレイヤーの操作を挿入設定するメソッド
    /// </summary>
    /// <param name="player"></param>
    public override void AccesObject(PlayerRef player, INoticePlayerInteract status)
    {
        //変更権限の有無
        if (Object.HasStateAuthority)
        {
            SetPlayerInputForce(player);
        }
        else
        {
            // 権限がない場合はRPCを使用してサーバーに要求
            RPC_RequestOperation(player);
        }
    }

    /// <summary>
    /// サーバーに操縦開始を要求するRPC
    /// </summary>
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestOperation(PlayerRef player)
    {
        SetPlayerInputForce(player);
    }

    private void SetPlayerInputForce(PlayerRef player)
    {
        // 権限がある場合は直接設定
        Object.AssignInputAuthority(player);
    }
}

