#nullable enable

using System;
using Unity.Collections;
using Unity.Netcode;

namespace CavalryFight.Services.Lobby
{
    /// <summary>
    /// プレイヤースロット
    /// </summary>
    /// <remarks>
    /// ルーム内の1つのプレイヤースロットを表します。
    /// 人間プレイヤーまたはCPUプレイヤーが占有できます。
    /// </remarks>
    [Serializable]
    public struct PlayerSlot : INetworkSerializable, IEquatable<PlayerSlot>
    {
        #region Fields

        /// <summary>
        /// スロットインデックス（0-7）
        /// </summary>
        public int SlotIndex;

        /// <summary>
        /// プレイヤーID
        /// </summary>
        /// <remarks>
        /// 人間プレイヤーの場合: ClientId
        /// CPUプレイヤーの場合: 負の値（-1, -2, etc.）
        /// 空きスロットの場合: ulong.MaxValue
        /// </remarks>
        public ulong PlayerId;

        /// <summary>
        /// プレイヤー名
        /// </summary>
        public FixedString64Bytes PlayerName;

        /// <summary>
        /// CPUプレイヤーかどうか
        /// </summary>
        public bool IsAI;

        /// <summary>
        /// AI難易度（AIの場合のみ有効）
        /// </summary>
        public AIDifficulty AIDifficulty;

        /// <summary>
        /// 準備完了フラグ
        /// </summary>
        /// <remarks>
        /// CPUプレイヤーは常にtrue
        /// </remarks>
        public bool IsReady;

        /// <summary>
        /// チーム番号（チーム戦の場合のみ使用）
        /// </summary>
        /// <remarks>
        /// 0 = チームA、1 = チームB
        /// -1 = チーム未割り当て
        /// </remarks>
        public int TeamIndex;

        /// <summary>
        /// カスタマイズプリセット名
        /// </summary>
        /// <remarks>
        /// プレイヤーが選択したカスタマイズプリセット。
        /// マッチ開始時に適用されます。
        /// </remarks>
        public FixedString64Bytes CustomizationPresetName;

        #endregion

        #region Constructors

        /// <summary>
        /// 空のスロットを作成します
        /// </summary>
        /// <param name="slotIndex">スロットインデックス</param>
        public PlayerSlot(int slotIndex)
        {
            SlotIndex = slotIndex;
            PlayerId = ulong.MaxValue; // 空きスロット
            PlayerName = new FixedString64Bytes();
            IsAI = false;
            AIDifficulty = AIDifficulty.Normal;
            IsReady = false;
            TeamIndex = -1;
            CustomizationPresetName = new FixedString64Bytes();
        }

        /// <summary>
        /// 人間プレイヤー用のスロットを作成します
        /// </summary>
        /// <param name="slotIndex">スロットインデックス</param>
        /// <param name="playerId">プレイヤーID</param>
        /// <param name="playerName">プレイヤー名</param>
        public PlayerSlot(int slotIndex, ulong playerId, string playerName)
        {
            SlotIndex = slotIndex;
            PlayerId = playerId;
            PlayerName = new FixedString64Bytes(playerName);
            IsAI = false;
            AIDifficulty = AIDifficulty.Normal;
            IsReady = false;
            TeamIndex = -1;
            CustomizationPresetName = new FixedString64Bytes();
        }

        /// <summary>
        /// CPUプレイヤー用のスロットを作成します
        /// </summary>
        /// <param name="slotIndex">スロットインデックス</param>
        /// <param name="aiIndex">AIインデックス（負の値）</param>
        /// <param name="difficulty">AI難易度</param>
        public PlayerSlot(int slotIndex, int aiIndex, AIDifficulty difficulty)
        {
            SlotIndex = slotIndex;
            PlayerId = (ulong)aiIndex; // 負の値をulongとして格納
            PlayerName = new FixedString64Bytes($"CPU {-aiIndex}");
            IsAI = true;
            AIDifficulty = difficulty;
            IsReady = true; // CPUは常に準備完了
            TeamIndex = -1;
            CustomizationPresetName = new FixedString64Bytes();
        }

        #endregion

        #region INetworkSerializable Implementation

        /// <summary>
        /// ネットワークシリアライゼーション
        /// </summary>
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref SlotIndex);
            serializer.SerializeValue(ref PlayerId);
            serializer.SerializeValue(ref PlayerName);
            serializer.SerializeValue(ref IsAI);
            serializer.SerializeValue(ref AIDifficulty);
            serializer.SerializeValue(ref IsReady);
            serializer.SerializeValue(ref TeamIndex);
            serializer.SerializeValue(ref CustomizationPresetName);
        }

        #endregion

        #region Methods

        /// <summary>
        /// スロットが空いているかどうかを取得します
        /// </summary>
        /// <returns>スロットが空いている場合はtrue</returns>
        public readonly bool IsEmpty()
        {
            return PlayerId == ulong.MaxValue;
        }

        /// <summary>
        /// スロットをクリアします
        /// </summary>
        public void Clear()
        {
            PlayerId = ulong.MaxValue;
            PlayerName = new FixedString64Bytes();
            IsAI = false;
            AIDifficulty = AIDifficulty.Normal;
            IsReady = false;
            TeamIndex = -1;
            CustomizationPresetName = new FixedString64Bytes();
        }

        #endregion

        #region IEquatable Implementation

        /// <summary>
        /// 指定されたPlayerSlotと等しいかどうかを判断します
        /// </summary>
        /// <param name="other">比較対象のPlayerSlot</param>
        /// <returns>等しい場合はtrue</returns>
        public bool Equals(PlayerSlot other)
        {
            return SlotIndex == other.SlotIndex &&
                   PlayerId == other.PlayerId &&
                   PlayerName.Equals(other.PlayerName) &&
                   IsAI == other.IsAI &&
                   AIDifficulty == other.AIDifficulty &&
                   IsReady == other.IsReady &&
                   TeamIndex == other.TeamIndex &&
                   CustomizationPresetName.Equals(other.CustomizationPresetName);
        }

        /// <summary>
        /// 指定されたオブジェクトと等しいかどうかを判断します
        /// </summary>
        /// <param name="obj">比較対象のオブジェクト</param>
        /// <returns>等しい場合はtrue</returns>
        public override bool Equals(object obj)
        {
            return obj is PlayerSlot other && Equals(other);
        }

        /// <summary>
        /// ハッシュコードを取得します
        /// </summary>
        /// <returns>ハッシュコード</returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(SlotIndex, PlayerId, PlayerName, IsAI, AIDifficulty, IsReady, TeamIndex, CustomizationPresetName);
        }

        /// <summary>
        /// 等値演算子
        /// </summary>
        public static bool operator ==(PlayerSlot left, PlayerSlot right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// 不等値演算子
        /// </summary>
        public static bool operator !=(PlayerSlot left, PlayerSlot right)
        {
            return !left.Equals(right);
        }

        #endregion
    }
}
