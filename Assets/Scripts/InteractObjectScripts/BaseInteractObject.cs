using Fusion;

using UnityEngine;

public abstract class BaseInteractObject : NetworkBehaviour, IInteractObject
{
    protected bool _isInteractable = false;

    protected bool _isButtonPressed = false;

    protected Vector2 _InputXY = default;

    public bool IsInteractable { get => _isInteractable; set => _isInteractable = value; }

    public bool IsButtonPressed { get => _isButtonPressed; set => _isButtonPressed = value; }

    public Vector2 InputXY { get => _InputXY; set => _InputXY = value; }

    public abstract void ControlInteractObject();
}


public interface IInteractObject
{
    /// <summary>
    /// インタラクトできるかどうかを示すプロパティ
    /// </summary>
    public bool IsInteractable { get; set; }

    public bool IsButtonPressed { get; set; }


  
}
