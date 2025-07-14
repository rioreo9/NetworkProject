using Fusion;
using UnityEngine;

public class InteractiveSwitch : BaseInteractButtonObject
{
    [SerializeField, Required]
    private GameObject _moveObj; // スイッチのアニメーター

    [Networked]
    public bool IsActive { get; private set; } = false; // スイッチの状態
    /// <summary>
    /// ボタンを押したときに呼び出されるメソッド
    /// </summary>
    public override void PushButton()
    {
        if (_moveObj.TryGetComponent<Animator>(out Animator animator))
        {
            CheckActive(animator);
            Debug.Log("アニメ更新");
        }
        // ボタンが押されたときの処理をここに実装
        print("スイッチが押されました");
    }

    public override void FixedUpdateNetwork()
    {
       Debug.Log("スイッチの状態: " + IsActive);
    }

    private void CheckActive(Animator animator)
    {
        if (IsActive)
        {
            animator.SetBool("MoveOn", false);
            IsActive = false;
        }
        else
        {
            animator.SetBool("MoveOn", true);
            IsActive = true;
        }
    }


}
