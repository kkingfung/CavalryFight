#nullable enable

using System;
using Unity.Collections;
using Unity.Netcode;

namespace CavalryFight.Services.Lobby
{
    /// <summary>
    /// ルーム設定
    /// </summary>
    /// <remarks>
    /// ゲームルームの設定情報を保持します。
    /// ホストのみが変更可能で、ゲストには読み取り専用です。
    /// </remarks>
    [Serializable]
    public struct RoomSettings : INetworkSerializable
    {
        #region Fields

        /// <summary>
        /// ルーム名
        /// </summary>
        public FixedString64Bytes RoomName;

        /// <summary>
        /// ゲームモード
        /// </summary>
        public GameMode GameMode;

        /// <summary>
        /// 最大プレイヤー数（人間 + CPU合計）
        /// </summary>
        /// <remarks>
        /// 最小: 2、最大: 8
        /// </remarks>
        public int MaxPlayers;

        /// <summary>
        /// パスワード（空の場合はパスワードなし）
        /// </summary>
        public FixedString64Bytes Password;

        /// <summary>
        /// 公開ルームかどうか
        /// </summary>
        /// <remarks>
        /// true: 公開ルーム（誰でも参加可能）
        /// false: プライベートルーム（招待制）
        /// </remarks>
        public bool IsPublic;

        /// <summary>
        /// マッチ制限時間（秒）
        /// </summary>
        /// <remarks>
        /// 0 = 無制限
        /// </remarks>
        public int TimeLimit;

        /// <summary>
        /// スコア目標
        /// </summary>
        /// <remarks>
        /// ゲームモードによって意味が異なります。
        /// ScoreMatch: 目標スコア
        /// Deathmatch: キル数
        /// </remarks>
        public int ScoreGoal;

        /// <summary>
        /// マップ名
        /// </summary>
        public FixedString64Bytes MapName;

        #endregion

        #region Constructors

        /// <summary>
        /// デフォルト設定を取得します
        /// </summary>
        /// <returns>デフォルト設定のRoomSettings</returns>
        public static RoomSettings CreateDefault()
        {
            return new RoomSettings
            {
                RoomName = new FixedString64Bytes("New Room"),
                GameMode = GameMode.Arena,
                MaxPlayers = 8,
                Password = new FixedString64Bytes(),
                IsPublic = false,
                TimeLimit = 300, // 5分
                ScoreGoal = 100,
                MapName = new FixedString64Bytes("DefaultArena")
            };
        }

        /// <summary>
        /// 指定された設定でRoomSettingsを初期化します
        /// </summary>
        /// <param name="roomName">ルーム名</param>
        /// <param name="gameMode">ゲームモード</param>
        /// <param name="maxPlayers">最大プレイヤー数</param>
        public RoomSettings(string roomName, GameMode gameMode, int maxPlayers)
        {
            RoomName = new FixedString64Bytes(roomName);
            GameMode = gameMode;
            MaxPlayers = UnityEngine.Mathf.Clamp(maxPlayers, 2, 8);
            Password = new FixedString64Bytes();
            IsPublic = false;
            TimeLimit = 300;
            ScoreGoal = 100;
            MapName = new FixedString64Bytes("DefaultArena");
        }

        #endregion

        #region INetworkSerializable Implementation

        /// <summary>
        /// ネットワークシリアライゼーション
        /// </summary>
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref RoomName);
            serializer.SerializeValue(ref GameMode);
            serializer.SerializeValue(ref MaxPlayers);
            serializer.SerializeValue(ref Password);
            serializer.SerializeValue(ref IsPublic);
            serializer.SerializeValue(ref TimeLimit);
            serializer.SerializeValue(ref ScoreGoal);
            serializer.SerializeValue(ref MapName);
        }

        #endregion

        #region Methods

        /// <summary>
        /// パスワードが設定されているかどうかを取得します
        /// </summary>
        /// <returns>パスワードが設定されている場合はtrue</returns>
        public readonly bool HasPassword()
        {
            return Password.Length > 0;
        }

        /// <summary>
        /// パスワードを検証します
        /// </summary>
        /// <param name="inputPassword">入力されたパスワード</param>
        /// <returns>パスワードが一致する場合はtrue</returns>
        public readonly bool ValidatePassword(string inputPassword)
        {
            if (!HasPassword())
            {
                return true; // パスワード設定なしの場合は常に成功
            }

            return Password.ToString() == inputPassword;
        }

        #endregion
    }
}
