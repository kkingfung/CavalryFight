#nullable enable

using System;
using UnityEngine;
using CavalryFight.Core.MVVM;
using CavalryFight.Core.Services;
using CavalryFight.Core.Commands;

namespace CavalryFight.Services.GameSettings
{
    /// <summary>
    /// ゲーム設定使用例のViewModel
    /// </summary>
    /// <remarks>
    /// GameSettingsServiceの使用方法を示すサンプル実装です。
    /// 設定メニューUIのViewModelとして使用できます。
    ///
    /// 主な機能:
    /// - 設定の読み込みと表示
    /// - 保留中の設定変更
    /// - 設定の適用、破棄、リセット
    /// - 変更検知とUI更新
    /// </remarks>
    public class SettingsUsageExampleViewModel : ViewModelBase
    {
        #region Fields

        private readonly IGameSettingsService _settingsService;

        // Audio settings
        private float _masterVolume;
        private float _bgmVolume;
        private float _sfxVolume;

        // Video settings
        private int _resolutionWidth;
        private int _resolutionHeight;
        private FullScreenMode _fullScreenMode;
        private int _qualityLevel;
        private bool _vSync;
        private int _targetFrameRate;
        private int _antiAliasing;

        // Gameplay settings
        private float _movementSensitivity;
        private float _cameraSensitivity;
        private bool _invertYAxis;

        #endregion

        #region Properties - Audio

        /// <summary>
        /// マスター音量を取得または設定します（0.0～1.0）
        /// </summary>
        public float MasterVolume
        {
            get { return _masterVolume; }
            set
            {
                if (SetProperty(ref _masterVolume, value))
                {
                    UpdatePendingAudioSettings();
                }
            }
        }

        /// <summary>
        /// BGM音量を取得または設定します（0.0～1.0）
        /// </summary>
        public float BgmVolume
        {
            get { return _bgmVolume; }
            set
            {
                if (SetProperty(ref _bgmVolume, value))
                {
                    UpdatePendingAudioSettings();
                }
            }
        }

        /// <summary>
        /// 効果音音量を取得または設定します（0.0～1.0）
        /// </summary>
        public float SfxVolume
        {
            get { return _sfxVolume; }
            set
            {
                if (SetProperty(ref _sfxVolume, value))
                {
                    UpdatePendingAudioSettings();
                }
            }
        }

        #endregion

        #region Properties - Video

        /// <summary>
        /// 解像度の幅を取得または設定します
        /// </summary>
        public int ResolutionWidth
        {
            get { return _resolutionWidth; }
            set
            {
                if (SetProperty(ref _resolutionWidth, value))
                {
                    UpdatePendingVideoSettings();
                }
            }
        }

        /// <summary>
        /// 解像度の高さを取得または設定します
        /// </summary>
        public int ResolutionHeight
        {
            get { return _resolutionHeight; }
            set
            {
                if (SetProperty(ref _resolutionHeight, value))
                {
                    UpdatePendingVideoSettings();
                }
            }
        }

        /// <summary>
        /// フルスクリーンモードを取得または設定します
        /// </summary>
        public FullScreenMode FullScreenMode
        {
            get { return _fullScreenMode; }
            set
            {
                if (SetProperty(ref _fullScreenMode, value))
                {
                    UpdatePendingVideoSettings();
                }
            }
        }

        /// <summary>
        /// 画質レベルを取得または設定します
        /// </summary>
        public int QualityLevel
        {
            get { return _qualityLevel; }
            set
            {
                if (SetProperty(ref _qualityLevel, value))
                {
                    UpdatePendingVideoSettings();
                }
            }
        }

        /// <summary>
        /// VSyncを取得または設定します
        /// </summary>
        public bool VSync
        {
            get { return _vSync; }
            set
            {
                if (SetProperty(ref _vSync, value))
                {
                    UpdatePendingVideoSettings();
                }
            }
        }

        /// <summary>
        /// ターゲットフレームレートを取得または設定します
        /// </summary>
        public int TargetFrameRate
        {
            get { return _targetFrameRate; }
            set
            {
                if (SetProperty(ref _targetFrameRate, value))
                {
                    UpdatePendingVideoSettings();
                }
            }
        }

        /// <summary>
        /// アンチエイリアシングを取得または設定します
        /// </summary>
        public int AntiAliasing
        {
            get { return _antiAliasing; }
            set
            {
                if (SetProperty(ref _antiAliasing, value))
                {
                    UpdatePendingVideoSettings();
                }
            }
        }

        #endregion

        #region Properties - Gameplay

        /// <summary>
        /// 移動感度を取得または設定します（0.0～1.0）
        /// </summary>
        public float MovementSensitivity
        {
            get { return _movementSensitivity; }
            set
            {
                if (SetProperty(ref _movementSensitivity, value))
                {
                    UpdatePendingGameplaySettings();
                }
            }
        }

