#nullable enable

using Unity.Collections;

namespace CavalryFight.Services.Lobby
{
    /// <summary>
    /// ルーム情報
    /// </summary>
    /// <remarks>
    /// 利用可能なルームの情報を保持します。
    /// </remarks>
    public class RoomInfo
    {
        /// <summary>
        /// ルームID（Unity Relay Allocation IDまたはルームの一意識別子）
        /// </summary>
        public string RoomId { get; set; } = "";

        /// <summary>
        /// ルーム名
        /// </summary>
        public string RoomName { get; set; } = "";

        /// <summary>
        /// ホスト名
        /// </summary>
        public string HostName { get; set; } = "";

        /// <summary>
        /// ゲームモード
        /// </summary>
        public GameMode GameMode { get; set; }

        /// <summary>
        /// マップ名
        /// </summary>
        public string MapName { get; set; } = "";

        /// <summary>
        /// 現在のプレイヤー数
        /// </summary>
        public int CurrentPlayers { get; set; }

        /// <summary>
        /// 最大プレイヤー数
        /// </summary>
        public int MaxPlayers { get; set; }

        /// <summary>
        /// パスワード保護されているかどうか
        /// </summary>
        public bool HasPassword { get; set; }

        /// <summary>
        /// ジョインコード
        /// </summary>
        public string JoinCode { get; set; } = "";

        /// <summary>
        /// 公開ルームかどうか
        /// </summary>
        public bool IsPublic { get; set; }

        /// <summary>
        /// ルームが満員かどうか
        /// </summary>
        public bool IsFull => CurrentPlayers >= MaxPlayers;
    }
}
