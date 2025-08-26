using System;
using VitalRouter;

public enum PageId
{
    None,
    Shop,
    Menu,
}

public readonly struct NavigateToPageCommand : ICommand
{
    public PageId TargetPageId { get; }
    public NavigateToPageCommand(PageId targetPageId)
    {
        TargetPageId = targetPageId;
    }
}
