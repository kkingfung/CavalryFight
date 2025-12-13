#nullable enable

using System;
using System.Collections;
using UnityEngine;

namespace CavalryFight.Services.Audio
{
    /// <summary>
    /// オーディオ管理サービスの実装
    /// </summary>
    /// <remarks>
    /// AudioSourceを使用してBGM、SEの再生とボリューム管理を行います。
    /// </remarks>
    public class AudioService : IAudioService
    {
        #region Events

        /// <summary>
        /// BGMが変更された時に発生します。
        /// </summary>
        public event EventHandler<AudioChangedEventArgs>? BgmChanged;

        /// <summary>
        /// ボリュームが変更された時に発生します。
        /// </summary>
        public event EventHandler<VolumeChangedEventArgs>? VolumeChanged;

        #endregion

        #region Fields

        private float _masterVolume = 1.0f;
        private float _bgmVolume = 0.7f;
        private float _sfxVolume = 1.0f;
        private bool _isBgmMuted = false;
        private bool _isSfxMuted = false;
        private string? _currentBgmName;
        private AudioManager? _audioManager;

        #endregion

        #region Properties

        /// <summary>
        /// マスターボリュームを取得または設定します（0.0～1.0）
        /// </summary>
        public float MasterVolume
        {
            get => _masterVolume;
            set
            {
                _masterVolume = Mathf.Clamp01(value);
                UpdateBgmVolume();
                VolumeChanged?.Invoke(this, new VolumeChangedEventArgs(VolumeType.Master, _masterVolume));
            }
        }

        /// <summary>
        /// BGMボリュームを取得または設定します（0.0～1.0）
        /// </summary>
        public float BgmVolume
        {
            get => _bgmVolume;
            set
            {
                _bgmVolume = Mathf.Clamp01(value);
                UpdateBgmVolume();
                VolumeChanged?.Invoke(this, new VolumeChangedEventArgs(VolumeType.Bgm, _bgmVolume));
            }
        }

        /// <summary>
        /// SEボリュームを取得または設定します（0.0～1.0）
        /// </summary>
        public float SfxVolume
        {
            get => _sfxVolume;
            set
            {
                _sfxVolume = Mathf.Clamp01(value);
                VolumeChanged?.Invoke(this, new VolumeChangedEventArgs(VolumeType.Sfx, _sfxVolume));
            }
        }

        /// <summary>
        /// BGMがミュートされているかを取得または設定します。
        /// </summary>
        public bool IsBgmMuted
        {
            get => _isBgmMuted;
            set
            {
                _isBgmMuted = value;
                UpdateBgmVolume();
            }
        }

        /// <summary>
        /// SEがミュートされているかを取得または設定します。
        /// </summary>
        public bool IsSfxMuted
        {
            get => _isSfxMuted;
            set => _isSfxMuted = value;
        }

        /// <summary>
        /// 現在再生中のBGM名を取得します。
        /// </summary>
        public string? CurrentBgmName => _currentBgmName;

        /// <summary>
        /// BGMが再生中かどうかを取得します。
        /// </summary>
        public bool IsBgmPlaying => _audioManager?.BgmSource?.isPlaying ?? false;

        #endregion

        #region IService Implementation

        /// <summary>
        /// サービスを初期化します。
        /// </summary>
        /// <remarks>
        /// ServiceLocatorに登録された直後に呼び出されます。
        /// オーディオ管理用のGameObjectとAudioSourceを作成します。
        /// </remarks>
        public void Initialize()
        {
            Debug.Log("[AudioService] Initializing...");

            // AudioManager用のGameObjectを作成
            var managerObject = new GameObject("AudioManager");
            GameObject.DontDestroyOnLoad(managerObject);
            _audioManager = managerObject.AddComponent<AudioManager>();
            _audioManager.Initialize(this);

            UpdateBgmVolume();

            Debug.Log("[AudioService] Initialized.");
        }

