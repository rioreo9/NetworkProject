using Fusion;
using System;
using UnityEngine;

public class EnemyAIBrain : NetworkBehaviour
{
    public enum AIState
    {
        Idle,
        Chase,
        Attack,
        Dead
    }

    [Networked] public AIState State { get; private set; }
    [Networked] private NetworkId TargetId { get; set; }

    private BaseEnemy _owner;

    // タイマー
    private TickTimer _reacquireTimer;
    private TickTimer _attackCooldown;

    // パラメータ（Ownerから参照）
    private float VisionRange => _owner != null ? _owner.VisionRange : 10f;
    private float AttackRange => _owner != null ? _owner.AttackRange : 5f;
    private float MoveSpeed => _owner != null ? _owner.MoveSpeed : 2f;
    private LayerMask TargetMask => _owner != null ? _owner.TargetMask : default;

    public void Initialize(BaseEnemy owner)
    {
        _owner = owner;
        if (!Object || !Object.HasStateAuthority) return;
        State = AIState.Idle;
        TargetId = default;
        _reacquireTimer = TickTimer.CreateFromSeconds(Runner, 0f);
        _attackCooldown = TickTimer.CreateFromSeconds(Runner, 0f);
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority || _owner == null) return;

        if (!_owner.IsAlive)
        {
            State = AIState.Dead;
        }

        switch (State)
        {
            case AIState.Idle:
                UpdateIdle();
                break;
            case AIState.Chase:
                UpdateChase();
                break;
            case AIState.Attack:
                UpdateAttack();
                break;
            case AIState.Dead:
                // 何もしない
                break;
        }
    }

    private void UpdateIdle()
    {
        // 周期的にターゲット再取得
        if (!_reacquireTimer.IsRunning || _reacquireTimer.Expired(Runner))
        {
            TryAcquireTarget();
            _reacquireTimer = TickTimer.CreateFromSeconds(Runner, 0.25f);
        }

        if (TryGetTarget(out var target))
        {
            float sqrDist = (target.position - _owner.transform.position).sqrMagnitude;
            if (sqrDist <= AttackRange * AttackRange)
            {
                State = AIState.Attack;
            }
            else
            {
                State = AIState.Chase;
            }
        }
    }

    private void UpdateChase()
    {
        if (!TryGetTarget(out var target))
        {
            State = AIState.Idle;
            return;
        }

        Vector3 toTarget = target.position - _owner.transform.position;
        toTarget.y = 0f;
        if (toTarget.sqrMagnitude <= AttackRange * AttackRange)
        {
            State = AIState.Attack;
            return;
        }

        if (toTarget.sqrMagnitude > VisionRange * VisionRange)
        {
            // ロスト
            State = AIState.Idle;
            TargetId = default;
            return;
        }

        // 回頭と前進
        if (toTarget != Vector3.zero)
        {
            Quaternion look = Quaternion.LookRotation(toTarget.normalized);
            _owner.transform.rotation = Quaternion.Slerp(_owner.transform.rotation, look, 10f * Runner.DeltaTime);
        }
        _owner.transform.position += _owner.transform.forward * MoveSpeed * Runner.DeltaTime;
    }

    private void UpdateAttack()
    {
        if (!TryGetTarget(out var target))
        {
            State = AIState.Idle;
            return;
        }

        Vector3 toTarget = target.position - _owner.transform.position;
        toTarget.y = 0f;
        float sqrDist = toTarget.sqrMagnitude;

        // 射程外なら追跡へ
        if (sqrDist > AttackRange * AttackRange)
        {
            State = AIState.Chase;
        }

        // ターゲットの方向へ向く
        if (toTarget != Vector3.zero)
        {
            Quaternion look = Quaternion.LookRotation(toTarget.normalized);
            _owner.transform.rotation = Quaternion.Slerp(_owner.transform.rotation, look, 10f * Runner.DeltaTime);
        }

        // クールダウン
        if (!_attackCooldown.IsRunning || _attackCooldown.Expired(Runner))
        {
            _owner.AttackTarget();
            _attackCooldown = TickTimer.CreateFromSeconds(Runner, 1.0f);
        }
    }

    private void TryAcquireTarget()
    {
        Collider[] hits = Physics.OverlapSphere(_owner.transform.position, VisionRange, TargetMask);
        Transform nearest = null;
        float nearestSqr = float.MaxValue;
        foreach (var hit in hits)
        {
            float sqr = (hit.transform.position - _owner.transform.position).sqrMagnitude;
            if (sqr < nearestSqr)
            {
                nearestSqr = sqr;
                nearest = hit.transform;
            }
        }

        if (nearest != null)
        {
            NetworkObject netObj = nearest.GetComponentInParent<NetworkObject>();
            if (netObj)
            {
                TargetId = netObj.Id;
            }
        }
    }

    private bool TryGetTarget(out Transform transform)
    {
        transform = null;
        if (!TargetId.IsValid) return false;
        if (Object.Runner.TryFindObject(TargetId, out NetworkObject targetObj))
        {
            transform = targetObj.transform;
            return transform != null;
        }
        return false;
    }
}
