using Fusion;

using Newtonsoft.Json.Bson;

using UnityEngine;

public class LaserGun : BaseInteractControlObject
{
    [SerializeField]
    private float _shootCooldown = 0.5f; // 発射クールダウン時間

    private float _currentShootCooldown = 0f; // 現在のクールダウン時間


    public override void FixedUpdateNetwork()
    {
    }

    public override void ControlObject()
    {
        if (Object.HasStateAuthority)
        {

        }
        else
        {

        }
    }

   
  

    private void Shoot()
    {

    }
}
