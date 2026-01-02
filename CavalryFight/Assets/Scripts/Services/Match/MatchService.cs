#nullable enable

using System;
using System.Collections.Generic;
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
        public event Action<ulong, int, HitLocation>? PlayerScored;

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

        #endregion

        #region Fields

        /// <summary>
        /// NetworkMatchManagerへの参照
        /// </summary>
        private NetworkMatchManager? _networkMatchManager;

        /// <summary>
        /// マッチ開始監視フラグ
        /// </summary>
        private bool _wasMatchStarted = false;

        /// <summary>
        /// 前回のマッチ状態
        /// </summary>
        private MatchState _previousState = MatchState.WaitingForPlayers;

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
        public bool IsMatchStarted => _networkMatchManager?.IsMatchStarted ?? false;

        /// <summary>
        /// 現在のマッチ状態を取得します
        /// </summary>
        public MatchState CurrentState
        {
            get
            {
                if (_networkMatchManager == null)
                {
                    return MatchState.WaitingForPlayers;
                }

                // NetworkMatchManagerの状態を確認
                if (!_networkMatchManager.IsMatchStarted)
                {
                    return MatchState.WaitingForPlayers;
                }

                // マッチが終了している場合
                if (_networkMatchManager.IsMatchEnded)
                {
                    return MatchState.Ended;
                }

                return MatchState.InProgress;
            }
        }

        /// <summary>
        /// 現在のゲームモードを取得します
        /// </summary>
        public GameMode CurrentGameMode => _currentGameMode;

        /// <summary>
        /// 残り時間（秒）を取得します
        /// </summary>
        public float RemainingTime => _networkMatchManager?.RemainingTime ?? 0f;

        /// <summary>
        /// マッチ経過時間（秒）を取得します
        /// </summary>
        public float MatchTime
        {
            get
            {
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
            // NetworkMatchManagerのインスタンスを検索
            _networkMatchManager = NetworkMatchManager.Instance;

            if (_networkMatchManager == null)
            {
                Debug.LogWarning("[MatchService] NetworkMatchManager instance not found. Service will wait for it to spawn.");
            }
            else
            {
                SubscribeToNetworkEvents();
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

            // マッチ開始状態の変化を監視
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

                // 状態変化を監視
                var currentState = CurrentState;
                if (currentState != _previousState)
                {
                    _previousState = currentState;
                    MatchStateChanged?.Invoke(currentState);
                }
            }
        }

        /// <summary>
        /// サービスを破棄します
        /// </summary>
        public void Dispose()
        {
            UnsubscribeFromNetworkEvents();
            _networkMatchManager = null;
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
                PlayerScored?.Invoke(hitResult.ShooterClientId, hitResult.ScoreAwarded, hitResult.HitLocation);
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

            Debug.Log($"[MatchService] Match result created: {result.PlayerScore} vs {result.EnemyScore}, Duration: {result.DurationText}");

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

        /// <summary>
        /// プレイヤーのスコア情報を取得します
        /// </summary>
        /// <param name="clientId">クライアントID</param>
        /// <returns>プレイヤースコア（null = 見つからない）</returns>
        public PlayerScore? GetPlayerScore(ulong clientId)
        {
            if (_networkMatchManager == null)
            {
                Debug.LogWarning("[MatchService] Cannot get player score: NetworkMatchManager not available.");
                return null;
            }

            return _networkMatchManager.GetPlayerScore(clientId);
        }

        /// <summary>
        /// すべてのプレイヤーのスコア情報を取得します
        /// </summary>
        /// <returns>プレイヤースコア配列</returns>
        public PlayerScore[] GetAllPlayerScores()
        {
            if (_networkMatchManager == null)
            {
                Debug.LogWarning("[MatchService] Cannot get all player scores: NetworkMatchManager not available.");
                return Array.Empty<PlayerScore>();
            }

            return _networkMatchManager.GetAllPlayerScores();
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
            if (_networkMatchManager == null)
            {
                return 0;
            }

            return _networkMatchManager.GetTeamScore(teamIndex);
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
