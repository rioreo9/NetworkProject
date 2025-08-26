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
    [SerializeField, Required] private CanvasGroup _canvasGroup;
    [SerializeField, Required] private Image _reticleImage;

    [Header("レティクルアイコン")]
    [SerializeField, Required]  private Sprite _defaultReticleSprite;
    [SerializeField, Required]  private Sprite _buttonSprite;
    [SerializeField, Required] private Sprite _gunSprite;
    [SerializeField, Required] private Sprite _toolSprite;
    [SerializeField, Required] private Sprite _moniterSprite;

    [SerializeField] private Color _defaultColor = Color.white;

    public void ShowReticle(ReticleType type)
    {

    }

    public void HideReticle()
    {
        _canvasGroup.alpha = 0;
    }

    public Sprite GetReticleIcon(ReticleType type)
    {
        return type switch
        {
            ReticleType.Default => _defaultReticleSprite,
            ReticleType.Button => _buttonSprite,
            ReticleType.Gun => _gunSprite,
            ReticleType.Tool => _toolSprite,
            ReticleType.Monitor => _moniterSprite,
            _ => _defaultReticleSprite
        };
    }
}
