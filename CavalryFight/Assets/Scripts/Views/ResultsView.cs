#nullable enable

using System;
using System.ComponentModel;
using CavalryFight.Core.MVVM;
using CavalryFight.Core.Services;
using CavalryFight.Services.Audio;
using CavalryFight.Services.Match;
using CavalryFight.Services.SceneManagement;
using CavalryFight.ViewModels;
using UnityEngine;
using UnityEngine.UIElements;

namespace CavalryFight.Views
{
    /// <summary>
    /// リザルト画面のView
    /// </summary>
    /// <remarks>
    /// マッチ終了後に表示される結果画面のUIを管理します。
    /// スコア、統計情報、リプレイ保存ボタンなどを表示します。
    /// </remarks>
    [RequireComponent(typeof(UIDocument))]
    public class ResultsView : UIToolkitViewBase<ResultsViewModel>
    {
        #region Serialized Fields

        [Header("Audio")]
        [SerializeField] private AudioClip? _victoryBGM;
        [SerializeField] private AudioClip? _defeatBGM;
        [SerializeField] private AudioClip? _drawBGM;
        [SerializeField] private AudioClip? _buttonClickSFX;

        #endregion

        #region UI Elements

        // Header
        private VisualElement? _resultBadge;
        private Label? _resultLabel;
        private Label? _playerTeamLabel;
        private Label? _playerScoreLabel;
        private Label? _enemyTeamLabel;
        private Label? _enemyScoreLabel;

        // Match Info
        private Label? _mapLabel;
        private Label? _gameModeLabel;
        private Label? _durationLabel;

        // Game Settings
        private Label? _maxPlayersLabel;
        private Label? _timeLimitLabel;
        private Label? _arrowLimitLabel;

        // Player Stats
        private Label? _arrowsFiredLabel;
        private Label? _hitsLabel;
        private Label? _accuracyLabel;
        private Label? _killsLabel;
        private Label? _deathsLabel;
        private Label? _kdRatioLabel;

        // Replay Section
        private VisualElement? _replaySavedBadge;
        private Button? _saveReplayButton;

        // Rematch Section (Multiplayer)
        private VisualElement? _rematchSection;
        private Label? _rematchStatusLabel;
        private Label? _rematchCountLabel;
        private Button? _voteRematchButton;
        private Button? _cancelVoteButton;

        // Footer Navigation
        private Button? _mainMenuButton;
        private Button? _lobbyButton;
        private Button? _returnToRoomButton;
        private Button? _playAgainButton;

        #endregion

        #region Services

        private IAudioService? _audioService;
        private ISceneManagementService? _sceneManagementService;

        #endregion

        #region Unity Lifecycle

        protected override void Awake()
        {
            base.Awake();

            _audioService = ServiceLocator.Instance.Get<IAudioService>();
            _sceneManagementService = ServiceLocator.Instance.Get<ISceneManagementService>();

            // マッチ結果データを取得（仮実装：実際はIMatchServiceから取得）
            var matchResult = GetMatchResult();
            ViewModel = new ResultsViewModel(matchResult);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            PlayResultBGM();
        }

        #endregion

        #region UI Setup

        protected override void OnRootVisualElementReady(VisualElement root)
        {
            base.OnRootVisualElementReady(root);

            GetUIElements();
            RegisterEventHandlers();
            UpdateUIFromViewModel();
        }

        private void GetUIElements()
        {
            // Header
            _resultBadge = Q<VisualElement>("ResultBadge");
            _resultLabel = Q<Label>("ResultLabel");
            _playerTeamLabel = Q<Label>("PlayerTeamLabel");
            _playerScoreLabel = Q<Label>("PlayerScoreLabel");
            _enemyTeamLabel = Q<Label>("EnemyTeamLabel");
            _enemyScoreLabel = Q<Label>("EnemyScoreLabel");

            // Match Info
            _mapLabel = Q<Label>("MapLabel");
            _gameModeLabel = Q<Label>("GameModeLabel");
            _durationLabel = Q<Label>("DurationLabel");

            // Game Settings
            _maxPlayersLabel = Q<Label>("MaxPlayersLabel");
            _timeLimitLabel = Q<Label>("TimeLimitLabel");
            _arrowLimitLabel = Q<Label>("ArrowLimitLabel");

            // Player Stats
            _arrowsFiredLabel = Q<Label>("ArrowsFiredValue");
            _hitsLabel = Q<Label>("HitsValue");
            _accuracyLabel = Q<Label>("AccuracyValue");
            _killsLabel = Q<Label>("KillsValue");
            _deathsLabel = Q<Label>("DeathsValue");
            _kdRatioLabel = Q<Label>("KDRatioValue");

            // Replay Section
            _replaySavedBadge = Q<VisualElement>("ReplaySavedBadge");
            _saveReplayButton = Q<Button>("SaveReplayButton");

            // Rematch Section
            _rematchSection = Q<VisualElement>("RematchSection");
            _rematchStatusLabel = Q<Label>("RematchStatusLabel");
            _rematchCountLabel = Q<Label>("RematchCountLabel");
            _voteRematchButton = Q<Button>("VoteRematchButton");
            _cancelVoteButton = Q<Button>("CancelVoteButton");

            // Footer Navigation
            _mainMenuButton = Q<Button>("MainMenuButton");
            _lobbyButton = Q<Button>("LobbyButton");
            _returnToRoomButton = Q<Button>("ReturnToRoomButton");
            _playAgainButton = Q<Button>("PlayAgainButton");
        }

