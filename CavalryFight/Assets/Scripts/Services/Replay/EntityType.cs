#nullable enable

namespace CavalryFight.Services.Replay
{
    /// <summary>
    /// エンティティのタイプ
    /// </summary>
    public enum EntityType
    {
        /// <summary>
        /// 不明
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// プレイヤー
        /// </summary>
        Player = 1,

        /// <summary>
        /// 敵
        /// </summary>
        Enemy = 2,

        /// <summary>
        /// 発射物（矢、槍等）
        /// </summary>
        Projectile = 3,

        /// <summary>
        /// その他の重要なオブジェクト
        /// </summary>
        Other = 4
    }
}
