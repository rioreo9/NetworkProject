using System;
using UnityEngine;
using UnityEngine.UI;
using VitalRouter;
using R3;

[Serializable]
public struct ButtonEntry
{
    public Button Button;
    public PageId TargetPageId;
}
public abstract class PageUIBase : MonoBehaviour, IInjectPageRouter
{
    [Header("ページ遷移ボタン")]
    [SerializeField]
    private ButtonEntry[] _buttons;

    private ICommandPublisher _publisher;

    public void SetNavigate(ICommandPublisher cmd)
    {
        _publisher = cmd;
    }

    private void Awake()
    {
        
        foreach (ButtonEntry button in _buttons)
        {
            if (button.Button == null) continue;

            button.Button.OnClickAsObservable()
                .Subscribe(_ => OnButtonClicked(button.TargetPageId))
                .AddTo(this);
        }

        Initialize();
    }

    protected abstract void Initialize();

    private void OnButtonClicked(PageId targetPageId)
    {
        _publisher?.PublishAsync(new NavigateToPageCommand(targetPageId));
    }
}
