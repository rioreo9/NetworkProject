using System;
using UnityEngine;

public class PlayerMove
{
    private Transform _transform;

    //キャラクターの状態を購読し、移動できるかの判定

    public PlayerMove(Transform transform)
    {
        _transform = transform;
    }

    /// <summary>
    /// 移動処理を行うメソッド
    /// </summary>
    /// <param name="direction"></param>
    /// <param name="speed"></param>
    public void Move(Vector2 direction, float speed)
    {
        if (direction == Vector2.zero) return;
        
        Vector3 normalizedDirection = direction.normalized;

        Vector3 newPosition = _transform.position + normalizedDirection * speed * Time.deltaTime;
        
        _transform.position = newPosition;
    }
}
