#nullable enable

using System;
using CavalryFight.Core.Services;
using UnityEngine;

namespace CavalryFight.Services.Audio
{
    /// <summary>
    /// オーディオ管理サービスのインターフェース
    /// </summary>
    /// <remarks>
    /// BGM、SEの再生、ボリューム管理を行います。
    /// MVVMパターンでのオーディオ制御を簡素化します。
    /// </remarks>
    public interface IAudioService : IService
    {
        #region Events

        /// <summary>
        /// BGMが変更された時に発生します。
        /// </summary>
        event EventHandler<AudioChangedEventArgs>? BgmChanged;

        /// <summary>
        /// ボリュームが変更された時に発生します。
        /// </summary>
        event EventHandler<VolumeChangedEventArgs>? VolumeChanged;

        #endregion

        #region Properties

        /// <summary>
        /// マスターボリュームを取得または設定します（0.0～1.0）
        /// </summary>
        float MasterVolume { get; set; }

        /// <summary>
        /// BGMボリュームを取得または設定します（0.0～1.0）
        /// </summary>
        float BgmVolume { get; set; }

        /// <summary>
        /// SEボリュームを取得または設定します（0.0～1.0）
        /// </summary>
        float SfxVolume { get; set; }

        /// <summary>
        /// BGMがミュートされているかを取得または設定します。
        /// </summary>
        bool IsBgmMuted { get; set; }

        /// <summary>
        /// SEがミュートされているかを取得または設定します。
        /// </summary>
        bool IsSfxMuted { get; set; }

        /// <summary>
        /// 現在再生中のBGM名を取得します。
        /// </summary>
        string? CurrentBgmName { get; }

        /// <summary>
        /// BGMが再生中かどうかを取得します。
        /// </summary>
        bool IsBgmPlaying { get; }

        #endregion

        #region BGM Control

        /// <summary>
        /// BGMを再生します。
        /// </summary>
        /// <param name="clip">再生するAudioClip</param>
        /// <param name="loop">ループ再生するか</param>
        /// <param name="fadeInDuration">フェードイン時間（秒）</param>
        void PlayBgm(AudioClip clip, bool loop = true, float fadeInDuration = 0f);

        /// <summary>
        /// BGMを停止します。
        /// </summary>
        /// <param name="fadeOutDuration">フェードアウト時間（秒）</param>
        void StopBgm(float fadeOutDuration = 0f);

        /// <summary>
        /// BGMを一時停止します。
        /// </summary>
        void PauseBgm();

        /// <summary>
        /// BGMを再開します。
        /// </summary>
        void ResumeBgm();

        #endregion

        #region SFX Control

        /// <summary>
        /// SEを再生します（ワンショット）
        /// </summary>
        /// <param name="clip">再生するAudioClip</param>
        /// <param name="volumeScale">ボリューム倍率（0.0～1.0）</param>
        void PlaySfx(AudioClip clip, float volumeScale = 1.0f);

        /// <summary>
        /// 3D空間でSEを再生します。
        /// </summary>
        /// <param name="clip">再生するAudioClip</param>
        /// <param name="position">再生位置</param>
        /// <param name="volumeScale">ボリューム倍率（0.0～1.0）</param>
        void PlaySfxAtPosition(AudioClip clip, Vector3 position, float volumeScale = 1.0f);

        #endregion

        #region Volume Control

        /// <summary>
        /// すべてのオーディオをミュートします。
        /// </summary>
        void MuteAll();

        /// <summary>
        /// すべてのオーディオのミュートを解除します。
        /// </summary>
        void UnmuteAll();

        #endregion
    }
}
