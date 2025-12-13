#nullable enable

namespace CavalryFight.Services.Lobby
{
    /// <summary>
    /// ゲームモード
    /// </summary>
    /// <remarks>
    /// トレーニングモードを除く、すべての対戦ゲームモードを定義します。
    /// </remarks>
    public enum GameMode
    {
        /// <summary>
        /// アリーナモード
        /// </summary>
        /// <remarks>
        /// 定められたアリーナで戦闘。制限時間内でのスコア獲得。
        /// </remarks>
        Arena = 0,

        /// <summary>
        /// スコアマッチ
        /// </summary>
        /// <remarks>
        /// 限られた矢でポイント獲得。ベストオブN形式の勝負。
        /// </remarks>
        ScoreMatch = 1,

        /// <summary>
        /// チームファイト
        /// </summary>
        /// <remarks>
        /// チーム対チームの協力プレイ。戦術と連携が重要。
        /// </remarks>
        TeamFight = 2,

        /// <summary>
        /// デスマッチ
        /// </summary>
        /// <remarks>
        /// 個人戦の自由戦闘。最後の一人まで戦う。
        /// </remarks>
        Deathmatch = 3,

        /// <summary>
        /// PvEモード
        /// </summary>
        /// <remarks>
        /// AIを相手にした戦闘。スキル向上とトレーニング。
        /// </remarks>
        PvE = 4
    }
}
