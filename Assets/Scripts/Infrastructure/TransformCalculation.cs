using System;
using UnityEngine;

public class TransformCalculation
{
    public static Vector3 GetMoveDirection(Transform cameraTransform, Vector2 input)
    {
        // カメラの前方向と右方向を取得（Y軸回転のみを考慮）
        Vector3 cameraForward = cameraTransform.forward;
        Vector3 cameraRight = cameraTransform.right;

        // Y軸成分を除去して水平面での方向ベクトルを作成
        cameraForward.y = 0f;
        cameraRight.y = 0f;

        // 正規化して単位ベクトルにする
        cameraForward = cameraForward.normalized;
        cameraRight = cameraRight.normalized;

        // 入力に基づいてカメラ基準の移動方向を計算
        return (cameraForward * input.y + cameraRight * input.x).normalized;
    }
}
