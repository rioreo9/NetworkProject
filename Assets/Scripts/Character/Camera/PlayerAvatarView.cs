
using Unity.Cinemachine;
using UnityEngine;

public class PlayerAvatarView : MonoBehaviour
{
    [SerializeField]
    private CinemachineCamera cinemachineCamera;

    public void MakeCameraTarget()
    {
        // CinemachineCameraの優先度を上げて、カメラの追従対象にする
        cinemachineCamera.Priority.Value = 100;
    }
}
