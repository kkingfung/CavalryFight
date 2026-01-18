#nullable enable

using UnityEngine;
using CavalryFight.Core.Services;
using CavalryFight.Services.Audio;

/// <summary>
/// 一定時間後に自動削除されるオーディオトリガー
/// </summary>
/// <remarks>
/// MasterStylizedProjectileシステムから使用されます。
/// AudioServiceを経由してSFXボリューム設定を反映します。
/// </remarks>
public class AudioTrigger : MonoBehaviour
{
    public float time = 3;
    public AudioClip? onClip;

    void Start()
    {
        if (onClip != null)
        {
            // AudioServiceを経由して再生（ボリューム設定を反映）
            var audioService = ServiceLocator.Instance.Get<IAudioService>();
            if (audioService != null)
            {
                audioService.PlaySfxAtPosition(onClip, transform.position);
            }
            else
            {
                // フォールバック: AudioServiceがない場合は直接再生
                var audio = gameObject.AddComponent<AudioSource>();
                audio.clip = onClip;
                audio.Play();
            }
        }
        AutoDestroy(time);
    }

    void AutoDestroy(float time)
    {
        Invoke("DestroyThis", time);
    }

    public void DestroyThis()
    {
        Destroy(gameObject);
    }
}
