#nullable enable

using System;
using System.Collections.Generic;
using CavalryFight.Core.Services;
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
        /// マッチが開始された時に発生します
        /// </summary>
        event Action? MatchStarted;

        /// <summary>
        /// マッチが終了した時に発生します
        /// </summary>
        event Action<ulong>? MatchEnded; // winnerClientId

        #endregion

        #region Properties

        /// <summary>
        /// マッチが開始されているかどうかを取得します
        /// </summary>
        bool IsMatchStarted { get; }

        /// <summary>
        /// 現在のスコアリング設定を取得します
        /// </summary>
        ScoringConfig CurrentScoringConfig { get; }

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
