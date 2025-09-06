using R3;
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using VitalRouter;
using VitalRouter.VContainer;

[Serializable]
public struct PageEntry
{
    
    public PageId Id;
   
    public GameObject PageUI;   
}

[Routes]
/// <summary>
/// UIページの表示/非表示を一元管理するルータ。
/// VitalRouter のコマンド <see cref="NavigateToPageCommand"/> を受け取り、
/// 指定ページのみを Active にし、それ以外を非表示にする。
/// </summary>
public partial class UIPageRouter : MonoBehaviour
{
    [SerializeField, Required]
    private List<PageEntry> _pages = new();

    private ReactiveProperty<PageId> _currentPageId = new();
    public ReadOnlyReactiveProperty<PageId> CurrentPageId => _currentPageId;

    private readonly Dictionary<PageId, GameObject> _map = new();
    private IDisposable _navigateSubscription;

    /// <summary>
    /// ページ定義から内部マップを構築し、初期状態では全ページを非表示にする。
    /// </summary>
    private void Start()
    {
        _map.Clear();
        foreach (PageEntry page in _pages)
        {
            if (page.PageUI != null) SetDictionary(page, page.PageUI);
        }
    }

    private void SetDictionary(PageEntry page, GameObject obj)
    {
        if (obj.TryGetComponent(out IInjectPageRouter router)) router.SetNavigate(Router.Default);

        _map[page.Id] = obj.gameObject;

        if (page.Id == PageId.Menu) return;
      
        obj.gameObject.SetActive(false);
    }

    /// <summary>
    /// ページ遷移コマンドのハンドラ。指定ページのみ表示する。
    /// </summary>
    public void OnNavigate(NavigateToPageCommand cmd)
    {
        ShowOnly(cmd.TargetPageId);
    }

    /// <summary>
    /// 指定したページ ID のみを表示し、他を非表示にする。
    /// 現在ページIDも更新する。
    /// </summary>
    /// <param name="id">表示するページ ID</param>
    public void ShowOnly(PageId id)
    {
        foreach (var pair in _map)
        {
            pair.Value.SetActive(pair.Key == id);
        }
        _currentPageId.Value = id;
    }

    private void OnEnable() => _navigateSubscription = this.MapTo(Router.Default);
    private void OnDisable() => _navigateSubscription?.Dispose();
}


