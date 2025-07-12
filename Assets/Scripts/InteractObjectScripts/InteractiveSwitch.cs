using System;

public class InteractiveSwitch : BaseInteractButtonObject
{

    /// <summary>
    /// ボタンを押したときに呼び出されるメソッド
    /// </summary>
    public override void PushButton()
    {
        // ボタンが押されたときの処理をここに実装
        print("スイッチが押されました");
    }
}
