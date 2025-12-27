#nullable enable

using System;
using System.Collections.Generic;
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
        /// マッチが開始された時に発生します
        /// </summary>
        public event Action? MatchStarted;

        /// <summary>
        /// マッチが終了した時に発生します
        /// </summary>
        public event Action<ulong>? MatchEnded;

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

        #endregion

        #region Properties

        /// <summary>
        /// マッチが開始されているかどうかを取得します
        /// </summary>
        public bool IsMatchStarted => _networkMatchManager?.IsMatchStarted ?? false;

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
                    MatchStarted?.Invoke();
                }

                _wasMatchStarted = isMatchStarted;
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
            MatchEnded?.Invoke(winnerClientId);
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
