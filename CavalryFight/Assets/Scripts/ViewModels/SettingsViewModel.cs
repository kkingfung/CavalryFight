#nullable enable

using CavalryFight.Core.Commands;
using CavalryFight.Core.MVVM;
using CavalryFight.Core.Services;
using CavalryFight.Services.GameSettings;
using CavalryFight.Services.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CavalryFight.ViewModels
{
    /// <summary>
    /// ターゲットフレームレート設定
    /// </summary>
    public enum TargetFrameRate
    {
        Fps30 = 0,
        Fps60 = 1,
        Fps120 = 2,
        Fps144 = 3,
        Unlimited = 4
    }

    /// <summary>
    /// アンチエイリアシングレベル
    /// </summary>
    public enum AntiAliasingLevel
    {
        Off = 0,
        MSAA2x = 1,
        MSAA4x = 2,
        MSAA8x = 3
    }

    /// <summary>
    /// 設定画面のViewModel
    /// </summary>
    /// <remarks>
    /// オーディオ、ビデオ、ゲームプレイ設定の変更と適用を管理します。
    /// </remarks>
    public class SettingsViewModel : ViewModelBase
    {
        #region Fields

        private readonly IGameSettingsService? _gameSettingsService;
        private readonly ISceneManagementService? _sceneManagementService;

        // Audio
        private float _masterVolume;
        private float _bgmVolume;
        private float _sfxVolume;

        // Video
        private FullScreenMode _displayMode;
        private int _resolutionIndex;
        private int _qualityLevelIndex;
        private bool _vSync;
        private TargetFrameRate _targetFrameRate;
        private AntiAliasingLevel _antiAliasingLevel;

        // Gameplay
        private float _movementSensitivity;
        private float _cameraSensitivity;
        private bool _invertYAxis;

        // Cached data
        private List<Resolution> _availableResolutions = new List<Resolution>();
        private List<string> _qualityLevelNames = new List<string>();

        #endregion

        #region Properties - Audio

        /// <summary>
        /// マスターボリュームを取得または設定します（0.0～1.0）
        /// </summary>
        public float MasterVolume
        {
            get => _masterVolume;
            set
            {
                if (SetProperty(ref _masterVolume, Mathf.Clamp01(value)))
                {
                    UpdatePendingAudioSettings();
                }
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
                if (SetProperty(ref _bgmVolume, Mathf.Clamp01(value)))
                {
                    UpdatePendingAudioSettings();
                }
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
                if (SetProperty(ref _sfxVolume, Mathf.Clamp01(value)))
                {
                    UpdatePendingAudioSettings();
                }
            }
        }

        #endregion

        #region Properties - Video

        /// <summary>
        /// 表示モードを取得または設定します
        /// </summary>
        public FullScreenMode DisplayMode
        {
            get => _displayMode;
            set
            {
                if (SetProperty(ref _displayMode, value))
                {
                    OnPropertyChanged(nameof(DisplayModeIndex));
                    UpdatePendingVideoSettings();
                }
            }
        }

        /// <summary>
        /// 表示モードのインデックスを取得または設定します（UI バインディング用）
        /// </summary>
        public int DisplayModeIndex
        {
            get => ConvertFullScreenModeToIndex(_displayMode);
            set => DisplayMode = ConvertIndexToFullScreenMode(value);
        }

        /// <summary>
        /// 解像度のインデックスを取得または設定します
        /// </summary>
        public int ResolutionIndex
        {
            get => _resolutionIndex;
            set
            {
                if (SetProperty(ref _resolutionIndex, value))
                {
                    UpdatePendingVideoSettings();
                }
            }
        }

        /// <summary>
        /// 画質レベルのインデックスを取得または設定します
        /// </summary>
        public int QualityLevelIndex
        {
            get => _qualityLevelIndex;
            set
            {
                if (SetProperty(ref _qualityLevelIndex, value))
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
            get => _vSync;
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
        public TargetFrameRate TargetFrameRate
        {
            get => _targetFrameRate;
            set
            {
                if (SetProperty(ref _targetFrameRate, value))
                {
                    OnPropertyChanged(nameof(TargetFrameRateIndex));
                    UpdatePendingVideoSettings();
                }
            }
        }

        /// <summary>
        /// ターゲットフレームレートのインデックスを取得または設定します（UI バインディング用）
        /// </summary>
        public int TargetFrameRateIndex
        {
            get => (int)_targetFrameRate;
            set => TargetFrameRate = (TargetFrameRate)value;
        }

        /// <summary>
        /// アンチエイリアシングレベルを取得または設定します
        /// </summary>
        public AntiAliasingLevel AntiAliasingLevel
        {
            get => _antiAliasingLevel;
            set
            {
                if (SetProperty(ref _antiAliasingLevel, value))
                {
                    OnPropertyChanged(nameof(AntiAliasingIndex));
                    UpdatePendingVideoSettings();
                }
            }
        }

        /// <summary>
        /// アンチエイリアシングのインデックスを取得または設定します（UI バインディング用）
        /// </summary>
        public int AntiAliasingIndex
        {
            get => (int)_antiAliasingLevel;
            set => AntiAliasingLevel = (AntiAliasingLevel)value;
        }

        /// <summary>
        /// 利用可能な解像度のリストを取得します
        /// </summary>
        public List<Resolution> AvailableResolutions => _availableResolutions;

        /// <summary>
        /// 画質レベル名のリストを取得します
        /// </summary>
        public List<string> QualityLevelNames => _qualityLevelNames;

        #endregion

        #region Properties - Gameplay

        /// <summary>
        /// 移動感度を取得または設定します（0.0～1.0）
        /// </summary>
        public float MovementSensitivity
        {
            get => _movementSensitivity;
            set
            {
                if (SetProperty(ref _movementSensitivity, Mathf.Clamp01(value)))
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
            get => _cameraSensitivity;
            set
            {
                if (SetProperty(ref _cameraSensitivity, Mathf.Clamp01(value)))
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
            get => _invertYAxis;
            set
            {
                if (SetProperty(ref _invertYAxis, value))
                {
                    UpdatePendingGameplaySettings();
                }
            }
        }

        #endregion

        #region Properties - Other

        /// <summary>
        /// 保留中の変更があるかどうかを取得します
        /// </summary>
        public bool HasPendingChanges => _gameSettingsService?.HasPendingChanges ?? false;

        #endregion

        #region Commands

        /// <summary>
        /// 設定を適用するコマンド
        /// </summary>
        public ICommand ApplySettingsCommand { get; }

        /// <summary>
        /// 設定をリセットするコマンド
        /// </summary>
        public ICommand ResetSettingsCommand { get; }

        /// <summary>
        /// メインメニューに戻るコマンド
        /// </summary>
        public ICommand BackToMenuCommand { get; }

        #endregion

        #region Constructor

        /// <summary>
        /// SettingsViewModelの新しいインスタンスを初期化します。
        /// </summary>
        public SettingsViewModel()
        {
            // サービスを取得
            _gameSettingsService = ServiceLocator.Instance.Get<IGameSettingsService>();
            _sceneManagementService = ServiceLocator.Instance.Get<ISceneManagementService>();

            // コマンドを初期化
            ApplySettingsCommand = new RelayCommand(OnApplySettings, CanApplySettings);
            ResetSettingsCommand = new RelayCommand(OnResetSettings);
            BackToMenuCommand = new RelayCommand(OnBackToMenu, CanBackToMenu);

            // 利用可能な解像度とQuality設定を取得
            InitializeDisplayOptions();

            // 現在の設定を読み込み
            LoadCurrentSettings();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 現在の設定を再読み込みします
        /// </summary>
        public void RefreshSettings()
        {
            LoadCurrentSettings();
        }

        #endregion

        #region Private Methods - Initialization

        /// <summary>
        /// 表示オプションを初期化します
        /// </summary>
        private void InitializeDisplayOptions()
        {
            // 解像度の取得（重複を除く）
            _availableResolutions = Screen.resolutions
                .Where(r => r.width >= 1280 && r.height >= 720) // 最小解像度フィルタ
                .GroupBy(r => new { r.width, r.height })
                .Select(g => g.First())
                .OrderBy(r => r.width)
                .ThenBy(r => r.height)
                .ToList();

            // Quality設定名の取得
            _qualityLevelNames = QualitySettings.names.ToList();
        }

        /// <summary>
        /// 現在の設定を読み込みます
        /// </summary>
        private void LoadCurrentSettings()
        {
            if (_gameSettingsService == null)
            {
                return;
            }

            var audioSettings = _gameSettingsService.GetAudioSettings();
            var videoSettings = _gameSettingsService.GetVideoSettings();
            var gameplaySettings = _gameSettingsService.GetGameplaySettings();

            // オーディオ設定
            _masterVolume = audioSettings.MasterVolume;
            _bgmVolume = audioSettings.BgmVolume;
            _sfxVolume = audioSettings.SfxVolume;

            // ビデオ設定
            _displayMode = videoSettings.FullScreenMode;
            _resolutionIndex = FindResolutionIndex(videoSettings.ResolutionWidth, videoSettings.ResolutionHeight);
            _qualityLevelIndex = videoSettings.QualityLevel;
            _vSync = videoSettings.VSync;
            _targetFrameRate = ConvertFrameRateToEnum(videoSettings.TargetFrameRate);
            _antiAliasingLevel = ConvertAntiAliasingToEnum(videoSettings.AntiAliasing);

            // ゲームプレイ設定
            _movementSensitivity = gameplaySettings.MovementSensitivity;
            _cameraSensitivity = gameplaySettings.CameraSensitivity;
            _invertYAxis = gameplaySettings.InvertYAxis;

            // プロパティ変更通知
            OnPropertiesChanged(
                nameof(MasterVolume),
                nameof(BgmVolume),
                nameof(SfxVolume),
                nameof(DisplayModeIndex),
                nameof(ResolutionIndex),
                nameof(QualityLevelIndex),
                nameof(VSync),
                nameof(TargetFrameRateIndex),
                nameof(AntiAliasingIndex),
                nameof(MovementSensitivity),
                nameof(CameraSensitivity),
                nameof(InvertYAxis)
            );
        }

        #endregion

        #region Private Methods - Update Pending Settings

        /// <summary>
        /// 保留中のオーディオ設定を更新します
        /// </summary>
        private void UpdatePendingAudioSettings()
        {
            if (_gameSettingsService == null)
            {
                return;
            }

            var pendingProfile = _gameSettingsService.PendingProfile.Clone();
            pendingProfile.Audio.MasterVolume = _masterVolume;
            pendingProfile.Audio.BgmVolume = _bgmVolume;
            pendingProfile.Audio.SfxVolume = _sfxVolume;

            _gameSettingsService.SetPendingSettings(pendingProfile);
            OnPropertyChanged(nameof(HasPendingChanges));
        }

        /// <summary>
        /// 保留中のビデオ設定を更新します
        /// </summary>
        private void UpdatePendingVideoSettings()
        {
            if (_gameSettingsService == null)
            {
                return;
            }

            var pendingProfile = _gameSettingsService.PendingProfile.Clone();

            // Display Mode
            pendingProfile.Video.FullScreenMode = _displayMode;

            // Resolution
            if (_resolutionIndex >= 0 && _resolutionIndex < _availableResolutions.Count)
            {
                var resolution = _availableResolutions[_resolutionIndex];
                pendingProfile.Video.ResolutionWidth = resolution.width;
                pendingProfile.Video.ResolutionHeight = resolution.height;
            }

            // Quality Level
            pendingProfile.Video.QualityLevel = _qualityLevelIndex;

            // VSync
            pendingProfile.Video.VSync = _vSync;

            // Target Frame Rate
            pendingProfile.Video.TargetFrameRate = ConvertEnumToFrameRate(_targetFrameRate);

            // Anti-Aliasing
            pendingProfile.Video.AntiAliasing = ConvertEnumToAntiAliasing(_antiAliasingLevel);

            _gameSettingsService.SetPendingSettings(pendingProfile);
            OnPropertyChanged(nameof(HasPendingChanges));
        }

        /// <summary>
        /// 保留中のゲームプレイ設定を更新します
        /// </summary>
        private void UpdatePendingGameplaySettings()
        {
            if (_gameSettingsService == null)
            {
                return;
            }

            var pendingProfile = _gameSettingsService.PendingProfile.Clone();
            pendingProfile.Gameplay.MovementSensitivity = _movementSensitivity;
            pendingProfile.Gameplay.CameraSensitivity = _cameraSensitivity;
            pendingProfile.Gameplay.InvertYAxis = _invertYAxis;

            _gameSettingsService.SetPendingSettings(pendingProfile);
            OnPropertyChanged(nameof(HasPendingChanges));
        }

        #endregion

        #region Private Methods - Conversion Helpers

        /// <summary>
        /// FullScreenModeをインデックスに変換します
        /// </summary>
        private int ConvertFullScreenModeToIndex(FullScreenMode mode)
        {
            return mode switch
            {
                FullScreenMode.ExclusiveFullScreen => 0,
                FullScreenMode.Windowed => 1,
                FullScreenMode.FullScreenWindow => 2,
                _ => 0
            };
        }

        /// <summary>
        /// インデックスをFullScreenModeに変換します
        /// </summary>
        private FullScreenMode ConvertIndexToFullScreenMode(int index)
        {
            return index switch
            {
                0 => FullScreenMode.ExclusiveFullScreen,
                1 => FullScreenMode.Windowed,
                2 => FullScreenMode.FullScreenWindow,
                _ => FullScreenMode.FullScreenWindow
            };
        }

        /// <summary>
        /// 解像度のインデックスを検索します
        /// </summary>
        private int FindResolutionIndex(int width, int height)
        {
            for (int i = 0; i < _availableResolutions.Count; i++)
            {
                if (_availableResolutions[i].width == width && _availableResolutions[i].height == height)
                {
                    return i;
                }
            }
            return 0; // デフォルトは最初の解像度
        }

        /// <summary>
        /// フレームレート値をenumに変換します
        /// </summary>
        private TargetFrameRate ConvertFrameRateToEnum(int frameRate)
        {
            return frameRate switch
            {
                30 => TargetFrameRate.Fps30,
                60 => TargetFrameRate.Fps60,
                120 => TargetFrameRate.Fps120,
                144 => TargetFrameRate.Fps144,
                -1 => TargetFrameRate.Unlimited,
                _ => TargetFrameRate.Fps60 // Default to 60
            };
        }

        /// <summary>
        /// enumをフレームレート値に変換します
        /// </summary>
        private int ConvertEnumToFrameRate(TargetFrameRate targetFrameRate)
        {
            return targetFrameRate switch
            {
                TargetFrameRate.Fps30 => 30,
                TargetFrameRate.Fps60 => 60,
                TargetFrameRate.Fps120 => 120,
                TargetFrameRate.Fps144 => 144,
                TargetFrameRate.Unlimited => -1,
                _ => 60
            };
        }

        /// <summary>
        /// アンチエイリアシング値をenumに変換します
        /// </summary>
        private AntiAliasingLevel ConvertAntiAliasingToEnum(int antiAliasing)
        {
            return antiAliasing switch
            {
                0 => AntiAliasingLevel.Off,
                1 << 1 => AntiAliasingLevel.MSAA2x,  // 2x MSAA
                1 << 2 => AntiAliasingLevel.MSAA4x,  // 4x MSAA
                1 << 3 => AntiAliasingLevel.MSAA8x,  // 8x MSAA
                _ => AntiAliasingLevel.MSAA4x         // Default to 4x
            };
        }

        /// <summary>
        /// enumをアンチエイリアシング値に変換します
        /// </summary>
        private int ConvertEnumToAntiAliasing(AntiAliasingLevel level)
        {
            return level switch
            {
                AntiAliasingLevel.Off => 0,
                AntiAliasingLevel.MSAA2x => 1 << 1,  // 2x MSAA
                AntiAliasingLevel.MSAA4x => 1 << 2,  // 4x MSAA
                AntiAliasingLevel.MSAA8x => 1 << 3,  // 8x MSAA
                _ => 1 << 2                           // Default to 4x
            };
        }

        #endregion

        #region Command Handlers

        /// <summary>
        /// 設定を適用できるかどうかを判定します
        /// </summary>
        private bool CanApplySettings()
        {
            return _gameSettingsService != null && HasPendingChanges;
        }

        /// <summary>
        /// 設定を適用します
        /// </summary>
        private void OnApplySettings()
        {
            if (_gameSettingsService == null)
            {
                return;
            }

            Debug.Log("[SettingsViewModel] Applying settings...");
            _gameSettingsService.ApplySettings();
            _gameSettingsService.SaveSettings();
            OnPropertyChanged(nameof(HasPendingChanges));
            Debug.Log("[SettingsViewModel] Settings applied and saved.");
        }

        /// <summary>
        /// 設定をリセットします
        /// </summary>
        private void OnResetSettings()
        {
            if (_gameSettingsService == null)
            {
                return;
            }

            Debug.Log("[SettingsViewModel] Resetting settings to default...");
            _gameSettingsService.ResetToDefault();
            LoadCurrentSettings();
            Debug.Log("[SettingsViewModel] Settings reset to default.");
        }

        /// <summary>
        /// メインメニューに戻れるかどうかを判定します
        /// </summary>
        private bool CanBackToMenu()
        {
            return _sceneManagementService != null && !_sceneManagementService.IsLoading;
        }

        /// <summary>
        /// メインメニューに戻ります
        /// </summary>
        private void OnBackToMenu()
        {
            Debug.Log("[SettingsViewModel] Returning to Main Menu...");
            _sceneManagementService?.LoadMainMenu();
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// リソースを解放します
        /// </summary>
        protected override void OnDispose()
        {
            base.OnDispose();
            Debug.Log("[SettingsViewModel] Disposed.");
        }

        #endregion
    }
}
