#nullable enable

using System;
using System.Collections.Generic;
using CavalryFight.Core.Services;
using CavalryFight.Services.Lobby;
using UnityEngine;

namespace CavalryFight.Services.Match
{
    /// <summary>
    /// マッチサービスインターフェース
    /// </summary>
    /// <remarks>
    /// マッチ中のゲームプレイ、スコア管理、矢の発射を管理します。
    /// NetworkMatchManagerのラッパーとして機能します。
    /// </remarks>
    public interface IMatchService : IService
    {
        #region Update

        /// <summary>
        /// サービスを更新します（MonoBehaviourのUpdateから呼び出す）
        /// </summary>
        void Update();

        #endregion

        #region Events

        /// <summary>
        /// 矢が発射された時に発生します
        /// </summary>
        /// <remarks>
        /// サーバーが検証した後、全クライアントに通知されます。
        /// このイベントで矢のビジュアル（projectile）を生成してください。
        /// </remarks>
        event Action<ArrowShotData>? ArrowFired;

        /// <summary>
        /// 命中があった時に発生します
        /// </summary>
        event Action<HitResult>? HitRegistered;

        /// <summary>
        /// プレイヤーのスコアが変更された時に発生します
        /// </summary>
        event Action<ulong, int>? PlayerScoreChanged; // clientId, newScore

        /// <summary>
        /// プレイヤーがスコアを獲得した時に発生します（詳細情報付き）
        /// </summary>
        event Action<ulong, int, HitLocation>? PlayerScored; // clientId, score, hitLocation

        /// <summary>
        /// マッチが開始された時に発生します
        /// </summary>
        event Action? MatchStarted;

        /// <summary>
        /// マッチが終了した時に発生します
        /// </summary>
        event Action<ulong>? MatchEnded; // winnerClientId

        /// <summary>
        /// マッチが終了した時に発生します（詳細情報付き）
        /// </summary>
        event Action<MatchEndResult>? MatchEndedWithResult;

        /// <summary>
        /// マッチ状態が変更された時に発生します
        /// </summary>
        event Action<MatchState>? MatchStateChanged;

        /// <summary>
        /// カウントダウンが更新された時に発生します
        /// </summary>
        event Action<int>? CountdownUpdated;

        #endregion

        #region Properties

        /// <summary>
        /// マッチが開始されているかどうかを取得します
        /// </summary>
        bool IsMatchStarted { get; }

        /// <summary>
        /// 現在のマッチ状態を取得します
        /// </summary>
        MatchState CurrentState { get; }

        /// <summary>
        /// 現在のゲームモードを取得します
        /// </summary>
        GameMode CurrentGameMode { get; }

        /// <summary>
        /// 残り時間（秒）を取得します
        /// </summary>
        float RemainingTime { get; }

        /// <summary>
        /// マッチ経過時間（秒）を取得します
        /// </summary>
        float MatchTime { get; }

        /// <summary>
        /// 現在のスコアリング設定を取得します
        /// </summary>
        ScoringConfig CurrentScoringConfig { get; }

        /// <summary>
        /// チームスコアを取得します
        /// </summary>
        /// <param name="teamIndex">チームインデックス</param>
        /// <returns>チームのスコア</returns>
        int GetTeamScore(int teamIndex);

        /// <summary>
        /// ローカルプレイヤーがハンターかどうかを取得します（ハンティングモード用）
        /// </summary>
        /// <param name="clientId">クライアントID</param>
        /// <returns>ハンターの場合true</returns>
        bool IsHunter(ulong clientId);

        /// <summary>
        /// プレイヤーがスタン中かどうかを取得します
        /// </summary>
        /// <param name="clientId">クライアントID</param>
        /// <returns>スタン中の場合true</returns>
        bool IsStunned(ulong clientId);

        #endregion

        #region Methods - Client

        /// <summary>
        /// 矢を発射します（クライアント）
        /// </summary>
        /// <param name="origin">発射位置</param>
        /// <param name="direction">発射方向</param>
        /// <param name="initialVelocity">初速</param>
        void FireArrow(Vector3 origin, Vector3 direction, float initialVelocity);

        /// <summary>
        /// プレイヤーのスコア情報を取得します
        /// </summary>
        /// <param name="clientId">クライアントID</param>
        /// <returns>プレイヤースコア（null = 見つからない）</returns>
        PlayerScore? GetPlayerScore(ulong clientId);

        /// <summary>
        /// すべてのプレイヤーのスコア情報を取得します
        /// </summary>
        /// <returns>プレイヤースコア配列</returns>
        PlayerScore[] GetAllPlayerScores();

        /// <summary>
        /// 最後に完了したマッチの結果を取得します
        /// </summary>
        /// <returns>マッチ結果（存在しない場合はnull）</returns>
        MatchResult? GetLastMatchResult();

        #endregion

        #region Methods - Server Only

        /// <summary>
        /// マッチを開始します（サーバーのみ）
        /// </summary>
        /// <param name="playerSlots">参加プレイヤーのスロット情報</param>
        /// <param name="arrowsPerPlayer">プレイヤーごとの矢の数</param>
        void StartMatch(IReadOnlyList<CavalryFight.Services.Lobby.PlayerSlot> playerSlots, int arrowsPerPlayer);

        /// <summary>
        /// マッチを終了します（サーバーのみ）
        /// </summary>
        /// <param name="winnerClientId">勝者のクライアントID</param>
        void EndMatch(ulong winnerClientId);

        /// <summary>
        /// スコアリング設定を更新します（サーバーのみ）
        /// </summary>
        /// <param name="config">新しいスコアリング設定</param>
        void UpdateScoringConfig(ScoringConfig config);

        #endregion
    }
}
