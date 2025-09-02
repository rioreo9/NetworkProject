using System;
using UnityEngine;


[Serializable]
public abstract class InterfaceReferenceBase
{
    [SerializeField]
    internal UnityEngine.Object _reference;

    internal abstract Type InterfaceType { get; }
}

[Serializable]
public sealed class InterfaceReference<T> : InterfaceReferenceBase where T : class
{
    internal override Type InterfaceType => typeof(T);

    public T Value
    {
        get
        {
            if (_reference == null) return null;

            if (_reference is T direct) return direct;

            if (_reference is GameObject go)
            {
                var comp = go.GetComponent(typeof(T));
                return comp as T;
            }

            return null;
        }
    }

    public bool HasValue => Value != null;
    public UnityEngine.Object RawReference => _reference;

    public static implicit operator T(InterfaceReference<T> r) => r?.Value;
}
