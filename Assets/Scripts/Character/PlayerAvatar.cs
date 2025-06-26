using Fusion;
using Unity.Mathematics;
using UnityEngine;
public class PlayerAvatar : NetworkBehaviour
{
    private NetworkCharacterController characterController;

    public override void Spawned()
    {
        characterController = GetComponent<NetworkCharacterController>();

        PlayerAvatarView view = GetComponent<PlayerAvatarView>();
        // 自身がアバターの権限を持っているなら、カメラの追従対象にする
        if (HasStateAuthority)
        {
            view.MakeCameraTarget();
        }
    }

    public override void FixedUpdateNetwork()
    {
        var cameraRotation = Quaternion.Euler(0f, Camera.main.transform.rotation.eulerAngles.y, 0f);
        // 移動
        var inputDirection = new Vector3(Input.GetAxis("Horizontal"), 0f, Input.GetAxis("Vertical"));
        characterController.Move(cameraRotation * inputDirection);
        // ジャンプ
        if (Input.GetKey(KeyCode.Space))
        {
            characterController.Jump();
        }
    }
}
