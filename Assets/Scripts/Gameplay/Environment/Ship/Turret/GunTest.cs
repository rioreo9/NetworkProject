using UnityEngine;

public class GunTest : MonoBehaviour
{
    [SerializeField]
    private GunEmplacementController gunEmplacementController;
    
    private void FixedUpdate()
    {
        // マウスのXY入力を取得してPlayerNetworkInputを作成
        PlayerNetworkInput input = new PlayerNetworkInput
        {
            LookInput = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")),
            CameraForwardDirection = Camera.main != null ? Camera.main.transform.forward : Vector3.forward
        };
        
        gunEmplacementController.DoRotation(input);
    }
}
