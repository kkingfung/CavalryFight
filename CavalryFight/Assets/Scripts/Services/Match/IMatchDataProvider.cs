#nullable enable

using System;
using CavalryFight.Services.Lobby;

namespace CavalryFight.Services.Match
{
    /// <summary>
    /// マッチデータを提供するインターフェース
    /// </summary>
    /// <remarks>
    /// MatchManagerがこのインターフェースを実装し、MatchServiceに登録することで、
    /// 異なるアセンブリ間でもマッチデータを共有できます。
    /// </remarks>
    public interface IMatchDataProvider
    {
        /// <summary>
        /// マッチが進行中かどうか
        /// </summary>
        bool IsMatchInProgress { get; }

        /// <summary>
        /// 現在のマッチ状態
        /// </summary>
        MatchState CurrentMatchState { get; }

        /// <summary>
        /// 残り時間（秒）
        /// </summary>
        float RemainingTime { get; }

        /// <summary>
        /// マッチ経過時間（秒）
        /// </summary>
        float MatchTime { get; }

        /// <summary>
        /// 現在のゲームモード
        /// </summary>
        GameMode CurrentGameMode { get; }

        /// <summary>
        /// プレイヤーのスコア情報を取得します
        /// </summary>
        PlayerScore? GetPlayerScore(ulong clientId);

        /// <summary>
        /// すべてのプレイヤーのスコア情報を取得します
        /// </summary>
        PlayerScore[] GetAllPlayerScores();

        /// <summary>
        /// チームスコアを取得します
        /// </summary>
        int GetTeamScore(int teamIndex);

        /// <summary>
        /// マッチ状態が変更された時に発生します
        /// </summary>
        event Action<MatchState>? MatchStateChanged;

        /// <summary>
        /// マッチが開始された時に発生します
        /// </summary>
        event Action? MatchStarted;

        /// <summary>
        /// マッチが終了した時に発生します
        /// </summary>
        event Action<MatchEndResult>? MatchEnded;

        /// <summary>
        /// プレイヤーがスコアを獲得した時に発生します
        /// </summary>
        event Action<ulong, int, HitLocation>? PlayerScored;

        /// <summary>
        /// カウントダウンが更新された時に発生します
        /// </summary>
        event Action<int>? CountdownUpdated;
    }
}
