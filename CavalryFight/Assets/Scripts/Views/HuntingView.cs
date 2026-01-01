#nullable enable

using System;
using CavalryFight.Core.MVVM;
using CavalryFight.Core.Services;
using CavalryFight.Services.Audio;
using CavalryFight.Services.Input;
using CavalryFight.Services.Lobby;
using CavalryFight.Services.Match;
using CavalryFight.Services.SceneManagement;
using CavalryFight.ViewModels;
using UnityEngine;
using UnityEngine.UIElements;

namespace CavalryFight.Views
{
    /// <summary>
    /// ハンティングシーンのView
    /// </summary>
    /// <remarks>
    /// ハンティングモード固有のUI（役割表示、スタン表示など）を管理します。
    /// </remarks>
    [RequireComponent(typeof(UIDocument))]
    public class HuntingView : UIToolkitViewBase<HuntingViewModel>
    {
        #region UI Elements

        // HUD
        private VisualElement? _hudPanel;
        private Label? _roleLabel;
        private Label? _scoreLabel;
        private Label? _timerLabel;
        private Label? _hitsLabel;
        private Label? _accuracyLabel;

        // Team Scores
        private VisualElement? _teamScorePanel;
        private Label? _team0ScoreLabel;
        private Label? _team1ScoreLabel;

        // Stun Indicator
        private VisualElement? _stunPanel;
        private Label? _stunLabel;
        private VisualElement? _stunProgressBar;

        // Countdown
        private VisualElement? _countdownPanel;
        private Label? _countdownLabel;

        // Scoreboard
        private VisualElement? _scoreboardPanel;
        private VisualElement? _scoreboardContainer;

        // Pause Menu
        private VisualElement? _pausePanel;
        private Button? _resumeButton;
        private Button? _settingsButton;
        private Button? _leaveMatchButton;

        // Match End
        private VisualElement? _matchEndPanel;
        private Label? _matchResultLabel;
        private Label? _finalScoreLabel;
        private Button? _continueButton;

        // Score Popup Container
        private VisualElement? _scorePopupContainer;

        #endregion

        #region Services

        private IAudioService? _audioService;
        private IInputService? _inputService;
        private ISceneManagementService? _sceneService;
        private IMatchService? _matchService;

        #endregion

        #region Serialized Fields

        [Header("Audio")]
        [SerializeField] private AudioClip? _matchStartSound;
        [SerializeField] private AudioClip? _countdownSound;
        [SerializeField] private AudioClip? _matchEndSound;
        [SerializeField] private AudioClip? _scoreSound;
        [SerializeField] private AudioClip? _stunSound;
        [SerializeField] private AudioClip? _buttonClickSound;

        [Header("Score Popup")]
        [SerializeField] private float _scorePopupDuration = 1.5f;

        #endregion

        #region Unity Lifecycle

        protected override void Awake()
        {
            base.Awake();

            _audioService = ServiceLocator.Instance.Get<IAudioService>();
            _inputService = ServiceLocator.Instance.Get<IInputService>();
            _sceneService = ServiceLocator.Instance.Get<ISceneManagementService>();
            _matchService = ServiceLocator.Instance.Get<IMatchService>();

            if (_sceneService != null)
            {
                ViewModel = new HuntingViewModel(_sceneService, _matchService);
            }
        }

        private void Update()
        {
            ViewModel?.Update();

            // スタン表示を更新
            UpdateStunIndicator();

            // 入力処理
            HandleInput();
        }

