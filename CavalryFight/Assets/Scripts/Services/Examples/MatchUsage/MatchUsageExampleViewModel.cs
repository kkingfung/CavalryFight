#nullable enable

using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace CavalryFight.Services.Match.Examples
{
    /// <summary>
    /// マッチサービス使用例ViewModel
    /// </summary>
    /// <remarks>
    /// MatchServiceの使い方を示すサンプルコード。
    /// 実際のゲームプレイでMatchServiceをどのように使用するかを示します。
    /// </remarks>
    public class MatchUsageExampleViewModel : MonoBehaviour
    {
        #region Fields

        /// <summary>
        /// マッチサービス
        /// </summary>
        private IMatchService? _matchService;

        /// <summary>
        /// 現在のプレイヤースコア表示用
        /// </summary>
        private PlayerScore? _currentPlayerScore;

        #endregion

        #region Unity Lifecycle

        /// <summary>
        /// 初期化
        /// </summary>
        private void Start()
        {
            // マッチサービスを作成して初期化
            _matchService = new MatchService();
            _matchService.Initialize();

            // イベントを購読
            SubscribeToMatchEvents();

            Debug.Log("[MatchUsageExample] MatchService initialized.");
        }

        /// <summary>
        /// 更新
        /// </summary>
        private void Update()
        {
            // サービスを更新
            _matchService?.Update();

            // デバッグ用：スペースキーで矢を発射
            if (UnityEngine.Input.GetKeyDown(KeyCode.Space) && _matchService != null && _matchService.IsMatchStarted)
            {
                ExampleFireArrow();
            }

            // デバッグ用：Sキーでスコアボードを表示
            if (UnityEngine.Input.GetKeyDown(KeyCode.S) && _matchService != null && _matchService.IsMatchStarted)
            {
                ExampleShowScoreboard();
            }
        }

        /// <summary>
        /// クリーンアップ
        /// </summary>
        private void OnDestroy()
        {
            UnsubscribeFromMatchEvents();
            _matchService?.Dispose();
        }

        #endregion

        #region Event Subscription

        /// <summary>
        /// マッチイベントを購読します
        /// </summary>
        private void SubscribeToMatchEvents()
        {
            if (_matchService == null)
            {
                return;
            }

            _matchService.MatchStarted += OnMatchStarted;
            _matchService.MatchEnded += OnMatchEnded;
            _matchService.ArrowFired += OnArrowFired;
            _matchService.HitRegistered += OnHitRegistered;
            _matchService.PlayerScoreChanged += OnPlayerScoreChanged;
        }

        /// <summary>
        /// マッチイベントの購読を解除します
        /// </summary>
        private void UnsubscribeFromMatchEvents()
        {
            if (_matchService == null)
            {
                return;
            }

            _matchService.MatchStarted -= OnMatchStarted;
            _matchService.MatchEnded -= OnMatchEnded;
            _matchService.ArrowFired -= OnArrowFired;
            _matchService.HitRegistered -= OnHitRegistered;
            _matchService.PlayerScoreChanged -= OnPlayerScoreChanged;
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// マッチ開始時のハンドラ
        /// </summary>
        private void OnMatchStarted()
        {
            Debug.Log("[MatchUsageExample] Match started!");

            // UIを表示
            // TODO: マッチUIを表示する処理を実装

            // 現在のプレイヤーのスコアを取得
            if (NetworkManager.Singleton != null && _matchService != null)
            {
                _currentPlayerScore = _matchService.GetPlayerScore(NetworkManager.Singleton.LocalClientId);
                Debug.Log($"[MatchUsageExample] Your initial arrows: {_currentPlayerScore?.RemainingArrows ?? 0}");
            }
        }

        /// <summary>
        /// マッチ終了時のハンドラ
        /// </summary>
        private void OnMatchEnded(ulong winnerClientId)
        {
            Debug.Log($"[MatchUsageExample] Match ended! Winner: {winnerClientId}");

            bool isLocalPlayerWinner = NetworkManager.Singleton != null &&
                                      NetworkManager.Singleton.LocalClientId == winnerClientId;

            if (isLocalPlayerWinner)
            {
                Debug.Log("[MatchUsageExample] YOU WON!");
                // TODO: 勝利UIを表示
            }
            else
            {
                Debug.Log("[MatchUsageExample] You lost.");
                // TODO: 敗北UIを表示
            }

            // スコアボードを表示
            ExampleShowScoreboard();
        }

        /// <summary>
        /// 命中登録時のハンドラ
        /// </summary>
        private void OnHitRegistered(HitResult hitResult)
        {
            if (hitResult.IsValidHit)
            {
                Debug.Log($"[MatchUsageExample] Hit! Shooter: {hitResult.ShooterClientId}, " +
                         $"Target: {hitResult.TargetClientId}, " +
                         $"Location: {hitResult.HitLocation}, " +
                         $"Score: {hitResult.ScoreAwarded}");

                // 命中エフェクトを表示
                // TODO: hitResult.HitPosition と hitResult.HitNormal を使ってエフェクトを表示

                // ローカルプレイヤーが射手の場合
                if (NetworkManager.Singleton != null &&
                    hitResult.ShooterClientId == NetworkManager.Singleton.LocalClientId)
                {
                    Debug.Log($"[MatchUsageExample] You hit {hitResult.HitLocation}! +{hitResult.ScoreAwarded} points!");
                    // TODO: ヒットマーカーUIを表示
                }

                // ローカルプレイヤーが被弾者の場合
                if (NetworkManager.Singleton != null &&
                    hitResult.TargetClientId == NetworkManager.Singleton.LocalClientId)
                {
                    Debug.Log($"[MatchUsageExample] You were hit in the {hitResult.HitLocation}!");
                    // TODO: 被弾エフェクトを表示
                }
            }
            else
            {
                Debug.Log($"[MatchUsageExample] Miss by shooter: {hitResult.ShooterClientId}");
            }
        }

        /// <summary>
        /// プレイヤースコア変更時のハンドラ
        /// </summary>
        private void OnPlayerScoreChanged(ulong clientId, int newScore)
        {
            Debug.Log($"[MatchUsageExample] Player {clientId} score changed to {newScore}");

            // ローカルプレイヤーのスコアを更新
            if (NetworkManager.Singleton != null &&
                clientId == NetworkManager.Singleton.LocalClientId &&
                _matchService != null)
            {
                _currentPlayerScore = _matchService.GetPlayerScore(clientId);
                Debug.Log($"[MatchUsageExample] Your score: {_currentPlayerScore?.Score ?? 0}, " +
                         $"Remaining arrows: {_currentPlayerScore?.RemainingArrows ?? 0}, " +
                         $"Accuracy: {(_currentPlayerScore?.GetAccuracy() ?? 0f) * 100f:F1}%");
            }

            // TODO: スコアボードUIを更新
        }

        /// <summary>
        /// 矢が発射された時のハンドラ
        /// </summary>
        private void OnArrowFired(ArrowShotData shotData)
        {
            Debug.Log($"[MatchUsageExample] Arrow fired by {shotData.ShooterClientId}");

            // 矢のビジュアルを生成
            // TODO: shotData.Origin, shotData.Direction, shotData.InitialVelocity を使って矢のプロジェクタイルを生成
            // 例: GameObject arrow = Instantiate(arrowPrefab, shotData.Origin, Quaternion.LookRotation(shotData.Direction));
            //     arrow.GetComponent<Rigidbody>().velocity = shotData.Direction * shotData.InitialVelocity;
        }

        #endregion

        #region Example Methods - Client

        /// <summary>
        /// 矢を発射する例
        /// </summary>
        private void ExampleFireArrow()
        {
            if (_matchService == null || !_matchService.IsMatchStarted)
            {
                Debug.LogWarning("[MatchUsageExample] Cannot fire arrow: match not started.");
                return;
            }

            // カメラまたはプレイヤーの向きから発射方向を取得
            // この例ではカメラの前方を使用
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogWarning("[MatchUsageExample] No main camera found.");
                return;
            }

            Vector3 origin = mainCamera.transform.position;
            Vector3 direction = mainCamera.transform.forward;
            float initialVelocity = 50f; // 初速 50m/s

            // 矢を発射
            _matchService.FireArrow(origin, direction, initialVelocity);

            Debug.Log("[MatchUsageExample] Fired arrow!");
        }

        /// <summary>
        /// スコアボードを表示する例
        /// </summary>
        private void ExampleShowScoreboard()
        {
            if (_matchService == null)
            {
                Debug.LogWarning("[MatchUsageExample] MatchService not available.");
                return;
            }

            var allScores = _matchService.GetAllPlayerScores();

            Debug.Log("=== SCOREBOARD ===");
            foreach (var score in allScores)
            {
                Debug.Log($"Player: {score.PlayerName.ToString()}, " +
                         $"Score: {score.Score}, " +
                         $"Arrows: {score.RemainingArrows}, " +
                         $"Hits: {score.HitCount}/{score.ShotCount}, " +
                         $"Accuracy: {score.GetAccuracy() * 100f:F1}%");
            }
            Debug.Log("==================");

            // TODO: UI上にスコアボードを表示
        }

        #endregion

        #region Example Methods - Server Only

        /// <summary>
        /// マッチを開始する例（サーバーのみ）
        /// </summary>
        /// <remarks>
        /// 実際にはロビーシステムから呼び出されることを想定
        /// </remarks>
        public void ExampleStartMatch()
        {
            if (_matchService == null)
            {
                Debug.LogError("[MatchUsageExample] MatchService not available.");
                return;
            }

            if (!NetworkManager.Singleton.IsServer)
            {
                Debug.LogError("[MatchUsageExample] Only server can start match.");
                return;
            }

            // ロビーからプレイヤースロット情報を取得
            // この例では仮のデータを使用
            var playerSlots = new List<CavalryFight.Services.Lobby.PlayerSlot>();

            // TODO: 実際のロビーデータから取得
            // var lobbyService = ...; // LobbyServiceの参照を取得
            // playerSlots = lobbyService.PlayerSlots;

            // ゲームモードに応じた矢の数を決定
            int arrowsPerPlayer = 10; // Arena モードの例

            // マッチ開始
            _matchService.StartMatch(playerSlots, arrowsPerPlayer);

            Debug.Log("[MatchUsageExample] Match started by server!");
        }

        /// <summary>
        /// マッチを終了する例（サーバーのみ）
        /// </summary>
        public void ExampleEndMatch(ulong winnerClientId)
        {
            if (_matchService == null)
            {
                Debug.LogError("[MatchUsageExample] MatchService not available.");
                return;
            }

            if (!NetworkManager.Singleton.IsServer)
            {
                Debug.LogError("[MatchUsageExample] Only server can end match.");
                return;
            }

            _matchService.EndMatch(winnerClientId);

            Debug.Log($"[MatchUsageExample] Match ended by server! Winner: {winnerClientId}");
        }

        /// <summary>
        /// カスタムスコアリング設定を適用する例（サーバーのみ）
        /// </summary>
        public void ExampleSetCustomScoring()
        {
            if (_matchService == null)
            {
                Debug.LogError("[MatchUsageExample] MatchService not available.");
                return;
            }

            if (!NetworkManager.Singleton.IsServer)
            {
                Debug.LogError("[MatchUsageExample] Only server can update scoring config.");
                return;
            }

            // カスタムスコアリング設定を作成
            var customScoring = new ScoringConfig
            {
                HeartScore = 200,    // 心臓をより高得点に
                HeadScore = 100,     // 頭部も高得点に
                TorsoScore = 50,     // 胴体は中得点
                ArmScore = 20,       // 腕は低得点
                LegScore = 20,       // 脚は低得点
                MountScore = 10      // 騎乗動物は最低得点
            };

            _matchService.UpdateScoringConfig(customScoring);

            Debug.Log("[MatchUsageExample] Custom scoring config applied!");
        }

        #endregion
    }
}
