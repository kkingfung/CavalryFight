#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using CavalryFight.Core.Commands;
using CavalryFight.Core.MVVM;
using CavalryFight.Core.Services;
using CavalryFight.Services.Lobby;
using CavalryFight.Services.Match;
using CavalryFight.Services.SceneManagement;
using CavalryFight.ViewModels.Data;
using UnityEngine;

namespace CavalryFight.ViewModels
{
    /// <summary>
    /// マッチシーンのViewModel
    /// </summary>
    /// <remarks>
    /// マッチ中のUI表示、スコアボード、ポーズ機能などを管理します。
    /// ゲームモードに関わらず共通のインターフェースを提供します。
    /// IMatchServiceを使用してマッチ情報にアクセスします。
    /// </remarks>
    public class MatchViewModel : ViewModelBase
    {
        #region Fields

        private readonly ISceneManagementService? _sceneService;
        private readonly IMatchService? _matchService;
        private readonly ILobbyService? _lobbyService;

        private GameMode _currentGameMode;
        private MatchState _matchState;
        private bool _isPaused;
        private bool _isScoreboardVisible;
        private float _remainingTime;
        private float _matchTime;
        private int _countdownValue;
        private float _timeLimit;

        // ローカルプレイヤー情報
        private ulong _localPlayerId;
        private int _localPlayerScore;
        private int _localPlayerRemainingArrows;
        private int _localPlayerHits;
        private int _localPlayerShots;

        // チームスコア（チームモード用）
        private int _team0Score;
        private int _team1Score;

        // スコアボード
        private List<PlayerScoreInfo> _playerScores = new List<PlayerScoreInfo>();

        #endregion

        #region Properties

        /// <summary>
        /// 現在のゲームモード
        /// </summary>
        public GameMode CurrentGameMode
        {
            get => _currentGameMode;
            private set => SetProperty(ref _currentGameMode, value);
        }

        /// <summary>
        /// ゲームモード名
        /// </summary>
        public string GameModeName => CurrentGameMode switch
        {
            GameMode.Arena => "Arena",
            GameMode.ScoreMatch => "Score Match",
            GameMode.TeamFight => "Team Fight",
            GameMode.Deathmatch => "Deathmatch",
            GameMode.Hunting => "Hunting",
            _ => "Unknown"
        };

        /// <summary>
        /// 現在のマッチ状態
        /// </summary>
        public MatchState MatchState
        {
            get => _matchState;
            private set
            {
                if (SetProperty(ref _matchState, value))
                {
                    OnPropertyChanged(nameof(IsMatchInProgress));
                    OnPropertyChanged(nameof(IsCountingDown));
                    OnPropertyChanged(nameof(IsMatchEnded));
                }
            }
        }

        /// <summary>
        /// マッチが進行中かどうか
        /// </summary>
        public bool IsMatchInProgress => MatchState == MatchState.InProgress;

        /// <summary>
        /// カウントダウン中かどうか
        /// </summary>
        public bool IsCountingDown => MatchState == MatchState.Countdown;

        /// <summary>
        /// マッチが終了したかどうか
        /// </summary>
        public bool IsMatchEnded => MatchState == MatchState.Ended;

        /// <summary>
        /// ポーズ中かどうか
        /// </summary>
        public bool IsPaused
        {
            get => _isPaused;
            set
            {
                if (SetProperty(ref _isPaused, value))
                {
                    PauseStateChanged?.Invoke(this, value);
                }
            }
        }

        /// <summary>
        /// スコアボードが表示されているかどうか
        /// </summary>
        public bool IsScoreboardVisible
        {
            get => _isScoreboardVisible;
            set => SetProperty(ref _isScoreboardVisible, value);
        }

        /// <summary>
        /// 残り時間（秒）
        /// </summary>
        public float RemainingTime
        {
            get => _remainingTime;
            private set
            {
                if (SetProperty(ref _remainingTime, value))
                {
                    OnPropertyChanged(nameof(RemainingTimeText));
                    OnPropertyChanged(nameof(HasTimeLimit));
                }
            }
        }

        /// <summary>
        /// 残り時間テキスト
        /// </summary>
        public string RemainingTimeText
        {
            get
            {
                if (!HasTimeLimit)
                {
                    return "--:--";
                }
                int minutes = Mathf.FloorToInt(RemainingTime / 60f);
                int seconds = Mathf.FloorToInt(RemainingTime % 60f);
                return $"{minutes}:{seconds:D2}";
            }
        }

        /// <summary>
        /// 時間制限があるかどうか
        /// </summary>
        public bool HasTimeLimit => _timeLimit > 0 || RemainingTime > 0;

