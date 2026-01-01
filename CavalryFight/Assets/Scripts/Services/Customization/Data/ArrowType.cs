#nullable enable

namespace CavalryFight.Services.Customization
{
    /// <summary>
    /// 矢のタイプ（MasterStylizedProjectilesのプレハブ種類）
    /// </summary>
    /// <remarks>
    /// 各タイプは対応するBullet/Muzzle/Hitプレハブセットを持ちます。
    /// プレハブはInspectorで ArrowBulletPrefabs 配列に設定します。
    /// </remarks>
    public enum ArrowType
    {
        /// <summary>通常の矢（Arrow/ArrowBullet）</summary>
        Arrow = 0,

        /// <summary>緑の矢（GreenArrow/GreenArrowBullet）</summary>
        GreenArrow = 1,

        /// <summary>シアンブルー弾（CyanBlueBullet/CyanBlueBullet）</summary>
        CyanBlue = 2,

        /// <summary>エネルギー爆発（EnergyExplosion/EnergyExplosionBullet）</summary>
        EnergyExplosion = 3,

        /// <summary>火球（Fireball/Fireball）</summary>
        Fireball = 4,

        /// <summary>雷爆発（LightningExplosion/LightningExplosionBullet）</summary>
        LightningExplosion = 5,

        /// <summary>紫の星（PurpleStar/PurpleStarBullet）</summary>
        PurpleStar = 6,

        /// <summary>手裏剣（Shurikens/ShurikenBullet）</summary>
        Shuriken = 7,

        /// <summary>小エネルギー弾（SmallEnergyBullet/SmallEnergyBullet）</summary>
        SmallEnergy = 8,

        /// <summary>小火炎弾（SmallFireBullet/SmallFireBullet）</summary>
        SmallFire = 9,

        /// <summary>小氷弾（SmallIceBullet/SmallIceBullet）</summary>
        SmallIce = 10,

        /// <summary>小光弾（SmallLightBullet/SmallLightBullet）</summary>
        SmallLight = 11,

        /// <summary>星（Star/StarBullet）</summary>
        Star = 12,

        /// <summary>風弾（WindShoot/WindBullet）</summary>
        Wind = 13
    }
}
