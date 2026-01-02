#nullable enable

#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using CavalryFight.Services.Lobby;
using CavalryFight.Services.Match;
using UnityEngine;

namespace CavalryFight.Development.MockData.MockServices
{
    /// <summary>
    /// モックのマッチサービス
    /// </summary>
    /// <remarks>
    /// 開発時に個別シーンをテストする際に使用します。
    /// モック設定に基づいてマッチ状態を提供します。
    /// </remarks>
    public class MockMatchService : IMatchService
    {
        #region Fields

        /// <summary>モック設定への参照</summary>
        private readonly MockSceneConfig? _config;

        /// <summary>キャッシュされたマッチ結果</summary>
        private MatchResult? _lastMatchResult;

        #endregion

        #region Constructor

        /// <summary>
        /// MockMatchServiceの新しいインスタンスを初期化します
        /// </summary>
        /// <param name="config">モック設定（nullの場合はデフォルト値を使用）</param>
        public MockMatchService(MockSceneConfig? config)
        {
            _config = config;
        }

        #endregion

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

        /// <summary>
        /// カウントダウンが更新された時に発生します
        /// </summary>
        public event Action<int>? CountdownUpdated;

        #endregion

        #region Properties

        /// <summary>
        /// マッチが開始されているかどうかを取得します
        /// </summary>
        public bool IsMatchStarted => true;

        /// <summary>
        /// 現在のマッチ状態を取得します
        /// </summary>
        public MatchState CurrentState => MatchState.InProgress;

        /// <summary>
        /// 現在のゲームモードを取得します
        /// </summary>
        public GameMode CurrentGameMode => GameMode.Arena;

        /// <summary>
        /// 残り時間（秒）を取得します
        /// </summary>
        public float RemainingTime
        {
            get
            {
                if (_config?.MatchSceneMockData != null)
                {
                    return _config.MatchSceneMockData.RemainingTime;
                }
                return 300f;
            }
        }

        /// <summary>
        /// マッチ経過時間（秒）を取得します
        /// </summary>
        public float MatchTime => 0f;

        /// <summary>
        /// 現在のスコアリング設定を取得します
        /// </summary>
        public ScoringConfig CurrentScoringConfig => new ScoringConfig
        {
            HeartScore = 200,
            HeadScore = 100,
            TorsoScore = 50,
            ArmScore = 25,
            LegScore = 25,
            MountScore = 10
        };

        #endregion

        #region Methods

        /// <summary>
        /// サービスを更新します（MonoBehaviourのUpdateから呼び出す）
        /// </summary>
        public void Update()
        {
            // モックでは何もしない
        }

        /// <summary>
        /// チームスコアを取得します
        /// </summary>
        /// <param name="teamIndex">チームインデックス</param>
        /// <returns>チームのスコア</returns>
        public int GetTeamScore(int teamIndex)
        {
            if (_config?.MatchSceneMockData != null)
            {
                return teamIndex == 0 ? _config.MatchSceneMockData.Team0Score : _config.MatchSceneMockData.Team1Score;
            }
            return 0;
        }

        /// <summary>
        /// ローカルプレイヤーがハンターかどうかを取得します（ハンティングモード用）
        /// </summary>
        /// <param name="clientId">クライアントID</param>
        /// <returns>ハンターの場合true</returns>
        public bool IsHunter(ulong clientId)
        {
            if (_config?.HuntingSceneMockData != null)
            {
                return _config.HuntingSceneMockData.IsLocalPlayerHunter;
            }
            return true;
        }

        /// <summary>
        /// プレイヤーがスタン中かどうかを取得します
        /// </summary>
        /// <param name="clientId">クライアントID</param>
        /// <returns>スタン中の場合true</returns>
        public bool IsStunned(ulong clientId)
        {
            return false;
        }

        /// <summary>
        /// 矢を発射します（クライアント）
        /// </summary>
        /// <param name="origin">発射位置</param>
        /// <param name="direction">発射方向</param>
        /// <param name="initialVelocity">初速</param>
        public void FireArrow(Vector3 origin, Vector3 direction, float initialVelocity)
        {
            Debug.Log($"[MockMatchService] FireArrow from {origin} direction {direction}");
        }

