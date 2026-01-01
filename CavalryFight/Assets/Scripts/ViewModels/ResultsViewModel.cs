#nullable enable

using System;
using CavalryFight.Core.Commands;
using CavalryFight.Core.MVVM;
using CavalryFight.Core.Services;
using CavalryFight.Services.Match;
using CavalryFight.Services.Replay;
using CavalryFight.Services.SceneManagement;
using UnityEngine;

namespace CavalryFight.ViewModels
{
    /// <summary>
    /// リザルト画面のViewModel
    /// </summary>
    /// <remarks>
    /// マッチ終了後に表示される結果画面を管理します。
    /// スコア、統計、リプレイ保存機能を提供します。
    /// </remarks>
    public class ResultsViewModel : ViewModelBase
    {
        #region Fields

        private readonly IReplayService? _replayService;
        private readonly ISceneManagementService? _sceneManagementService;
        private MatchResult _matchResult;
        private bool _isReplaySaved;
        private bool _isSavingReplay;
        private bool _hasVotedRematch;
        private int _rematchVoteCount;

        #endregion

        #region Properties

        /// <summary>
        /// マッチ結果データ
        /// </summary>
        public MatchResult MatchResult
        {
            get => _matchResult;
            set
            {
                if (SetProperty(ref _matchResult, value))
                {
                    // 関連プロパティの変更通知
                    OnPropertyChanged(nameof(ResultText));
                    OnPropertyChanged(nameof(ScoreText));
                    OnPropertyChanged(nameof(MapName));
                    OnPropertyChanged(nameof(GameMode));
                    OnPropertyChanged(nameof(DurationText));
                    OnPropertyChanged(nameof(IsVictory));
                    OnPropertyChanged(nameof(IsDraw));
                    OnPropertyChanged(nameof(IsDefeat));
                    OnPropertyChanged(nameof(LocalStats));
                    OnPropertyChanged(nameof(CanSaveReplay));
                }
            }
        }

        /// <summary>
        /// 結果テキスト（VICTORY/DEFEAT/DRAW）
        /// </summary>
        public string ResultText => MatchResult.ResultText;

        /// <summary>
        /// スコアテキスト
        /// </summary>
        public string ScoreText => MatchResult.ScoreText;

        /// <summary>
        /// プレイヤースコア
        /// </summary>
        public int PlayerScore => MatchResult.PlayerScore;

        /// <summary>
        /// 敵スコア
        /// </summary>
        public int EnemyScore => MatchResult.EnemyScore;

        /// <summary>
        /// チーム戦かどうか
        /// </summary>
        public bool IsTeamMatch => MatchResult.IsTeamMatch;

        /// <summary>
        /// プレイヤー/チームA名
        /// </summary>
        public string PlayerTeamName => MatchResult.IsTeamMatch ? MatchResult.TeamAName : "You";

        /// <summary>
        /// 敵/チームB名
        /// </summary>
        public string EnemyTeamName => MatchResult.IsTeamMatch ? MatchResult.TeamBName : "Enemy";

        /// <summary>
        /// マップ名
        /// </summary>
        public string MapName => MatchResult.MapName;

        /// <summary>
        /// ゲームモード
        /// </summary>
        public string GameMode => MatchResult.GameMode;

        /// <summary>
        /// 持続時間テキスト
        /// </summary>
        public string DurationText => MatchResult.DurationText;

        /// <summary>
        /// 最大プレイヤー数
        /// </summary>
        public int MaxPlayers => MatchResult.MaxPlayers;

        /// <summary>
        /// 制限時間テキスト
        /// </summary>
        public string TimeLimitText => MatchResult.TimeLimitText;

        /// <summary>
        /// 矢制限テキスト
        /// </summary>
        public string ArrowLimitText => MatchResult.ArrowLimitText;

        /// <summary>
        /// 勝利かどうか
        /// </summary>
        public bool IsVictory => MatchResult.IsVictory;

        /// <summary>
        /// 引き分けかどうか
        /// </summary>
        public bool IsDraw => MatchResult.IsDraw;

        /// <summary>
        /// 敗北かどうか
        /// </summary>
        public bool IsDefeat => MatchResult.IsDefeat;

        /// <summary>
        /// ローカルプレイヤーの統計
        /// </summary>
        public PlayerStatistics LocalStats => MatchResult.LocalPlayerStats;

        /// <summary>
        /// リプレイが保存されたかどうか
        /// </summary>
        public bool IsReplaySaved
        {
            get => _isReplaySaved;
            private set
            {
                if (SetProperty(ref _isReplaySaved, value))
                {
                    OnPropertyChanged(nameof(CanSaveReplay));
                    OnPropertyChanged(nameof(CanWatchReplay));
                }
            }
        }

        /// <summary>
        /// リプレイ保存中かどうか
        /// </summary>
        public bool IsSavingReplay
        {
            get => _isSavingReplay;
            private set
            {
                if (SetProperty(ref _isSavingReplay, value))
                {
                    OnPropertyChanged(nameof(CanSaveReplay));
                }
            }
        }

