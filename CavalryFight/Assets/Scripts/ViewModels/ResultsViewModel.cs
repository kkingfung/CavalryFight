#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using CavalryFight.Core.Commands;
using CavalryFight.Core.MVVM;
using CavalryFight.Core.Services;
using CavalryFight.Services.Lobby;
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
        private readonly ISceneManagementService _sceneManagementService;
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
        /// スコア順でソートされた全プレイヤー統計リスト
        /// </summary>
        public IReadOnlyList<PlayerStatistics> SortedPlayerStats { get; private set; } = new List<PlayerStatistics>();

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
        public bool AllPlayersAgreed => ActivePlayerCount > 0 && RematchVoteCount >= ActivePlayerCount;

        /// <summary>
        /// リマッチに投票可能かどうか
        /// </summary>
        public bool CanVoteRematch => IsMultiplayerMatch && !HasVotedRematch && !AllPlayersAgreed;

        /// <summary>
        /// リマッチ投票をキャンセル可能かどうか
        /// </summary>
        public bool CanCancelVote => IsMultiplayerMatch && HasVotedRematch && !AllPlayersAgreed;

        /// <summary>
        /// ルームに戻るボタンが無効かどうか（投票済みの場合は無効）
        /// </summary>
        public bool IsReturnToRoomDisabled => HasVotedRematch;

        /// <summary>
        /// アクティブなプレイヤー数（退出していないプレイヤー）
        /// </summary>
        public int ActivePlayerCount => SortedPlayerStats.Count(p => !p.HasLeft && !p.IsNPC);

        /// <summary>
        /// リマッチ投票テキスト（アクティブプレイヤー数ベース）
        /// </summary>
        public string VoteCountText => $"{RematchVoteCount} / {ActivePlayerCount} voted";

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

        /// <summary>
        /// プレイヤーがルームを退出したイベント
        /// </summary>
        public event EventHandler<PlayerStatistics>? PlayerLeft;

        /// <summary>
        /// ホストがルームを退出したイベント
        /// </summary>
        public event EventHandler? HostLeft;

        /// <summary>
        /// リーダーボード更新イベント
        /// </summary>
        public event EventHandler? LeaderboardUpdated;

        #endregion

        #region Constructor

        /// <summary>
        /// ResultsViewModelの新しいインスタンスを初期化します
        /// </summary>
        /// <param name="matchResult">マッチ結果データ</param>
        /// <param name="sceneManagementService">シーン管理サービス</param>
        /// <param name="replayService">リプレイサービス（オプション）</param>
        public ResultsViewModel(MatchResult matchResult, ISceneManagementService sceneManagementService, IReplayService? replayService = null)
        {
            _matchResult = matchResult ?? throw new ArgumentNullException(nameof(matchResult));
            _sceneManagementService = sceneManagementService ?? throw new ArgumentNullException(nameof(sceneManagementService));
            _replayService = replayService;

            // コマンド初期化
            SaveReplayCommand = new RelayCommand(ExecuteSaveReplay, () => CanSaveReplay);
            MainMenuCommand = new RelayCommand(ExecuteMainMenu);
            LobbyCommand = new RelayCommand(ExecuteLobby);
            ReturnToRoomCommand = new RelayCommand(ExecuteReturnToRoom, () => CanReturnToRoom);
            PlayAgainCommand = new RelayCommand(ExecutePlayAgain, () => IsSinglePlayerMatch);
            VoteRematchCommand = new RelayCommand(ExecuteVoteRematch, () => CanVoteRematch);
            CancelVoteCommand = new RelayCommand(ExecuteCancelVote, () => CanCancelVote);

            // プレイヤー統計をソート
            SortPlayerStats();

            Debug.Log($"[ResultsViewModel] Initialized with result: {ResultText} ({ScoreText})");
        }

        /// <summary>
        /// プレイヤー統計をスコア順（降順）でソートし、ランクを設定します
        /// </summary>
        private void SortPlayerStats()
        {
            var allStats = MatchResult.AllPlayerStats;

            // スコア順にソート（同スコアの場合はK/D比、次にキル数でソート）
            var sorted = allStats
                .OrderByDescending(p => p.Score)
                .ThenByDescending(p => p.KDRatio)
                .ThenByDescending(p => p.Kills)
                .ToList();

            // ランクを設定
            for (int i = 0; i < sorted.Count; i++)
            {
                sorted[i].Rank = i + 1;
            }

            SortedPlayerStats = sorted;
            OnPropertyChanged(nameof(SortedPlayerStats));
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
            HandleLocalPlayerLeaving();
            _sceneManagementService.LoadMainMenu();
        }

        /// <summary>
        /// ロビーに戻ります
        /// </summary>
        private void ExecuteLobby()
        {
            Debug.Log("[ResultsViewModel] Returning to lobby...");
            HandleLocalPlayerLeaving();
            _sceneManagementService.LoadLobby();
        }

        /// <summary>
        /// ルームに戻ります
        /// </summary>
        private void ExecuteReturnToRoom()
        {
            if (!CanReturnToRoom || IsReturnToRoomDisabled)
            {
                Debug.LogWarning("[ResultsViewModel] Cannot return to room: room is closed or voted for rematch");
                return;
            }

            Debug.Log($"[ResultsViewModel] Returning to room: {MatchResult.OriginalRoomId}");
            HandleLocalPlayerLeaving();
            _sceneManagementService.LoadMatchRoom();
        }

        /// <summary>
        /// 再プレイします（シングルプレイヤー用）
        /// </summary>
        private void ExecutePlayAgain()
        {
            Debug.Log("[ResultsViewModel] Playing again...");
            _sceneManagementService.LoadMatch();
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

            // ローカルプレイヤーの統計も更新
            var localPlayer = SortedPlayerStats.FirstOrDefault(p => p.IsLocalPlayer);
            if (localPlayer != null)
            {
                localPlayer.HasVotedRematch = true;
            }

            Debug.Log($"[ResultsViewModel] Voted for rematch ({RematchVoteCount}/{ActivePlayerCount})");

            // プロパティ変更通知
            OnPropertyChanged(nameof(VoteCountText));
            OnPropertyChanged(nameof(IsReturnToRoomDisabled));
            RematchVoteChanged?.Invoke(this, EventArgs.Empty);
            LeaderboardUpdated?.Invoke(this, EventArgs.Empty);

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

            // ローカルプレイヤーの統計も更新
            var localPlayer = SortedPlayerStats.FirstOrDefault(p => p.IsLocalPlayer);
            if (localPlayer != null)
            {
                localPlayer.HasVotedRematch = false;
            }

            Debug.Log($"[ResultsViewModel] Cancelled rematch vote ({RematchVoteCount}/{ActivePlayerCount})");

            // プロパティ変更通知
            OnPropertyChanged(nameof(VoteCountText));
            OnPropertyChanged(nameof(IsReturnToRoomDisabled));
            RematchVoteChanged?.Invoke(this, EventArgs.Empty);
            LeaderboardUpdated?.Invoke(this, EventArgs.Empty);
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
                _sceneManagementService.LoadMatch();
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
            OnPropertyChanged(nameof(VoteCountText));
            RematchVoteChanged?.Invoke(this, EventArgs.Empty);

            // 全員が同意したかチェック
            CheckAllPlayersAgreed();
        }

        /// <summary>
        /// プレイヤーの投票状態を更新します（ネットワーク経由で呼ばれる）
        /// </summary>
        /// <param name="playerId">プレイヤーID</param>
        /// <param name="hasVoted">投票したかどうか</param>
        public void UpdatePlayerVoteStatus(string playerId, bool hasVoted)
        {
            var player = SortedPlayerStats.FirstOrDefault(p => p.PlayerId == playerId);
            if (player != null)
            {
                player.HasVotedRematch = hasVoted;

                // 投票数を再計算
                RematchVoteCount = SortedPlayerStats.Count(p => p.HasVotedRematch && !p.HasLeft && !p.IsNPC);

                OnPropertyChanged(nameof(VoteCountText));
                OnPropertyChanged(nameof(RematchStatusText));
                LeaderboardUpdated?.Invoke(this, EventArgs.Empty);
                RematchVoteChanged?.Invoke(this, EventArgs.Empty);

                Debug.Log($"[ResultsViewModel] Player {player.PlayerName} vote updated: {hasVoted} ({RematchVoteCount}/{ActivePlayerCount})");

                // 全員が同意したかチェック
                CheckAllPlayersAgreed();
            }
        }

        /// <summary>
        /// プレイヤーがルームを退出したことを処理します（ネットワーク経由で呼ばれる）
        /// </summary>
        /// <param name="playerId">プレイヤーID</param>
        public void HandlePlayerLeft(string playerId)
        {
            var player = SortedPlayerStats.FirstOrDefault(p => p.PlayerId == playerId);
            if (player != null)
            {
                player.HasLeft = true;
                player.HasVotedRematch = false; // 退出 = 投票取り消し

                // 投票数を再計算
                RematchVoteCount = SortedPlayerStats.Count(p => p.HasVotedRematch && !p.HasLeft && !p.IsNPC);

                Debug.Log($"[ResultsViewModel] Player {player.PlayerName} left the room ({RematchVoteCount}/{ActivePlayerCount})");

                // プロパティ変更通知
                OnPropertyChanged(nameof(ActivePlayerCount));
                OnPropertyChanged(nameof(VoteCountText));
                OnPropertyChanged(nameof(RematchStatusText));

                // イベント発火
                PlayerLeft?.Invoke(this, player);
                LeaderboardUpdated?.Invoke(this, EventArgs.Empty);
                RematchVoteChanged?.Invoke(this, EventArgs.Empty);

                // ホストが退出した場合
                if (player.IsHost)
                {
                    Debug.Log("[ResultsViewModel] Host has left the room!");
                    HostLeft?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    // 残りのプレイヤーで全員同意チェック
                    CheckAllPlayersAgreed();
                }
            }
        }

        /// <summary>
        /// ホストがルームを退出したことを処理します（ネットワーク経由で呼ばれる）
        /// </summary>
        /// <remarks>
        /// ゲストプレイヤーがホストの切断を検知した時に呼ばれます。
        /// ホストの退出通知を行い、ロビーへの遷移をトリガーします。
        /// </remarks>
        public void HandleHostLeft()
        {
            Debug.Log("[ResultsViewModel] Host disconnected notification received");
            HostLeft?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// ローカルプレイヤーがルームを退出する際の処理
        /// </summary>
        /// <remarks>
        /// MainMenu, Lobby, ReturnToRoomボタン押下時に呼ばれ、投票取り消しとして扱われます
        /// </remarks>
        public void HandleLocalPlayerLeaving()
        {
            // ローカルプレイヤーの投票をキャンセル
            if (HasVotedRematch)
            {
                HasVotedRematch = false;
                RematchVoteCount--;
                Debug.Log($"[ResultsViewModel] Local player leaving, vote cancelled ({RematchVoteCount}/{ActivePlayerCount})");
            }

            // ネットワークに退出を通知
            var lobbyService = ServiceLocator.Instance.Get<ILobbyService>();
            if (lobbyService != null && lobbyService.IsInRoom)
            {
                Debug.Log("[ResultsViewModel] Notifying lobby service of player leaving");
                lobbyService.LeaveRoom();
            }
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
