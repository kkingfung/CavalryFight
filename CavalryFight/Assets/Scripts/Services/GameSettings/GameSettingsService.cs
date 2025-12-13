#nullable enable

using System;
using System.IO;
using UnityEngine;
using CavalryFight.Core.Services;
using CavalryFight.Services.Audio;
using CavalryFight.Services.Input;

namespace CavalryFight.Services.GameSettings
{
    /// <summary>
    /// ゲーム設定管理サービスの実装
    /// </summary>
    /// <remarks>
    /// ゲーム設定をJSON形式でファイルに保存/読み込みします。
    /// Application.persistentDataPath を使用してユーザー設定を永続化します。
    /// 他のサービス（Audio、Input等）と連携して設定を適用します。
    /// </remarks>
    public class GameSettingsService : IGameSettingsService
    {
        #region Constants

        private const string SETTINGS_FILE_NAME = "GameSettings.json";

        #endregion

        #region Events

        /// <summary>
        /// 設定が変更された時に発生します。
        /// </summary>
        public event EventHandler<SettingsChangedEventArgs>? SettingsChanged;

        /// <summary>
        /// 設定が適用された時に発生します。
        /// </summary>
        public event EventHandler? SettingsApplied;

        /// <summary>
        /// 設定がリセットされた時に発生します。
        /// </summary>
        public event EventHandler? SettingsReset;

        #endregion

        #region Fields

        private SettingsProfile _currentProfile;
        private SettingsProfile _pendingProfile;
        private string _settingsFilePath;

        #endregion

        #region Properties

        /// <summary>
        /// 現在の設定プロファイルを取得します。
        /// </summary>
        public SettingsProfile CurrentProfile => _currentProfile;

        /// <summary>
        /// 保留中の設定（まだ適用されていない設定）を取得します。
        /// </summary>
        public SettingsProfile PendingProfile => _pendingProfile;

        /// <summary>
        /// 保留中の変更があるかどうかを取得します。
        /// </summary>
        public bool HasPendingChanges { get; private set; }

        #endregion

        #region Constructor

        /// <summary>
        /// GameSettingsServiceの新しいインスタンスを初期化します。
        /// </summary>
        public GameSettingsService()
        {
            _settingsFilePath = Path.Combine(Application.persistentDataPath, SETTINGS_FILE_NAME);
            _currentProfile = SettingsProfile.CreateDefault();
            _pendingProfile = _currentProfile.Clone();
            HasPendingChanges = false;
        }

        #endregion

        #region IService Implementation

        /// <summary>
        /// サービスを初期化します。
        /// </summary>
        /// <remarks>
        /// 保存された設定があれば読み込み、なければデフォルトを使用します。
        /// </remarks>
        public void Initialize()
        {
            Debug.Log("[GameSettingsService] Initializing...");

            // 保存された設定を読み込む
            if (!LoadSettings())
            {
                Debug.Log("[GameSettingsService] No saved settings found. Using default settings.");
                _currentProfile = SettingsProfile.CreateDefault();
                _pendingProfile = _currentProfile.Clone();
            }

            // 設定を即座に適用
            ApplySettingsInternal(_currentProfile, saveAfterApply: false);

            Debug.Log("[GameSettingsService] Initialized.");
        }

        /// <summary>
        /// サービスを破棄し、リソースを解放します。
        /// </summary>
        public void Dispose()
        {
            Debug.Log("[GameSettingsService] Disposing...");

            // 現在の設定を保存
            SaveSettings();

            // イベントハンドラをクリア
            SettingsChanged = null;
            SettingsApplied = null;
            SettingsReset = null;
        }

        #endregion

        #region Settings Management

        /// <summary>
        /// 設定を保留します（まだ適用しない）
        /// </summary>
        /// <param name="profile">保留する設定</param>
        public void SetPendingSettings(SettingsProfile profile)
        {
            _pendingProfile = profile.Clone();
            HasPendingChanges = true;

            Debug.Log("[GameSettingsService] Pending settings updated.");
        }

        /// <summary>
        /// 保留中の設定を適用します。
        /// </summary>
        public void ApplySettings()
        {
            if (!HasPendingChanges)
            {
                Debug.Log("[GameSettingsService] No pending changes to apply.");
                return;
            }

            Debug.Log("[GameSettingsService] Applying settings...");

            // 保留中の設定を現在の設定にコピー
            _currentProfile = _pendingProfile.Clone();
            HasPendingChanges = false;

            // 設定を適用
            ApplySettingsInternal(_currentProfile, saveAfterApply: true);

            // イベントを発火
            SettingsApplied?.Invoke(this, EventArgs.Empty);

            Debug.Log("[GameSettingsService] Settings applied successfully.");
        }

        /// <summary>
        /// 保留中の変更を破棄します。
        /// </summary>
        public void DiscardPendingChanges()
        {
            _pendingProfile = _currentProfile.Clone();
            HasPendingChanges = false;

            Debug.Log("[GameSettingsService] Pending changes discarded.");
        }

        /// <summary>
        /// 設定をデフォルトにリセットします。
        /// </summary>
        public void ResetToDefault()
        {
            Debug.Log("[GameSettingsService] Resetting to default settings...");

            _currentProfile = SettingsProfile.CreateDefault();
            _pendingProfile = _currentProfile.Clone();
            HasPendingChanges = false;

            // 設定を適用
            ApplySettingsInternal(_currentProfile, saveAfterApply: true);

            // イベントを発火
            SettingsReset?.Invoke(this, EventArgs.Empty);

            Debug.Log("[GameSettingsService] Reset to default completed.");
        }

