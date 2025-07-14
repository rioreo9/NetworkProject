using Fusion;
using System;
using UnityEngine;

public class InteractiveSwitch : BaseInteractButtonObject
{
    [SerializeField,Required]
    private GameObject _moveObj; // スイッチのアニメーター
    /// <summary>
    /// ボタンを押したときに呼び出されるメソッド
    /// </summary>
    public override void PushButton()
    {
        if (_moveObj.TryGetComponent<Animator>(out Animator animator))
        {
            if (animator.GetBool("MoveOn"))
            {
                animator.SetBool("MoveOn", false);
            }
            else
            {
                animator.SetBool("MoveOn", true);
            }
            Debug.Log("アニメ更新");        
        }
        // ボタンが押されたときの処理をここに実装
        print("スイッチが押されました");
    }
}
