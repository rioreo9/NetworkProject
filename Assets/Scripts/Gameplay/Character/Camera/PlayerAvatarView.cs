using Fusion;
using UnityEngine;

/// <summary>
/// プレイヤーアバタービュー統合クラス
/// 各専用コントローラーを統合して管理
/// 単一責任の原則に従い、主に調整役として機能
/// </summary>

[RequireComponent(typeof(PlayerCameraController))]
[RequireComponent(typeof(PlayerInteractionController))]
[RequireComponent(typeof(PlayerToolController))]
public class PlayerAvatarView : NetworkBehaviour
{
   
    private PlayerCameraController _cameraController;
   
    private PlayerInteractionController _interactionController;
   
    private PlayerToolController _toolManager;

    /// <summary>
    /// カメラの初期化処理
    /// ローカルプレイヤーのスポーン時に呼び出される
    /// </summary>
    public void SetCamera()
    {
        _cameraController = GetComponent<PlayerCameraController>();
        _interactionController = GetComponent<PlayerInteractionController>();
        _toolManager = GetComponent<PlayerToolController>();

        _cameraController.InitializeCamera();
        _interactionController.Initialize(_cameraController, _toolManager);
    }

    /// <summary>
    /// ネットワーク対応の固定更新処理
    /// 入力権限を持つプレイヤーのみ処理を実行
    /// </summary>
    public override void FixedUpdateNetwork()
    {
        // 入力権限がない場合は処理をスキップ
        if (!Object.HasInputAuthority) return;

        // ネットワーク入力を取得
        if (!GetInput(out PlayerNetworkInput input)|| Runner.IsResimulation) return;

        // カメラ回転処理
        _cameraController.HandleCameraRotation(input.LookInput);

        _interactionController.ProbeInteractionTarget();

        // インタラクション処理
        if (input.InteractPressed.IsSet(MyButtons.Interact))
        {
            _interactionController.HandleInteraction();
        }

        // ツールドロップ処理
        if (input.DropPressed.IsSet(MyButtons.Drop))
        {
            _toolManager.DropTool();
        }
    }

    /// <summary>
    /// カメラ感度を動的に変更する処理
    /// </summary>
    public void SetMouseSensitivity(float sensitivity)
    {
        _cameraController.SetMouseSensitivity(sensitivity);
    }

    /// <summary>
    /// Y軸反転設定を変更する処理
    /// </summary>
    public void SetInvertYAxis(bool invert)
    {
        _cameraController.SetInvertYAxis(invert);
    }
}
