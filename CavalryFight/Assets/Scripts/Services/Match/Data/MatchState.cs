#nullable enable

namespace CavalryFight.Services.Match
{
    /// <summary>
    /// マッチ状態
    /// </summary>
    /// <remarks>
    /// マッチの進行状態を表します。
    /// ViewModels層からアクセス可能なサービスデータ型です。
    /// </remarks>
    public enum MatchState
    {
        /// <summary>プレイヤー待機中</summary>
        WaitingForPlayers = 0,

        /// <summary>カウントダウン中</summary>
        Countdown = 1,

        /// <summary>マッチ進行中</summary>
        InProgress = 2,

        /// <summary>一時停止中</summary>
        Paused = 3,

        /// <summary>マッチ終了</summary>
        Ended = 4
    }
}
