#nullable enable

using System;
using Unity.Netcode;

namespace CavalryFight.Services.Match
{
    /// <summary>
    /// スコアリング設定
    /// </summary>
    /// <remarks>
    /// 各命中部位のスコアを定義します。
    /// </remarks>
    [Serializable]
    public struct ScoringConfig : INetworkSerializable
    {
        #region Fields

        /// <summary>
        /// 心臓命中時のスコア
        /// </summary>
        public int HeartScore;

        /// <summary>
        /// 頭部命中時のスコア
        /// </summary>
        public int HeadScore;

        /// <summary>
        /// 胴体命中時のスコア
        /// </summary>
        public int TorsoScore;

        /// <summary>
        /// 腕命中時のスコア
        /// </summary>
        public int ArmScore;

        /// <summary>
        /// 脚命中時のスコア
        /// </summary>
        public int LegScore;

        /// <summary>
        /// 騎乗動物命中時のスコア
        /// </summary>
        public int MountScore;

        #endregion

        #region Constructors

        /// <summary>
        /// デフォルト設定を取得します
        /// </summary>
        /// <returns>デフォルトのスコアリング設定</returns>
        public static ScoringConfig CreateDefault()
        {
            return new ScoringConfig
            {
                HeartScore = 100,    // 心臓 = 最高得点
                HeadScore = 50,      // 頭部 = 高得点
                TorsoScore = 30,     // 胴体 = 中得点
                ArmScore = 10,       // 腕 = 低得点
                LegScore = 10,       // 脚 = 低得点
                MountScore = 5       // 騎乗動物 = 最低得点
            };
        }

        #endregion

        #region INetworkSerializable Implementation

        /// <summary>
        /// ネットワークシリアライゼーション
        /// </summary>
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref HeartScore);
            serializer.SerializeValue(ref HeadScore);
            serializer.SerializeValue(ref TorsoScore);
            serializer.SerializeValue(ref ArmScore);
            serializer.SerializeValue(ref LegScore);
            serializer.SerializeValue(ref MountScore);
        }

        #endregion

        #region Methods

        /// <summary>
        /// 命中部位に応じたスコアを取得します
        /// </summary>
        /// <param name="hitLocation">命中部位</param>
        /// <returns>獲得スコア</returns>
        public readonly int GetScore(HitLocation hitLocation)
        {
            return hitLocation switch
            {
                HitLocation.Heart => HeartScore,
                HitLocation.Head => HeadScore,
                HitLocation.Torso => TorsoScore,
                HitLocation.Arm => ArmScore,
                HitLocation.Leg => LegScore,
                HitLocation.Mount => MountScore,
                HitLocation.Miss => 0,
                _ => 0
            };
        }

        #endregion
    }
}
