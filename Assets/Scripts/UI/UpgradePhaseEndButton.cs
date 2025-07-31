using UnityEngine;
using VContainer;
using R3;
using VitalRouter;

public class UpgradePhaseEndButton : PhaseEndButtonBase
{
    /// <summary>
    /// VContainerから手動で依存性を注入するメソッド
    /// </summary>

    protected override void ChangeActiveButton(GameState state)
    {
        if (state == GameState.UpgradePhase)
        {
            _buttonObject.SetActive(true);
        }
        else
        {
            _buttonObject.SetActive(false);
        }
    }

    protected override void NoticeState()
    {
        _publisher.PublishAsync
                (new GameStateChangeCommand(ChangeStateType.UpgradePhaseEnd));
    }

}
