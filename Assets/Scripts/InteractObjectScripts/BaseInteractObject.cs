using Fusion;

/// <summary>
/// 峯廻制作
/// 操縦・操作できるオブジェクトの基底クラス(主に船についているタレットなど)
/// </summary>
public abstract class BaseInteractObject : NetworkBehaviour, IInteractableControllable
{
    // インタラクトできるかどうかのフラグ
    protected bool _isInteractable = false;

    /// <summary>
    /// インタラクトできるかどうかのフラグ
    /// </summary>
    public bool IsInteractable { get => _isInteractable; set => _isInteractable = value; }

    /// <summary>
    /// そのオブジェクトをコントロールするためのメソッド
    /// </summary>
    /// <param name="networkInput">Networkに対応したInput</param>
    public abstract void ControlObject(NetworkInput networkInput);
}