        private void RegisterEventHandlers()
        {
            _saveReplayButton?.RegisterCallback<ClickEvent>(OnSaveReplayClicked);
            _watchReplayButton?.RegisterCallback<ClickEvent>(OnWatchReplayClicked);
            _rematchButton?.RegisterCallback<ClickEvent>(OnRematchClicked);
            _backToMenuButton?.RegisterCallback<ClickEvent>(OnBackToMenuClicked);
        }

        private void UnregisterEventHandlers()
        {
            _saveReplayButton?.UnregisterCallback<ClickEvent>(OnSaveReplayClicked);
            _watchReplayButton?.UnregisterCallback<ClickEvent>(OnWatchReplayClicked);
            _rematchButton?.UnregisterCallback<ClickEvent>(OnRematchClicked);
            _backToMenuButton?.UnregisterCallback<ClickEvent>(OnBackToMenuClicked);
        }

        #endregion

        #region UI Update

        private void UpdateUIFromViewModel()
        {
            if (ViewModel == null) return;

            UpdateResultHeader();
            UpdateMatchInfo();
            UpdateGameSettings();
            UpdatePlayerStats();
            UpdateReplayButtons();
        }

        private void UpdateResultHeader()
        {
            if (ViewModel == null) return;

            // 結果ラベル
            if (_resultLabel != null)
            {
                _resultLabel.text = ViewModel.ResultText;
            }

            // 結果バッジのスタイルクラスを更新
            if (_resultBadge != null)
            {
                _resultBadge.RemoveFromClassList("result-victory");
                _resultBadge.RemoveFromClassList("result-defeat");
                _resultBadge.RemoveFromClassList("result-draw");

                if (ViewModel.IsDraw)
                {
                    _resultBadge.AddToClassList("result-draw");
                }
                else if (ViewModel.IsVictory)
                {
                    _resultBadge.AddToClassList("result-victory");
                }
                else
                {
                    _resultBadge.AddToClassList("result-defeat");
                }
            }

            // チーム名ラベル（個人戦/チーム戦で切り替え）
            if (_playerTeamLabel != null)
            {
                _playerTeamLabel.text = ViewModel.PlayerTeamName;
            }

            if (_enemyTeamLabel != null)
            {
                _enemyTeamLabel.text = ViewModel.EnemyTeamName;
            }

            // スコアラベル
            if (_playerScoreLabel != null)
            {
                _playerScoreLabel.text = ViewModel.PlayerScore.ToString();
            }

            if (_enemyScoreLabel != null)
            {
                _enemyScoreLabel.text = ViewModel.EnemyScore.ToString();
            }
        }

        private void UpdateMatchInfo()
        {
            if (ViewModel == null) return;

            if (_mapLabel != null)
            {
                _mapLabel.text = ViewModel.MapName;
            }

            if (_gameModeLabel != null)
            {
                _gameModeLabel.text = ViewModel.GameMode;
            }

            if (_durationLabel != null)
            {
                _durationLabel.text = ViewModel.DurationText;
            }
        }

        private void UpdateGameSettings()
        {
            if (ViewModel == null) return;

            if (_maxPlayersLabel != null)
            {
                _maxPlayersLabel.text = ViewModel.MaxPlayers.ToString();
            }

            if (_timeLimitLabel != null)
            {
                _timeLimitLabel.text = ViewModel.TimeLimitText;
            }

            if (_arrowLimitLabel != null)
            {
                _arrowLimitLabel.text = ViewModel.ArrowLimitText;
            }
        }

        private void UpdatePlayerStats()
        {
            if (ViewModel == null) return;

            var stats = ViewModel.LocalStats;

            if (_arrowsFiredLabel != null)
            {
                _arrowsFiredLabel.text = stats.ArrowsFired.ToString();
            }

            if (_hitsLabel != null)
            {
                _hitsLabel.text = stats.Hits.ToString();
            }

            if (_accuracyLabel != null)
            {
                _accuracyLabel.text = stats.AccuracyText;
            }

            if (_killsLabel != null)
            {
                _killsLabel.text = stats.Kills.ToString();
            }

            if (_deathsLabel != null)
            {
                _deathsLabel.text = stats.Deaths.ToString();
            }

            if (_kdRatioLabel != null)
            {
                _kdRatioLabel.text = stats.KDRatioText;
            }
        }

