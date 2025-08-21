using Fusion;
using UnityEngine;

/// <summary>
/// 敵AIの各状態が実装すべきインターフェース。
/// Enter/Exit では軽量な初期化・片付けのみを行い、
/// 実処理（索敵・移動・攻撃）は NetworkUpdate() で行います。
/// </summary>
public interface IEnemyState
{
    void Enter();
    void NetworkUpdate();
    void Exit();
}