        /// <summary>
        /// サービスを破棄し、リソースを解放します。
        /// </summary>
        /// <remarks>
        /// イベントハンドラをクリアし、AudioManagerを破棄します。
        /// </remarks>
        public void Dispose()
        {
            Debug.Log("[AudioService] Disposing...");

            // イベントハンドラをクリア
            BgmChanged = null;
            VolumeChanged = null;

            // AudioManagerを破棄
            if (_audioManager != null)
            {
                GameObject.Destroy(_audioManager.gameObject);
                _audioManager = null;
            }
        }

        #endregion

        #region BGM Control

        /// <summary>
        /// BGMを再生します。
        /// </summary>
        /// <param name="clip">再生するAudioClip</param>
        /// <param name="loop">ループ再生するか</param>
        /// <param name="fadeInDuration">フェードイン時間（秒）</param>
        public void PlayBgm(AudioClip clip, bool loop = true, float fadeInDuration = 0f)
        {
            if (clip == null)
            {
                Debug.LogWarning("[AudioService] BGM clip is null.");
                return;
            }

            if (_audioManager?.BgmSource == null)
            {
                Debug.LogError("[AudioService] BGM AudioSource is not initialized.");
                return;
            }

            _currentBgmName = clip.name;

            if (fadeInDuration > 0f)
            {
                _audioManager.StartCoroutine(FadeInBgm(clip, loop, fadeInDuration));
            }
            else
            {
                _audioManager.BgmSource.clip = clip;
                _audioManager.BgmSource.loop = loop;
                _audioManager.BgmSource.Play();
            }

            BgmChanged?.Invoke(this, new AudioChangedEventArgs(clip.name));
            Debug.Log($"[AudioService] BGM started: {clip.name}");
        }

        /// <summary>
        /// BGMを停止します。
        /// </summary>
        /// <param name="fadeOutDuration">フェードアウト時間（秒）</param>
        public void StopBgm(float fadeOutDuration = 0f)
        {
            if (_audioManager?.BgmSource == null)
                return;

            if (fadeOutDuration > 0f)
            {
                _audioManager.StartCoroutine(FadeOutBgm(fadeOutDuration));
            }
            else
            {
                _audioManager.BgmSource.Stop();
                _currentBgmName = null;
                BgmChanged?.Invoke(this, new AudioChangedEventArgs(string.Empty));
            }

            Debug.Log("[AudioService] BGM stopped.");
        }

        /// <summary>
        /// BGMを一時停止します。
        /// </summary>
        public void PauseBgm()
        {
            if (_audioManager?.BgmSource == null)
                return;

            _audioManager.BgmSource.Pause();
            Debug.Log("[AudioService] BGM paused.");
        }

        /// <summary>
        /// BGMを再開します。
        /// </summary>
        public void ResumeBgm()
        {
            if (_audioManager?.BgmSource == null)
                return;

            _audioManager.BgmSource.UnPause();
            Debug.Log("[AudioService] BGM resumed.");
        }

        #endregion

        #region SFX Control

        /// <summary>
        /// SEを再生します（ワンショット）
        /// </summary>
        /// <param name="clip">再生するAudioClip</param>
        /// <param name="volumeScale">ボリューム倍率（0.0～1.0）</param>
        public void PlaySfx(AudioClip clip, float volumeScale = 1.0f)
        {
            if (clip == null || _isSfxMuted)
                return;

            if (_audioManager?.SfxSource == null)
            {
                Debug.LogError("[AudioService] SFX AudioSource is not initialized.");
                return;
            }

            float volume = _masterVolume * _sfxVolume * Mathf.Clamp01(volumeScale);
            _audioManager.SfxSource.PlayOneShot(clip, volume);
        }

