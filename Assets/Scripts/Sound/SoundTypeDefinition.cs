using UnityEngine;

[CreateAssetMenu(fileName = "SoundTypeDefinition", menuName = "Sound/Sound Type Definition")]
public class SoundTypeDefinition : ScriptableObject
{
    [Header("サウンドタイプ設定")]
    [SerializeField] private string[] soundTypeNames = new string[0];

    public string[] SoundTypeNames => soundTypeNames;

    public void UpdateSoundTypes(string[] newTypes)
    {
        soundTypeNames = newTypes;
    }
}
