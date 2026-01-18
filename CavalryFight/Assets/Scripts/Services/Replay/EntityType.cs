#nullable enable

namespace CavalryFight.Services.Replay
{
    /// <summary>
    /// エンティティのタイプ
    /// </summary>
    /// <remarks>
    /// リプレイシステムで記録・再生するエンティティの種類を定義します。
    /// Huntingモードでは狼やAI獲物なども含みます。
    /// </remarks>
    public enum EntityType
    {
        /// <summary>
        /// 不明
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// プレイヤー（騎手）
        /// </summary>
        Player = 1,

        /// <summary>
        /// 敵（AI騎手）
        /// </summary>
        Enemy = 2,

        /// <summary>
        /// 発射物（矢、槍等）
        /// </summary>
        Projectile = 3,

        /// <summary>
        /// 馬
        /// </summary>
        Mount = 4,

        /// <summary>
        /// 狼（Huntingモード用、サードパーティアセット）
        /// </summary>
        Wolf = 5,

        /// <summary>
        /// 獲物（Huntingモード用、鹿・イノシシ等）
        /// </summary>
        Prey = 6,

        /// <summary>
        /// AI動物（汎用、狼や獲物以外の動物）
        /// </summary>
        AIAnimal = 7,

        /// <summary>
        /// NPC（非戦闘キャラクター）
        /// </summary>
        NPC = 8,

        /// <summary>
        /// その他の重要なオブジェクト
        /// </summary>
        Other = 99
    }
}