        /// <summary>
        /// リプレイ保存が可能かどうか
        /// </summary>
        public bool CanSaveReplay => MatchResult.HasReplayData && !IsReplaySaved && !IsSavingReplay;

        /// <summary>
        /// リプレイ視聴が可能かどうか
        /// </summary>
        public bool CanWatchReplay => IsReplaySaved && !string.IsNullOrEmpty(MatchResult.SavedReplayId);

        /// <summary>
        /// マルチプレイヤーマッチかどうか
        /// </summary>
        public bool IsMultiplayerMatch => MatchResult.IsMultiplayerMatch;

        /// <summary>
        /// ルームに戻れるかどうか
        /// </summary>
        public bool CanReturnToRoom => MatchResult.CanReturnToRoom;

        /// <summary>
        /// シングルプレイヤーまたはAI戦かどうか
        /// </summary>
        public bool IsSinglePlayerMatch => !IsMultiplayerMatch;

        /// <summary>
        /// リマッチに投票したかどうか
        /// </summary>
        public bool HasVotedRematch
        {
            get => _hasVotedRematch;
            private set
            {
                if (SetProperty(ref _hasVotedRematch, value))
                {
                    OnPropertyChanged(nameof(CanVoteRematch));
                    OnPropertyChanged(nameof(CanCancelVote));
                }
            }
        }

        /// <summary>
        /// リマッチに投票したプレイヤー数
        /// </summary>
        public int RematchVoteCount
        {
            get => _rematchVoteCount;
            private set
            {
                if (SetProperty(ref _rematchVoteCount, value))
                {
                    OnPropertyChanged(nameof(RematchCountText));
                    OnPropertyChanged(nameof(AllPlayersAgreed));
                }
            }
        }

        /// <summary>
        /// 総プレイヤー数
        /// </summary>
        public int TotalPlayers => MatchResult.CurrentPlayerCount;

        /// <summary>
        /// リマッチ投票数のテキスト
        /// </summary>
        public string RematchCountText => $"{RematchVoteCount} / {TotalPlayers} players agreed";

        /// <summary>
        /// リマッチ状態テキスト
        /// </summary>
        public string RematchStatusText => AllPlayersAgreed ? "All players ready!" : "Waiting for players...";

        /// <summary>
        /// 全プレイヤーがリマッチに同意したかどうか
        /// </summary>
        public bool AllPlayersAgreed => TotalPlayers > 0 && RematchVoteCount >= TotalPlayers;

        /// <summary>
        /// リマッチに投票可能かどうか
        /// </summary>
        public bool CanVoteRematch => IsMultiplayerMatch && !HasVotedRematch && !AllPlayersAgreed;

        /// <summary>
        /// リマッチ投票をキャンセル可能かどうか
        /// </summary>
        public bool CanCancelVote => IsMultiplayerMatch && HasVotedRematch && !AllPlayersAgreed;

        #endregion

        #region Commands

        /// <summary>
        /// リプレイを保存するコマンド
        /// </summary>
        public ICommand SaveReplayCommand { get; }

        /// <summary>
        /// メインメニューに戻るコマンド
        /// </summary>
        public ICommand MainMenuCommand { get; }

        /// <summary>
        /// ロビーに戻るコマンド
        /// </summary>
        public ICommand LobbyCommand { get; }

        /// <summary>
        /// ルームに戻るコマンド
        /// </summary>
        public ICommand ReturnToRoomCommand { get; }

        /// <summary>
        /// 再プレイするコマンド（シングルプレイヤー用）
        /// </summary>
        public ICommand PlayAgainCommand { get; }

        /// <summary>
        /// リマッチに投票するコマンド
        /// </summary>
        public ICommand VoteRematchCommand { get; }

        /// <summary>
        /// リマッチ投票をキャンセルするコマンド
        /// </summary>
        public ICommand CancelVoteCommand { get; }

        #endregion

        #region Events

        /// <summary>
        /// リプレイ保存完了イベント
        /// </summary>
        public event EventHandler<string>? ReplaySaved;

        /// <summary>
        /// リマッチ投票変更イベント
        /// </summary>
        public event EventHandler? RematchVoteChanged;

        /// <summary>
        /// 全プレイヤーがリマッチに同意したイベント
        /// </summary>
        public event EventHandler? AllPlayersAgreedToRematch;

        #endregion

        #region Constructor

        /// <summary>
        /// ResultsViewModelの新しいインスタンスを初期化します
        /// </summary>
        /// <param name="matchResult">マッチ結果データ</param>
        public ResultsViewModel(MatchResult matchResult)
        {
            _matchResult = matchResult ?? throw new ArgumentNullException(nameof(matchResult));
            _replayService = ServiceLocator.Instance.Get<IReplayService>();
            _sceneManagementService = ServiceLocator.Instance.Get<ISceneManagementService>();

            // コマンド初期化
            SaveReplayCommand = new RelayCommand(ExecuteSaveReplay, () => CanSaveReplay);
            MainMenuCommand = new RelayCommand(ExecuteMainMenu);
            LobbyCommand = new RelayCommand(ExecuteLobby);
            ReturnToRoomCommand = new RelayCommand(ExecuteReturnToRoom, () => CanReturnToRoom);
            PlayAgainCommand = new RelayCommand(ExecutePlayAgain, () => IsSinglePlayerMatch);
            VoteRematchCommand = new RelayCommand(ExecuteVoteRematch, () => CanVoteRematch);
            CancelVoteCommand = new RelayCommand(ExecuteCancelVote, () => CanCancelVote);

            Debug.Log($"[ResultsViewModel] Initialized with result: {ResultText} ({ScoreText})");
        }