        /// <summary>
        /// カメラ感度を取得または設定します（0.0～1.0）
        /// </summary>
        public float CameraSensitivity
        {
            get { return _cameraSensitivity; }
            set
            {
                if (SetProperty(ref _cameraSensitivity, value))
                {
                    UpdatePendingGameplaySettings();
                }
            }
        }

        /// <summary>
        /// カメラY軸反転を取得または設定します
        /// </summary>
        public bool InvertYAxis
        {
            get { return _invertYAxis; }
            set
            {
                if (SetProperty(ref _invertYAxis, value))
                {
                    UpdatePendingGameplaySettings();
                }
            }
        }

        #endregion

        #region Properties - State

        /// <summary>
        /// 保留中の変更があるかどうかを取得します
        /// </summary>
        /// <remarks>
        /// ApplyボタンやDiscardボタンの有効/無効を制御するために使用します
        /// </remarks>
        public bool HasPendingChanges
        {
            get { return _settingsService.HasPendingChanges; }
        }

        #endregion

        #region Commands

        /// <summary>
        /// 設定を適用するコマンド
        /// </summary>
        public ICommand ApplySettingsCommand { get; }

        /// <summary>
        /// 保留中の変更を破棄するコマンド
        /// </summary>
        public ICommand DiscardChangesCommand { get; }

        /// <summary>
        /// 設定をデフォルトにリセットするコマンド
        /// </summary>
        public ICommand ResetToDefaultCommand { get; }

        #endregion

        #region Constructor