        /// <summary>
        /// 入力を処理します
        /// </summary>
        private void HandleInput()
        {
            if (_inputService == null)
            {
                return;
            }

            // スコアボード表示切り替え（Tabキー）
            if (_inputService.GetScoreboardButtonDown())
            {
                ViewModel?.ShowScoreboard();
            }
            else if (_inputService.GetScoreboardButtonUp())
            {
                ViewModel?.HideScoreboard();
            }

            // ポーズ切り替え（Escapeキー）
            if (_inputService.GetMenuButtonDown())
            {
                ViewModel?.TogglePause();
            }
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
            // HUD
            _hudPanel = Q<VisualElement>("HUDPanel");
            _roleLabel = Q<Label>("RoleLabel");
            _scoreLabel = Q<Label>("ScoreLabel");
            _timerLabel = Q<Label>("TimerLabel");
            _hitsLabel = Q<Label>("HitsLabel");
            _accuracyLabel = Q<Label>("AccuracyLabel");

            // Team Scores
            _teamScorePanel = Q<VisualElement>("TeamScorePanel");
            _team0ScoreLabel = Q<Label>("Team0ScoreLabel");
            _team1ScoreLabel = Q<Label>("Team1ScoreLabel");

            // Stun Indicator
            _stunPanel = Q<VisualElement>("StunPanel");
            _stunLabel = Q<Label>("StunLabel");
            _stunProgressBar = Q<VisualElement>("StunProgressBar");

            // Countdown
            _countdownPanel = Q<VisualElement>("CountdownPanel");
            _countdownLabel = Q<Label>("CountdownLabel");

            // Scoreboard
            _scoreboardPanel = Q<VisualElement>("ScoreboardPanel");
            _scoreboardContainer = Q<VisualElement>("ScoreboardContainer");

            // Pause Menu
            _pausePanel = Q<VisualElement>("PausePanel");
            _resumeButton = Q<Button>("ResumeButton");
            _settingsButton = Q<Button>("SettingsButton");
            _leaveMatchButton = Q<Button>("LeaveMatchButton");

            // Match End
            _matchEndPanel = Q<VisualElement>("MatchEndPanel");
            _matchResultLabel = Q<Label>("MatchResultLabel");
            _finalScoreLabel = Q<Label>("FinalScoreLabel");
            _continueButton = Q<Button>("ContinueButton");

            // Score Popup
            _scorePopupContainer = Q<VisualElement>("ScorePopupContainer");
        }

        private void RegisterEventHandlers()
        {
            _resumeButton?.RegisterCallback<ClickEvent>(OnResumeClicked);
            _settingsButton?.RegisterCallback<ClickEvent>(OnSettingsClicked);
            _leaveMatchButton?.RegisterCallback<ClickEvent>(OnLeaveMatchClicked);
            _continueButton?.RegisterCallback<ClickEvent>(OnContinueClicked);
        }

        private void UpdateUIFromViewModel()
        {
            if (ViewModel == null)
            {
                return;
            }

            // 役割表示
            if (_roleLabel != null)
            {
                _roleLabel.text = ViewModel.RoleText;
                _roleLabel.RemoveFromClassList("role--hunter");
                _roleLabel.RemoveFromClassList("role--wolf");
                _roleLabel.AddToClassList(ViewModel.IsLocalPlayerHunter ? "role--hunter" : "role--wolf");
            }

            // 初期状態を反映
            UpdateHUD();
            UpdatePausePanel();
            UpdateCountdownPanel();
            UpdateMatchEndPanel();
            UpdateStunIndicator();
        }

        #endregion

        #region UI Updates

        private void UpdateHUD()
        {
            if (ViewModel == null)
            {
                return;
            }

            if (_scoreLabel != null)
            {
                _scoreLabel.text = ViewModel.LocalPlayerScore.ToString();
            }

            if (_timerLabel != null)
            {
                _timerLabel.text = ViewModel.RemainingTimeText;
            }

            if (_hitsLabel != null)
            {
                _hitsLabel.text = ViewModel.LocalPlayerHits.ToString();
            }

            if (_accuracyLabel != null)
            {
                _accuracyLabel.text = ViewModel.AccuracyText;
            }

            // チームスコア
            if (_team0ScoreLabel != null)
            {
                _team0ScoreLabel.text = ViewModel.Team0Score.ToString();
            }

            if (_team1ScoreLabel != null)
            {
                _team1ScoreLabel.text = ViewModel.Team1Score.ToString();
            }
        }

        private void UpdateStunIndicator()
        {
            if (_stunPanel == null || ViewModel == null)
            {
                return;
            }

            bool showStun = ViewModel.IsLocalPlayerStunned;
            _stunPanel.style.display = showStun ? DisplayStyle.Flex : DisplayStyle.None;

            if (showStun && _stunProgressBar != null)
            {
                float progress = ViewModel.StunRemainingTime / 3f; // 仮の最大スタン時間
                _stunProgressBar.style.width = Length.Percent(progress * 100f);
            }
        }

        private void UpdatePausePanel()
        {
            if (_pausePanel == null || ViewModel == null)
            {
                return;
            }

            _pausePanel.style.display = ViewModel.IsPaused ? DisplayStyle.Flex : DisplayStyle.None;

            if (_hudPanel != null)
            {
                _hudPanel.style.opacity = ViewModel.IsPaused ? 0.5f : 1f;
            }
        }

