using Fusion;
using UnityEngine;
using Unity.Cinemachine;
public class GunFireControl : BaseTurret
{
    public override void Fire(Transform shotPosition,bool isShot)
    {
        if(!isShot) return;
        Vector3 bulletSpawnPosition = transform.position + shotPosition.forward * 0.5f;
        // 弾丸を生成
        BulletMove bullet = Runner.Spawn(_bulletPrefab, bulletSpawnPosition, Quaternion.identity);

        // 弾丸の初期化
        bullet.Init(_shotPosition.forward);
    }
}

public abstract class BaseTurret : NetworkBehaviour, IFireTurret
{
    // アームのGameObject
    [SerializeField, Required]
    protected Transform _shotPosition = default;

    // 弾丸プレハブ
    [SerializeField, Required]
    protected BulletMove _bulletPrefab = default;

    //シネマシーンカメラ
    [SerializeField, Required]
    protected CinemachineCamera _camera = default;

    public abstract void Fire(Transform shotPosition,bool isShot);
}


public interface IFireTurret
{
    public void Fire(Transform shotPosition,bool isShot);
}
