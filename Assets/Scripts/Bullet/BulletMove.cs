using Fusion;
using System;
using UnityEngine;

/// <summary>
/// ネットワーク同期対応弾丸移動システム
/// Photon Fusion2のラグ補償機能を使用した高精度な弾丸処理
/// </summary>
public class BulletMove : NetworkBehaviour
{
    /// <summary>
    /// 弾丸の寿命タイマー（ネットワーク同期）
    /// TickTimerを使用してサーバータイムベースで正確な時間管理
    /// </summary>
    [Networked] private TickTimer life { get; set; }
    
    /// <summary>
    /// 弾丸の寿命（秒）
    /// 生成時に設定され、ネットワーク上で同期される
    /// </summary>
    [Networked] public float lifeTime { get; set; }
    
    /// <summary>
    /// 弾丸の移動速度（単位/秒）
    /// ネットワーク同期されるため全クライアントで一致
    /// </summary>
    [Networked] public float Speed { get; set; }

    /// <summary>
    /// 当たり判定対象のレイヤーマスク
    /// Inspectorで設定可能、敵・壁・オブジェクトなどを指定
    /// </summary>
    [SerializeField]
    public LayerMask hitMask;

    /// <summary>
    /// 弾丸初期化処理
    /// 弾丸生成直後に呼び出され、寿命タイマーを開始
    /// </summary>
    public void Init()
    {
        // サーバーのTickベースで正確な寿命タイマーを作成
        life = TickTimer.CreateFromSeconds(Runner, lifeTime);
    }

    /// <summary>
    /// Photon Fusion用のネットワーク固定更新処理
    /// 物理計算と同期されたタイミングで実行される
    /// 全クライアントで決定的な結果を保証
    /// </summary>
    public override void FixedUpdateNetwork()
    {
        // 寿命チェック：期限切れの場合は弾丸を削除
        if (life.Expired(Runner))
        {
            // ネットワークオブジェクトとして適切に削除
            Runner.Despawn(Object);
        }
        else
        {
            // 弾丸移動処理
            // Runner.DeltaTimeを使用してネットワークタイムベースで移動
            transform.position += Speed * transform.forward * Runner.DeltaTime;
            
            // 当たり判定処理を実行
            //HitAction();
        }
    }

    /// <summary>
    /// 弾丸の当たり判定処理
    /// ラグ補償機能を使用して高精度な当たり判定を実装
    /// 高速弾丸でも確実に判定できるよう継続的レイキャストを使用
    /// </summary>
    private void HitAction()
    {
        // ラグ補償レイキャスト：ネットワーク遅延を考慮した当たり判定
        Runner.LagCompensation.Raycast(
            transform.position + transform.forward * 0.15f, // レイ開始位置（弾丸の少し前方）
            transform.forward,                              // レイの方向（弾丸の進行方向）
            Speed * Runner.DeltaTime,                      // レイの長さ（1フレーム分の移動距離）
            Object.InputAuthority,                         // 入力権限（誰の入力による弾丸か）
            out var hit,                                   // ヒット結果を格納
            hitMask.value,                                 // 判定対象レイヤー
            HitOptions.None);                             // 追加オプション

        // デバッグ用レイ可視化（エディタ上で赤線として表示）
        // 実際のレイより5倍長く表示して見やすくする
        Debug.DrawRay(
            transform.position + transform.forward * 0.15f,
            transform.forward * Speed * Runner.DeltaTime * 5,
            Color.red, Runner.DeltaTime);

        // 着弾処理
        if (hit.GameObject != null)
        {
            // TODO: 着弾時の処理を実装
            // - ダメージ適用
            // - ヒットエフェクト再生
            // - 弾丸削除
            // - スコア計算など
        }
    }
}