        /// <summary>
        /// 3D空間でSEを再生します。
        /// </summary>
        /// <param name="clip">再生するAudioClip</param>
        /// <param name="position">再生位置</param>
        /// <param name="volumeScale">ボリューム倍率（0.0～1.0）</param>
        public void PlaySfxAtPosition(AudioClip clip, Vector3 position, float volumeScale = 1.0f)
        {
            if (clip == null || _isSfxMuted)
                return;

            float volume = _masterVolume * _sfxVolume * Mathf.Clamp01(volumeScale);
            AudioSource.PlayClipAtPoint(clip, position, volume);
        }

        #endregion

        #region Volume Control

        /// <summary>
        /// すべてのオーディオをミュートします。
        /// </summary>
        public void MuteAll()
        {
            IsBgmMuted = true;
            IsSfxMuted = true;
            Debug.Log("[AudioService] All audio muted.");
        }

        /// <summary>
        /// すべてのオーディオのミュートを解除します。
        /// </summary>
        public void UnmuteAll()
        {
            IsBgmMuted = false;
            IsSfxMuted = false;
            Debug.Log("[AudioService] All audio unmuted.");
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// BGMのボリュームを更新します。
        /// </summary>
        private void UpdateBgmVolume()
        {
            if (_audioManager?.BgmSource == null)
                return;

            _audioManager.BgmSource.volume = _isBgmMuted ? 0f : _masterVolume * _bgmVolume;
        }

        /// <summary>
        /// BGMをフェードインします。
        /// </summary>
        /// <param name="clip">再生するAudioClip</param>
        /// <param name="loop">ループ再生するか</param>
        /// <param name="duration">フェードイン時間（秒）</param>
        /// <returns>コルーチン</returns>
        private IEnumerator FadeInBgm(AudioClip clip, bool loop, float duration)
        {
            if (_audioManager?.BgmSource == null)
                yield break;

            var bgmSource = _audioManager.BgmSource;
            bgmSource.clip = clip;
            bgmSource.loop = loop;
            bgmSource.volume = 0f;
            bgmSource.Play();

            float targetVolume = _isBgmMuted ? 0f : _masterVolume * _bgmVolume;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                bgmSource.volume = Mathf.Lerp(0f, targetVolume, t);
                yield return null;
            }

            bgmSource.volume = targetVolume;
        }

        /// <summary>
        /// BGMをフェードアウトします。
        /// </summary>
        /// <param name="duration">フェードアウト時間（秒）</param>
        /// <returns>コルーチン</returns>
        private IEnumerator FadeOutBgm(float duration)
        {
            if (_audioManager?.BgmSource == null)
                yield break;

            var bgmSource = _audioManager.BgmSource;
            float startVolume = bgmSource.volume;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                bgmSource.volume = Mathf.Lerp(startVolume, 0f, t);
                yield return null;
            }

            bgmSource.volume = 0f;
            bgmSource.Stop();
            _currentBgmName = null;
        }

        #endregion

        #region AudioManager MonoBehaviour

        /// <summary>
        /// オーディオ管理用のMonoBehaviour
        /// </summary>
        /// <remarks>
        /// AudioServiceの内部クラスとして定義し、AudioSourceを管理します。
        /// </remarks>
        private class AudioManager : MonoBehaviour
        {
            /// <summary>
            /// BGM用のAudioSource
            /// </summary>
            public AudioSource? BgmSource { get; private set; }

            /// <summary>
            /// SE用のAudioSource
            /// </summary>
            public AudioSource? SfxSource { get; private set; }

            /// <summary>
            /// AudioManagerを初期化します。
            /// </summary>
            /// <param name="service">親となるAudioService</param>
            public void Initialize(AudioService service)
            {
                // BGM用AudioSource
                BgmSource = gameObject.AddComponent<AudioSource>();
                BgmSource.playOnAwake = false;
                BgmSource.loop = true;

                // SE用AudioSource
                SfxSource = gameObject.AddComponent<AudioSource>();
                SfxSource.playOnAwake = false;
                SfxSource.loop = false;

                Debug.Log("[AudioManager] Initialized.");
            }
        }

        #endregion
    }
}
