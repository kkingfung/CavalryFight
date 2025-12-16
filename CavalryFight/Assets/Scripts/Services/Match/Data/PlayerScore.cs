#nullable enable

using System;
using Unity.Collections;
using Unity.Netcode;

namespace CavalryFight.Services.Match
{
    /// <summary>
    /// プレイヤースコア
    /// </summary>
    /// <remarks>
    /// 個々のプレイヤーのスコア情報を保持します。
    /// </remarks>
    [Serializable]
    public struct PlayerScore : INetworkSerializable, IEquatable<PlayerScore>
    {
        #region Fields

        /// <summary>
        /// プレイヤーのクライアントID
        /// </summary>
        public ulong ClientId;

        /// <summary>
        /// プレイヤー名
        /// </summary>
        public FixedString64Bytes PlayerName;

        /// <summary>
        /// 現在のスコア
        /// </summary>
        public int Score;

        /// <summary>
        /// 残り矢の数
        /// </summary>
        public int RemainingArrows;

        /// <summary>
        /// 命中回数
        /// </summary>
        public int HitCount;

        /// <summary>
        /// 発射回数
        /// </summary>
        public int ShotCount;

        /// <summary>
        /// チームインデックス（-1 = チームなし）
        /// </summary>
        public int TeamIndex;

        #endregion

        #region Constructors

        /// <summary>
        /// PlayerScoreを初期化します
        /// </summary>
        /// <param name="clientId">クライアントID</param>
        /// <param name="playerName">プレイヤー名</param>
        /// <param name="initialArrows">初期矢数</param>
        /// <param name="teamIndex">チームインデックス</param>
        public PlayerScore(ulong clientId, string playerName, int initialArrows, int teamIndex = -1)
        {
            ClientId = clientId;
            PlayerName = new FixedString64Bytes(playerName);
            Score = 0;
            RemainingArrows = initialArrows;
            HitCount = 0;
            ShotCount = 0;
            TeamIndex = teamIndex;
        }

        #endregion

        #region INetworkSerializable Implementation

        /// <summary>
        /// ネットワークシリアライゼーション
        /// </summary>
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref ClientId);
            serializer.SerializeValue(ref PlayerName);
            serializer.SerializeValue(ref Score);
            serializer.SerializeValue(ref RemainingArrows);
            serializer.SerializeValue(ref HitCount);
            serializer.SerializeValue(ref ShotCount);
            serializer.SerializeValue(ref TeamIndex);
        }

        #endregion

        #region IEquatable Implementation

        /// <summary>
        /// 指定されたPlayerScoreと等しいかどうかを判断します
        /// </summary>
        public bool Equals(PlayerScore other)
        {
            return ClientId == other.ClientId &&
                   PlayerName.Equals(other.PlayerName) &&
                   Score == other.Score &&
                   RemainingArrows == other.RemainingArrows &&
                   HitCount == other.HitCount &&
                   ShotCount == other.ShotCount &&
                   TeamIndex == other.TeamIndex;
        }

        /// <summary>
        /// 指定されたオブジェクトと等しいかどうかを判断します
        /// </summary>
        public override bool Equals(object obj)
        {
            return obj is PlayerScore other && Equals(other);
        }

        /// <summary>
        /// ハッシュコードを取得します
        /// </summary>
        public override int GetHashCode()
        {
            return HashCode.Combine(ClientId, PlayerName, Score, RemainingArrows, HitCount, ShotCount, TeamIndex);
        }

        /// <summary>
        /// 等値演算子
        /// </summary>
        public static bool operator ==(PlayerScore left, PlayerScore right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// 不等値演算子
        /// </summary>
        public static bool operator !=(PlayerScore left, PlayerScore right)
        {
            return !left.Equals(right);
        }

        #endregion

        #region Methods

        /// <summary>
        /// 命中率を取得します
        /// </summary>
        /// <returns>命中率（0.0 ~ 1.0）</returns>
        public readonly float GetAccuracy()
        {
            if (ShotCount == 0)
            {
                return 0f;
            }
            return (float)HitCount / ShotCount;
        }

        #endregion
    }
}
