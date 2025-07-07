using Fusion;
using System;
using Unity.Cinemachine;
using UnityEngine;

public class PlayerMove
{
    private Transform _cameraTransform;

    public PlayerMove(Transform cameraTransform)
    {
        _cameraTransform = cameraTransform;
    }

    /// <summary>
    /// 移動処理を行うメソッド
    /// </summary>
    /// <param name="direction"></param>
    /// <param name="speed"></param>
    public Vector3 DoMove(Vector2 direction, float speed, float deltaTime)
    {
        if (direction == Vector2.zero) return Vector3.zero;
        
        Vector3 moveDirection = TransformCalculation.GetMoveDirection(_cameraTransform, direction);

        return moveDirection * speed * deltaTime;
    }
}
