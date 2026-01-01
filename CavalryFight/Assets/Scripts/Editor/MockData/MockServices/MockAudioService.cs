#nullable enable

#if UNITY_EDITOR

using System;
using CavalryFight.Services.Audio;
using UnityEngine;

namespace CavalryFight.Editor.MockData.MockServices
{
    /// <summary>
    /// モックのオーディオサービス
    /// </summary>
    /// <remarks>
    /// 開発時に個別シーンをテストする際に使用します。
    /// 実際の音声再生は行わず、デバッグログを出力します。
    /// </remarks>
    public class MockAudioService : IAudioService
    {
        #region Events

        /// <summary>
        /// BGMが変更された時に発生します
        /// </summary>
        public event EventHandler<AudioChangedEventArgs>? BgmChanged;

        /// <summary>
        /// ボリュームが変更された時に発生します
        /// </summary>
        public event EventHandler<VolumeChangedEventArgs>? VolumeChanged;

        #endregion

        #region Properties

        /// <summary>
        /// マスターボリュームを取得または設定します（0.0～1.0）
        /// </summary>
        public float MasterVolume { get; set; } = 1f;

        /// <summary>
        /// BGMボリュームを取得または設定します（0.0～1.0）
        /// </summary>
        public float BgmVolume { get; set; } = 1f;

        /// <summary>
        /// SEボリュームを取得または設定します（0.0～1.0）
        /// </summary>
        public float SfxVolume { get; set; } = 1f;

        /// <summary>
        /// BGMがミュートされているかを取得または設定します
        /// </summary>
        public bool IsBgmMuted { get; set; }

        /// <summary>
        /// SEがミュートされているかを取得または設定します
        /// </summary>
        public bool IsSfxMuted { get; set; }

        /// <summary>
        /// 現在再生中のBGM名を取得します
        /// </summary>
        public string? CurrentBgmName { get; private set; }

        /// <summary>
        /// BGMが再生中かどうかを取得します
        /// </summary>
        public bool IsBgmPlaying { get; private set; }

        #endregion

        #region Methods

        /// <summary>
        /// BGMを再生します
        /// </summary>
        /// <param name="clip">再生するAudioClip</param>
        /// <param name="loop">ループ再生するか</param>
        /// <param name="fadeInDuration">フェードイン時間（秒）</param>
        public void PlayBgm(AudioClip clip, bool loop = true, float fadeInDuration = 0)
        {
            CurrentBgmName = clip?.name ?? "Unknown";
            IsBgmPlaying = true;
            Debug.Log($"[MockAudioService] PlayBgm: {CurrentBgmName} (loop: {loop}, fade: {fadeInDuration}s)");
        }

        /// <summary>
        /// BGMを停止します
        /// </summary>
        /// <param name="fadeOutDuration">フェードアウト時間（秒）</param>
        public void StopBgm(float fadeOutDuration = 0)
        {
            IsBgmPlaying = false;
            Debug.Log($"[MockAudioService] StopBgm (fade: {fadeOutDuration}s)");
        }

        /// <summary>
        /// BGMを一時停止します
        /// </summary>
        public void PauseBgm()
        {
            IsBgmPlaying = false;
            Debug.Log("[MockAudioService] PauseBgm");
        }

        /// <summary>
        /// BGMを再開します
        /// </summary>
        public void ResumeBgm()
        {
            IsBgmPlaying = true;
            Debug.Log("[MockAudioService] ResumeBgm");
        }

        /// <summary>
        /// SEを再生します（ワンショット）
        /// </summary>
        /// <param name="clip">再生するAudioClip</param>
        /// <param name="volumeScale">ボリューム倍率（0.0～1.0）</param>
        public void PlaySfx(AudioClip clip, float volumeScale = 1)
        {
            Debug.Log($"[MockAudioService] PlaySfx: {clip?.name ?? "null"} (volume: {volumeScale})");
        }

        /// <summary>
        /// 3D空間でSEを再生します
        /// </summary>
        /// <param name="clip">再生するAudioClip</param>
        /// <param name="position">再生位置</param>
        /// <param name="volumeScale">ボリューム倍率（0.0～1.0）</param>
        public void PlaySfxAtPosition(AudioClip clip, Vector3 position, float volumeScale = 1)
        {
            Debug.Log($"[MockAudioService] PlaySfxAtPosition: {clip?.name ?? "null"} at {position} (volume: {volumeScale})");
        }

        /// <summary>
        /// すべてのオーディオをミュートします
        /// </summary>
        public void MuteAll()
        {
            IsBgmMuted = true;
            IsSfxMuted = true;
            Debug.Log("[MockAudioService] MuteAll");
        }

        /// <summary>
        /// すべてのオーディオのミュートを解除します
        /// </summary>
        public void UnmuteAll()
        {
            IsBgmMuted = false;
            IsSfxMuted = false;
            Debug.Log("[MockAudioService] UnmuteAll");
        }

        #endregion
    }
}

#endif
