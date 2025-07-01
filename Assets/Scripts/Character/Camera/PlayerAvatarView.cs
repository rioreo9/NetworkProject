
using Unity.Cinemachine;
using UnityEngine;

public class PlayerAvatarView : MonoBehaviour, ISetPlayerInformation
{
    public void SetCamera(CinemachineCamera camera)
    {
        // カメラの設定を行う
        if (camera != null)
        {
            MakeCameraTarget(camera);
        }
        else
        {
            Debug.LogWarning("CinemachineCamera is null. Cannot set camera target.");
        }
    }

    private void MakeCameraTarget(CinemachineCamera camera)
    {
        // CinemachineCameraの優先度を上げて、カメラの追従対象にする
        camera.Priority.Value = 100;
    }
}
