#nullable enable

using System;
using System.Collections.Generic;
using CavalryFight.Core.Services;
using CavalryFight.Services.Lobby;
using Unity.Netcode;
using UnityEngine;

namespace CavalryFight.Services.Match
{
    /// <summary>
    /// マッチサービス
    /// </summary>
    /// <remarks>
    /// マッチ中のゲームプレイを管理するサービス。
    /// NetworkMatchManagerのラッパーとして機能します。
    /// </remarks>
    public class MatchService : IMatchService
    {
        #region Events

        /// <summary>
        /// 矢が発射された時に発生します
        /// </summary>
        public event Action<ArrowShotData>? ArrowFired;

        /// <summary>
        /// 命中があった時に発生します
        /// </summary>
        public event Action<HitResult>? HitRegistered;

        /// <summary>
        /// プレイヤーのスコアが変更された時に発生します
        /// </summary>
        public event Action<ulong, int>? PlayerScoreChanged;

        /// <summary>
        /// プレイヤーがスコアを獲得した時に発生します（詳細情報付き）
        /// </summary>
        public event Action<ulong, int, HitLocation, Vector3>? PlayerScored;

        /// <summary>
        /// マッチが開始された時に発生します
        /// </summary>
        public event Action? MatchStarted;

        /// <summary>
        /// マッチが終了した時に発生します
        /// </summary>
        public event Action<ulong>? MatchEnded;

        /// <summary>
        /// マッチが終了した時に発生します（詳細情報付き）
        /// </summary>
        public event Action<MatchEndResult>? MatchEndedWithResult;

        /// <summary>
        /// マッチ状態が変更された時に発生します
        /// </summary>
        public event Action<MatchState>? MatchStateChanged;

        /// <summary>
        /// カウントダウンが更新された時に発生します
        /// </summary>
        public event Action<int>? CountdownUpdated;

        #endregion

        #region Fields

        /// <summary>
        /// NetworkMatchManagerへの参照
        /// </summary>
        private NetworkMatchManager? _networkMatchManager;

        /// <summary>
        /// マッチデータプロバイダー（MatchManager等が登録）
        /// </summary>
        private IMatchDataProvider? _matchDataProvider;

        /// <summary>
        /// マッチ開始監視フラグ
        /// </summary>
        private bool _wasMatchStarted = false;

        /// <summary>
        /// 前回のマッチ状態
        /// </summary>
        private MatchState _previousState = MatchState.WaitingForPlayers;

        /// <summary>
        /// NetworkMatchManager未検出の警告を出力済みかどうか
        /// </summary>
        private bool _hasLoggedManagerNotFound = false;

        /// <summary>
        /// マッチ開始時刻
        /// </summary>
        private float _matchStartTime;

        /// <summary>
        /// 現在のゲームモード
        /// </summary>
        private GameMode _currentGameMode = GameMode.Hunting;

        /// <summary>
        /// 最後のマッチ結果
        /// </summary>
        private MatchResult? _lastMatchResult;

        #endregion

        #region Properties

        /// <summary>
        /// マッチが開始されているかどうかを取得します
        /// </summary>
        public bool IsMatchStarted
        {
            get
            {
                // IMatchDataProviderを優先（MatchManagerがローカルモードで動作している場合）
                if (_matchDataProvider != null)
                {
                    return _matchDataProvider.IsMatchInProgress;
                }
                if (_networkMatchManager != null)
                {
                    return _networkMatchManager.IsMatchStarted;
                }
                return false;
            }
        }

        /// <summary>
        /// 現在のマッチ状態を取得します
        /// </summary>
        public MatchState CurrentState
        {
            get
            {
                // IMatchDataProviderを優先（MatchManagerがローカルモードで動作している場合）
                if (_matchDataProvider != null)
                {
                    return _matchDataProvider.CurrentMatchState;
                }

                if (_networkMatchManager != null)
                {
                    if (!_networkMatchManager.IsMatchStarted)
                    {
                        return MatchState.WaitingForPlayers;
                    }
                    if (_networkMatchManager.IsMatchEnded)
                    {
                        return MatchState.Ended;
                    }
                    return MatchState.InProgress;
                }

                return MatchState.WaitingForPlayers;
            }
        }

        /// <summary>
        /// 現在のゲームモードを取得します
        /// </summary>
        public GameMode CurrentGameMode
        {
            get
            {
                if (_matchDataProvider != null)
                {
                    return _matchDataProvider.CurrentGameMode;
                }
                return _currentGameMode;
            }
        }

