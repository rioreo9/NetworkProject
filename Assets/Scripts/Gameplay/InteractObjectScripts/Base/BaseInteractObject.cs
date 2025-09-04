using Fusion;

/// <summary>
/// 峯廻制作
/// 操縦・操作できるオブジェクトの基底クラス(主に船についているタレットなど)
/// </summary>
public abstract class BaseInteractControlObject : NetworkBehaviour, IInteractableControllable
{
    [Networked, OnChangedRender(nameof(UpdateLocalInteractableFlag))]
    /// <summary>
    /// インタラクトできるかどうかのフラグ
    /// </summary>
    public bool IsInteractable { get; protected set; } = false;

    public new NetworkObject Object => Object;

    protected bool _isInteractable = true;

    private void UpdateLocalInteractableFlag()
    {
        _isInteractable = IsInteractable;
    }

    /// <summary>
    /// そのオブジェクトをコントロールするためのメソッド
    /// </summary>
    /// <param name="networkInput">Networkに対応したInput</param>
    public abstract void AccesObject(PlayerRef player, INoticePlayerInteract status);

    protected abstract void ReleseObject(INoticePlayerInteract status);
}
