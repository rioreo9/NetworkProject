using R3;
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using VitalRouter;

[Serializable]
public struct PageEntry : ISerializationCallbackReceiver
{
    public PageId Id;
    public BaseInteractContObject InteractCont;
    public PageUIBase PageUI;

    public void SetRoot(BaseInteractContObject go)
    {
        InteractCont = go;
        PageUI = null;
    }

    public void SetPageUI(PageUIBase ui)
    {
        PageUI = ui;
        InteractCont = null;
    }

    // Unityシリアライズ後の整合性維持（XOR）
    void ISerializationCallbackReceiver.OnBeforeSerialize() { }

    void ISerializationCallbackReceiver.OnAfterDeserialize()
    {
        if (InteractCont != null && PageUI != null)
        {
            // どちらかを優先（例: pageUIを優先）
            InteractCont = null;
        }
    }
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

    /// <summary>
    /// ページ定義から内部マップを構築し、初期状態では全ページを非表示にする。
    /// </summary>
    private void Awake()
    {
        _map.Clear();
        foreach (PageEntry page in _pages)
        {
            if (page.InteractCont == null && page.PageUI == null) continue;

            if (page.PageUI != null)
            {
                SetDictionary(page, page.PageUI);
            }
            else if (page.InteractCont != null)
            {
                SetDictionary(page, page.InteractCont);
            }         
        }
    }

    private void SetDictionary(PageEntry page, Component obj)
    {
        if (obj is IInjectPageRouter router) router.SetNavigate(Router.Default);

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
}

public interface IInjectPageRouter
{
    /// <summary>
    /// ページ遷移コマンドの Publisher を注入する。
    /// </summary>
    /// <param name="cmd">コマンド Publisher</param>
    public void SetNavigate(ICommandPublisher cmd);
}
