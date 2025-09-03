using Fusion;
using UnityEngine;
public class GunFireControl : BaseTurret
{
    /// <summary>
    /// 弾を発射するメソッド
    /// </summary>
    /// <param name="shotPosition">発射するPosition</param>
    /// <param name="isShot">ボタンを押しているかのBool</param>
    /// <returns>発射できたかどうか</returns>
    public override bool Fire(Transform shotPosition, bool isShot)
    {
        if (!isShot)
        {
            return false;
        }

        if (Runner.SimulationTime < _lastFireTime + _fireRate)
        {
            return false;
        }

        // 発射時間を更新
        _lastFireTime = (float)Runner.SimulationTime;

        Vector3 bulletSpawnPosition = shotPosition.position + shotPosition.forward * 0.5f;
        // 弾丸を生成
        BulletMove bullet = Runner.Spawn(_bulletPrefab, bulletSpawnPosition, Quaternion.identity);

        // 弾丸の初期化
        bullet.Init(shotPosition.forward);

        // 発射できたらTrueを返す
        return true;
    }
}

public abstract class BaseTurret : NetworkBehaviour
{
    // アームのGameObject
    [SerializeField, Required]
    protected Transform _shotPosition = default;

    // 弾丸プレハブ
    [SerializeField, Required]
    protected BulletMove _bulletPrefab = default;

    // 発射レート（秒)
    [SerializeField]
    protected float _fireRate = 0.5f;

    protected float _lastFireTime = 0f;

    public abstract bool Fire(Transform shotPosition, bool isShot);
}
