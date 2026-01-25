#nullable enable

namespace CavalryFight.Services.Lobby
{
    /// <summary>
    /// マップ名
    /// </summary>
    /// <remarks>
    /// 利用可能なマップの列挙型です。
    /// FieldLoaderのFieldEntriesおよびMapConfigと一致させてください。
    /// プレハブ名: Assets/Prefabs/Field/ 配下
    /// </remarks>
    public enum MapName
    {
        /// <summary>アリーナ（Arena.prefab）</summary>
        Arena = 0,

        /// <summary>森（Forest.prefab）</summary>
        Forest = 1,

        /// <summary>自然（Nature.prefab）</summary>
        Nature = 2,

        /// <summary>プレイグラウンド（PlayGround.prefab）</summary>
        PlayGround = 3,

        /// <summary>トレーニングルーム（TrainingRoom.prefab）</summary>
        TrainingRoom = 4
    }
}
