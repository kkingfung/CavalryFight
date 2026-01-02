#nullable enable

namespace CavalryFight.Services.Customization
{
    /// <summary>
    /// 矢のタイプ（MasterStylizedProjectilesのプレハブ種類）
    /// </summary>
    /// <remarks>
    /// 各タイプは対応するBullet/Muzzle/Hitプレハブセットを持ちます。
    /// プレハブはInspectorで ArrowTypeConfig に設定します。
    /// </remarks>
    public enum ArrowType
    {
        /// <summary>通常の矢（Arrow/ArrowBullet）</summary>
        Arrow = 0,

        /// <summary>青い弾（BlueShoot）</summary>
        BlueShoot = 1,

        /// <summary>シアンブルー弾（CyanBlueBullet）</summary>
        CyanBlue = 2,

        /// <summary>エネルギー爆発（EnergyExplosion）</summary>
        EnergyExplosion = 3,

        /// <summary>火球（Fireball）</summary>
        Fireball = 4,

        /// <summary>緑の矢（GreenArrow）</summary>
        GreenArrow = 5,

        /// <summary>雷爆発（LightningExplosion）</summary>
        LightningExplosion = 6,

        /// <summary>ミサイル（Missile）</summary>
        Missile = 7,

        /// <summary>オレンジ銃弾（OrangeGunShot）</summary>
        OrangeGunShot = 8,

        /// <summary>オレンジスパークル（OrangeSparkleShoot）</summary>
        OrangeSparkle = 9,

        /// <summary>紫の星（PurpleStar）</summary>
        PurpleStar = 10,

        /// <summary>赤い剣ビーム（RedSwordBeam）</summary>
        RedSwordBeam = 11,

        /// <summary>紫の弾（Shoot_Purple）</summary>
        ShootPurple = 12,

        /// <summary>赤い弾（Shoot_Red）</summary>
        ShootRed = 13,

        /// <summary>手裏剣（Shuriken）</summary>
        Shuriken = 14,

        /// <summary>複数手裏剣（Shurikens）</summary>
        Shurikens = 15,

        /// <summary>小エネルギー弾（SmallEnergyBullet）</summary>
        SmallEnergy = 16,

        /// <summary>小火炎弾（SmallFireBullet）</summary>
        SmallFire = 17,

        /// <summary>小氷弾（SmallIceBullet）</summary>
        SmallIce = 18,

        /// <summary>小光弾（SmallLightBullet）</summary>
        SmallLight = 19,

        /// <summary>星（Star）</summary>
        Star = 20,

        /// <summary>竜巻弾（TornadoShoots）</summary>
        Tornado = 21,

        /// <summary>風弾（WindShoot）</summary>
        Wind = 22,

        /// <summary>黄色い剣ビーム（YellowSwordBeam）</summary>
        YellowSwordBeam = 23
    }
}
