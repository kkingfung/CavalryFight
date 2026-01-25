#nullable enable

using System;
using CavalryFight.Services.Customization;

namespace CavalryFight.Services.Replay
{
    /// <summary>
    /// リプレイ用エンティティのカスタマイズデータ
    /// </summary>
    /// <remarks>
    /// 敵キャラクターのカスタマイズ情報を保持します。
    /// EntityIdでリプレイフレームのスナップショットと紐付けます。
    /// </remarks>
    [Serializable]
    public class ReplayEntityCustomization
    {
        /// <summary>
        /// エンティティID（EntitySnapshotのEntityIdと一致）
        /// </summary>
        public string EntityId = string.Empty;

        /// <summary>
        /// 表示名
        /// </summary>
        public string DisplayName = string.Empty;

        /// <summary>
        /// エンティティタイプ
        /// </summary>
        /// <remarks>
        /// Player, Enemy, Wolf, Prey など
        /// </remarks>
        public EntityType EntityType = EntityType.Enemy;

        /// <summary>
        /// キャラクターカスタマイズ（騎手用）
        /// </summary>
        /// <remarks>
        /// EntityType が Player または Enemy の場合に使用
        /// </remarks>
        public CharacterCustomization Character = new();

        /// <summary>
        /// 馬カスタマイズ（騎手用）
        /// </summary>
        /// <remarks>
        /// EntityType が Player または Enemy の場合に使用
        /// </remarks>
        public MountCustomization Mount = new();

        /// <summary>
        /// チームインデックス（ウルフ等の着色用）
        /// </summary>
        public int TeamIndex = 0;

        /// <summary>
        /// ReplayEntityCustomizationの新しいインスタンスを初期化します
        /// </summary>
        public ReplayEntityCustomization()
        {
        }

        /// <summary>
        /// ReplayEntityCustomizationの新しいインスタンスを初期化します
        /// </summary>
        /// <param name="entityId">エンティティID</param>
        /// <param name="displayName">表示名</param>
        public ReplayEntityCustomization(string entityId, string displayName)
        {
            EntityId = entityId;
            DisplayName = displayName;
        }

        /// <summary>
        /// ReplayEntityCustomizationの新しいインスタンスを初期化します
        /// </summary>
        /// <param name="entityId">エンティティID</param>
        /// <param name="displayName">表示名</param>
        /// <param name="character">キャラクターカスタマイズ</param>
        /// <param name="mount">馬カスタマイズ</param>
        public ReplayEntityCustomization(
            string entityId,
            string displayName,
            CharacterCustomization character,
            MountCustomization mount)
        {
            EntityId = entityId;
            DisplayName = displayName;
            EntityType = EntityType.Enemy;
            Character = character;
            Mount = mount;
        }

        /// <summary>
        /// ウルフエンティティ用のReplayEntityCustomizationを作成します
        /// </summary>
        /// <param name="entityId">エンティティID</param>
        /// <param name="displayName">表示名</param>
        /// <param name="teamIndex">チームインデックス</param>
        public ReplayEntityCustomization(string entityId, string displayName, int teamIndex)
        {
            EntityId = entityId;
            DisplayName = displayName;
            EntityType = EntityType.Wolf;
            TeamIndex = teamIndex;
        }
    }
}