        /// <summary>
        /// 残り時間（秒）を取得します
        /// </summary>
        public float RemainingTime
        {
            get
            {
                // IMatchDataProviderを優先（MatchManagerがローカルモードで動作している場合）
                if (_matchDataProvider != null)
                {
                    return _matchDataProvider.RemainingTime;
                }
                if (_networkMatchManager != null)
                {
                    return _networkMatchManager.RemainingTime;
                }
                return 0f;
            }
        }

        /// <summary>
        /// マッチ経過時間（秒）を取得します
        /// </summary>
        public float MatchTime
        {
            get
            {
                if (_matchDataProvider != null)
                {
                    return _matchDataProvider.MatchTime;
                }
                if (!IsMatchStarted)
                {
                    return 0f;
                }
                return Time.time - _matchStartTime;
            }
        }

        /// <summary>
        /// 現在のスコアリング設定を取得します
        /// </summary>
        public ScoringConfig CurrentScoringConfig => _networkMatchManager?.CurrentScoringConfig ?? ScoringConfig.CreateDefault();

        #endregion

        #region Initialization

        /// <summary>
        /// サービスを初期化します
        /// </summary>
        public void Initialize()
        {
            // フラグをリセット
            _hasLoggedManagerNotFound = false;

            // NetworkMatchManagerのインスタンスを検索
            _networkMatchManager = NetworkMatchManager.Instance;

            if (_networkMatchManager != null)
            {
                _hasLoggedManagerNotFound = false;
                SubscribeToNetworkEvents();
            }
            else
            {
                if (!_hasLoggedManagerNotFound)
                {
                    Debug.Log("[MatchService] NetworkMatchManager not found. Service will wait for it to spawn.");
                    _hasLoggedManagerNotFound = true;
                }
            }
        }

        /// <summary>
        /// サービスを更新します（MonoBehaviourのUpdateから呼び出す）
        /// </summary>
        public void Update()
        {
            // NetworkMatchManagerが見つかっていない場合は検索
            if (_networkMatchManager == null)
            {
                var newManager = NetworkMatchManager.Instance;
                if (newManager != null)
                {
                    _networkMatchManager = newManager;
                    SubscribeToNetworkEvents();
                }
            }
            else if (_networkMatchManager != NetworkMatchManager.Instance && NetworkMatchManager.Instance != null)
            {
                // マネージャーが置き換わった場合
                UnsubscribeFromNetworkEvents();
                _networkMatchManager = NetworkMatchManager.Instance;
                SubscribeToNetworkEvents();
            }

            // マッチ開始状態の変化を監視（NetworkMatchManagerが存在する場合）
            if (_networkMatchManager != null)
            {
                bool isMatchStarted = _networkMatchManager.IsMatchStarted;

                if (isMatchStarted && !_wasMatchStarted)
                {
                    // マッチが開始された
                    _matchStartTime = Time.time;
                    MatchStarted?.Invoke();
                }

                _wasMatchStarted = isMatchStarted;
            }

            // 状態変化を監視
            var currentState = CurrentState;
            if (currentState != _previousState)
            {
                _previousState = currentState;
                MatchStateChanged?.Invoke(currentState);
            }
        }

        /// <summary>
        /// サービスを破棄します
        /// </summary>
        public void Dispose()
        {
            UnsubscribeFromNetworkEvents();
            UnregisterMatchDataProvider();
            _networkMatchManager = null;
        }

        #endregion

        #region Match Data Provider

        /// <summary>
        /// マッチデータプロバイダーを登録します
        /// </summary>
        /// <remarks>
        /// MatchManagerなど、異なるアセンブリからマッチデータを提供するオブジェクトが
        /// このメソッドを呼び出して登録します。
        /// </remarks>
        /// <param name="provider">マッチデータプロバイダー</param>
        public void RegisterMatchDataProvider(IMatchDataProvider provider)
        {
            if (_matchDataProvider != null)
            {
                Debug.LogWarning("[MatchService] Match data provider already registered. Unregistering previous provider.");
                UnregisterMatchDataProvider();
            }

            _matchDataProvider = provider;
            SubscribeToProviderEvents();

            Debug.Log($"[MatchService] Match data provider registered. _matchDataProvider is now set: {_matchDataProvider != null}");
        }