        /// <summary>
        /// プレイヤーのスコア情報を取得します
        /// </summary>
        /// <param name="clientId">クライアントID</param>
        /// <returns>プレイヤースコア（null = 見つからない）</returns>
        public PlayerScore? GetPlayerScore(ulong clientId)
        {
            return new PlayerScore
            {
                ClientId = clientId,
                Score = _config?.MatchSceneMockData?.LocalPlayerScore ?? 100,
                HitCount = 15,
                ShotCount = 30
            };
        }

        /// <summary>
        /// すべてのプレイヤーのスコア情報を取得します
        /// </summary>
        /// <returns>プレイヤースコア配列</returns>
        public PlayerScore[] GetAllPlayerScores()
        {
            return new[]
            {
                new PlayerScore { ClientId = 0, Score = 100, HitCount = 15, ShotCount = 30 },
                new PlayerScore { ClientId = 1, Score = 80, HitCount = 12, ShotCount = 28 },
                new PlayerScore { ClientId = 2, Score = 60, HitCount = 8, ShotCount = 25 },
                new PlayerScore { ClientId = 3, Score = 40, HitCount = 5, ShotCount = 20 }
            };
        }

        /// <summary>
        /// マッチを開始します（サーバーのみ）
        /// </summary>
        /// <param name="playerSlots">参加プレイヤーのスロット情報</param>
        /// <param name="arrowsPerPlayer">プレイヤーごとの矢の数</param>
        public void StartMatch(IReadOnlyList<PlayerSlot> playerSlots, int arrowsPerPlayer)
        {
            Debug.Log($"[MockMatchService] StartMatch with {playerSlots.Count} players, {arrowsPerPlayer} arrows each");
        }

        /// <summary>
        /// マッチを終了します（サーバーのみ）
        /// </summary>
        /// <param name="winnerClientId">勝者のクライアントID</param>
        public void EndMatch(ulong winnerClientId)
        {
            Debug.Log($"[MockMatchService] EndMatch, winner: {winnerClientId}");
        }

        /// <summary>
        /// スコアリング設定を更新します（サーバーのみ）
        /// </summary>
        /// <param name="config">新しいスコアリング設定</param>
        public void UpdateScoringConfig(ScoringConfig config)
        {
            Debug.Log("[MockMatchService] UpdateScoringConfig");
        }

        #endregion

        #region Mock-specific Methods

        /// <summary>
        /// モックのマッチ結果を取得します
        /// </summary>
        /// <returns>モックマッチ結果、設定がない場合はDevBootstrapのデフォルト値</returns>
        public MatchResult GetMockMatchResult()
        {
            if (_lastMatchResult != null)
            {
                return _lastMatchResult;
            }

            if (_config?.ResultsSceneMockData != null)
            {
                _lastMatchResult = _config.ResultsSceneMockData.CreateMatchResult();
                return _lastMatchResult;
            }

            // DevBootstrapからデフォルトを取得
            _lastMatchResult = DevBootstrap.GetMockMatchResult();
            return _lastMatchResult!;
        }

        /// <summary>
        /// モックのマッチ結果を設定します
        /// </summary>
        /// <param name="result">設定するマッチ結果</param>
        public void SetMockMatchResult(MatchResult result)
        {
            _lastMatchResult = result;
        }

        /// <summary>
        /// 最後に完了したマッチの結果を取得します
        /// </summary>
        /// <returns>マッチ結果（存在しない場合はnull）</returns>
        public MatchResult? GetLastMatchResult()
        {
            return GetMockMatchResult();
        }

        /// <summary>
        /// サービスを初期化します
        /// </summary>
        public void Initialize()
        {
            Debug.Log("[MockMatchService] Initialize");
        }

        /// <summary>
        /// サービスを破棄します
        /// </summary>
        public void Dispose()
        {
            Debug.Log("[MockMatchService] Dispose");
        }

        #endregion
    }
}

#endif
