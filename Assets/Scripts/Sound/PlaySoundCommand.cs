using VitalRouter;
using UnityEngine;

public readonly struct PlaySoundCommand : ICommand
{
    public PlaySoundCommand(SoundType soundType, Vector3 pos, float volume = 1.0f)
    {
        SoundType = soundType;
        Position = pos;
        Volume = volume;
    }

    public SoundType SoundType { get; }
    public Vector3 Position { get; }
    public float Volume { get; }
}

public readonly struct StopSoundCommand : ICommand
{
    public StopSoundCommand(SoundType soundType)
    {
        SoundType = soundType;
    }
    public SoundType SoundType { get; }
}

public readonly struct SetCategoryVolumeCommand : ICommand
{
    public SetCategoryVolumeCommand(SoundCategory category, float volume)
    {
        Category = category;
        Volume = volume;
    }
    public SoundCategory Category { get; }
    public float Volume { get; }
}

public interface IInjectPageRouter
{
    /// <summary>
    /// ページ遷移コマンドの Publisher を注入する。
    /// </summary>
    /// <param name="cmd">コマンド Publisher</param>
    public void SetNavigate(ICommandPublisher cmd);
}

// 音声カテゴリ
public enum SoundCategory
{
    Music,      // BGM
    SFX,        // 効果音
    Voice,      // 音声
    Ambient     // 環境音
}
