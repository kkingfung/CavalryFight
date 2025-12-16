#nullable enable

namespace CavalryFight.Services.Lobby
{
    /// <summary>
    /// AI難易度
    /// </summary>
    /// <remarks>
    /// CPUプレイヤーの難易度レベルを定義します。
    /// </remarks>
    public enum AIDifficulty
    {
        /// <summary>
        /// 簡単
        /// </summary>
        /// <remarks>
        /// 初心者向けのAI。反応が遅く、ミスが多い。
        /// </remarks>
        Easy = 0,

        /// <summary>
        /// 普通
        /// </summary>
        /// <remarks>
        /// 標準的なAI。バランスの取れた難易度。
        /// </remarks>
        Normal = 1,

        /// <summary>
        /// 難しい
        /// </summary>
        /// <remarks>
        /// 上級者向けのAI。反応が速く、正確。
        /// </remarks>
        Hard = 2,

        /// <summary>
        /// エキスパート
        /// </summary>
        /// <remarks>
        /// 最高難易度のAI。ほぼ完璧なプレイ。
        /// </remarks>
        Expert = 3
    }
}
