#nullable enable

namespace CavalryFight.Services.Replay
{
    /// <summary>
    /// ハイライトのタイプ
    /// </summary>
    public enum HighlightType
    {
        /// <summary>
        /// 不明
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// スコア獲得
        /// </summary>
        Score = 1,

        /// <summary>
        /// 決定的な一撃（敵への重要なダメージ等）
        /// </summary>
        DecisiveStrike = 2,

        /// <summary>
        /// マッチ勝利の瞬間
        /// </summary>
        MatchWinner = 3,

        /// <summary>
        /// カスタムハイライト
        /// </summary>
        Custom = 100
    }
}
