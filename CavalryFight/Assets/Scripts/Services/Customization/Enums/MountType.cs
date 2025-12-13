#nullable enable

namespace CavalryFight.Services.Customization
{
    /// <summary>
    /// 騎乗動物のタイプ
    /// </summary>
    public enum MountType
    {
        /// <summary>
        /// リアルな馬
        /// </summary>
        HorseRealistic = 0,

        /// <summary>
        /// ポリアート風の馬
        /// </summary>
        HorsePolyArt = 1,

        /// <summary>
        /// マインクラフト風の馬
        /// </summary>
        HorseMinecraft = 2,

        /// <summary>
        /// ペガサス（翼のある馬）
        /// </summary>
        Pegasus = 3,

        /// <summary>
        /// ユニコーン（角のある馬）
        /// </summary>
        Unicorn = 4
    }
}
