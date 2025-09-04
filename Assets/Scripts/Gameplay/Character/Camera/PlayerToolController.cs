using Fusion;
using UnityEngine;

/// <summary>
/// プレイヤーツール管理専用クラス
/// ツールの保持、ドロップ、コピー生成を担当
/// </summary>
public class PlayerToolController : NetworkBehaviour
{
    [Networked] public bool Collected { get; private set; }

    [SerializeField, Required]
    private Transform _localHandParent;
    [SerializeField, Required]
    private Transform _networkHandParent;

    private IInteractableTool _currentTool;

    /// <summary>
    /// 現在のツールを取得
    /// </summary>
    public IInteractableTool CurrentTool => _currentTool;

    /// <summary>
    /// ツールを保持する
    /// </summary>
    public void PickUpTool(IInteractableTool tool, RaycastHit hit)
    {
        // 元々所持しているツールがある場合は、インタラクト不可状態に設定
        _currentTool?.RPC_SetInteractable(false);
        _currentTool?.LocalItemInteractable(false);

        _currentTool = tool;
        _currentTool?.RPC_SetInteractable(true);
        _currentTool?.LocalItemInteractable(true);

        RPC_PickUpItem(hit.collider.GetComponent<NetworkObject>());

        CreateToolCopy(hit.collider.gameObject);

    }

    /// <summary>
    /// ツールをドロップする
    /// </summary>
    public void DropTool()
    {
        if (_currentTool != null)
        {
            _currentTool.RPC_SetInteractable(false);
            _currentTool.LocalItemInteractable(false);
            _currentTool = null;
        }
    }

    /// <summary>
    /// 現在持っているツールを使用する
    /// </summary>
    public bool UseTool(RaycastHit hit)
    {
        if (_currentTool == null) return false;

        if (_currentTool.CheckInteractableObject(hit))
        {
            _currentTool = null;
            return true;
        }
        return false;
    }

    /// <summary>
    /// ツールのコピーオブジェクト生成処理
    /// </summary>
    private void CreateToolCopy(GameObject originalTool)
    {
        if (_localHandParent == null || _networkHandParent == null) return;

        // ローカルプレイヤー用の位置設定
        _currentTool.SetCopyItemPosition(_localHandParent.position, _localHandParent);
    }

    /// <summary>
    /// アイテムピックアップのネットワークRPC処理
    /// </summary>
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_PickUpItem(NetworkObject networkObject)
    {
        if (Collected) return;

        if (networkObject == null || _networkHandParent == null) return;

        BasePickUpToolObject tool = networkObject.GetComponent<BasePickUpToolObject>();
        if (tool != null)
        {
            _currentTool = tool;
        }

        // リモートプレイヤー（他のプレイヤー）用の処理
        tool.SetINetItemPosition(_networkHandParent.position, _networkHandParent);
    }
}
