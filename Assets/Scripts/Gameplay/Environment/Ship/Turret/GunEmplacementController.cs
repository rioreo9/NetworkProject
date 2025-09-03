using Fusion;

using UnityEngine;

public class GunEmplacementController : NetworkBehaviour
{
    public void DoRotation(PlayerNetworkInput input)
    {
        Vector3 cameraDirection = input.CameraForwardDirection;

        // 正規化してからキャラクターの回転を設定
        cameraDirection.Normalize();

        transform.rotation = Quaternion.LookRotation(cameraDirection);
    }
}