        /// <summary>
        /// マッチ経過時間
        /// </summary>
        public float MatchTime
        {
            get => _matchTime;
            private set
            {
                if (SetProperty(ref _matchTime, value))
                {
                    OnPropertyChanged(nameof(MatchTimeText));
                }
            }
        }

        /// <summary>
        /// マッチ経過時間テキスト
        /// </summary>
        public string MatchTimeText
        {
            get
            {
                int minutes = Mathf.FloorToInt(MatchTime / 60f);
                int seconds = Mathf.FloorToInt(MatchTime % 60f);
                return $"{minutes}:{seconds:D2}";
            }
        }

        /// <summary>
        /// カウントダウン値
        /// </summary>
        public int CountdownValue
        {
            get => _countdownValue;
            private set => SetProperty(ref _countdownValue, value);
        }

        /// <summary>
        /// ローカルクライアントID
        /// </summary>
        public ulong LocalClientId => _localPlayerId;

        /// <summary>
        /// ローカルプレイヤーのスコア
        /// </summary>
        public int LocalPlayerScore
        {
            get => _localPlayerScore;
            private set => SetProperty(ref _localPlayerScore, value);
        }

        /// <summary>
        /// ローカルプレイヤーの残り矢数
        /// </summary>
        public int LocalPlayerRemainingArrows
        {
            get => _localPlayerRemainingArrows;
            private set
            {
                if (SetProperty(ref _localPlayerRemainingArrows, value))
                {
                    OnPropertyChanged(nameof(IsUnlimitedArrows));
                    OnPropertyChanged(nameof(ArrowsText));
                }
            }
        }

        /// <summary>
        /// 矢が無制限かどうか
        /// </summary>
        public bool IsUnlimitedArrows => LocalPlayerRemainingArrows < 0;

        /// <summary>
        /// 矢数テキスト
        /// </summary>
        public string ArrowsText => IsUnlimitedArrows ? "∞" : LocalPlayerRemainingArrows.ToString();

        /// <summary>
        /// ローカルプレイヤーの命中数
        /// </summary>
        public int LocalPlayerHits
        {
            get => _localPlayerHits;
            private set
            {
                if (SetProperty(ref _localPlayerHits, value))
                {
                    OnPropertyChanged(nameof(LocalPlayerAccuracy));
                    OnPropertyChanged(nameof(AccuracyText));
                }
            }
        }

        /// <summary>
        /// ローカルプレイヤーの発射数
        /// </summary>
        public int LocalPlayerShots
        {
            get => _localPlayerShots;
            private set
            {
                if (SetProperty(ref _localPlayerShots, value))
                {
                    OnPropertyChanged(nameof(LocalPlayerAccuracy));
                    OnPropertyChanged(nameof(AccuracyText));
                }
            }
        }

        /// <summary>
        /// ローカルプレイヤーの命中率
        /// </summary>
        public float LocalPlayerAccuracy
        {
            get
            {
                if (LocalPlayerShots == 0)
                {
                    return 0f;
                }
                return (float)LocalPlayerHits / LocalPlayerShots * 100f;
            }
        }

        /// <summary>
        /// 命中率テキスト
        /// </summary>
        public string AccuracyText => $"{LocalPlayerAccuracy:F1}%";

        /// <summary>
        /// チームモードかどうか
        /// </summary>
        public bool IsTeamMode => CurrentGameMode == GameMode.TeamFight || CurrentGameMode == GameMode.Hunting;

        /// <summary>
        /// チーム0のスコア
        /// </summary>
        public int Team0Score
        {
            get => _team0Score;
            private set => SetProperty(ref _team0Score, value);
        }

        /// <summary>
        /// チーム1のスコア
        /// </summary>
        public int Team1Score
        {
            get => _team1Score;
            private set => SetProperty(ref _team1Score, value);
        }

        /// <summary>
        /// プレイヤースコア一覧（スコアボード用）
        /// </summary>
        public IReadOnlyList<PlayerScoreInfo> PlayerScores => _playerScores;

        #endregion

        #region Events

        /// <summary>
        /// ポーズ状態が変更された時に発生します
        /// </summary>
        public event EventHandler<bool>? PauseStateChanged;

        /// <summary>
        /// マッチが開始された時に発生します
        /// </summary>
        public event EventHandler? MatchStarted;

        /// <summary>
        /// マッチが終了した時に発生します
        /// </summary>
        public event EventHandler<MatchEndResult>? MatchEnded;

        /// <summary>
        /// プレイヤーがスコアを獲得した時に発生します
        /// </summary>
        public event EventHandler<ScoreGainedEventArgs>? ScoreGained;

        /// <summary>
        /// プレイヤーが倒された時に発生します
        /// </summary>
        public event EventHandler<PlayerKilledEventArgs>? PlayerKilled;