        private void UpdateCountdownPanel()
        {
            if (_countdownPanel == null || ViewModel == null)
            {
                return;
            }

            bool showCountdown = ViewModel.IsCountingDown && ViewModel.CountdownValue > 0;
            _countdownPanel.style.display = showCountdown ? DisplayStyle.Flex : DisplayStyle.None;

            if (_countdownLabel != null && showCountdown)
            {
                _countdownLabel.text = ViewModel.CountdownValue.ToString();
            }
        }

        private void UpdateScoreboard()
        {
            if (_scoreboardPanel == null || _scoreboardContainer == null || ViewModel == null)
            {
                return;
            }

            _scoreboardPanel.style.display = ViewModel.IsScoreboardVisible
                ? DisplayStyle.Flex
                : DisplayStyle.None;

            if (!ViewModel.IsScoreboardVisible)
            {
                return;
            }

            _scoreboardContainer.Clear();

            foreach (var player in ViewModel.PlayerScores)
            {
                var row = CreateScoreboardRow(player);
                _scoreboardContainer.Add(row);
            }
        }

        private VisualElement CreateScoreboardRow(HuntingPlayerScoreInfo player)
        {
            var row = new VisualElement();
            row.AddToClassList("scoreboard-row");

            if (player.IsLocalPlayer)
            {
                row.AddToClassList("scoreboard-row--local");
            }

            // チームカラー
            row.AddToClassList($"scoreboard-row--team{player.TeamIndex}");

            // 役割アイコン
            var roleLabel = new Label(player.RoleText);
            roleLabel.AddToClassList("scoreboard-role");
            roleLabel.AddToClassList(player.IsHunter ? "scoreboard-role--hunter" : "scoreboard-role--wolf");
            row.Add(roleLabel);

            // プレイヤー名
            var nameLabel = new Label(player.PlayerName);
            nameLabel.AddToClassList("scoreboard-name");
            row.Add(nameLabel);

            // スコア
            var scoreLabel = new Label(player.Score.ToString());
            scoreLabel.AddToClassList("scoreboard-score");
            row.Add(scoreLabel);

            // ヒット数（ハンターのみ表示）
            if (player.IsHunter)
            {
                var hitsLabel = new Label(player.Hits.ToString());
                hitsLabel.AddToClassList("scoreboard-hits");
                row.Add(hitsLabel);

                var accuracyLabel = new Label(player.AccuracyText);
                accuracyLabel.AddToClassList("scoreboard-accuracy");
                row.Add(accuracyLabel);
            }

            return row;
        }

        private void UpdateMatchEndPanel()
        {
            if (_matchEndPanel == null || ViewModel == null)
            {
                return;
            }

            _matchEndPanel.style.display = ViewModel.IsMatchEnded
                ? DisplayStyle.Flex
                : DisplayStyle.None;
        }

        private void ShowMatchResult(MatchEndResult result)
        {
            if (_matchEndPanel == null || _matchResultLabel == null || _finalScoreLabel == null)
            {
                return;
            }

            _matchEndPanel.style.display = DisplayStyle.Flex;

            // 勝敗判定（ViewModelからローカルプレイヤーのチーム情報を取得）
            int localTeam = ViewModel?.LocalPlayerTeamIndex ?? 0;

            bool isWinner = (ulong)localTeam == result.WinnerId;
            bool isDraw = result.WinnerId == ulong.MaxValue;

            if (isDraw)
            {
                _matchResultLabel.text = "DRAW";
                _matchResultLabel.RemoveFromClassList("result-victory");
                _matchResultLabel.RemoveFromClassList("result-defeat");
                _matchResultLabel.AddToClassList("result-draw");
            }
            else
            {
                _matchResultLabel.text = isWinner ? "VICTORY" : "DEFEAT";
                _matchResultLabel.RemoveFromClassList("result-victory");
                _matchResultLabel.RemoveFromClassList("result-defeat");
                _matchResultLabel.RemoveFromClassList("result-draw");
                _matchResultLabel.AddToClassList(isWinner ? "result-victory" : "result-defeat");
            }

            _finalScoreLabel.text = $"Team Score: {ViewModel?.LocalPlayerScore ?? 0}";

            PlaySound(_matchEndSound);
        }

        #endregion

        #region Score Popup

        private void ShowScorePopup(int score, HitLocation location)
        {
            if (_scorePopupContainer == null)
            {
                return;
            }

            string locationText = location switch
            {
                HitLocation.Heart => "HEART!",
                HitLocation.Head => "HEADSHOT!",
                _ => ""
            };

            var popup = new Label($"+{score} {locationText}");
            popup.AddToClassList("score-popup");

            if (score >= 50)
            {
                popup.AddToClassList("score-popup--critical");
            }
            else if (score >= 30)
            {
                popup.AddToClassList("score-popup--high");
            }

            _scorePopupContainer.Add(popup);

            popup.schedule.Execute(() =>
            {
                popup.RemoveFromHierarchy();
            }).StartingIn((long)(_scorePopupDuration * 1000));

            PlaySound(_scoreSound);
        }

