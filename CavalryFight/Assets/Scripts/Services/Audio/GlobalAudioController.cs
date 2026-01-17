#nullable enable

using System.Collections.Generic;
using UnityEngine;
using CavalryFight.Core.Services;

namespace CavalryFight.Services.Audio
{
    /// <summary>
    /// グローバルオーディオコントローラー
    /// </summary>
    /// <remarks>
    /// シーン内のすべてのAudioSourceのボリュームを制御します。
    /// サードパーティアセット（Malbers Horse、P09など）のAudioSourceも含めて
    /// ボリューム設定を適用します。
    /// </remarks>
    public class GlobalAudioController : MonoBehaviour
    {
        #region Singleton

        private static GlobalAudioController? _instance;

        /// <summary>
        /// シングルトンインスタンス
        /// </summary>
        public static GlobalAudioController? Instance => _instance;

        #endregion

        #region Private Fields

        private IAudioService? _audioService;
        private float _lastMasterVolume = 1f;
        private float _lastSfxVolume = 1f;

        /// <summary>
        /// 除外するAudioSource（BGM用など）
        /// </summary>
        private readonly HashSet<AudioSource> _excludedSources = new HashSet<AudioSource>();

        /// <summary>
        /// 元のボリュームを保存（AudioSource -> 元のボリューム）
        /// </summary>
        private readonly Dictionary<AudioSource, float> _originalVolumes = new Dictionary<AudioSource, float>();

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // シングルトン設定
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            _audioService = ServiceLocator.Instance.Get<IAudioService>();

            if (_audioService != null)
            {
                _lastMasterVolume = _audioService.MasterVolume;
                _lastSfxVolume = _audioService.SfxVolume;

                // ボリューム変更イベントを購読
                _audioService.VolumeChanged += OnVolumeChanged;
            }

            Debug.Log("[GlobalAudioController] Initialized.");
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }

            if (_audioService != null)
            {
                _audioService.VolumeChanged -= OnVolumeChanged;
            }
        }

        private void LateUpdate()
        {
            // 定期的にボリュームを適用（新しいAudioSourceが追加された場合に対応）
            if (_audioService != null)
            {
                float currentMaster = _audioService.MasterVolume;
                float currentSfx = _audioService.SfxVolume;

                // ボリュームが変わった場合のみ更新
                if (!Mathf.Approximately(_lastMasterVolume, currentMaster) ||
                    !Mathf.Approximately(_lastSfxVolume, currentSfx))
                {
                    _lastMasterVolume = currentMaster;
                    _lastSfxVolume = currentSfx;
                    ApplyVolumeToAllSources();
                }
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// AudioSourceを除外リストに追加します（BGM用など）
        /// </summary>
        public void ExcludeSource(AudioSource source)
        {
            if (source != null)
            {
                _excludedSources.Add(source);
            }
        }

        /// <summary>
        /// AudioSourceを除外リストから削除します
        /// </summary>
        public void IncludeSource(AudioSource source)
        {
            if (source != null)
            {
                _excludedSources.Remove(source);
            }
        }

        /// <summary>
        /// すべてのAudioSourceにボリュームを適用します
        /// </summary>
        public void ApplyVolumeToAllSources()
        {
            if (_audioService == null)
            {
                return;
            }

            float volumeMultiplier = _audioService.MasterVolume * _audioService.SfxVolume;

            // シーン内のすべてのAudioSourceを取得
            AudioSource[] allSources = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);

            foreach (var source in allSources)
            {
                if (source == null || _excludedSources.Contains(source))
                {
                    continue;
                }

                // 元のボリュームを保存（初回のみ）
                if (!_originalVolumes.ContainsKey(source))
                {
                    _originalVolumes[source] = source.volume;
                }

                // ボリュームを適用
                float originalVolume = _originalVolumes[source];
                source.volume = originalVolume * volumeMultiplier;
            }

            // 破棄されたAudioSourceをクリーンアップ
            CleanupDestroyedSources();

            Debug.Log($"[GlobalAudioController] Applied volume to {allSources.Length} sources. Multiplier: {volumeMultiplier:F2}");
        }

        /// <summary>
        /// ボリュームをリセットします（元のボリュームに戻す）
        /// </summary>
        public void ResetVolumes()
        {
            foreach (var kvp in _originalVolumes)
            {
                if (kvp.Key != null)
                {
                    kvp.Key.volume = kvp.Value;
                }
            }

            _originalVolumes.Clear();
            Debug.Log("[GlobalAudioController] Volumes reset.");
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// ボリューム変更イベントハンドラー
        /// </summary>
        private void OnVolumeChanged(object? sender, VolumeChangedEventArgs e)
        {
            if (e.Type == VolumeType.Master || e.Type == VolumeType.Sfx)
            {
                ApplyVolumeToAllSources();
            }
        }

        /// <summary>
        /// 破棄されたAudioSourceをクリーンアップ
        /// </summary>
        private void CleanupDestroyedSources()
        {
            var keysToRemove = new List<AudioSource>();

            foreach (var kvp in _originalVolumes)
            {
                if (kvp.Key == null)
                {
                    keysToRemove.Add(kvp.Key);
                }
            }

            foreach (var key in keysToRemove)
            {
                _originalVolumes.Remove(key);
            }

            _excludedSources.RemoveWhere(s => s == null);
        }

        #endregion
    }
}
