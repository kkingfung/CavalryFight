#nullable enable

using System;
using Unity.Netcode;
using UnityEngine;

namespace CavalryFight.Services.Match
{
    /// <summary>
    /// 矢の発射データ
    /// </summary>
    /// <remarks>
    /// クライアントからサーバーへ送信される矢の発射情報。
    /// サーバー側で軌道検証と命中判定を行うために使用されます。
    /// </remarks>
    [Serializable]
    public struct ArrowShotData : INetworkSerializable
    {
        #region Fields

        /// <summary>
        /// 発射位置
        /// </summary>
        public Vector3 Origin;

        /// <summary>
        /// 発射方向
        /// </summary>
        public Vector3 Direction;

        /// <summary>
        /// 発射速度（初速）
        /// </summary>
        public float InitialVelocity;

        /// <summary>
        /// 発射時刻（ネットワークタイム）
        /// </summary>
        public float FireTime;

        /// <summary>
        /// 射手のクライアントID
        /// </summary>
        public ulong ShooterClientId;

        #endregion

        #region Constructors

        /// <summary>
        /// ArrowShotDataを初期化します
        /// </summary>
        /// <param name="origin">発射位置</param>
        /// <param name="direction">発射方向</param>
        /// <param name="initialVelocity">初速</param>
        /// <param name="fireTime">発射時刻</param>
        /// <param name="shooterClientId">射手のクライアントID</param>
        public ArrowShotData(Vector3 origin, Vector3 direction, float initialVelocity, float fireTime, ulong shooterClientId)
        {
            Origin = origin;
            Direction = direction.normalized;
            InitialVelocity = initialVelocity;
            FireTime = fireTime;
            ShooterClientId = shooterClientId;
        }

        #endregion

        #region INetworkSerializable Implementation

        /// <summary>
        /// ネットワークシリアライゼーション
        /// </summary>
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Origin);
            serializer.SerializeValue(ref Direction);
            serializer.SerializeValue(ref InitialVelocity);
            serializer.SerializeValue(ref FireTime);
            serializer.SerializeValue(ref ShooterClientId);
        }

        #endregion
    }
}
