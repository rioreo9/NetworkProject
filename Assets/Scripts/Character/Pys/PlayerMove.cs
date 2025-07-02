using System;
using Unity.Cinemachine;
using UnityEngine;

public class PlayerMove
{
    private Transform _transform;

    private Transform _cameraTransform;

    //キャラクターの状態を購読し、移動できるかの判定

    public PlayerMove(Transform transform, CinemachineCamera camera)
    {
        _transform = transform;
        _cameraTransform = camera.transform;
    }

    /// <summary>
    /// 移動処理を行うメソッド
    /// </summary>
    /// <param name="direction"></param>
    /// <param name="speed"></param>
    public void DoMove(Vector2 direction, float speed, float deltaTime)
    {
        if (direction == Vector2.zero) return;
        
        Vector3 moveDirection = TransformCalculation.GetMoveDirection(_cameraTransform, direction);

        Vector3 newPosition = _transform.position + moveDirection * speed * deltaTime;

        _transform.position = newPosition;
    }
}
