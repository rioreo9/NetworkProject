using R3;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using VitalRouter;

public abstract class PhaseEndButtonBase : MonoBehaviour
{
    [SerializeField, Required]
    protected GameObject _buttonObject;
    [SerializeField, Required]
    protected Button _button;

    protected IGameStateNotice _gameStateNotice;

    protected ICommandPublisher _publisher;

    [Inject]
    public void InjectDependencies(IGameStateNotice notice, ICommandPublisher publisher)
    {
        _gameStateNotice = notice;
        _publisher = publisher;

        _gameStateNotice.GameStateRP?.Subscribe(ChangeActiveButton);

        _button?.OnClickAsObservable()
            .Subscribe(_ => NoticeState())
            .AddTo(this);

        Debug.Log($"{this.GetType().Name} injected dependencies.");
    }

    protected abstract void NoticeState();
    protected abstract void ChangeActiveButton(GameState state);  
}
