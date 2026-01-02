#nullable enable

using System;
using System.Collections.Generic;
using CavalryFight.Core.MVVM;
using CavalryFight.Services.Lobby;
using CavalryFight.Services.Match;
using CavalryFight.Services.SceneManagement;
using CavalryFight.ViewModels.Data;
using UnityEngine;

namespace CavalryFight.ViewModels
{
    /// <summary>
    /// ハンティングシーンのViewModel
    /// </summary>
    /// <remarks>
    /// ハンティングモードのUI状態とゲームロジックの仲介を管理します。
    /// IMatchServiceを使用してマッチ情報にアクセスします。
    /// </remarks>
    public class HuntingViewModel : ViewModelBase
    {
        #region Fields

        private readonly ISceneManagementService _sceneService;
        private readonly IMatchService? _matchService;

        // マッチ状態
        private MatchState _matchState;
        private bool _isPaused;
        private bool _isScoreboardVisible;

        // タイマー
        private float _remainingTime;
        private int _countdownValue;
        private bool _isCountingDown;

        // ローカルプレイヤー情報
        private int _localPlayerScore;
        private int _localPlayerHits;
        private int _localPlayerShots;
        private bool _isLocalPlayerHunter;
        private bool _isLocalPlayerStunned;
        private float _stunRemainingTime;

        // チームスコア
        private int _team0Score;
        private int _team1Score;

        // ローカルプレイヤーチーム
        private int _localPlayerTeamIndex;

        #endregion

        #region Properties

        /// <summary>
        /// 現在のゲームモード名
        /// </summary>
        public string GameModeName => "Hunting";

        /// <summary>
        /// 現在のマッチ状態
        /// </summary>
        public MatchState MatchState
        {
            get => _matchState;
            private set => SetProperty(ref _matchState, value);
        }

        /// <summary>
        /// ポーズ中かどうか
        /// </summary>
        public bool IsPaused
        {
            get => _isPaused;
            private set
            {
                if (SetProperty(ref _isPaused, value))
                {
                    PauseStateChanged?.Invoke(this, value);
                }
            }
        }

        /// <summary>
        /// スコアボード表示中かどうか
        /// </summary>
        public bool IsScoreboardVisible
        {
            get => _isScoreboardVisible;
            private set => SetProperty(ref _isScoreboardVisible, value);
        }

        /// <summary>
        /// 残り時間
        /// </summary>
        public float RemainingTime
        {
            get => _remainingTime;
            private set => SetProperty(ref _remainingTime, value);
        }