        #endregion

        #region Command Methods

        /// <summary>
        /// リプレイを保存します
        /// </summary>
        private void ExecuteSaveReplay()
        {
            if (_replayService == null || !MatchResult.HasReplayData)
            {
                Debug.LogWarning("[ResultsViewModel] Cannot save replay: service unavailable or no replay data");
                return;
            }

            IsSavingReplay = true;
            Debug.Log("[ResultsViewModel] Saving replay...");

            try
            {
                // 現在のリプレイデータを取得して保存
                var currentReplay = _replayService.CurrentReplay;
                if (currentReplay != null)
                {
                    _replayService.SaveReplay(currentReplay);
                    MatchResult.SavedReplayId = currentReplay.ReplayId;
                    IsReplaySaved = true;
                    ReplaySaved?.Invoke(this, currentReplay.ReplayId);
                    Debug.Log($"[ResultsViewModel] Replay saved: {currentReplay.ReplayId}");
                }
                else
                {
                    Debug.LogWarning("[ResultsViewModel] No current replay data to save");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ResultsViewModel] Failed to save replay: {ex.Message}");
            }
            finally
            {
                IsSavingReplay = false;
            }
        }

        /// <summary>
        /// メインメニューに戻ります
        /// </summary>
        private void ExecuteMainMenu()
        {
            Debug.Log("[ResultsViewModel] Returning to main menu...");
            _sceneManagementService?.LoadMainMenu();
        }

        /// <summary>
        /// ロビーに戻ります
        /// </summary>
        private void ExecuteLobby()
        {
            Debug.Log("[ResultsViewModel] Returning to lobby...");
            _sceneManagementService?.LoadLobby();
        }

        /// <summary>
        /// ルームに戻ります
        /// </summary>
        private void ExecuteReturnToRoom()
        {
            if (!CanReturnToRoom)
            {
                Debug.LogWarning("[ResultsViewModel] Cannot return to room: room is closed");
                return;
            }

            Debug.Log($"[ResultsViewModel] Returning to room: {MatchResult.OriginalRoomId}");
            _sceneManagementService?.LoadMatchRoom();
        }

        /// <summary>
        /// 再プレイします（シングルプレイヤー用）
        /// </summary>
        private void ExecutePlayAgain()
        {
            Debug.Log("[ResultsViewModel] Playing again...");
            _sceneManagementService?.LoadMatch();
        }

        /// <summary>
        /// リマッチに投票します
        /// </summary>
        private void ExecuteVoteRematch()
        {
            if (!CanVoteRematch)
            {
                Debug.LogWarning("[ResultsViewModel] Cannot vote for rematch");
                return;
            }

            HasVotedRematch = true;
            RematchVoteCount++;

            Debug.Log($"[ResultsViewModel] Voted for rematch ({RematchVoteCount}/{TotalPlayers})");
            RematchVoteChanged?.Invoke(this, EventArgs.Empty);

            // 全員が同意したかチェック
            CheckAllPlayersAgreed();
        }

        /// <summary>
        /// リマッチ投票をキャンセルします
        /// </summary>
        private void ExecuteCancelVote()
        {
            if (!CanCancelVote)
            {
                Debug.LogWarning("[ResultsViewModel] Cannot cancel vote");
                return;
            }

            HasVotedRematch = false;
            RematchVoteCount--;

            Debug.Log($"[ResultsViewModel] Cancelled rematch vote ({RematchVoteCount}/{TotalPlayers})");
            RematchVoteChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 全員がリマッチに同意したかチェックし、同意していれば遷移します
        /// </summary>
        private void CheckAllPlayersAgreed()
        {
            if (AllPlayersAgreed)
            {
                Debug.Log("[ResultsViewModel] All players agreed to rematch!");
                AllPlayersAgreedToRematch?.Invoke(this, EventArgs.Empty);

                // 全員同意したらマッチに遷移
                _sceneManagementService?.LoadMatch();
            }
        }

        /// <summary>
        /// 他プレイヤーのリマッチ投票を更新します（ネットワーク経由で呼ばれる）
        /// </summary>
        /// <param name="voteCount">現在の投票数</param>
        public void UpdateRematchVotes(int voteCount)
        {
            RematchVoteCount = voteCount;
            OnPropertyChanged(nameof(RematchStatusText));
            RematchVoteChanged?.Invoke(this, EventArgs.Empty);

            // 全員が同意したかチェック
            CheckAllPlayersAgreed();
        }

        #endregion

        #region Dispose

        /// <summary>
        /// リソースを解放します
        /// </summary>
        protected override void OnDispose()
        {
            base.OnDispose();
            Debug.Log("[ResultsViewModel] Disposed");
        }

        #endregion
    }
}