        private void UpdateReplayButtons()
        {
            if (ViewModel == null) return;

            // リプレイ保存済みバッジ
            if (_replaySavedBadge != null)
            {
                _replaySavedBadge.style.display = ViewModel.IsReplaySaved
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
            }

            // 保存ボタン
            if (_saveReplayButton != null)
            {
                _saveReplayButton.SetEnabled(ViewModel.CanSaveReplay);
                _saveReplayButton.style.display = ViewModel.IsReplaySaved
                    ? DisplayStyle.None
                    : DisplayStyle.Flex;
            }

            // 視聴ボタン
            if (_watchReplayButton != null)
            {
                _watchReplayButton.style.display = ViewModel.IsReplaySaved
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
            }
        }

        #endregion

        #region Event Handlers

        private void OnSaveReplayClicked(ClickEvent evt)
        {
            PlayButtonSound();
            ViewModel?.SaveReplayCommand.Execute(null);
        }

        private void OnWatchReplayClicked(ClickEvent evt)
        {
            PlayButtonSound();
            ViewModel?.WatchReplayCommand.Execute(null);
        }

        private void OnRematchClicked(ClickEvent evt)
        {
            PlayButtonSound();
            ViewModel?.RematchCommand.Execute(null);
        }

        private void OnBackToMenuClicked(ClickEvent evt)
        {
            PlayButtonSound();
            ViewModel?.BackToMenuCommand.Execute(null);
        }

        #endregion

        #region Audio

        private void PlayResultBGM()
        {
            if (_audioService == null || ViewModel == null) return;

            AudioClip? bgm = null;

            if (ViewModel.IsDraw && _drawBGM != null)
            {
                bgm = _drawBGM;
            }
            else if (ViewModel.IsVictory && _victoryBGM != null)
            {
                bgm = _victoryBGM;
            }
            else if (ViewModel.IsDefeat && _defeatBGM != null)
            {
                bgm = _defeatBGM;
            }

            if (bgm != null)
            {
                _audioService.PlayBgm(bgm);
            }
        }

        private void PlayButtonSound()
        {
            if (_audioService != null && _buttonClickSFX != null)
            {
                _audioService.PlaySfx(_buttonClickSFX);
            }
        }

        #endregion

        #region ViewModel Binding

        protected override void BindViewModel(ResultsViewModel viewModel)
        {
            base.BindViewModel(viewModel);
            viewModel.PropertyChanged += OnViewModelPropertyChanged;
            viewModel.ReplaySaved += OnReplaySaved;
        }

        protected override void UnbindViewModel()
        {
            if (ViewModel != null)
            {
                ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
                ViewModel.ReplaySaved -= OnReplaySaved;
            }

            UnregisterEventHandlers();
            base.UnbindViewModel();
        }

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(ResultsViewModel.IsReplaySaved):
                case nameof(ResultsViewModel.IsSavingReplay):
                case nameof(ResultsViewModel.CanSaveReplay):
                case nameof(ResultsViewModel.CanWatchReplay):
                    UpdateReplayButtons();
                    break;

                case nameof(ResultsViewModel.MatchResult):
                    UpdateUIFromViewModel();
                    break;
            }
        }

        private void OnReplaySaved(object? sender, string replayId)
        {
            Debug.Log($"[ResultsView] Replay saved: {replayId}");
            UpdateReplayButtons();
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// マッチ結果を取得します
        /// </summary>
        /// <remarks>
        /// TODO: 実際の実装では IMatchService から結果を取得する
        /// </remarks>
        private MatchResult GetMatchResult()
        {
            // TODO: IMatchService から実際のマッチ結果を取得
            // var matchService = ServiceLocator.Instance.Get<IMatchService>();
            // return matchService?.GetLastMatchResult() ?? CreateMockResult();

            return CreateMockResult();
        }

        /// <summary>
        /// モックのマッチ結果を作成します（開発用）
        /// </summary>
        private MatchResult CreateMockResult()
        {
            return new MatchResult
            {
                MapName = "Arena",
                GameMode = "Deathmatch",
                MatchDuration = 300f,
                PlayerScore = 10,
                EnemyScore = 7,
                HasReplayData = true,
                LocalPlayerStats = new PlayerStatistics
                {
                    PlayerId = "local",
                    PlayerName = "Player",
                    Score = 10,
                    ArrowsFired = 50,
                    Hits = 15,
                    Kills = 10,
                    Deaths = 3,
                    Headshots = 2,
                    LongestKillStreak = 4
                }
            };
        }

        #endregion
    }
}
