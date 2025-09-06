using System;
using Fusion;
using UnityEngine;

public class ShipMovement : NetworkBehaviour
{
    [Header("移動設定")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 90f; // 度/秒
    
    [Header("移動精度")]
    [SerializeField] private float waypointReachDistance = 0.5f;
    [SerializeField] private float rotationThreshold = 1f; // 度

    [Networked]
    public Vector3 _currentWaypoint { get; private set; }

    private bool _isMoving = false;
    private bool _isRotating = false;

    // 次の目標地点を設定するRPC
    public void SetWayPoint(Vector3 nextPoint)
    {
        _currentWaypoint = nextPoint;
        _isMoving = true;
        _isRotating = true;
    }

    public void DoMove()
    {
        if (_currentWaypoint == Vector3.zero || !_isMoving) return;

        // 目標地点への方向を計算
        Vector3 directionToWaypoint = (_currentWaypoint - transform.position).normalized;
        
        // 現在の向きと目標方向の角度差を計算
        float targetAngle = Mathf.Atan2(directionToWaypoint.x, directionToWaypoint.z) * Mathf.Rad2Deg;
        float currentAngle = transform.eulerAngles.y;
        float angleDifference = Mathf.DeltaAngle(currentAngle, targetAngle);

        // 回転処理
        if (Mathf.Abs(angleDifference) > rotationThreshold)
        {
            _isRotating = true;
            float rotationStep = rotationSpeed * Runner.DeltaTime;
            float newAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, rotationStep);
            transform.rotation = Quaternion.Euler(0, newAngle, 0);
        }
        else
        {
            _isRotating = false;
        }

        // 移動処理（回転が完了してから、または回転中でも少しずつ移動）
        if (!_isRotating || Mathf.Abs(angleDifference) < 45f)
        {
            Vector3 moveDirection = transform.forward;
            transform.position += moveDirection * moveSpeed * Runner.DeltaTime;
        }

        // 目標地点に到達したかチェック
        float distanceToWaypoint = Vector3.Distance(transform.position, _currentWaypoint);
        if (distanceToWaypoint <= waypointReachDistance)
        {
            _isMoving = false;
            _isRotating = false;
        }
    }

    // 移動状態の取得
    public bool IsMoving => _isMoving;
    public bool IsRotating => _isRotating;
    
    // 目標地点までの距離を取得
    public float GetDistanceToWaypoint()
    {
        if (_currentWaypoint == Vector3.zero) return float.MaxValue;
        return Vector3.Distance(transform.position, _currentWaypoint);
    }

    // 移動を停止
    public void StopMovement()
    {
        _isMoving = false;
        _isRotating = false;
    }

    // 移動速度の設定
    public void SetMoveSpeed(float newSpeed)
    {
        moveSpeed = newSpeed;
    }

    // 回転速度の設定
    public void SetRotationSpeed(float newSpeed)
    {
        rotationSpeed = newSpeed;
    }
}