        /// <summary>
        /// 設定をファイルに保存します。
        /// </summary>
        /// <returns>保存に成功した場合true</returns>
        public bool SaveSettings()
        {
            try
            {
                // プロファイルをJSON化
                string json = _currentProfile.ToJson();

                // ディレクトリが存在しない場合は作成
                string? directory = Path.GetDirectoryName(_settingsFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // ファイルに書き込み
                File.WriteAllText(_settingsFilePath, json);

                Debug.Log($"[GameSettingsService] Settings saved to: {_settingsFilePath}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameSettingsService] Failed to save settings: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 設定をファイルから読み込みます。
        /// </summary>
        /// <returns>読み込みに成功した場合true</returns>
        public bool LoadSettings()
        {
            try
            {
                // ファイルが存在しない場合は失敗
                if (!File.Exists(_settingsFilePath))
                {
                    Debug.Log($"[GameSettingsService] Settings file not found: {_settingsFilePath}");
                    return false;
                }

                // ファイルから読み込み
                string json = File.ReadAllText(_settingsFilePath);

                // JSONからプロファイルを作成
                var profile = SettingsProfile.FromJson(json);
                if (profile == null)
                {
                    Debug.LogError("[GameSettingsService] Failed to deserialize settings.");
                    return false;
                }

                _currentProfile = profile;
                _pendingProfile = _currentProfile.Clone();
                HasPendingChanges = false;

                Debug.Log($"[GameSettingsService] Settings loaded: {_currentProfile.ProfileName}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameSettingsService] Failed to load settings: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Specific Settings Access

        /// <summary>
        /// オーディオ設定を取得します。
        /// </summary>
        /// <returns>オーディオ設定</returns>
        public AudioSettings GetAudioSettings()
        {
            return _currentProfile.Audio;
        }

        /// <summary>
        /// ビデオ設定を取得します。
        /// </summary>
        /// <returns>ビデオ設定</returns>
        public VideoSettings GetVideoSettings()
        {
            return _currentProfile.Video;
        }

        /// <summary>
        /// ゲームプレイ設定を取得します。
        /// </summary>
        /// <returns>ゲームプレイ設定</returns>
        public GameplaySettings GetGameplaySettings()
        {
            return _currentProfile.Gameplay;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 設定を実際に適用します（内部用）
        /// </summary>
        /// <param name="profile">適用する設定</param>
        /// <param name="saveAfterApply">適用後に保存するか</param>
        private void ApplySettingsInternal(SettingsProfile profile, bool saveAfterApply)
        {
            // オーディオ設定を適用
            ApplyAudioSettings(profile.Audio);

            // ビデオ設定を適用
            ApplyVideoSettings(profile.Video);

            // ゲームプレイ設定を適用
            ApplyGameplaySettings(profile.Gameplay);

            // 保存フラグが立っている場合は保存
            if (saveAfterApply)
            {
                SaveSettings();
            }
        }

        /// <summary>
        /// オーディオ設定を適用します。
        /// </summary>
        /// <param name="settings">オーディオ設定</param>
        private void ApplyAudioSettings(AudioSettings settings)
        {
            try
            {
                var audioService = ServiceLocator.Instance.Get<IAudioService>();
                if (audioService != null)
                {
                    audioService.MasterVolume = settings.MasterVolume;
                    audioService.BgmVolume = settings.BgmVolume;
                    audioService.SfxVolume = settings.SfxVolume;

                    Debug.Log("[GameSettingsService] Audio settings applied.");
                }
                else
                {
                    Debug.LogWarning("[GameSettingsService] AudioService not found.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameSettingsService] Failed to apply audio settings: {ex.Message}");
            }
        }

        /// <summary>
        /// ビデオ設定を適用します。
        /// </summary>
        /// <param name="settings">ビデオ設定</param>
        private void ApplyVideoSettings(VideoSettings settings)
        {
            try
            {
                // 解像度とフルスクリーンモード
                Screen.SetResolution(settings.ResolutionWidth, settings.ResolutionHeight, settings.FullScreenMode);

                // 画質レベル
                QualitySettings.SetQualityLevel(settings.QualityLevel, applyExpensiveChanges: true);

                // VSync
                QualitySettings.vSyncCount = settings.VSync ? 1 : 0;

                // ターゲットフレームレート
                Application.targetFrameRate = settings.TargetFrameRate;

                // アンチエイリアシング
                QualitySettings.antiAliasing = settings.AntiAliasing;

                Debug.Log("[GameSettingsService] Video settings applied.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameSettingsService] Failed to apply video settings: {ex.Message}");
            }
        }

        /// <summary>
        /// ゲームプレイ設定を適用します。
        /// </summary>
        /// <param name="settings">ゲームプレイ設定</param>
        private void ApplyGameplaySettings(GameplaySettings settings)
        {
            try
            {
                var inputService = ServiceLocator.Instance.Get<IInputService>();
                if (inputService != null)
                {
                    inputService.MovementSensitivity = settings.MovementSensitivity;
                    inputService.CameraSensitivity = settings.CameraSensitivity;
                    inputService.InvertYAxis = settings.InvertYAxis;

                    Debug.Log("[GameSettingsService] Gameplay settings applied to InputService.");
                }
                else
                {
                    Debug.LogWarning("[GameSettingsService] InputService not found.");
                }

                // 難易度などその他の設定はゲーム固有の処理が必要
                // 必要に応じてイベントを発火してゲーム側で処理
                SettingsChanged?.Invoke(this, new SettingsChangedEventArgs("Gameplay"));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameSettingsService] Failed to apply gameplay settings: {ex.Message}");
            }
        }

        #endregion
    }
}
