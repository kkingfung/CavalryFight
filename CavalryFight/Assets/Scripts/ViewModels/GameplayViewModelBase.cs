#nullable enable

using System;
using CavalryFight.Core.MVVM;
using CavalryFight.Services.Match;
using CavalryFight.Services.SceneManagement;
using UnityEngine;

namespace CavalryFight.ViewModels
{
    /// <summary>
    /// ゲームプレイ画面の共通ViewModel基底クラス
    /// </summary>
    /// <remarks>
    /// MatchViewModel と HuntingViewModel の共通機能を提供します。
    /// マッチ状態管理、タイマー、ポーズ、スコアボード表示などの基本機能を含みます。
    /// </remarks>
    public abstract class GameplayViewModelBase : ViewModelBase
    {
        #region Fields

        protected readonly ISceneManagementService? SceneService;
        protected readonly IMatchService? MatchService;

        private MatchState _matchState;
        private bool _isPaused;
        private bool _isScoreboardVisible;
        private float _remainingTime;
        private int _countdownValue;
        private bool _isCountingDown;

        // ローカルプレイヤー情報
        private int _localPlayerScore;
        private int _localPlayerHits;
        private int _localPlayerShots;

        // チームスコア
        private int _team0Score;
        private int _team1Score;

        #endregion

        #region Properties

        /// <summary>
        /// ゲームモード名（派生クラスで実装）
        /// </summary>
        public abstract string GameModeName { get; }

