using R3;
using VContainer;
using VitalRouter;
public class PreparationEndButton : PhaseEndButtonBase
{

    protected override void ChangeActiveButton(GameState state)
    {
        if (state == GameState.Preparation)
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
               (new GameStateChangeCommand(ChangeStateType.PreparationEnd));
    }
}
