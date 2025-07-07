using Fusion;
using System;
using Unity.Cinemachine;
using UnityEngine;

public class PlayerMove : NetworkBehaviour
{
    private Transform _cameraTransform;

    //キャラクターの状態を購読し、移動できるかの判定

    public override void Spawned()
    {
        
    }

    public override void FixedUpdateNetwork()
    {
        if (GetInput(out PlayerNetworkInput input))
        {
            Debug.Log($"PlayerMove: {input.MovementInput}");

            transform.position += new Vector3(input.MovementInput.x, 0, input.MovementInput.y) * 5f * Runner.DeltaTime;
        }
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

        transform.position += moveDirection * speed * deltaTime;
    }
}
