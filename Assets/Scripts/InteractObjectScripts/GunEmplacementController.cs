using Fusion;
using UnityEngine;

public class GunEmplacementController : NetworkBehaviour
{
    [Networked]
    public PlayerRef _currentOperatorP { get; private set; }

    public override void FixedUpdateNetwork()
    {
        if (!GetInput(out PlayerNetworkInput input)) return;
        Debug.Log($"GunEmplacementController FixedUpdateNetwork: {input}");
        DoRotation(input);
    }

    /// <summary>
    /// プレイヤーの操作を挿入設定するメソッド
    /// </summary>
    /// <param name="player"></param>
    public void SetPlayerRef(PlayerRef player)
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
        Debug.Log($"RPCでPlayerRefが設定されました: {player}");
    }

    private void SetPlayerInputForce(PlayerRef player)
    {
        // 権限がある場合は直接設定
        Object.AssignInputAuthority(player);
        Debug.Log($"砲台の入力権限をプレイヤー {player} に移譲しました");
        Debug.Log($"Object.HasInputAuthority: {Object.HasInputAuthority}");
        Debug.Log($"Object.InputAuthority: {Object.InputAuthority}");
    }

    private void DoRotation(PlayerNetworkInput input)
    {
        Vector3 cameraDirection = input.CameraForwardDirection;

        // 正規化してからキャラクターの回転を設定
        cameraDirection.Normalize();

        transform.rotation = Quaternion.LookRotation(cameraDirection);
    }
}
