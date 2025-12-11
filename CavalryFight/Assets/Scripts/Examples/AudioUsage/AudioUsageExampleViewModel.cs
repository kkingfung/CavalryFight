#nullable enable

using CavalryFight.Core.MVVM;
using CavalryFight.Core.Commands;
using CavalryFight.Core.Services;
using CavalryFight.Services.Audio;
using UnityEngine;

namespace CavalryFight.Examples.AudioUsage
{
    /// <summary>
    /// オーディオサービス使用例のViewModel
    /// </summary>
    /// <remarks>
    /// AudioServiceの基本的な使い方を示すサンプルです。
    /// BGM再生、SE再生、ボリューム調整の例を含みます。
    /// </remarks>
    public class AudioUsageExampleViewModel : ViewModelBase
    {
        #region Fields

        private readonly IAudioService _audioService;

        private string _statusMessage;
        private string _currentBgm;
        private float _masterVolume;
        private float _bgmVolume;
        private float _sfxVolume;

        #endregion

        #region Properties

        /// <summary>
        /// ステータスメッセージを取得します。
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            private set => SetProperty(ref _statusMessage, value);
        }

        /// <summary>
        /// 現在のBGM名を取得します。
        /// </summary>
        public string CurrentBgm
        {
            get => _currentBgm;
            private set => SetProperty(ref _currentBgm, value);
        }

