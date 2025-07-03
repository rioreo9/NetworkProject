using Fusion;
/// <summary>
/// 峯廻制作
/// インタラクト可能なボタンの基底クラス
/// </summary>
public abstract class BaseInteractButtonObject : NetworkBehaviour, IInteractableButton
{
    //インタラクト可能かどうかのフラグ
    protected bool _isInteractable = true;

    /// <summary>
    /// インタラクト可能かどうかのフラグ
    /// </summary>
    public bool IsInteractable { get => _isInteractable; set => _isInteractable = value; }

    /// <summary>
    /// ボタンを押したときに呼び出されるメソッド
    /// </summary>
    public abstract void PushButton();
}

