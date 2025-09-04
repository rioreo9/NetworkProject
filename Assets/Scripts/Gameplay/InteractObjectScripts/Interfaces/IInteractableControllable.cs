using Fusion;
/// <summary>
/// 峯廻制作
/// 操縦・操作可能なオブジェクトのインターフェース
/// </summary>
public interface IInteractableControllable
{
    
    /// <summary>
    /// そのオブジェクトが操作可能かどうか
    /// </summary>
    public bool IsInteractable { get;}

    public NetworkObject Object { get; }

    /// <summary>
    /// コントロールするためのメソッド
    /// </summary>
    /// <param name="networkInput">Networkに対応したInput</param>
    public void AccesObject(PlayerRef player, INoticePlayerInteract status);
}
