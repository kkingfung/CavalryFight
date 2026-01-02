#nullable enable

using System;
using CavalryFight.Services.Lobby;
using Unity.Netcode;

namespace CavalryFight.Services.Match
{
    /// <summary>
    /// マッチ終了結果
    /// </summary>
    /// <remarks>
    /// マッチ終了時の情報を保持します。
    /// ViewModels層からアクセス可能なサービスデータ型です。
    /// </remarks>
    [Serializable]
    public struct MatchEndResult : INetworkSerializable
    {
        /// <summary>勝者のクライアントID（チームモードの場合はチームインデックス）</summary>
        public ulong WinnerId;

        /// <summary>マッチ時間（秒）</summary>
        public float MatchDuration;

        /// <summary>ゲームモード</summary>
        public GameMode GameMode;

        /// <summary>チームモードかどうか</summary>
        public bool IsTeamMode;

        /// <summary>
        /// ネットワークシリアライズ
        /// </summary>
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref WinnerId);
            serializer.SerializeValue(ref MatchDuration);
            serializer.SerializeValue(ref GameMode);
            serializer.SerializeValue(ref IsTeamMode);
        }
    }
}
