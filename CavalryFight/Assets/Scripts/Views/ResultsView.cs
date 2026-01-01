#nullable enable

using System;
using System.ComponentModel;
using CavalryFight.Core.MVVM;
using CavalryFight.Core.Services;
using CavalryFight.Services.Audio;
using CavalryFight.Services.Match;
using CavalryFight.Services.Replay;
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

        // Leaderboard
        private ScrollView? _leaderboardContainer;

        // Replay Section
        private VisualElement? _replaySavedBadge;
        private Button? _saveReplayButton;

        // Vote Section (Footer right - Multiplayer)
        private VisualElement? _voteSection;
        private Label? _rematchStatusLabel;
        private Label? _rematchCountLabel;
        private Button? _voteRematchButton;
        private Button? _cancelVoteButton;

        // Play Again Section (Footer right - Single player)
        private VisualElement? _playAgainSection;
        private Button? _playAgainButton;

        // Footer Navigation (Left)
        private Button? _mainMenuButton;
        private Button? _lobbyButton;
        private Button? _returnToRoomButton;

        // Host Left Notification
        private VisualElement? _hostLeftOverlay;

        #endregion

        #region Services

        private IAudioService? _audioService;
        private ISceneManagementService? _sceneManagementService;
        private IReplayService? _replayService;

        #endregion

        #region Unity Lifecycle

        protected override void Awake()
        {
            base.Awake();

            // サービスを取得
            _audioService = ServiceLocator.Instance.Get<IAudioService>();
            _sceneManagementService = ServiceLocator.Instance.Get<ISceneManagementService>();
            _replayService = ServiceLocator.Instance.Get<IReplayService>();

            // マッチ結果データを取得（仮実装：実際はIMatchServiceから取得）
            var matchResult = GetMatchResult();

            // ViewModelを作成（サービスをコンストラクタで注入）
            ViewModel = new ResultsViewModel(matchResult, _sceneManagementService!, _replayService);
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

            // Leaderboard
            _leaderboardContainer = Q<ScrollView>("LeaderboardContainer");

            // Replay Section
            _replaySavedBadge = Q<VisualElement>("ReplaySavedBadge");
            _saveReplayButton = Q<Button>("SaveReplayButton");

            // Vote Section (Footer right)
            _voteSection = Q<VisualElement>("VoteSection");
            _rematchStatusLabel = Q<Label>("RematchStatusLabel");
            _rematchCountLabel = Q<Label>("RematchCountLabel");
            _voteRematchButton = Q<Button>("VoteRematchButton");
            _cancelVoteButton = Q<Button>("CancelVoteButton");

            // Play Again Section
            _playAgainSection = Q<VisualElement>("PlayAgainSection");
            _playAgainButton = Q<Button>("PlayAgainButton");

            // Footer Navigation
            _mainMenuButton = Q<Button>("MainMenuButton");
            _lobbyButton = Q<Button>("LobbyButton");
            _returnToRoomButton = Q<Button>("ReturnToRoomButton");

            // Host Left Notification
            _hostLeftOverlay = Q<VisualElement>("HostLeftOverlay");
        }

        private void RegisterEventHandlers()
        {
            _saveReplayButton?.RegisterCallback<ClickEvent>(OnSaveReplayClicked);
            _voteRematchButton?.RegisterCallback<ClickEvent>(OnVoteRematchClicked);
            _cancelVoteButton?.RegisterCallback<ClickEvent>(OnCancelVoteClicked);
            _mainMenuButton?.RegisterCallback<ClickEvent>(OnMainMenuClicked);
            _lobbyButton?.RegisterCallback<ClickEvent>(OnLobbyClicked);
            _returnToRoomButton?.RegisterCallback<ClickEvent>(OnReturnToRoomClicked);
            _playAgainButton?.RegisterCallback<ClickEvent>(OnPlayAgainClicked);
        }

        private void UnregisterEventHandlers()
        {
            _saveReplayButton?.UnregisterCallback<ClickEvent>(OnSaveReplayClicked);
            _voteRematchButton?.UnregisterCallback<ClickEvent>(OnVoteRematchClicked);
            _cancelVoteButton?.UnregisterCallback<ClickEvent>(OnCancelVoteClicked);
            _mainMenuButton?.UnregisterCallback<ClickEvent>(OnMainMenuClicked);
            _lobbyButton?.UnregisterCallback<ClickEvent>(OnLobbyClicked);
            _returnToRoomButton?.UnregisterCallback<ClickEvent>(OnReturnToRoomClicked);
            _playAgainButton?.UnregisterCallback<ClickEvent>(OnPlayAgainClicked);
        }

        #endregion

        #region UI Update

        private void UpdateUIFromViewModel()
        {
            if (ViewModel == null) return;

            UpdateResultHeader();
            UpdateMatchInfo();
            UpdateGameSettings();
            UpdateLeaderboard();
            UpdateReplayButtons();
            UpdateNavigationButtons();
            UpdateVoteSection();
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

        private void UpdateLeaderboard()
        {
            if (ViewModel == null || _leaderboardContainer == null) return;

            // 既存の行をクリア
            _leaderboardContainer.Clear();

            // ソートされたプレイヤー統計を表示
            foreach (var stats in ViewModel.SortedPlayerStats)
            {
                var row = CreatePlayerRow(stats);
                _leaderboardContainer.Add(row);
            }
        }

        /// <summary>
        /// プレイヤー行を作成します
        /// </summary>
        private VisualElement CreatePlayerRow(PlayerStatistics stats)
        {
            var row = new VisualElement();
            row.AddToClassList("player-row");

            // ローカルプレイヤーの場合はハイライト
            if (stats.IsLocalPlayer)
            {
                row.AddToClassList("player-row-local");
            }

            // 1位の場合は金色ハイライト
            if (stats.Rank == 1)
            {
                row.AddToClassList("player-row-first");
            }

            // 死亡している場合は薄く表示
            if (!stats.IsAlive)
            {
                row.AddToClassList("player-row-dead");
            }

            // 退出した場合はさらに薄く表示
            if (stats.HasLeft)
            {
                row.AddToClassList("player-row-left");
            }

            // ランク
            var rankLabel = new Label(stats.Rank.ToString());
            rankLabel.AddToClassList("player-row-value");
            rankLabel.AddToClassList("player-row-rank");
            if (stats.Rank <= 3)
            {
                rankLabel.AddToClassList($"rank-{stats.Rank}");
            }
            row.Add(rankLabel);

            // プレイヤー名（退出した場合は表示を変更）
            var displayName = stats.PlayerName;
            if (stats.HasLeft)
            {
                displayName += " (Left)";
            }
            else if (stats.IsHost)
            {
                displayName += " [Host]";
            }
            var nameLabel = new Label(displayName);
            nameLabel.AddToClassList("player-row-value");
            nameLabel.AddToClassList("player-row-name");
            row.Add(nameLabel);

            // スコア
            var scoreLabel = new Label(stats.Score.ToString());
            scoreLabel.AddToClassList("player-row-value");
            scoreLabel.AddToClassList("player-row-score");
            row.Add(scoreLabel);

            // キル数
            var killsLabel = new Label(stats.Kills.ToString());
            killsLabel.AddToClassList("player-row-value");
            killsLabel.AddToClassList("player-row-stat");
            row.Add(killsLabel);

            // デス数
            var deathsLabel = new Label(stats.Deaths.ToString());
            deathsLabel.AddToClassList("player-row-value");
            deathsLabel.AddToClassList("player-row-stat");
            row.Add(deathsLabel);

            // K/D比
            var kdLabel = new Label(stats.KDRatioText);
            kdLabel.AddToClassList("player-row-value");
            kdLabel.AddToClassList("player-row-stat");
            row.Add(kdLabel);

            // 命中率
            var accLabel = new Label(stats.AccuracyText);
            accLabel.AddToClassList("player-row-value");
            accLabel.AddToClassList("player-row-stat");
            row.Add(accLabel);

            // ステータス（マルチプレイヤーの場合は投票状態も表示）
            string statusText;
            string statusClass;

            if (stats.HasLeft)
            {
                statusText = "Left";
                statusClass = "status-left";
            }
            else if (stats.IsNPC)
            {
                statusText = stats.IsAlive ? "Alive" : "Dead";
                statusClass = stats.IsAlive ? "status-alive" : "status-dead";
            }
            else if (ViewModel?.IsMultiplayerMatch == true)
            {
                // マルチプレイヤーの場合は投票状態を表示
                if (stats.HasVotedRematch)
                {
                    statusText = "Voted";
                    statusClass = "status-voted";
                }
                else
                {
                    statusText = "Waiting";
                    statusClass = "status-not-voted";
                }
            }
            else
            {
                statusText = stats.IsAlive ? "Alive" : "Dead";
                statusClass = stats.IsAlive ? "status-alive" : "status-dead";
            }

            var statusLabel = new Label(statusText);
            statusLabel.AddToClassList("player-row-value");
            statusLabel.AddToClassList("player-row-status");
            statusLabel.AddToClassList(statusClass);
            row.Add(statusLabel);

            return row;
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
        }

        private void UpdateNavigationButtons()
        {
            if (ViewModel == null) return;

            // シングルプレイヤーの場合はPlay Againセクションを表示、マルチの場合はVoteセクションを表示
            if (_playAgainSection != null)
            {
                _playAgainSection.style.display = ViewModel.IsSinglePlayerMatch
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
            }

            if (_voteSection != null)
            {
                _voteSection.style.display = ViewModel.IsMultiplayerMatch
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
            }

            // ルームがまだ開いている場合のみReturn to Roomを表示
            // ただし、投票済みの場合は無効化
            if (_returnToRoomButton != null)
            {
                _returnToRoomButton.style.display = ViewModel.CanReturnToRoom
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
                _returnToRoomButton.SetEnabled(!ViewModel.IsReturnToRoomDisabled);
            }
        }

        private void UpdateVoteSection()
        {
            if (ViewModel == null) return;

            // 投票状態ラベル
            if (_rematchStatusLabel != null)
            {
                _rematchStatusLabel.text = ViewModel.RematchStatusText;

                // 全員同意した場合は緑色に
                if (ViewModel.AllPlayersAgreed)
                {
                    _rematchStatusLabel.AddToClassList("rematch-ready");
                }
                else
                {
                    _rematchStatusLabel.RemoveFromClassList("rematch-ready");
                }
            }

            // 投票人数ラベル
            if (_rematchCountLabel != null)
            {
                _rematchCountLabel.text = ViewModel.VoteCountText;
            }

            // 投票ボタン（投票していない場合のみ表示）
            if (_voteRematchButton != null)
            {
                _voteRematchButton.style.display = ViewModel.CanVoteRematch
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
                _voteRematchButton.SetEnabled(ViewModel.CanVoteRematch);
            }

            // キャンセルボタン（投票済みかつ全員同意していない場合のみ表示）
            if (_cancelVoteButton != null)
            {
                _cancelVoteButton.style.display = ViewModel.CanCancelVote
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
                _cancelVoteButton.SetEnabled(ViewModel.CanCancelVote);
            }

            // Return to Roomボタンの有効/無効を更新
            if (_returnToRoomButton != null)
            {
                _returnToRoomButton.SetEnabled(!ViewModel.IsReturnToRoomDisabled);
            }
        }

        #endregion

        #region Event Handlers

        private void OnSaveReplayClicked(ClickEvent evt)
        {
            PlayButtonSound();
            ViewModel?.SaveReplayCommand.Execute(null);
        }

        private void OnVoteRematchClicked(ClickEvent evt)
        {
            PlayButtonSound();
            ViewModel?.VoteRematchCommand.Execute(null);
        }

        private void OnCancelVoteClicked(ClickEvent evt)
        {
            PlayButtonSound();
            ViewModel?.CancelVoteCommand.Execute(null);
        }

        private void OnMainMenuClicked(ClickEvent evt)
        {
            PlayButtonSound();
            ViewModel?.MainMenuCommand.Execute(null);
        }

        private void OnLobbyClicked(ClickEvent evt)
        {
            PlayButtonSound();
            ViewModel?.LobbyCommand.Execute(null);
        }

        private void OnReturnToRoomClicked(ClickEvent evt)
        {
            PlayButtonSound();
            ViewModel?.ReturnToRoomCommand.Execute(null);
        }

        private void OnPlayAgainClicked(ClickEvent evt)
        {
            PlayButtonSound();
            ViewModel?.PlayAgainCommand.Execute(null);
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
            viewModel.RematchVoteChanged += OnRematchVoteChanged;
            viewModel.AllPlayersAgreedToRematch += OnAllPlayersAgreedToRematch;
            viewModel.PlayerLeft += OnPlayerLeft;
            viewModel.HostLeft += OnHostLeft;
            viewModel.LeaderboardUpdated += OnLeaderboardUpdated;
        }

        protected override void UnbindViewModel()
        {
            if (ViewModel != null)
            {
                ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
                ViewModel.ReplaySaved -= OnReplaySaved;
                ViewModel.RematchVoteChanged -= OnRematchVoteChanged;
                ViewModel.AllPlayersAgreedToRematch -= OnAllPlayersAgreedToRematch;
                ViewModel.PlayerLeft -= OnPlayerLeft;
                ViewModel.HostLeft -= OnHostLeft;
                ViewModel.LeaderboardUpdated -= OnLeaderboardUpdated;
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
                    UpdateReplayButtons();
                    break;

                case nameof(ResultsViewModel.MatchResult):
                    UpdateUIFromViewModel();
                    break;

                case nameof(ResultsViewModel.HasVotedRematch):
                case nameof(ResultsViewModel.RematchVoteCount):
                case nameof(ResultsViewModel.AllPlayersAgreed):
                case nameof(ResultsViewModel.CanVoteRematch):
                case nameof(ResultsViewModel.CanCancelVote):
                case nameof(ResultsViewModel.VoteCountText):
                case nameof(ResultsViewModel.RematchStatusText):
                    UpdateVoteSection();
                    break;

                case nameof(ResultsViewModel.CanReturnToRoom):
                case nameof(ResultsViewModel.IsReturnToRoomDisabled):
                    UpdateNavigationButtons();
                    break;

                case nameof(ResultsViewModel.ActivePlayerCount):
                    UpdateVoteSection();
                    UpdateLeaderboard();
                    break;
            }
        }

        private void OnReplaySaved(object? sender, string replayId)
        {
            Debug.Log($"[ResultsView] Replay saved: {replayId}");
            UpdateReplayButtons();
        }

        private void OnRematchVoteChanged(object? sender, EventArgs e)
        {
            Debug.Log("[ResultsView] Rematch vote changed");
            UpdateVoteSection();
        }

        private void OnAllPlayersAgreedToRematch(object? sender, EventArgs e)
        {
            Debug.Log("[ResultsView] All players agreed to rematch!");
            // UIは自動的にViewModelによってマッチに遷移されるため、ここでは何もしない
        }

        private void OnPlayerLeft(object? sender, PlayerStatistics player)
        {
            Debug.Log($"[ResultsView] Player left: {player.PlayerName}");
            UpdateLeaderboard();
        }

        private void OnHostLeft(object? sender, EventArgs e)
        {
            Debug.Log("[ResultsView] Host has left the room!");
            ShowHostLeftNotification();
        }

        private void OnLeaderboardUpdated(object? sender, EventArgs e)
        {
            UpdateLeaderboard();
        }

        /// <summary>
        /// ホスト退出通知を表示し、一定時間後にロビーに遷移
        /// </summary>
        private void ShowHostLeftNotification()
        {
            if (_hostLeftOverlay != null)
            {
                _hostLeftOverlay.style.display = DisplayStyle.Flex;
            }

            // 3秒後にロビーに遷移
            StartCoroutine(ReturnToLobbyAfterDelay(3f));
        }

        private System.Collections.IEnumerator ReturnToLobbyAfterDelay(float delay)
        {
            yield return new UnityEngine.WaitForSeconds(delay);
            _sceneManagementService?.LoadLobby();
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
            var localPlayer = new PlayerStatistics
            {
                PlayerId = "local",
                PlayerName = "You",
                Score = 10,
                ArrowsFired = 50,
                Hits = 15,
                Kills = 10,
                Deaths = 3,
                Headshots = 2,
                LongestKillStreak = 4,
                IsLocalPlayer = true,
                IsAlive = true,
                IsHost = true // ローカルプレイヤーがホスト
            };

            var result = new MatchResult
            {
                MapName = "Arena",
                GameMode = "Deathmatch",
                MatchDuration = 300f,
                MaxPlayers = 8,
                TimeLimit = 300,
                PlayerScore = 10,
                EnemyScore = 7,
                HasReplayData = true,
                IsMultiplayerMatch = true, // マルチプレイヤーとしてテスト
                CanReturnToRoom = true,
                LocalPlayerStats = localPlayer,
                AllPlayerStats = new System.Collections.Generic.List<PlayerStatistics>
                {
                    localPlayer,
                    new PlayerStatistics
                    {
                        PlayerId = "player2",
                        PlayerName = "Knight",
                        Score = 8,
                        ArrowsFired = 45,
                        Hits = 12,
                        Kills = 8,
                        Deaths = 4,
                        IsNPC = false,
                        IsAlive = true,
                        HasVotedRematch = true // 既に投票済み
                    },
                    new PlayerStatistics
                    {
                        PlayerId = "player3",
                        PlayerName = "Archer",
                        Score = 7,
                        ArrowsFired = 60,
                        Hits = 18,
                        Kills = 7,
                        Deaths = 5,
                        IsNPC = false,
                        IsAlive = false
                    },
                    new PlayerStatistics
                    {
                        PlayerId = "ai1",
                        PlayerName = "Bot_Warrior",
                        Score = 5,
                        ArrowsFired = 30,
                        Hits = 8,
                        Kills = 5,
                        Deaths = 6,
                        IsNPC = true,
                        IsAlive = true
                    },
                    new PlayerStatistics
                    {
                        PlayerId = "ai2",
                        PlayerName = "Bot_Hunter",
                        Score = 4,
                        ArrowsFired = 40,
                        Hits = 10,
                        Kills = 4,
                        Deaths = 7,
                        IsNPC = true,
                        IsAlive = false
                    },
                    new PlayerStatistics
                    {
                        PlayerId = "ai3",
                        PlayerName = "Bot_Scout",
                        Score = 3,
                        ArrowsFired = 25,
                        Hits = 5,
                        Kills = 3,
                        Deaths = 8,
                        IsNPC = true,
                        IsAlive = false
                    }
                }
            };

            return result;
        }

        #endregion
    }
}