        /// <summary>
        /// 現在のマッチ状態
        /// </summary>
        public MatchState MatchState
        {
            get => _matchState;
            protected set
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
        /// マッチが終了したかどうか
        /// </summary>
        public bool IsMatchEnded => MatchState == MatchState.Ended;

        /// <summary>
        /// ポーズ中かどうか
        /// </summary>
        public bool IsPaused
        {
            get => _isPaused;
            protected set
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
            protected set => SetProperty(ref _isScoreboardVisible, value);
        }

        /// <summary>
        /// 残り時間
        /// </summary>
        public float RemainingTime
        {
            get => _remainingTime;
            protected set
            {
                if (SetProperty(ref _remainingTime, value))
                {
                    OnPropertyChanged(nameof(RemainingTimeText));
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
            protected set => SetProperty(ref _countdownValue, value);
        }

        /// <summary>
        /// カウントダウン中かどうか
        /// </summary>
        public bool IsCountingDown
        {
            get => _isCountingDown;
            protected set => SetProperty(ref _isCountingDown, value);
        }

        /// <summary>
        /// ローカルプレイヤーのスコア
        /// </summary>
        public int LocalPlayerScore
        {
            get => _localPlayerScore;
            protected set => SetProperty(ref _localPlayerScore, value);
        }

        /// <summary>
        /// ローカルプレイヤーのヒット数
        /// </summary>
        public int LocalPlayerHits
        {
            get => _localPlayerHits;
            protected set => SetProperty(ref _localPlayerHits, value);
        }

        /// <summary>
        /// ローカルプレイヤーのショット数
        /// </summary>
        public int LocalPlayerShots
        {
            get => _localPlayerShots;
            protected set => SetProperty(ref _localPlayerShots, value);
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
        /// チーム0のスコア
        /// </summary>
        public int Team0Score
        {
            get => _team0Score;
            protected set => SetProperty(ref _team0Score, value);
        }

        /// <summary>
        /// チーム1のスコア
        /// </summary>
        public int Team1Score
        {
            get => _team1Score;
            protected set => SetProperty(ref _team1Score, value);
        }

        /// <summary>
        /// ローカルクライアントID
        /// </summary>
        public ulong LocalClientId => Unity.Netcode.NetworkManager.Singleton?.LocalClientId ?? 0;

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

        #endregion

        #region Constructor

        protected GameplayViewModelBase(ISceneManagementService? sceneService, IMatchService? matchService)
        {
            SceneService = sceneService;
            MatchService = matchService;

            if (MatchService != null)
            {
                SubscribeToMatchService();
                UpdateFromMatchService();
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// フレーム更新（派生クラスでオーバーライド可能）
        /// </summary>
        public virtual void Update()
        {
            if (MatchService == null)
            {
                return;
            }

            RemainingTime = MatchService.RemainingTime;
            Team0Score = MatchService.GetTeamScore(0);
            Team1Score = MatchService.GetTeamScore(1);

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
        public virtual void ShowScoreboard()
        {
            IsScoreboardVisible = true;
        }

        /// <summary>
        /// スコアボードを非表示にします
        /// </summary>
        public virtual void HideScoreboard()
        {
            IsScoreboardVisible = false;
        }

        /// <summary>
        /// マッチを離脱します
        /// </summary>
        public virtual void LeaveMatch()
        {
            SceneService?.LoadMainMenu();
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// IMatchServiceのイベントを購読します
        /// </summary>
        protected virtual void SubscribeToMatchService()
        {
            if (MatchService == null)
            {
                return;
            }

            MatchService.MatchStateChanged += OnMatchStateChanged;
            MatchService.MatchStarted += OnMatchStartedHandler;
            MatchService.MatchEndedWithResult += OnMatchEndedHandler;
            MatchService.PlayerScored += OnPlayerScored;
        }

        /// <summary>
        /// IMatchServiceのイベント購読を解除します
        /// </summary>
        protected virtual void UnsubscribeFromMatchService()
        {
            if (MatchService == null)
            {
                return;
            }

            MatchService.MatchStateChanged -= OnMatchStateChanged;
            MatchService.MatchStarted -= OnMatchStartedHandler;
            MatchService.MatchEndedWithResult -= OnMatchEndedHandler;
            MatchService.PlayerScored -= OnPlayerScored;
        }

        /// <summary>
        /// IMatchServiceから状態を更新します
        /// </summary>
        protected virtual void UpdateFromMatchService()
        {
            if (MatchService == null)
            {
                return;
            }

            MatchState = MatchService.CurrentState;
            RemainingTime = MatchService.RemainingTime;
            Team0Score = MatchService.GetTeamScore(0);
            Team1Score = MatchService.GetTeamScore(1);

            // ローカルプレイヤー情報を更新
            var localClientId = Unity.Netcode.NetworkManager.Singleton?.LocalClientId ?? 0;
            var playerScore = MatchService.GetPlayerScore(localClientId);

            if (playerScore.HasValue)
            {
                LocalPlayerScore = playerScore.Value.Score;
                LocalPlayerHits = playerScore.Value.HitCount;
                LocalPlayerShots = playerScore.Value.ShotCount;
            }
        }

        /// <summary>
        /// MatchStartedイベントを発火します
        /// </summary>
        protected void RaiseMatchStarted()
        {
            MatchStarted?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// MatchEndedイベントを発火します
        /// </summary>
        protected void RaiseMatchEnded(MatchEndResult result)
        {
            MatchEnded?.Invoke(this, result);
        }

        #endregion

        #region Event Handlers

        private void OnMatchStateChanged(MatchState state)
        {
            MatchState = state;
            IsCountingDown = state == MatchState.Countdown;
        }

        private void OnMatchStartedHandler()
        {
            IsCountingDown = false;
            RaiseMatchStarted();
        }

        private void OnMatchEndedHandler(MatchEndResult result)
        {
            MatchState = MatchState.Ended;
            RaiseMatchEnded(result);
        }

        /// <summary>
        /// プレイヤーがスコアを獲得した時のハンドラ（派生クラスでオーバーライド可能）
        /// </summary>
        /// <param name="clientId">クライアントID</param>
        /// <param name="score">獲得スコア</param>
        /// <param name="location">ヒット部位</param>
        /// <param name="hitPosition">ヒット位置（ワールド座標）</param>
        protected virtual void OnPlayerScored(ulong clientId, int score, HitLocation location, Vector3 hitPosition)
        {
            var localClientId = Unity.Netcode.NetworkManager.Singleton?.LocalClientId ?? 0;

            if (clientId == localClientId)
            {
                LocalPlayerScore += score;
                LocalPlayerHits++;
            }

            // チームスコアを更新
            Team0Score = MatchService?.GetTeamScore(0) ?? 0;
            Team1Score = MatchService?.GetTeamScore(1) ?? 0;
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