        /// <summary>
        /// 残り時間テキスト
        /// </summary>
        public string RemainingTimeText
        {
            get
            {
                int minutes = Mathf.FloorToInt(_remainingTime / 60f);
                int seconds = Mathf.FloorToInt(_remainingTime % 60f);
                return $"{minutes}:{seconds:00}";
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
        /// カウントダウン中かどうか
        /// </summary>
        public bool IsCountingDown
        {
            get => _isCountingDown;
            private set => SetProperty(ref _isCountingDown, value);
        }

        /// <summary>
        /// ローカルプレイヤーのスコア
        /// </summary>
        public int LocalPlayerScore
        {
            get => _localPlayerScore;
            private set => SetProperty(ref _localPlayerScore, value);
        }

        /// <summary>
        /// ローカルプレイヤーのヒット数
        /// </summary>
        public int LocalPlayerHits
        {
            get => _localPlayerHits;
            private set => SetProperty(ref _localPlayerHits, value);
        }

        /// <summary>
        /// ローカルプレイヤーのショット数
        /// </summary>
        public int LocalPlayerShots
        {
            get => _localPlayerShots;
            private set => SetProperty(ref _localPlayerShots, value);
        }

        /// <summary>
        /// 命中率テキスト
        /// </summary>
        public string AccuracyText
        {
            get
            {
                if (_localPlayerShots == 0)
                {
                    return "0.0%";
                }
                float accuracy = (float)_localPlayerHits / _localPlayerShots * 100f;
                return $"{accuracy:F1}%";
            }
        }

        /// <summary>
        /// ローカルプレイヤーがハンターかどうか
        /// </summary>
        public bool IsLocalPlayerHunter
        {
            get => _isLocalPlayerHunter;
            private set => SetProperty(ref _isLocalPlayerHunter, value);
        }

        /// <summary>
        /// ローカルプレイヤーがスタン中かどうか
        /// </summary>
        public bool IsLocalPlayerStunned
        {
            get => _isLocalPlayerStunned;
            private set => SetProperty(ref _isLocalPlayerStunned, value);
        }

        /// <summary>
        /// スタン残り時間
        /// </summary>
        public float StunRemainingTime
        {
            get => _stunRemainingTime;
            private set => SetProperty(ref _stunRemainingTime, value);
        }

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
        /// マッチが終了したかどうか
        /// </summary>
        public bool IsMatchEnded => _matchState == MatchState.Ended;

        /// <summary>
        /// 役割テキスト
        /// </summary>
        public string RoleText => _isLocalPlayerHunter ? "Hunter" : "Wolf";

        /// <summary>
        /// ローカルプレイヤーのチームインデックス
        /// </summary>
        public int LocalPlayerTeamIndex
        {
            get => _localPlayerTeamIndex;
            private set => SetProperty(ref _localPlayerTeamIndex, value);
        }

        /// <summary>
        /// プレイヤースコア情報一覧
        /// </summary>
        public List<HuntingPlayerScoreInfo> PlayerScores { get; } = new List<HuntingPlayerScoreInfo>();

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
        /// スコアを獲得した時に発生します
        /// </summary>
        public event EventHandler<HuntingScoreEventArgs>? ScoreGained;

        /// <summary>
        /// ウルフがスタンさせられた時に発生します
        /// </summary>
        public event EventHandler<ulong>? WolfStunned;

        /// <summary>
        /// ハンターがスタンさせられた時に発生します
        /// </summary>
        public event EventHandler<ulong>? HunterStunned;

        #endregion

        #region Constructor

        public HuntingViewModel(ISceneManagementService sceneService, IMatchService? matchService = null)
        {
            _sceneService = sceneService;
            _matchService = matchService;

            if (_matchService != null)
            {
                SubscribeToMatchService();
                UpdateFromMatchService();
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// フレーム更新
        /// </summary>
        public void Update()
        {
            if (_matchService == null)
            {
                return;
            }

            RemainingTime = _matchService.RemainingTime;
            Team0Score = _matchService.GetTeamScore(0);
            Team1Score = _matchService.GetTeamScore(1);

            // スタン状態を更新
            var localClientId = Unity.Netcode.NetworkManager.Singleton?.LocalClientId ?? 0;
            IsLocalPlayerStunned = _matchService.IsStunned(localClientId);

            // プロパティ変更を通知
            OnPropertyChanged(nameof(RemainingTimeText));
            OnPropertyChanged(nameof(AccuracyText));
        }

        /// <summary>
        /// ポーズを切り替えます
        /// </summary>
        public void TogglePause()
        {
            if (_matchState != MatchState.InProgress && _matchState != MatchState.Paused)
            {
                return;
            }

            IsPaused = !IsPaused;
        }

        /// <summary>
        /// 再開します
        /// </summary>
        public void Resume()
        {
            IsPaused = false;
        }

        /// <summary>
        /// スコアボードを表示します
        /// </summary>
        public void ShowScoreboard()
        {
            IsScoreboardVisible = true;
            UpdatePlayerScores();
        }

        /// <summary>
        /// スコアボードを非表示にします
        /// </summary>
        public void HideScoreboard()
        {
            IsScoreboardVisible = false;
        }

        /// <summary>
        /// マッチを離脱します
        /// </summary>
        public void LeaveMatch()
        {
            _sceneService?.LoadMainMenu();
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
            _matchService.CountdownUpdated += OnCountdownUpdated;
            _matchService.MatchStarted += OnMatchStarted;
            _matchService.MatchEndedWithResult += OnMatchEnded;
            _matchService.PlayerScored += OnPlayerScored;
        }

        private void UnsubscribeFromMatchService()
        {
            if (_matchService == null)
            {
                return;
            }

            _matchService.MatchStateChanged -= OnMatchStateChanged;
            _matchService.CountdownUpdated -= OnCountdownUpdated;
            _matchService.MatchStarted -= OnMatchStarted;
            _matchService.MatchEndedWithResult -= OnMatchEnded;
            _matchService.PlayerScored -= OnPlayerScored;
        }

        private void UpdateFromMatchService()
        {
            if (_matchService == null)
            {
                return;
            }

            MatchState = _matchService.CurrentState;
            RemainingTime = _matchService.RemainingTime;
            Team0Score = _matchService.GetTeamScore(0);
            Team1Score = _matchService.GetTeamScore(1);

            // ローカルプレイヤー情報を更新
            var localClientId = Unity.Netcode.NetworkManager.Singleton?.LocalClientId ?? 0;
            var playerScore = _matchService.GetPlayerScore(localClientId);

            if (playerScore.HasValue)
            {
                LocalPlayerScore = playerScore.Value.Score;
                LocalPlayerHits = playerScore.Value.HitCount;
                LocalPlayerShots = playerScore.Value.ShotCount;
                LocalPlayerTeamIndex = playerScore.Value.TeamIndex;
            }

            // 役割を確認
            IsLocalPlayerHunter = _matchService.IsHunter(localClientId);
        }

        private void UpdatePlayerScores()
        {
            PlayerScores.Clear();

            if (_matchService == null)
            {
                return;
            }

            var localClientId = Unity.Netcode.NetworkManager.Singleton?.LocalClientId ?? 0;
            var allScores = _matchService.GetAllPlayerScores();

            foreach (var score in allScores)
            {
                PlayerScores.Add(new HuntingPlayerScoreInfo
                {
                    ClientId = score.ClientId,
                    PlayerName = score.PlayerName.ToString(),
                    Score = score.Score,
                    Hits = score.HitCount,
                    Shots = score.ShotCount,
                    TeamIndex = score.TeamIndex,
                    IsHunter = _matchService.IsHunter(score.ClientId),
                    IsLocalPlayer = score.ClientId == localClientId
                });
            }

            // チームとスコアでソート
            PlayerScores.Sort((a, b) =>
            {
                if (a.TeamIndex != b.TeamIndex)
                {
                    return a.TeamIndex.CompareTo(b.TeamIndex);
                }
                return b.Score.CompareTo(a.Score);
            });

            OnPropertyChanged(nameof(PlayerScores));
        }

        #endregion

        #region Event Handlers

        private void OnMatchStateChanged(MatchState state)
        {
            MatchState = state;
            IsCountingDown = state == MatchState.Countdown;
        }

        private void OnCountdownUpdated(int seconds)
        {
            CountdownValue = seconds;
        }

        private void OnMatchStarted()
        {
            IsCountingDown = false;
            MatchStarted?.Invoke(this, EventArgs.Empty);
        }

        private void OnMatchEnded(MatchEndResult result)
        {
            MatchState = MatchState.Ended;
            MatchEnded?.Invoke(this, result);
        }

        private void OnPlayerScored(ulong clientId, int score, HitLocation location)
        {
            var localClientId = Unity.Netcode.NetworkManager.Singleton?.LocalClientId ?? 0;

            if (clientId == localClientId)
            {
                LocalPlayerScore += score;
                LocalPlayerHits++;

                ScoreGained?.Invoke(this, new HuntingScoreEventArgs
                {
                    ClientId = clientId,
                    Score = score,
                    Location = location
                });
            }

            // チームスコアを更新
            Team0Score = _matchService?.GetTeamScore(0) ?? 0;
            Team1Score = _matchService?.GetTeamScore(1) ?? 0;
        }

        #endregion

        #region Dispose

        protected override void OnDispose()
        {
            UnsubscribeFromMatchService();
            base.OnDispose();
        }

        #endregion
    }
}
