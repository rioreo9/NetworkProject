using System;
using VitalRouter;

public enum PageId
{
    None,
    Shop,
    Menu,
    ChangePoint,
    SelectMenu
}

public readonly struct NavigateToPageCommand : ICommand
{
    public PageId TargetPageId { get; }
    public NavigateToPageCommand(PageId targetPageId)
    {
        TargetPageId = targetPageId;
    }
}
