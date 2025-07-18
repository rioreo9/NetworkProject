using System;

public class EnemyWakame : BaseEnemy
{
    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        transform.position += transform.forward * Runner.DeltaTime;
    }

    public override void Initialize()
    {
    
    }
    public override void AttackTarget()
    {
       
    }
}
