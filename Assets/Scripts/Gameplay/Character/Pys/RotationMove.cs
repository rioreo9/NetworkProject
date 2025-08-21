using System;
using Unity.Cinemachine;
using Unity.Mathematics;
using UnityEngine;

public class RotationMove
{
    private Transform _transform; // キャラクターのTransform

    private Transform _cameraTransform; // カメラの参照

    public RotationMove(Transform transform, CinemachineCamera camera)
    {
        _transform = transform;
        _cameraTransform = camera?.transform;
    }

    /// <summary>
    /// キャラクターの回転処理を行うメソッド
    /// Y軸回転のみでカメラのフォワード方向に向ける
    /// </summary>
    public quaternion DoRotation()
    {
        if (_cameraTransform == null || _transform == null) return quaternion.identity;
    
        // カメラのフォワード方向を取得
        Vector3 cameraDirection = _cameraTransform.forward;
        
        // Y成分を0にしてY軸回転のみにする
        cameraDirection.y = 0f;
        
        // 正規化してからキャラクターの回転を設定
        cameraDirection.Normalize();
       return Quaternion.LookRotation(cameraDirection);
    }
}