        /// <summary>
        /// マスターボリュームを取得または設定します（0.0～1.0）
        /// </summary>
        public float MasterVolume
        {
            get => _masterVolume;
            set
            {
                if (SetProperty(ref _masterVolume, value))
                {
                    _audioService.MasterVolume = value;
                    StatusMessage = $"Master volume set to {value:F2}";
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
                if (SetProperty(ref _bgmVolume, value))
                {
                    _audioService.BgmVolume = value;
                    StatusMessage = $"BGM volume set to {value:F2}";
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
                if (SetProperty(ref _sfxVolume, value))
                {
                    _audioService.SfxVolume = value;
                    StatusMessage = $"SFX volume set to {value:F2}";
                }
            }
        }

        /// <summary>
        /// BGMが再生中かどうかを取得します。
        /// </summary>
        public bool IsBgmPlaying => _audioService.IsBgmPlaying;

        #endregion

        #region Commands

        /// <summary>
        /// BGMを再生するコマンド
        /// </summary>
        public ICommand PlayBgmCommand { get; }

        /// <summary>
        /// BGMを停止するコマンド
        /// </summary>
        public ICommand StopBgmCommand { get; }

        /// <summary>
        /// BGMを一時停止するコマンド
        /// </summary>
        public ICommand PauseBgmCommand { get; }

        /// <summary>
        /// BGMを再開するコマンド
        /// </summary>
        public ICommand ResumeBgmCommand { get; }

        /// <summary>
        /// SEを再生するコマンド
        /// </summary>
        public ICommand PlaySfxCommand { get; }

        /// <summary>
        /// すべてミュートするコマンド
        /// </summary>
        public ICommand MuteAllCommand { get; }

        /// <summary>
        /// ミュート解除するコマンド
        /// </summary>
        public ICommand UnmuteAllCommand { get; }

        #endregion

        #region Constructor

        /// <summary>
        /// AudioUsageExampleViewModelの新しいインスタンスを初期化します。
        /// </summary>
        public AudioUsageExampleViewModel()
        {
            // サービスを取得
            _audioService = ServiceLocator.Instance.Get<IAudioService>();

            _statusMessage = "Ready";
            _currentBgm = _audioService.CurrentBgmName ?? "None";
            _masterVolume = _audioService.MasterVolume;
            _bgmVolume = _audioService.BgmVolume;
            _sfxVolume = _audioService.SfxVolume;

            // イベントを購読
            _audioService.BgmChanged += OnBgmChanged;
            _audioService.VolumeChanged += OnVolumeChanged;

            // コマンドの初期化
            PlayBgmCommand = new RelayCommand(
                execute: PlayBgm,
                canExecute: () => !_audioService.IsBgmPlaying
            );

            StopBgmCommand = new RelayCommand(
                execute: StopBgm,
                canExecute: () => _audioService.IsBgmPlaying
            );

            PauseBgmCommand = new RelayCommand(
                execute: PauseBgm,
                canExecute: () => _audioService.IsBgmPlaying
            );

            ResumeBgmCommand = new RelayCommand(ResumeBgm);

            PlaySfxCommand = new RelayCommand(PlaySfx);

            MuteAllCommand = new RelayCommand(MuteAll);

            UnmuteAllCommand = new RelayCommand(UnmuteAll);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// BGMを再生します。
        /// </summary>
        private void PlayBgm()
        {
            // 実際のプロジェクトではResourcesやAddressablesからロード
            var bgmClip = Resources.Load<AudioClip>("Audio/BGM/SampleBGM");

            if (bgmClip != null)
            {
                _audioService.PlayBgm(bgmClip, loop: true, fadeInDuration: 1.0f);
                StatusMessage = "BGM started with 1s fade-in";
            }
            else
            {
                StatusMessage = "BGM clip not found at Resources/Audio/BGM/SampleBGM";
                Debug.LogWarning("[AudioUsageExample] BGM clip not found");
            }

            OnPropertyChanged(nameof(IsBgmPlaying));
            RaiseCommandsCanExecuteChanged();
        }

        /// <summary>
        /// BGMを停止します。
        /// </summary>
        private void StopBgm()
        {
            _audioService.StopBgm(fadeOutDuration: 1.0f);
            StatusMessage = "BGM stopped with 1s fade-out";

            OnPropertyChanged(nameof(IsBgmPlaying));
            RaiseCommandsCanExecuteChanged();
        }

        /// <summary>
        /// BGMを一時停止します。
        /// </summary>
        private void PauseBgm()
        {
            _audioService.PauseBgm();
            StatusMessage = "BGM paused";

            OnPropertyChanged(nameof(IsBgmPlaying));
            RaiseCommandsCanExecuteChanged();
        }

        /// <summary>
        /// BGMを再開します。
        /// </summary>
        private void ResumeBgm()
        {
            _audioService.ResumeBgm();
            StatusMessage = "BGM resumed";

            OnPropertyChanged(nameof(IsBgmPlaying));
            RaiseCommandsCanExecuteChanged();
        }

        /// <summary>
        /// SEを再生します。
        /// </summary>
        private void PlaySfx()
        {
            var sfxClip = Resources.Load<AudioClip>("Audio/SFX/SampleSFX");

            if (sfxClip != null)
            {
                _audioService.PlaySfx(sfxClip, volumeScale: 1.0f);
                StatusMessage = "SFX played";
            }
            else
            {
                StatusMessage = "SFX clip not found at Resources/Audio/SFX/SampleSFX";
                Debug.LogWarning("[AudioUsageExample] SFX clip not found");
            }
        }

        /// <summary>
        /// すべてミュートします。
        /// </summary>
        private void MuteAll()
        {
            _audioService.MuteAll();
            StatusMessage = "All audio muted";
        }

        /// <summary>
        /// ミュート解除します。
        /// </summary>
        private void UnmuteAll()
        {
            _audioService.UnmuteAll();
            StatusMessage = "All audio unmuted";
        }

        /// <summary>
        /// BGMが変更された時のイベントハンドラ
        /// </summary>
        /// <param name="sender">イベント送信元</param>
        /// <param name="e">イベント引数</param>
        private void OnBgmChanged(object? sender, AudioChangedEventArgs e)
        {
            CurrentBgm = e.ClipName;
            StatusMessage = $"BGM changed to: {e.ClipName}";
            Debug.Log($"[AudioUsageExample] BGM changed to: {e.ClipName}");
        }

        /// <summary>
        /// ボリュームが変更された時のイベントハンドラ
        /// </summary>
        /// <param name="sender">イベント送信元</param>
        /// <param name="e">イベント引数</param>
        private void OnVolumeChanged(object? sender, VolumeChangedEventArgs e)
        {
            Debug.Log($"[AudioUsageExample] {e.Type} volume changed to: {e.Volume:F2}");
        }

        /// <summary>
        /// すべてのコマンドのCanExecuteChangedを発行します。
        /// </summary>
        private void RaiseCommandsCanExecuteChanged()
        {
            (PlayBgmCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (StopBgmCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (PauseBgmCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// ViewModelの破棄処理
        /// </summary>
        protected override void OnDispose()
        {
            // イベントの購読解除
            _audioService.BgmChanged -= OnBgmChanged;
            _audioService.VolumeChanged -= OnVolumeChanged;

            Debug.Log("[AudioUsageExample] ViewModel disposed.");
            base.OnDispose();
        }

        #endregion
    }
}
