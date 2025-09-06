using System;
using UnityEngine;
using UnityEngine.UI;

public enum ReticleType
{
    Default,
    Button,
    Gun,
    Tool,
    Monitor
}

public class CenterReticleUI : MonoBehaviour
{
    [SerializeField, Required] private Image _reticleImage;

    [Header("レティクルアイコン")]
    [SerializeField, Required]  private Sprite _defaultReticleSprite;
    [SerializeField, Required]  private Sprite _buttonSprite;
    [SerializeField, Required] private Sprite _gunSprite;
    [SerializeField, Required] private Sprite _toolSprite;
    //[SerializeField, Required] private Sprite _moniterSprite;

    [SerializeField] private Color _defaultColor = Color.white;

    private void ShowReticle(Sprite sprite)
    {
        if (_reticleImage == null) return;
        _reticleImage.sprite = sprite;
    }

    public void GetReticleIcon(Component type)
    {
        Sprite sprite = null;

        switch (type)
        {
            case  BaseInteractButtonObject t :
                sprite = _buttonSprite;
                break;
            case BaseInteractContObject t:
                sprite = _gunSprite;
                break;
            case BasePickUpToolObject t:
                sprite = _toolSprite;
                break;
        }
        if(sprite == null) sprite = _defaultReticleSprite;

        ShowReticle(sprite);
    }
}