        /// <summary>
        /// メインメニューへ戻る要求が発生した時に発生します
        /// </summary>
        public event EventHandler? BackToMenuRequested;

        #endregion

        #region Commands

        /// <summary>
        /// ポーズ切り替えコマンド
        /// </summary>
        public ICommand TogglePauseCommand { get; }

        /// <summary>
        /// スコアボード表示切り替えコマンド
        /// </summary>
        public ICommand ToggleScoreboardCommand { get; }

        /// <summary>
        /// マッチを退出するコマンド
        /// </summary>
        public ICommand LeaveMatchCommand { get; }

        #endregion

        #region Constructor

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public MatchViewModel(ISceneManagementService sceneService, IMatchService? matchService = null)
        {
            _sceneService = sceneService ?? throw new ArgumentNullException(nameof(sceneService));
            _matchService = matchService ?? ServiceLocator.Instance.Get<IMatchService>();
            _lobbyService = ServiceLocator.Instance.Get<ILobbyService>();

            // コマンド初期化
            TogglePauseCommand = new RelayCommand(TogglePause, () => IsMatchInProgress);
            ToggleScoreboardCommand = new RelayCommand(ToggleScoreboard);
            LeaveMatchCommand = new RelayCommand(LeaveMatch);

            // IMatchServiceのイベントを購読
            SubscribeToMatchService();

            // 初期状態を設定
            if (_matchService != null)
            {
                CurrentGameMode = _matchService.CurrentGameMode;
                MatchState = _matchService.CurrentState;
            }

            Debug.Log("[MatchViewModel] Initialized");
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// ポーズを切り替えます
        /// </summary>
        public void TogglePause()
        {
            IsPaused = !IsPaused;
        }

        /// <summary>
        /// ゲームを再開します
        /// </summary>
        public void Resume()
        {
            IsPaused = false;
        }

        /// <summary>
        /// スコアボード表示を切り替えます
        /// </summary>
        public void ToggleScoreboard()
        {
            IsScoreboardVisible = !IsScoreboardVisible;
        }

        /// <summary>
        /// スコアボードを表示します
        /// </summary>
        public void ShowScoreboard()
        {
            IsScoreboardVisible = true;
            UpdateScoreboard();
        }

        /// <summary>
        /// スコアボードを非表示にします
        /// </summary>
        public void HideScoreboard()
        {
            IsScoreboardVisible = false;
        }

        /// <summary>
        /// マッチを退出します
        /// </summary>
        /// <remarks>
        /// マッチを終了してルームに戻ります。
        /// ルームからは退出しません（ルームは維持されます）。
        /// ホストがマッチを退出すると、全プレイヤーがルームに戻ります。
        /// </remarks>
        public void LeaveMatch()
        {
            Debug.Log("[MatchViewModel] Leaving match, returning to room...");

            BackToMenuRequested?.Invoke(this, EventArgs.Empty);

            // ホストの場合は全プレイヤーにルームへの復帰を通知
            if (_lobbyService != null && _lobbyService.IsHost)
            {
                Debug.Log("[MatchViewModel] Host is leaving, notifying all players to return to room...");
                _lobbyService.RequestReturnToRoom();
            }

            // ルームに戻る（ルームからは退出しない）
            _sceneService?.LoadMatchRoom();
        }

        /// <summary>
        /// 毎フレーム更新
        /// </summary>
        public void Update()
        {
            if (_matchService == null)
            {
                return;
            }

            // 時間の更新
            MatchTime = _matchService.MatchTime;
            RemainingTime = _matchService.RemainingTime;

            // ローカルプレイヤーのスコアを更新
            UpdateLocalPlayerScore();

            // チームスコアを更新
            if (IsTeamMode)
            {
                Team0Score = _matchService.GetTeamScore(0);
                Team1Score = _matchService.GetTeamScore(1);
            }
        }

        /// <summary>
        /// ローカルプレイヤーIDを設定します
        /// </summary>
        public void SetLocalPlayerId(ulong playerId)
        {
            _localPlayerId = playerId;
            Debug.Log($"[MatchViewModel] Local player ID set: {playerId}");
        }

        #endregion

        #region Private Methods

        private void SubscribeToMatchService()
        {
            if (_matchService == null)
            {
                return;
            }

            _matchService.MatchStateChanged += OnMatchStateChanged;
            _matchService.MatchStarted += OnMatchStarted;
            _matchService.MatchEndedWithResult += OnMatchEnded;
            _matchService.PlayerScored += OnPlayerScored;
            _matchService.HitRegistered += OnHitRegistered;
            _matchService.CountdownUpdated += OnCountdownUpdated;
        }

        private void UnsubscribeFromMatchService()
        {
            if (_matchService == null)
            {
                return;
            }

            _matchService.MatchStateChanged -= OnMatchStateChanged;
            _matchService.MatchStarted -= OnMatchStarted;
            _matchService.MatchEndedWithResult -= OnMatchEnded;
            _matchService.PlayerScored -= OnPlayerScored;
            _matchService.HitRegistered -= OnHitRegistered;
            _matchService.CountdownUpdated -= OnCountdownUpdated;
        }

        private void OnMatchStateChanged(MatchState newState)
        {
            MatchState = newState;
        }

        private void OnCountdownUpdated(int seconds)
        {
            CountdownValue = seconds;
        }

        private void OnMatchStarted()
        {
            MatchStarted?.Invoke(this, EventArgs.Empty);
        }

        private void OnMatchEnded(MatchEndResult result)
        {
            // マッチ終了時に最終スコアを同期
            // Update()のUpdateLocalPlayerScore()は継続して呼ばれるが、
            // ネットワーク同期の遅延を考慮して明示的に更新する
            UpdateLocalPlayerScore();

            Debug.Log($"[MatchViewModel] OnMatchEnded: LocalPlayerScore={LocalPlayerScore}, WinnerId={result.WinnerId}");

            MatchEnded?.Invoke(this, result);
        }

        private void OnPlayerScored(ulong clientId, int score, HitLocation hitLocation, Vector3 hitPosition)
        {
            ScoreGained?.Invoke(this, new ScoreGainedEventArgs(clientId, score, hitLocation, hitPosition));

            // ローカルプレイヤーの場合はUIを更新
            if (clientId == _localPlayerId)
            {
                LocalPlayerScore += score;
                LocalPlayerHits++;
            }

            // スコアボードを更新
            UpdateScoreboard();
        }

        private void OnHitRegistered(HitResult hitResult)
        {
            // 命中時にキルフィード用のイベントを発火
            // プレイヤー名を取得してPlayerKilledイベントを発火
            string killerName = GetPlayerName(hitResult.ShooterClientId);
            string victimName = GetPlayerName(hitResult.TargetClientId);

            PlayerKilled?.Invoke(this, new PlayerKilledEventArgs(
                hitResult.TargetClientId,   // victimId
                hitResult.ShooterClientId,  // killerId
                victimName,
                killerName
            ));
        }

        /// <summary>
        /// クライアントIDからプレイヤー名を取得します
        /// </summary>
        /// <param name="clientId">クライアントID</param>
        /// <returns>プレイヤー名（見つからない場合は"Player {clientId}"）</returns>
        private string GetPlayerName(ulong clientId)
        {
            if (_matchService == null)
            {
                return $"Player {clientId}";
            }

            var playerScore = _matchService.GetPlayerScore(clientId);
            if (playerScore.HasValue)
            {
                return playerScore.Value.PlayerName.ToString();
            }

            return $"Player {clientId}";
        }

        private void UpdateLocalPlayerScore()
        {
            if (_matchService == null)
            {
                return;
            }

            var playerScore = _matchService.GetPlayerScore(_localPlayerId);

            if (playerScore.HasValue)
            {
                LocalPlayerScore = playerScore.Value.Score;
                LocalPlayerRemainingArrows = playerScore.Value.RemainingArrows;
                LocalPlayerHits = playerScore.Value.HitCount;
                LocalPlayerShots = playerScore.Value.ShotCount;

                // Debug.Log($"[MatchViewModel] UpdateLocalPlayerScore: ClientId={_localPlayerId}, Score={playerScore.Value.Score}, Hits={playerScore.Value.HitCount}, Shots={playerScore.Value.ShotCount}");
            }
            else
            {
                // Debug.LogWarning($"[MatchViewModel] UpdateLocalPlayerScore: No score found for ClientId={_localPlayerId}");
            }
        }

        private void UpdateScoreboard()
        {
            if (_matchService == null)
            {
                return;
            }

            _playerScores.Clear();
            var allScores = _matchService.GetAllPlayerScores();

            foreach (var score in allScores)
            {
                _playerScores.Add(new PlayerScoreInfo
                {
                    ClientId = score.ClientId,
                    PlayerName = score.PlayerName.ToString(),
                    Score = score.Score,
                    Hits = score.HitCount,
                    Shots = score.ShotCount,
                    TeamIndex = score.TeamIndex,
                    IsLocalPlayer = score.ClientId == _localPlayerId
                });
            }

            // スコア順にソート
            _playerScores.Sort((a, b) => b.Score.CompareTo(a.Score));

            OnPropertyChanged(nameof(PlayerScores));
        }

        protected override void OnDispose()
        {
            UnsubscribeFromMatchService();
            base.OnDispose();
        }

        #endregion
    }
}