        /// <summary>
        /// マッチデータプロバイダーの登録を解除します
        /// </summary>
        public void UnregisterMatchDataProvider()
        {
            if (_matchDataProvider == null)
            {
                return;
            }

            UnsubscribeFromProviderEvents();
            _matchDataProvider = null;

            Debug.Log("[MatchService] Match data provider unregistered.");
        }

        /// <summary>
        /// プロバイダーイベントを購読します
        /// </summary>
        private void SubscribeToProviderEvents()
        {
            if (_matchDataProvider == null)
            {
                return;
            }

            _matchDataProvider.MatchStateChanged += OnProviderMatchStateChanged;
            _matchDataProvider.MatchStarted += OnProviderMatchStarted;
            _matchDataProvider.MatchEnded += OnProviderMatchEnded;
            _matchDataProvider.PlayerScored += OnProviderPlayerScored;
            _matchDataProvider.CountdownUpdated += OnProviderCountdownUpdated;
        }

        /// <summary>
        /// プロバイダーイベントの購読を解除します
        /// </summary>
        private void UnsubscribeFromProviderEvents()
        {
            if (_matchDataProvider == null)
            {
                return;
            }

            _matchDataProvider.MatchStateChanged -= OnProviderMatchStateChanged;
            _matchDataProvider.MatchStarted -= OnProviderMatchStarted;
            _matchDataProvider.MatchEnded -= OnProviderMatchEnded;
            _matchDataProvider.PlayerScored -= OnProviderPlayerScored;
            _matchDataProvider.CountdownUpdated -= OnProviderCountdownUpdated;
        }

        /// <summary>
        /// プロバイダーからマッチ状態変更が通知された時のハンドラ
        /// </summary>
        private void OnProviderMatchStateChanged(MatchState state)
        {
            MatchStateChanged?.Invoke(state);
        }

        /// <summary>
        /// プロバイダーからマッチ開始が通知された時のハンドラ
        /// </summary>
        private void OnProviderMatchStarted()
        {
            _matchStartTime = Time.time;
            MatchStarted?.Invoke();
        }

        /// <summary>
        /// プロバイダーからマッチ終了が通知された時のハンドラ
        /// </summary>
        private void OnProviderMatchEnded(MatchEndResult result)
        {
            // マッチ結果を生成して保存
            _lastMatchResult = CreateMatchResult(result.WinnerId);

            MatchEnded?.Invoke(result.WinnerId);
            MatchEndedWithResult?.Invoke(result);
        }

        /// <summary>
        /// プロバイダーからプレイヤースコアが通知された時のハンドラ
        /// </summary>
        private void OnProviderPlayerScored(ulong clientId, int score, HitLocation hitLocation, Vector3 hitPosition)
        {
            PlayerScored?.Invoke(clientId, score, hitLocation, hitPosition);
        }

        /// <summary>
        /// プロバイダーからカウントダウン更新が通知された時のハンドラ
        /// </summary>
        private void OnProviderCountdownUpdated(int seconds)
        {
            CountdownUpdated?.Invoke(seconds);
        }

        #endregion

        #region Event Subscription

        /// <summary>
        /// ネットワークイベントを購読します
        /// </summary>
        private void SubscribeToNetworkEvents()
        {
            if (_networkMatchManager == null)
            {
                return;
            }

            _networkMatchManager.ArrowFired += OnArrowFired;
            _networkMatchManager.HitRegistered += OnHitRegistered;
            _networkMatchManager.PlayerScoreChanged += OnPlayerScoreChanged;
            _networkMatchManager.MatchEnded += OnMatchEnded;

            Debug.Log("[MatchService] Subscribed to NetworkMatchManager events.");
        }

