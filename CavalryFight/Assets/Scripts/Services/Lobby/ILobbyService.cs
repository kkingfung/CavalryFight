#nullable enable

using System;
using System.Collections.Generic;
using CavalryFight.Core.Services;

namespace CavalryFight.Services.Lobby
{
    /// <summary>
    /// ロビーサービスのインターフェース
    /// </summary>
    /// <remarks>
    /// マルチプレイヤーロビーの作成、参加、管理機能を提供します。
    /// Unity Netcode for GameObjectsとUnity Multiplayer Servicesを使用します。
    /// </remarks>
    public interface ILobbyService : IService
    {
        #region Events

        /// <summary>
        /// ルームが作成された時に発生します
        /// </summary>
        event Action<string>? RoomCreated; // joinCode

        /// <summary>
        /// ルームに参加した時に発生します
        /// </summary>
        event Action? RoomJoined;

        /// <summary>
        /// ルームから退出した時に発生します
        /// </summary>
        event Action? RoomLeft;

        /// <summary>
        /// ルーム設定が変更された時に発生します
        /// </summary>
        event Action<RoomSettings>? RoomSettingsChanged;

        /// <summary>
        /// プレイヤーがルームに参加した時に発生します
        /// </summary>
        event Action<ulong>? PlayerJoined; // playerId

        /// <summary>
        /// プレイヤーがルームから退出した時に発生します
        /// </summary>
        event Action<ulong>? PlayerLeft; // playerId

        /// <summary>
        /// プレイヤーの準備状態が変更された時に発生します
        /// </summary>
        event Action<ulong, bool>? PlayerReadyChanged; // playerId, isReady

        /// <summary>
        /// マッチが開始される時に発生します
        /// </summary>
        event Action? MatchStarting;

        /// <summary>
        /// エラーが発生した時に発生します
        /// </summary>
        event Action<string>? ErrorOccurred;

        #endregion

        #region Properties

        /// <summary>
        /// 現在のルーム設定を取得します
        /// </summary>
        RoomSettings CurrentRoomSettings { get; }

        /// <summary>
        /// 現在のプレイヤースロットリストを取得します
        /// </summary>
        IReadOnlyList<PlayerSlot> PlayerSlots { get; }

        /// <summary>
        /// ローカルプレイヤー情報を取得します
        /// </summary>
        LobbyPlayerInfo? LocalPlayerInfo { get; }

        /// <summary>
        /// ホストかどうかを取得します
        /// </summary>
        bool IsHost { get; }

        /// <summary>
        /// ルームに参加しているかどうかを取得します
        /// </summary>
        bool IsInRoom { get; }

        /// <summary>
        /// 現在のジョインコードを取得します
        /// </summary>
        string? CurrentJoinCode { get; }

        #endregion

        #region Host Methods

        /// <summary>
        /// ルームを作成します（ホスト）
        /// </summary>
        /// <param name="roomSettings">ルーム設定</param>
        /// <param name="playerName">プレイヤー名</param>
        /// <returns>成功した場合はtrue</returns>
        bool CreateRoom(RoomSettings roomSettings, string playerName);

        /// <summary>
        /// ルーム設定を変更します（ホストのみ）
        /// </summary>
        /// <param name="roomSettings">新しいルーム設定</param>
        /// <returns>成功した場合はtrue</returns>
        bool UpdateRoomSettings(RoomSettings roomSettings);

        /// <summary>
        /// CPUプレイヤーを追加します（ホストのみ）
        /// </summary>
        /// <param name="difficulty">AI難易度</param>
        /// <param name="teamIndex">チームインデックス（-1の場合は未割り当て）</param>
        /// <returns>成功した場合はtrue</returns>
        bool AddCPUPlayer(AIDifficulty difficulty, int teamIndex = -1);

        /// <summary>
        /// CPUプレイヤーを削除します（ホストのみ）
        /// </summary>
        /// <param name="slotIndex">スロットインデックス</param>
        /// <returns>成功した場合はtrue</returns>
        bool RemoveCPUPlayer(int slotIndex);

        /// <summary>
        /// CPU難易度を変更します（ホストのみ）
        /// </summary>
        /// <param name="slotIndex">スロットインデックス</param>
        /// <param name="difficulty">新しい難易度</param>
        /// <returns>成功した場合はtrue</returns>
        bool ChangeCPUDifficulty(int slotIndex, AIDifficulty difficulty);

        /// <summary>
        /// プレイヤーをキックします（ホストのみ）
        /// </summary>
        /// <param name="playerId">プレイヤーID</param>
        /// <returns>成功した場合はtrue</returns>
        bool KickPlayer(ulong playerId);

        /// <summary>
        /// マッチを開始します（ホストのみ）
        /// </summary>
        /// <returns>成功した場合はtrue</returns>
        bool StartMatch();

        #endregion

        #region Guest Methods

        /// <summary>
        /// ジョインコードを使用してルームに参加します（ゲスト）
        /// </summary>
        /// <param name="joinCode">ジョインコード</param>
        /// <param name="playerName">プレイヤー名</param>
        /// <param name="password">パスワード（必要な場合）</param>
        /// <returns>成功した場合はtrue</returns>
        bool JoinRoom(string joinCode, string playerName, string password = "");

        #endregion

        #region Common Methods

        /// <summary>
        /// ルームから退出します
        /// </summary>
        void LeaveRoom();

        /// <summary>
        /// 準備状態を切り替えます
        /// </summary>
        /// <param name="isReady">準備完了かどうか</param>
        void SetReady(bool isReady);

        /// <summary>
        /// カスタマイズプリセットを設定します
        /// </summary>
        /// <param name="presetName">プリセット名</param>
        void SetCustomizationPreset(string presetName);

        /// <summary>
        /// プレイヤースロット情報を取得します
        /// </summary>
        /// <param name="slotIndex">スロットインデックス</param>
        /// <returns>プレイヤースロット</returns>
        PlayerSlot? GetPlayerSlot(int slotIndex);

        /// <summary>
        /// プレイヤーIDからスロットインデックスを取得します
        /// </summary>
        /// <param name="playerId">プレイヤーID</param>
        /// <returns>スロットインデックス（見つからない場合は-1）</returns>
        int GetSlotIndexByPlayerId(ulong playerId);

        #endregion
    }
}
