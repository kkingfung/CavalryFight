#nullable enable

using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace CavalryFight.Services.Match
{
    /// <summary>
    /// ネットワークマッチマネージャー
    /// </summary>
    /// <remarks>
    /// マッチ中のゲームプレイRPCを管理します。
    /// 矢の発射、命中判定、スコア管理を行います。
    /// </remarks>
    public class NetworkMatchManager : NetworkBehaviour
    {
        #region Singleton

        /// <summary>
        /// シングルトンインスタンス
        /// </summary>
        public static NetworkMatchManager? Instance { get; private set; }

        #endregion

        #region Network Variables

        /// <summary>
        /// スコアリング設定
        /// </summary>
        private NetworkVariable<ScoringConfig> _scoringConfig = new NetworkVariable<ScoringConfig>(
            ScoringConfig.CreateDefault(),
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        /// <summary>
        /// プレイヤースコアリスト
        /// </summary>
        private NetworkList<PlayerScore> _playerScores = null!;

        /// <summary>
        /// マッチ開始済みフラグ
        /// </summary>
        private NetworkVariable<bool> _matchStarted = new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        #endregion

        #region Fields

        /// <summary>
        /// レイキャスト用のレイヤーマスク
        /// </summary>
        [SerializeField] private LayerMask _hitDetectionLayerMask = ~0;

        /// <summary>
        /// 最大レイキャスト距離
        /// </summary>
        [SerializeField] private float _maxRaycastDistance = 200f;

        #endregion

        #region Events

        /// <summary>
        /// 矢が発射された時に発生します（全クライアント）
        /// </summary>
        /// <remarks>
        /// サーバーが矢の発射を検証した後、全クライアントに通知されます。
        /// クライアント側で矢のビジュアル（projectile）を生成するために使用します。
        /// </remarks>
        public event Action<ArrowShotData>? ArrowFired;

        /// <summary>
        /// 命中があった時に発生します
        /// </summary>
        public event Action<HitResult>? HitRegistered;

        /// <summary>
        /// プレイヤーのスコアが変更された時に発生します
        /// </summary>
        public event Action<ulong, int>? PlayerScoreChanged; // clientId, newScore

        /// <summary>
        /// マッチが終了した時に発生します
        /// </summary>
        public event Action<ulong>? MatchEnded; // winnerClientId

        #endregion

        #region Properties

        /// <summary>
        /// 現在のスコアリング設定を取得します
        /// </summary>
        public ScoringConfig CurrentScoringConfig => _scoringConfig.Value;

        /// <summary>
        /// マッチが開始されているかどうかを取得します
        /// </summary>
        public bool IsMatchStarted => _matchStarted.Value;

        #endregion

        #region Unity Lifecycle

        /// <summary>
        /// Awake時の初期化
        /// </summary>
        private void Awake()
        {
            // シングルトン設定
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            _playerScores = new NetworkList<PlayerScore>();
        }

        /// <summary>
        /// OnDestroy時のクリーンアップ
        /// </summary>
        public override void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            base.OnDestroy();
        }

        #endregion

        #region NetworkBehaviour Overrides

        /// <summary>
        /// NetworkBehaviourの初期化時に呼ばれます
        /// </summary>
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            // イベント購読
            _playerScores.OnListChanged += OnPlayerScoresListChanged;

            if (IsServer)
            {
                Debug.Log("[NetworkMatchManager] Server started.");
            }
            else
            {
                Debug.Log("[NetworkMatchManager] Client started.");
            }
        }

        /// <summary>
        /// NetworkBehaviourの破棄時に呼ばれます
        /// </summary>
        public override void OnNetworkDespawn()
        {
            _playerScores.OnListChanged -= OnPlayerScoresListChanged;

            base.OnNetworkDespawn();
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
            if (!IsServer)
            {
                Debug.LogError("[NetworkMatchManager] Only server can start match.");
                return;
            }

            // プレイヤースコアを初期化
            _playerScores.Clear();

            foreach (var slot in playerSlots)
            {
                if (!slot.IsEmpty() && !slot.IsAI)
                {
                    var playerScore = new PlayerScore(
                        slot.PlayerId,
                        slot.PlayerName.ToString(),
                        arrowsPerPlayer,
                        slot.TeamIndex
                    );
                    _playerScores.Add(playerScore);
                }
            }

            _matchStarted.Value = true;

            Debug.Log($"[NetworkMatchManager] Match started with {_playerScores.Count} players, {arrowsPerPlayer} arrows each.");
        }

        /// <summary>
        /// マッチを終了します（サーバーのみ）
        /// </summary>
        /// <param name="winnerClientId">勝者のクライアントID</param>
        public void EndMatch(ulong winnerClientId)
        {
            if (!IsServer)
            {
                Debug.LogError("[NetworkMatchManager] Only server can end match.");
                return;
            }

            _matchStarted.Value = false;

            // 勝者を通知
            NotifyMatchEndedClientRpc(winnerClientId);

            Debug.Log($"[NetworkMatchManager] Match ended. Winner: {winnerClientId}");
        }

        /// <summary>
        /// スコアリング設定を更新します（サーバーのみ）
        /// </summary>
        /// <param name="config">新しいスコアリング設定</param>
        public void UpdateScoringConfig(ScoringConfig config)
        {
            if (!IsServer)
            {
                Debug.LogError("[NetworkMatchManager] Only server can update scoring config.");
                return;
            }

            _scoringConfig.Value = config;

            Debug.Log($"[NetworkMatchManager] Scoring config updated: Heart={config.HeartScore}, Head={config.HeadScore}, Torso={config.TorsoScore}");
        }

        #endregion

        #region RPC Methods - Arrow Shooting

        /// <summary>
        /// 矢の発射をサーバーに通知します
        /// </summary>
        /// <param name="shotData">発射データ</param>
        /// <param name="rpcParams">RPCパラメータ</param>
        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        public void FireArrowServerRpc(ArrowShotData shotData, RpcParams rpcParams = default)
        {
            ulong senderClientId = rpcParams.Receive.SenderClientId;

            // 送信者の検証
            if (shotData.ShooterClientId != senderClientId)
            {
                Debug.LogWarning($"[NetworkMatchManager] Client {senderClientId} attempted to fire arrow as {shotData.ShooterClientId}");
                return;
            }

            // プレイヤーのスコア情報を取得
            int playerIndex = GetPlayerScoreIndex(senderClientId);
            if (playerIndex == -1)
            {
                Debug.LogWarning($"[NetworkMatchManager] Player {senderClientId} not found in scores.");
                return;
            }

            var playerScore = _playerScores[playerIndex];

            // 矢が残っているか確認
            if (playerScore.RemainingArrows <= 0)
            {
                Debug.LogWarning($"[NetworkMatchManager] Player {senderClientId} has no arrows remaining.");
                return;
            }

            // 矢を1本減らす
            playerScore.RemainingArrows--;
            playerScore.ShotCount++;
            _playerScores[playerIndex] = playerScore;

            // 全クライアントに矢の発射を通知
            NotifyArrowFiredClientRpc(shotData);

            // 命中判定を実行
            HitResult hitResult = PerformHitDetection(shotData);

            // 命中した場合、スコアを加算
            if (hitResult.IsValidHit)
            {
                AddScoreToPlayer(senderClientId, hitResult.ScoreAwarded);

                // 命中回数を増やす
                playerScore = _playerScores[playerIndex];
                playerScore.HitCount++;
                _playerScores[playerIndex] = playerScore;
            }

            // すべてのクライアントに命中結果を通知
            NotifyHitResultClientRpc(hitResult);

            Debug.Log($"[NetworkMatchManager] Arrow fired by {senderClientId}. Hit: {hitResult.IsValidHit}, Location: {hitResult.HitLocation}, Score: {hitResult.ScoreAwarded}");
        }

        #endregion

        #region Hit Detection

        /// <summary>
        /// 命中判定を実行します（サーバーのみ）
        /// </summary>
        /// <param name="shotData">発射データ</param>
        /// <returns>命中結果</returns>
        private HitResult PerformHitDetection(ArrowShotData shotData)
        {
            // レイキャストで命中判定
            if (Physics.Raycast(shotData.Origin, shotData.Direction, out RaycastHit hit, _maxRaycastDistance, _hitDetectionLayerMask))
            {
                // 命中した部位を判定
                HitLocation hitLocation = DetermineHitLocation(hit);

                // 被弾者のClientIdを取得
                ulong targetClientId = GetTargetClientId(hit.collider);

                if (targetClientId == 0)
                {
                    // 環境オブジェクトに命中（スコアなし）
                    return HitResult.CreateMiss(shotData.ShooterClientId);
                }

                // スコアを計算
                int scoreAwarded = _scoringConfig.Value.GetScore(hitLocation);

                return HitResult.CreateValidHit(
                    shotData.ShooterClientId,
                    targetClientId,
                    hitLocation,
                    scoreAwarded,
                    hit.point,
                    hit.normal
                );
            }

            // ミス
            return HitResult.CreateMiss(shotData.ShooterClientId);
        }

        /// <summary>
        /// 命中部位を判定します
        /// </summary>
        /// <param name="hit">レイキャスト結果</param>
        /// <returns>命中部位</returns>
        private HitLocation DetermineHitLocation(RaycastHit hit)
        {
            // ヒットボックスコンポーネントから部位を取得
            var hitbox = PlayerNetworkIdentity.GetHitboxFromCollider(hit.collider);

            if (hitbox != null)
            {
                return hitbox.HitLocation;
            }

            // ヒットボックスがない場合はデフォルトで胴体
            Debug.LogWarning($"[NetworkMatchManager] Hit collider {hit.collider.name} has no HitboxComponent. Defaulting to Torso.");
            return HitLocation.Torso;
        }

        /// <summary>
        /// 被弾者のクライアントIDを取得します
        /// </summary>
        /// <param name="hitCollider">命中したコライダー</param>
        /// <returns>クライアントID（0 = プレイヤーではない）</returns>
        private ulong GetTargetClientId(Collider hitCollider)
        {
            // PlayerNetworkIdentityから取得
            return PlayerNetworkIdentity.GetClientIdFromCollider(hitCollider);
        }

        #endregion

        #region Score Management

        /// <summary>
        /// プレイヤーにスコアを加算します（サーバーのみ）
        /// </summary>
        /// <param name="clientId">クライアントID</param>
        /// <param name="scoreToAdd">加算スコア</param>
        private void AddScoreToPlayer(ulong clientId, int scoreToAdd)
        {
            int playerIndex = GetPlayerScoreIndex(clientId);
            if (playerIndex == -1)
            {
                Debug.LogWarning($"[NetworkMatchManager] Player {clientId} not found for score addition.");
                return;
            }

            var playerScore = _playerScores[playerIndex];
            playerScore.Score += scoreToAdd;
            _playerScores[playerIndex] = playerScore;

            Debug.Log($"[NetworkMatchManager] Player {clientId} scored {scoreToAdd}. Total: {playerScore.Score}");
        }

        /// <summary>
        /// プレイヤースコアのインデックスを取得します
        /// </summary>
        /// <param name="clientId">クライアントID</param>
        /// <returns>インデックス（-1 = 見つからない）</returns>
        private int GetPlayerScoreIndex(ulong clientId)
        {
            for (int i = 0; i < _playerScores.Count; i++)
            {
                if (_playerScores[i].ClientId == clientId)
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// プレイヤースコアを取得します
        /// </summary>
        /// <param name="clientId">クライアントID</param>
        /// <returns>プレイヤースコア</returns>
        public PlayerScore? GetPlayerScore(ulong clientId)
        {
            int index = GetPlayerScoreIndex(clientId);
            if (index == -1)
            {
                return null;
            }
            return _playerScores[index];
        }

        /// <summary>
        /// すべてのプレイヤースコアを取得します
        /// </summary>
        /// <returns>プレイヤースコア配列</returns>
        public PlayerScore[] GetAllPlayerScores()
        {
            var scores = new PlayerScore[_playerScores.Count];
            for (int i = 0; i < _playerScores.Count; i++)
            {
                scores[i] = _playerScores[i];
            }
            return scores;
        }

        #endregion

        #region Client RPC Methods

        /// <summary>
        /// 矢の発射を全クライアントに通知します
        /// </summary>
        /// <param name="shotData">発射データ</param>
        [Rpc(SendTo.Everyone)]
        private void NotifyArrowFiredClientRpc(ArrowShotData shotData)
        {
            ArrowFired?.Invoke(shotData);
        }

        /// <summary>
        /// 命中結果を全クライアントに通知します
        /// </summary>
        /// <param name="hitResult">命中結果</param>
        [Rpc(SendTo.Everyone)]
        private void NotifyHitResultClientRpc(HitResult hitResult)
        {
            HitRegistered?.Invoke(hitResult);
        }

        /// <summary>
        /// マッチ終了を全クライアントに通知します
        /// </summary>
        /// <param name="winnerClientId">勝者のクライアントID</param>
        [Rpc(SendTo.Everyone)]
        private void NotifyMatchEndedClientRpc(ulong winnerClientId)
        {
            MatchEnded?.Invoke(winnerClientId);
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// プレイヤースコアリスト変更時のハンドラ
        /// </summary>
        private void OnPlayerScoresListChanged(NetworkListEvent<PlayerScore> changeEvent)
        {
            if (changeEvent.Type == NetworkListEvent<PlayerScore>.EventType.Value)
            {
                var playerScore = changeEvent.Value;
                PlayerScoreChanged?.Invoke(playerScore.ClientId, playerScore.Score);
            }
        }

        #endregion
    }
}