        #endregion

        #region Event Handlers

        private void OnResumeClicked(ClickEvent evt)
        {
            PlayButtonSound();
            ViewModel?.Resume();
        }

        private void OnSettingsClicked(ClickEvent evt)
        {
            PlayButtonSound();
            _sceneService?.LoadSettingsWithReturn(ReturnDestination.Hunting);
        }

        private void OnLeaveMatchClicked(ClickEvent evt)
        {
            PlayButtonSound();
            ViewModel?.LeaveMatch();
        }

        private void OnContinueClicked(ClickEvent evt)
        {
            PlayButtonSound();
            _sceneService?.LoadResults();
        }

        #endregion

        #region ViewModel Binding

        protected override void BindViewModel(HuntingViewModel viewModel)
        {
            base.BindViewModel(viewModel);

            viewModel.PropertyChanged += OnViewModelPropertyChanged;
            viewModel.PauseStateChanged += OnPauseStateChanged;
            viewModel.MatchStarted += OnMatchStarted;
            viewModel.MatchEnded += OnMatchEnded;
            viewModel.ScoreGained += OnScoreGained;
            viewModel.HunterStunned += OnHunterStunned;
        }

        protected override void UnbindViewModel()
        {
            if (ViewModel != null)
            {
                ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
                ViewModel.PauseStateChanged -= OnPauseStateChanged;
                ViewModel.MatchStarted -= OnMatchStarted;
                ViewModel.MatchEnded -= OnMatchEnded;
                ViewModel.ScoreGained -= OnScoreGained;
                ViewModel.HunterStunned -= OnHunterStunned;
            }
            base.UnbindViewModel();
        }

        private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(HuntingViewModel.LocalPlayerScore):
                case nameof(HuntingViewModel.RemainingTimeText):
                case nameof(HuntingViewModel.LocalPlayerHits):
                case nameof(HuntingViewModel.AccuracyText):
                case nameof(HuntingViewModel.Team0Score):
                case nameof(HuntingViewModel.Team1Score):
                    UpdateHUD();
                    break;

                case nameof(HuntingViewModel.CountdownValue):
                case nameof(HuntingViewModel.IsCountingDown):
                    UpdateCountdownPanel();
                    if (ViewModel?.IsCountingDown == true && ViewModel.CountdownValue > 0)
                    {
                        PlaySound(_countdownSound);
                    }
                    break;

                case nameof(HuntingViewModel.IsScoreboardVisible):
                case nameof(HuntingViewModel.PlayerScores):
                    UpdateScoreboard();
                    break;

                case nameof(HuntingViewModel.IsMatchEnded):
                    UpdateMatchEndPanel();
                    break;

                case nameof(HuntingViewModel.IsLocalPlayerStunned):
                    if (ViewModel?.IsLocalPlayerStunned == true)
                    {
                        PlaySound(_stunSound);
                    }
                    break;
            }
        }

        private void OnPauseStateChanged(object? sender, bool isPaused)
        {
            UpdatePausePanel();
            Time.timeScale = isPaused ? 0f : 1f;
        }

        private void OnMatchStarted(object? sender, EventArgs e)
        {
            PlaySound(_matchStartSound);

            if (_countdownPanel != null)
            {
                _countdownPanel.style.display = DisplayStyle.None;
            }
        }

        private void OnMatchEnded(object? sender, MatchEndResult result)
        {
            ShowMatchResult(result);
        }

        private void OnScoreGained(object? sender, HuntingScoreEventArgs e)
        {
            ShowScorePopup(e.Score, e.Location);
        }

        private void OnHunterStunned(object? sender, ulong clientId)
        {
            Debug.Log($"[HuntingView] Hunter {clientId} was stunned");
        }

        #endregion

        #region Audio

        private void PlayButtonSound()
        {
            PlaySound(_buttonClickSound);
        }

        private void PlaySound(AudioClip? clip)
        {
            if (clip != null && _audioService != null)
            {
                _audioService.PlaySfx(clip);
            }
        }

        #endregion

        #region Cleanup

        protected override void OnDestroy()
        {
            Time.timeScale = 1f;
            base.OnDestroy();
        }

        #endregion
    }
}
