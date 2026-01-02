#nullable enable

using System;

namespace CavalryFight.ViewModels.Data
{
    /// <summary>
    /// プレイヤー死亡イベント引数
    /// </summary>
    public class PlayerKilledEventArgs : EventArgs
    {
        /// <summary>
        /// 被害者のクライアントID
        /// </summary>
        public ulong VictimId { get; }

        /// <summary>
        /// キラーのクライアントID
        /// </summary>
        public ulong KillerId { get; }

        /// <summary>
        /// 被害者の名前
        /// </summary>
        public string VictimName { get; }

        /// <summary>
        /// キラーの名前
        /// </summary>
        public string KillerName { get; }

        public PlayerKilledEventArgs(ulong victimId, ulong killerId, string victimName = "", string killerName = "")
        {
            VictimId = victimId;
            KillerId = killerId;
            VictimName = string.IsNullOrEmpty(victimName) ? $"Player {victimId}" : victimName;
            KillerName = string.IsNullOrEmpty(killerName) ? $"Player {killerId}" : killerName;
        }
    }
}
