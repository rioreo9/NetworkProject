
/// <summary>
/// 峯廻制作
/// インタラクト可能なボタンのインターフェース
/// </summary>
public interface IInteractableButton
{
    /// <summary>
    /// Pushできるかのフラグ
    /// </summary>
    public bool IsInteractable { get; set; }

    /// <summary>
    /// Pushしたときのメソッド
    /// </summary>
    public void PushButton();
}

