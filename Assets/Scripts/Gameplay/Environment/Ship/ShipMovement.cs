using System;
using Fusion;
using UnityEngine;

public class ShipMovement : NetworkBehaviour
{
    public override void FixedUpdateNetwork()
    {
        DoMove();
    }

    private void DoMove()
    {
      //transform.position += Vector3.forward * 5f * Runner.DeltaTime;
    }
}