        /// <summary>
        /// SettingsUsageExampleViewModelの新しいインスタンスを初期化します
        /// </summary>
        public SettingsUsageExampleViewModel()
        {
            // サービスを取得
            _settingsService = ServiceLocator.Instance.Get<IGameSettingsService>()
                ?? throw new InvalidOperationException("GameSettingsService is not registered.");

            // イベントを購読
            _settingsService.SettingsApplied += OnSettingsApplied;
            _settingsService.SettingsReset += OnSettingsReset;
            _settingsService.SettingsChanged += OnSettingsChanged;

            // コマンドを初期化
            ApplySettingsCommand = new RelayCommand(ExecuteApplySettings, CanExecuteApplySettings);
            DiscardChangesCommand = new RelayCommand(ExecuteDiscardChanges, CanExecuteDiscardChanges);
            ResetToDefaultCommand = new RelayCommand(ExecuteResetToDefault);

            // 現在の設定を読み込み
            LoadCurrentSettings();

            Debug.Log("[SettingsUsageExample] ViewModel initialized.");
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 現在の設定をUIに反映します
        /// </summary>
        /// <remarks>
        /// 設定メニューを開いた時に呼び出してください
        /// </remarks>
        public void LoadCurrentSettings()
        {
            var profile = _settingsService.CurrentProfile;

            // Audio
            _masterVolume = profile.Audio.MasterVolume;
            _bgmVolume = profile.Audio.BgmVolume;
            _sfxVolume = profile.Audio.SfxVolume;

            // Video
            _resolutionWidth = profile.Video.ResolutionWidth;
            _resolutionHeight = profile.Video.ResolutionHeight;
            _fullScreenMode = profile.Video.FullScreenMode;
            _qualityLevel = profile.Video.QualityLevel;
            _vSync = profile.Video.VSync;
            _targetFrameRate = profile.Video.TargetFrameRate;
            _antiAliasing = profile.Video.AntiAliasing;

            // Gameplay
            _movementSensitivity = profile.Gameplay.MovementSensitivity;
            _cameraSensitivity = profile.Gameplay.CameraSensitivity;
            _invertYAxis = profile.Gameplay.InvertYAxis;

            // 全プロパティの変更通知
            OnPropertyChanged(string.Empty);

            Debug.Log("[SettingsUsageExample] Current settings loaded.");
        }

        #endregion

        #region Private Methods - Pending Settings Update

        /// <summary>
        /// 保留中のオーディオ設定を更新します
        /// </summary>
        private void UpdatePendingAudioSettings()
        {
            var pendingProfile = _settingsService.PendingProfile.Clone();
            pendingProfile.Audio.MasterVolume = _masterVolume;
            pendingProfile.Audio.BgmVolume = _bgmVolume;
            pendingProfile.Audio.SfxVolume = _sfxVolume;

            _settingsService.SetPendingSettings(pendingProfile);
            OnPropertyChanged(nameof(HasPendingChanges));

            Debug.Log($"[SettingsUsageExample] Audio settings updated (Pending). Master: {_masterVolume:F2}, BGM: {_bgmVolume:F2}, SFX: {_sfxVolume:F2}");
        }

        /// <summary>
        /// 保留中のビデオ設定を更新します
        /// </summary>
        private void UpdatePendingVideoSettings()
        {
            var pendingProfile = _settingsService.PendingProfile.Clone();
            pendingProfile.Video.ResolutionWidth = _resolutionWidth;
            pendingProfile.Video.ResolutionHeight = _resolutionHeight;
            pendingProfile.Video.FullScreenMode = _fullScreenMode;
            pendingProfile.Video.QualityLevel = _qualityLevel;
            pendingProfile.Video.VSync = _vSync;
            pendingProfile.Video.TargetFrameRate = _targetFrameRate;
            pendingProfile.Video.AntiAliasing = _antiAliasing;

            _settingsService.SetPendingSettings(pendingProfile);
            OnPropertyChanged(nameof(HasPendingChanges));

            Debug.Log($"[SettingsUsageExample] Video settings updated (Pending). Resolution: {_resolutionWidth}x{_resolutionHeight}, Quality: {_qualityLevel}");
        }

        /// <summary>
        /// 保留中のゲームプレイ設定を更新します
        /// </summary>
        private void UpdatePendingGameplaySettings()
        {
            var pendingProfile = _settingsService.PendingProfile.Clone();
            pendingProfile.Gameplay.MovementSensitivity = _movementSensitivity;
            pendingProfile.Gameplay.CameraSensitivity = _cameraSensitivity;
            pendingProfile.Gameplay.InvertYAxis = _invertYAxis;

            _settingsService.SetPendingSettings(pendingProfile);
            OnPropertyChanged(nameof(HasPendingChanges));

            Debug.Log($"[SettingsUsageExample] Gameplay settings updated (Pending). MoveSensitivity: {_movementSensitivity:F2}, CameraSensitivity: {_cameraSensitivity:F2}");
        }

        #endregion

        #region Command Methods

        /// <summary>
        /// 設定を適用します
        /// </summary>
        private void ExecuteApplySettings()
        {
            Debug.Log("[SettingsUsageExample] Applying settings...");
            _settingsService.ApplySettings();
            // SettingsAppliedイベントで通知される
        }

        /// <summary>
        /// 設定を適用できるかどうかを判定します
        /// </summary>
        /// <returns>保留中の変更がある場合true</returns>
        private bool CanExecuteApplySettings()
        {
            return _settingsService.HasPendingChanges;
        }

        /// <summary>
        /// 保留中の変更を破棄します
        /// </summary>
        private void ExecuteDiscardChanges()
        {
            Debug.Log("[SettingsUsageExample] Discarding changes...");
            _settingsService.DiscardPendingChanges();

            // UIを現在の設定に戻す
            LoadCurrentSettings();

            OnPropertyChanged(nameof(HasPendingChanges));
        }

        /// <summary>
        /// 変更を破棄できるかどうかを判定します
        /// </summary>
        /// <returns>保留中の変更がある場合true</returns>
        private bool CanExecuteDiscardChanges()
        {
            return _settingsService.HasPendingChanges;
        }

        /// <summary>
        /// 設定をデフォルトにリセットします
        /// </summary>
        private void ExecuteResetToDefault()
        {
            Debug.Log("[SettingsUsageExample] Resetting to default...");
            _settingsService.ResetToDefault();
            // SettingsResetイベントで通知される
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// 設定が適用された時に呼ばれます
        /// </summary>
        private void OnSettingsApplied(object? sender, EventArgs e)
        {
            Debug.Log("[SettingsUsageExample] Settings applied event received.");

            // UIを更新（実際にはすでに一致しているはず）
            LoadCurrentSettings();

            // コマンドの実行可能状態を更新
            OnPropertyChanged(nameof(HasPendingChanges));
        }

        /// <summary>
        /// 設定がリセットされた時に呼ばれます
        /// </summary>
        private void OnSettingsReset(object? sender, EventArgs e)
        {
            Debug.Log("[SettingsUsageExample] Settings reset event received.");

            // UIをデフォルト設定に更新
            LoadCurrentSettings();

            // コマンドの実行可能状態を更新
            OnPropertyChanged(nameof(HasPendingChanges));
        }

        /// <summary>
        /// 設定が変更された時に呼ばれます
        /// </summary>
        private void OnSettingsChanged(object? sender, SettingsChangedEventArgs e)
        {
            Debug.Log($"[SettingsUsageExample] Settings changed: {e.Category}");
        }

        #endregion

        #region Dispose

        /// <summary>
        /// リソースを解放します
        /// </summary>
        protected override void OnDispose()
        {
            // イベント購読を解除
            _settingsService.SettingsApplied -= OnSettingsApplied;
            _settingsService.SettingsReset -= OnSettingsReset;
            _settingsService.SettingsChanged -= OnSettingsChanged;

            Debug.Log("[SettingsUsageExample] ViewModel disposed.");
        }

        #endregion
    }
}
