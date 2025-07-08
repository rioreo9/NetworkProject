
using Unity.Cinemachine;
using UnityEngine;

public class PlayerAvatarView : MonoBehaviour
{
    public void SetCamera()
    {
        CinemachineCamera freeLookCamera = FindFirstObjectByType<CinemachineCamera>();

        freeLookCamera.LookAt = transform;
        freeLookCamera.Follow = transform;
    }

   
}
