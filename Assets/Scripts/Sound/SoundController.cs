using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using VitalRouter;

[Routes]
public partial class SoundController : MonoBehaviour
{
    [Header("サウンド設定")]
    [SerializeField] private AudioClip[] _audioClips;
    [SerializeField] private SoundTypeDefinition _soundTypeDefinition;
    [Header("オーディオソース")]
    [SerializeField] private AudioSource _audioSource;

    private Dictionary<string, AudioClip> _soundMap = new Dictionary<string, AudioClip>();
    private IDisposable _disposable;

    private void Awake()
    {
        for(int i = 0; _audioClips.Length > i; i++)
        {
            if (_audioClips[i] == null) continue;

           SetDictionary(_audioClips[i]);
            Debug.Log($"SoundClip {_audioClips[i].name} を登録しました" );
        }
    }

    private void SetDictionary(AudioClip clip)
    {
        for (int i = 0; _soundTypeDefinition.SoundTypeNames.Length > i; i++)
        {
            if (clip.name == _soundTypeDefinition.SoundTypeNames[i])
            {
                if (!_soundMap.ContainsKey(clip.name))
                {
                    _soundMap.Add(clip.name, clip);
                }
                else
                {
                    Debug.LogWarning($"SoundType {_soundTypeDefinition.SoundTypeNames[i]} は既に登録されています");
                }
                return;
            }
        }
    }

    public void PlaySound(PlaySoundCommand cmd)
    {
        int index = (int)cmd.SoundType;

        if(_soundMap.TryGetValue(_soundTypeDefinition.SoundTypeNames[index], out AudioClip clip))
        {
            AudioSource.PlayClipAtPoint(clip, cmd.Position, cmd.Volume);
            Debug.Log($"SoundType {_soundTypeDefinition.SoundTypeNames[index]} を再生しました");
        }
        else
        {
            Debug.LogWarning($"SoundType {_soundTypeDefinition.SoundTypeNames[index]} は登録されていません");
            return;
        }
    }

    private void OnEnable() => _disposable = this.MapTo(Router.Default);
    private void OnDisable() => _disposable?.Dispose();
}