        /// <summary>
        /// ネットワークイベントの購読を解除します
        /// </summary>
        private void UnsubscribeFromNetworkEvents()
        {
            if (_networkMatchManager == null)
            {
                return;
            }

            _networkMatchManager.ArrowFired -= OnArrowFired;
            _networkMatchManager.HitRegistered -= OnHitRegistered;
            _networkMatchManager.PlayerScoreChanged -= OnPlayerScoreChanged;
            _networkMatchManager.MatchEnded -= OnMatchEnded;

            Debug.Log("[MatchService] Unsubscribed from NetworkMatchManager events.");
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// 矢が発射された時のハンドラ
        /// </summary>
        private void OnArrowFired(ArrowShotData shotData)
        {
            ArrowFired?.Invoke(shotData);
        }

        /// <summary>
        /// 命中があった時のハンドラ
        /// </summary>
        private void OnHitRegistered(HitResult hitResult)
        {
            HitRegistered?.Invoke(hitResult);

            // 有効な命中の場合、詳細スコアイベントを発火
            if (hitResult.IsValidHit && hitResult.ScoreAwarded > 0)
            {
                PlayerScored?.Invoke(hitResult.ShooterClientId, hitResult.ScoreAwarded, hitResult.HitLocation, hitResult.HitPosition);
            }
        }

        /// <summary>
        /// プレイヤースコアが変更された時のハンドラ
        /// </summary>
        private void OnPlayerScoreChanged(ulong clientId, int newScore)
        {
            PlayerScoreChanged?.Invoke(clientId, newScore);
        }

        /// <summary>
        /// マッチが終了した時のハンドラ
        /// </summary>
        private void OnMatchEnded(ulong winnerClientId)
        {
            // マッチ結果を生成して保存
            _lastMatchResult = CreateMatchResult(winnerClientId);

            MatchEnded?.Invoke(winnerClientId);

            // 詳細結果も発火
            if (_lastMatchResult != null)
            {
                var endResult = new MatchEndResult
                {
                    WinnerId = winnerClientId,
                    MatchDuration = _lastMatchResult.MatchDuration,
                    GameMode = _currentGameMode,
                    IsTeamMode = _lastMatchResult.IsTeamMatch
                };
                MatchEndedWithResult?.Invoke(endResult);
            }
        }

        /// <summary>
        /// マッチ結果を生成します
        /// </summary>
        /// <param name="winnerClientId">勝者のクライアントID</param>
        /// <returns>マッチ結果</returns>
        private MatchResult CreateMatchResult(ulong winnerClientId)
        {
            var result = new MatchResult();

            // 基本情報
            result.GameMode = _currentGameMode.ToString();
            result.MatchDuration = MatchTime;
            result.FinishedAt = System.DateTime.Now;

            // ルーム設定を取得（LobbyServiceから）
            var lobbyService = ServiceLocator.Instance.Get<Lobby.ILobbyService>();
            if (lobbyService != null)
            {
                var roomSettings = lobbyService.CurrentRoomSettings;
                result.MapName = roomSettings.MapName.ToString();
                result.MaxPlayers = roomSettings.MaxPlayers;
                result.TimeLimit = roomSettings.TimeLimit;
                result.ArrowLimit = roomSettings.ArrowLimit;
                result.IsRoomStillOpen = lobbyService.IsInRoom;
                result.OriginalRoomId = lobbyService.CurrentJoinCode ?? "";

                Debug.Log($"[MatchService] Room settings applied to result: Map={result.MapName}, MaxPlayers={result.MaxPlayers}, TimeLimit={result.TimeLimit}, ArrowLimit={result.ArrowLimit}");
            }
            else
            {
                Debug.LogWarning("[MatchService] LobbyService not available, using default values for room settings");
                result.MapName = "Arena";
                result.MaxPlayers = 8;
                result.TimeLimit = 300;
                result.ArrowLimit = 0;
            }

            // チーム情報
            result.IsTeamMatch = _currentGameMode == GameMode.TeamFight || _currentGameMode == GameMode.Hunting;
            if (result.IsTeamMatch)
            {
                result.TeamAName = "Team A";
                result.TeamBName = "Team B";
            }

            // ローカルプレイヤーのスコア情報を取得
            ulong localClientId = 0;
            if (Unity.Netcode.NetworkManager.Singleton != null)
            {
                localClientId = Unity.Netcode.NetworkManager.Singleton.LocalClientId;
            }

            var localScore = GetPlayerScore(localClientId);
            var allScores = GetAllPlayerScores();

            if (localScore.HasValue)
            {
                result.PlayerScore = localScore.Value.Score;

                // ローカルプレイヤーの統計を設定
                result.LocalPlayerStats = new PlayerStatistics
                {
                    PlayerId = localClientId.ToString(),
                    PlayerName = localScore.Value.PlayerName.ToString(),
                    Score = localScore.Value.Score,
                    Hits = localScore.Value.HitCount,
                    ArrowsFired = localScore.Value.ShotCount,
                    IsLocalPlayer = true
                };
            }

            // 敵スコアを計算（全プレイヤーのスコア合計からローカルを除く）
            int enemyTotalScore = 0;
            foreach (var score in allScores)
            {
                if (score.ClientId != localClientId)
                {
                    enemyTotalScore += score.Score;
                }

                // 全プレイヤーの統計を追加
                result.AllPlayerStats.Add(new PlayerStatistics
                {
                    PlayerId = score.ClientId.ToString(),
                    PlayerName = score.PlayerName.ToString(),
                    Score = score.Score,
                    Hits = score.HitCount,
                    ArrowsFired = score.ShotCount,
                    IsLocalPlayer = score.ClientId == localClientId
                });
            }
            result.EnemyScore = enemyTotalScore;

            // マルチプレイヤー情報
            result.IsMultiplayerMatch = Unity.Netcode.NetworkManager.Singleton != null
                && Unity.Netcode.NetworkManager.Singleton.IsConnectedClient;
            result.CurrentPlayerCount = allScores.Length;

            // リプレイデータの有無を確認（ReplayRecorderから取得）
            var replayRecorder = ServiceLocator.Instance.Get<Replay.IReplayRecorder>();
            if (replayRecorder != null)
            {
                result.HasReplayData = replayRecorder.CurrentRecording != null;
                Debug.Log($"[MatchService] Replay data available: {result.HasReplayData}, CurrentRecording ReplayId: {replayRecorder.CurrentRecording?.ReplayId ?? "null"}");
            }
            else
            {
                result.HasReplayData = false;
                Debug.LogWarning("[MatchService] ReplayRecorder not available, replay data unavailable");
            }

            Debug.Log($"[MatchService] Match result created: {result.ResultText} ({result.ScoreText}), Map={result.MapName}, Duration={result.DurationText}");

            return result;
        }

        #endregion

        #region Client Methods

        /// <summary>
        /// 矢を発射します（クライアント）
        /// </summary>
        /// <param name="origin">発射位置</param>
        /// <param name="direction">発射方向</param>
        /// <param name="initialVelocity">初速</param>
        public void FireArrow(Vector3 origin, Vector3 direction, float initialVelocity)
        {
            if (_networkMatchManager == null)
            {
                Debug.LogError("[MatchService] Cannot fire arrow: NetworkMatchManager not available.");
                return;
            }

            if (!NetworkManager.Singleton.IsClient)
            {
                Debug.LogError("[MatchService] Cannot fire arrow: Not connected to network.");
                return;
            }

            if (!IsMatchStarted)
            {
                Debug.LogWarning("[MatchService] Cannot fire arrow: Match not started.");
                return;
            }

            // 現在のネットワークタイムを取得
            float fireTime = (float)NetworkManager.Singleton.ServerTime.Time;

            // ArrowShotDataを作成
            var shotData = new ArrowShotData(
                origin,
                direction,
                initialVelocity,
                fireTime,
                NetworkManager.Singleton.LocalClientId
            );

            // サーバーに送信
            _networkMatchManager.FireArrowServerRpc(shotData);

            Debug.Log($"[MatchService] Arrow fired: origin={origin}, direction={direction}, velocity={initialVelocity}");
        }

        // GetPlayerScoreのデバッグカウンター
        private int _getPlayerScoreDebugCounter = 0;

        /// <summary>
        /// プレイヤーのスコア情報を取得します
        /// </summary>
        /// <param name="clientId">クライアントID</param>
        /// <returns>プレイヤースコア（null = 見つからない、またはマッチが開始されていない場合）</returns>
        public PlayerScore? GetPlayerScore(ulong clientId)
        {
            _getPlayerScoreDebugCounter++;
            bool shouldLog = _getPlayerScoreDebugCounter <= 10;

            // IMatchDataProviderを優先（MatchManagerがローカルモードで動作している場合）
            if (_matchDataProvider != null)
            {
                return _matchDataProvider.GetPlayerScore(clientId);
            }

            if (_networkMatchManager != null)
            {
                return _networkMatchManager.GetPlayerScore(clientId);
            }

            // マッチ外では正常な状態
            return null;
        }

        /// <summary>
        /// すべてのプレイヤーのスコア情報を取得します
        /// </summary>
        /// <returns>プレイヤースコア配列（マッチが開始されていない場合は空配列）</returns>
        public PlayerScore[] GetAllPlayerScores()
        {
            // IMatchDataProviderを優先（MatchManagerがローカルモードで動作している場合）
            if (_matchDataProvider != null)
            {
                return _matchDataProvider.GetAllPlayerScores();
            }

            if (_networkMatchManager != null)
            {
                return _networkMatchManager.GetAllPlayerScores();
            }

            // マッチ外では正常な状態
            return Array.Empty<PlayerScore>();
        }

        /// <summary>
        /// 最後に完了したマッチの結果を取得します
        /// </summary>
        /// <returns>マッチ結果（存在しない場合はnull）</returns>
        public MatchResult? GetLastMatchResult()
        {
            return _lastMatchResult;
        }

        /// <summary>
        /// チームスコアを取得します
        /// </summary>
        /// <param name="teamIndex">チームインデックス</param>
        /// <returns>チームのスコア</returns>
        public int GetTeamScore(int teamIndex)
        {
            // IMatchDataProviderを優先（MatchManagerがローカルモードで動作している場合）
            if (_matchDataProvider != null)
            {
                return _matchDataProvider.GetTeamScore(teamIndex);
            }

            if (_networkMatchManager != null)
            {
                return _networkMatchManager.GetTeamScore(teamIndex);
            }

            return 0;
        }

        /// <summary>
        /// プレイヤーがハンターかどうかを取得します（ハンティングモード用）
        /// </summary>
        /// <param name="clientId">クライアントID</param>
        /// <returns>ハンターの場合true</returns>
        public bool IsHunter(ulong clientId)
        {
            if (_networkMatchManager == null)
            {
                return false;
            }

            return _networkMatchManager.IsHunter(clientId);
        }

        /// <summary>
        /// プレイヤーがスタン中かどうかを取得します
        /// </summary>
        /// <param name="clientId">クライアントID</param>
        /// <returns>スタン中の場合true</returns>
        public bool IsStunned(ulong clientId)
        {
            if (_networkMatchManager == null)
            {
                return false;
            }

            return _networkMatchManager.IsStunned(clientId);
        }

        #endregion

        #region Server Methods

        /// <summary>
        /// マッチを開始します（サーバーのみ）
        /// </summary>
        /// <param name="playerSlots">参加プレイヤーのスロット情報</param>
        /// <param name="arrowsPerPlayer">プレイヤーごとの矢の数</param>
        public void StartMatch(IReadOnlyList<CavalryFight.Services.Lobby.PlayerSlot> playerSlots, int arrowsPerPlayer)
        {
            if (_networkMatchManager == null)
            {
                Debug.LogError("[MatchService] Cannot start match: NetworkMatchManager not available.");
                return;
            }

            if (NetworkManager.Singleton == null
                || !NetworkManager.Singleton.IsServer)
            {
                Debug.LogError("[MatchService] Cannot start match: Only server can start match.");
                return;
            }

            _networkMatchManager.StartMatch(playerSlots, arrowsPerPlayer);

            Debug.Log($"[MatchService] Match started with {playerSlots.Count} players, {arrowsPerPlayer} arrows each.");
        }

        /// <summary>
        /// マッチを終了します（サーバーのみ）
        /// </summary>
        /// <param name="winnerClientId">勝者のクライアントID</param>
        public void EndMatch(ulong winnerClientId)
        {
            if (_networkMatchManager == null)
            {
                Debug.LogError("[MatchService] Cannot end match: NetworkMatchManager not available.");
                return;
            }

            if (NetworkManager.Singleton == null
                || !NetworkManager.Singleton.IsServer)
            {
                Debug.LogError("[MatchService] Cannot end match: Only server can end match.");
                return;
            }

            _networkMatchManager.EndMatch(winnerClientId);

            Debug.Log($"[MatchService] Match ended. Winner: {winnerClientId}");
        }

        /// <summary>
        /// スコアリング設定を更新します（サーバーのみ）
        /// </summary>
        /// <param name="config">新しいスコアリング設定</param>
        public void UpdateScoringConfig(ScoringConfig config)
        {
            if (_networkMatchManager == null)
            {
                Debug.LogError("[MatchService] Cannot update scoring config: NetworkMatchManager not available.");
                return;
            }

            if (NetworkManager.Singleton == null
                || !NetworkManager.Singleton.IsServer)
            {
                Debug.LogError("[MatchService] Cannot update scoring config: Only server can update config.");
                return;
            }

            _networkMatchManager.UpdateScoringConfig(config);

            Debug.Log($"[MatchService] Scoring config updated.");
        }

        #endregion
    }
}
