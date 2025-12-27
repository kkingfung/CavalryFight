#nullable enable

namespace CavalryFight.Services.Customization
{
    /// <summary>
    /// 角のマテリアル
    /// </summary>
    /// <remarks>
    /// 角に適用するマテリアルを指定します。
    /// マテリアルは「5 - Materials & Textures/Horns」フォルダにあります。
    /// </remarks>
    public enum HornMaterial
    {
        /// <summary>黒い角（ポリアート）</summary>
        BlackPA = 0,

        /// <summary>黒い角（リアリスティック）</summary>
        BlackRealistic = 1,

        /// <summary>赤い角（ポリアート）</summary>
        RedPA = 2,

        /// <summary>白い角（ポリアート）</summary>
        WhitePA = 3,

        /// <summary>白い角（パロミノ）</summary>
        WhitePalomino = 4,

        /// <summary>白い角（リアリスティック）</summary>
        WhiteRealistic = 5
    }
}
