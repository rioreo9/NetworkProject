using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.EventSystems;

/// <summary>
/// ボタンアニメーションの制御
/// テストのため、MonoBehaviour継承してます
/// </summary>
public class ButtonAnimation: MonoBehaviour, 
    IPointerClickHandler,
    IPointerDownHandler,
    IPointerUpHandler,
    IPointerEnterHandler,
    IPointerExitHandler,
    ISelectHandler,
    IDeselectHandler,
    ISubmitHandler
{
    [SerializeField]
    private Button _button = default;
    [SerializeField]
    private Image _buttonImage = default;
    [SerializeField]
    private Color _buttonHighlightColor = default;
    [SerializeField]
    private Image _outlineImage = default;
    [SerializeField]
    private Color _outlineHighlightColor = default;

    /// <summary>
    /// クリック
    /// (長押しの場合、終わるまで呼ばれない・クリックした場所と離した場所が同じ場合に呼ばれる)
    /// </summary>
    /// <param name="eventData"></param>
    public void OnPointerClick(PointerEventData eventData)
    {
        if(!_button.interactable)
        {
            return;
        }

        transform.DOKill();
        transform.localScale = Vector3.one;
        transform.DOPunchScale(Vector3.one * 0.1f, 0.2f, 10, 1).SetLink(gameObject);
        Debug.Log("クリック");
    }

    /// <summary>
    /// クリックした瞬間
    /// </summary>
    /// <param name="eventData"></param>
    public void OnPointerDown(PointerEventData eventData)
    {
        if(!_button.interactable)
        {
            return;
        }
        transform.DOKill();
        transform.localScale = Vector3.one;
        transform.DOPunchScale(Vector3.one * 0.1f, 0.2f, 10, 1).SetLink(gameObject);
        Debug.Log("クリックしたとき");
    }

    /// <summary>
    /// クリックを辞めたとき(終わったとき)
    /// </summary>
    /// <param name="eventData"></param>
    public void OnPointerUp(PointerEventData eventData)
    {
        if(!_button.interactable)
        {
            return;
        }
        transform.localScale = Vector3.one;
        transform.DOPunchScale(Vector3.one * -0.1f, 0.2f, 10, 1).SetLink(gameObject);
        Debug.Log("クリックを辞めたとき");
    }

    /// <summary>
    /// マウスカーソルが乗ったとき
    /// </summary>
    /// <param name="eventData"></param>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if(!_button.interactable)
        {
            return;
        }
        transform.DOScale(1.1f, 0.2f).SetLink(gameObject);
        _buttonImage.color = _buttonHighlightColor;
        _outlineImage.color = _outlineHighlightColor;
        Debug.Log("マウスカーソルが乗ったとき");
    }

    /// <summary>
    /// マウスカーソルが離れたとき
    /// </summary>
    /// <param name="eventData"></param>
    public void OnPointerExit(PointerEventData eventData)
    {
        if(!_button.interactable)
        {
            return;
        }
        transform.DOScale(1.0f, 0.2f).SetLink(gameObject);
        _buttonImage.color = Color.white;
        _outlineImage.color = Color.white;
        Debug.Log("マウスカーソルが離れたとき");
    }

    /// <summary>
    /// 選択
    /// </summary>
    /// <param name="eventData"></param>
    public void OnSelect(BaseEventData eventData)
    {
        if(!_button.interactable)
        {
            return;
        }
        _buttonImage.color = _buttonHighlightColor;
        _outlineImage.color = _outlineHighlightColor;
        transform.DOScale(1.1f, 0.2f).SetLink(gameObject);
        //Debug.Log("選択");
    }

    /// <summary>
    /// 選択やめたとき
    /// </summary>
    /// <param name="eventData"></param>
    public void OnDeselect(BaseEventData eventData)
    {
        if(!_button.interactable)
        {
            return;
        }
        _outlineImage.color = Color.white;
        _buttonImage.color = Color.white;
        transform.DOScale(1.0f, 0.2f).SetLink(gameObject);
        Debug.Log("選択やめたとき");
    }

    /// <summary>
    /// 決定
    /// </summary>
    /// <param name="eventData"></param>
    public void OnSubmit(BaseEventData eventData)
    {
        if(!_button.interactable)
        {
            return;
        }
        transform.DOKill();
        transform.localScale = Vector3.one;
        transform.DOPunchScale(Vector3.one * 0.1f, 0.2f, 10, 1).SetLink(gameObject);
        Debug.Log("決定");
    }
}
