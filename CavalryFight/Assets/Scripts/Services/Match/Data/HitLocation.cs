#nullable enable

namespace CavalryFight.Services.Match
{
    /// <summary>
    /// 命中部位
    /// </summary>
    /// <remarks>
    /// 弓矢が命中した部位を表します。
    /// 部位によって獲得スコアが異なります。
    /// </remarks>
    public enum HitLocation
    {
        /// <summary>
        /// 心臓（最高得点）
        /// </summary>
        Heart = 0,

        /// <summary>
        /// 頭部（高得点）
        /// </summary>
        Head = 1,

        /// <summary>
        /// 胴体（中得点）
        /// </summary>
        Torso = 2,

        /// <summary>
        /// 腕（低得点）
        /// </summary>
        Arm = 3,

        /// <summary>
        /// 脚（低得点）
        /// </summary>
        Leg = 4,

        /// <summary>
        /// 騎乗動物（最低得点）
        /// </summary>
        Mount = 5,

        /// <summary>
        /// 外れ（0点）
        /// </summary>
        Miss = 6
    }
}
