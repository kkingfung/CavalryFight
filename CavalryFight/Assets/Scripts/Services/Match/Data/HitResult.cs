#nullable enable

using System;
using Unity.Netcode;
using UnityEngine;

namespace CavalryFight.Services.Match
{
    /// <summary>
    /// 命中結果
    /// </summary>
    /// <remarks>
    /// サーバーからクライアントへ送信される命中判定の結果。
    /// スコア加算やエフェクト表示に使用されます。
    /// </remarks>
    [Serializable]
    public struct HitResult : INetworkSerializable
    {
        #region Fields

        /// <summary>
        /// 射手のクライアントID
        /// </summary>
        public ulong ShooterClientId;

        /// <summary>
        /// 被弾者のクライアントID
        /// </summary>
        public ulong TargetClientId;

        /// <summary>
        /// 命中部位
        /// </summary>
        public HitLocation HitLocation;

        /// <summary>
        /// 獲得スコア
        /// </summary>
        public int ScoreAwarded;

        /// <summary>
        /// 命中位置
        /// </summary>
        public Vector3 HitPosition;

        /// <summary>
        /// 命中法線
        /// </summary>
        public Vector3 HitNormal;

        /// <summary>
        /// 命中が有効かどうか
        /// </summary>
        /// <remarks>
        /// false = ミス、壁に命中、無効な判定など
        /// </remarks>
        public bool IsValidHit;

        #endregion

        #region Constructors

        /// <summary>
        /// 有効な命中結果を作成します
        /// </summary>
        public static HitResult CreateValidHit(
            ulong shooterClientId,
            ulong targetClientId,
            HitLocation hitLocation,
            int scoreAwarded,
            Vector3 hitPosition,
            Vector3 hitNormal)
        {
            return new HitResult
            {
                ShooterClientId = shooterClientId,
                TargetClientId = targetClientId,
                HitLocation = hitLocation,
                ScoreAwarded = scoreAwarded,
                HitPosition = hitPosition,
                HitNormal = hitNormal,
                IsValidHit = true
            };
        }

        /// <summary>
        /// 無効な命中結果（ミス）を作成します
        /// </summary>
        public static HitResult CreateMiss(ulong shooterClientId)
        {
            return new HitResult
            {
                ShooterClientId = shooterClientId,
                TargetClientId = 0,
                HitLocation = HitLocation.Miss,
                ScoreAwarded = 0,
                HitPosition = Vector3.zero,
                HitNormal = Vector3.zero,
                IsValidHit = false
            };
        }

        #endregion

        #region INetworkSerializable Implementation

        /// <summary>
        /// ネットワークシリアライゼーション
        /// </summary>
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref ShooterClientId);
            serializer.SerializeValue(ref TargetClientId);
            serializer.SerializeValue(ref HitLocation);
            serializer.SerializeValue(ref ScoreAwarded);
            serializer.SerializeValue(ref HitPosition);
            serializer.SerializeValue(ref HitNormal);
            serializer.SerializeValue(ref IsValidHit);
        }

        #endregion
    }
}
