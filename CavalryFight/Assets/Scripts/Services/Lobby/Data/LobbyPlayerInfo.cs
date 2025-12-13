#nullable enable

using System;

namespace CavalryFight.Services.Lobby
{
    /// <summary>
    /// ロビープレイヤー情報
    /// </summary>
    /// <remarks>
    /// プレイヤーのメタデータを保持します。
    /// ネットワーク同期されないローカルデータです。
    /// </remarks>
    [Serializable]
    public class LobbyPlayerInfo
    {
        #region Fields

        /// <summary>
        /// プレイヤーID
        /// </summary>
        public ulong PlayerId { get; set; }

        /// <summary>
        /// プレイヤー名
        /// </summary>
        public string PlayerName { get; set; } = "Player";

        /// <summary>
        /// ホストかどうか
        /// </summary>
        public bool IsHost { get; set; }

        /// <summary>
        /// ローカルプレイヤーかどうか
        /// </summary>
        public bool IsLocalPlayer { get; set; }

        /// <summary>
        /// カスタマイズプリセット名
        /// </summary>
        public string CustomizationPresetName { get; set; } = string.Empty;

        #endregion

        #region Constructors

        /// <summary>
        /// LobbyPlayerInfoの新しいインスタンスを初期化します
        /// </summary>
        public LobbyPlayerInfo()
        {
        }

        /// <summary>
        /// LobbyPlayerInfoの新しいインスタンスを初期化します
        /// </summary>
        /// <param name="playerId">プレイヤーID</param>
        /// <param name="playerName">プレイヤー名</param>
        /// <param name="isHost">ホストかどうか</param>
        public LobbyPlayerInfo(ulong playerId, string playerName, bool isHost)
        {
            PlayerId = playerId;
            PlayerName = playerName;
            IsHost = isHost;
            IsLocalPlayer = false;
        }

        #endregion
    }
}
