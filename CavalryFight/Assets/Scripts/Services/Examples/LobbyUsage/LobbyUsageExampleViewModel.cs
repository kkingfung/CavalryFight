#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using CavalryFight.Core.Services;
using CavalryFight.Services.Lobby;
using Unity.Collections;
using UnityEngine;

namespace CavalryFight.Examples.LobbyUsage
{
    /// <summary>
    /// ロビーサービス使用例のViewModel
    /// </summary>
    /// <remarks>
    /// マルチプレイヤーロビーシステムの使い方を示すサンプルコードです。
    /// ホスト側とゲスト側の両方の操作例を含みます。
    ///
    /// セットアップ手順:
    /// 1. NetworkLobbyManagerプレハブを作成してシーンに配置
    /// 2. NetworkLobbyManagerにNetworkRoomDataコンポーネントを追加
    /// 3. NetworkManagerをシーンに配置してUnityTransportを設定
    /// 4. ServiceLocatorにLobbyServiceを登録
    /// 5. LobbyService.SetNetworkLobbyManager()でNetworkLobbyManagerを設定
    /// </remarks>
    public class LobbyUsageExampleViewModel
    {
        #region Fields

        /// <summary>
        /// ロビーサービス
        /// </summary>
        private ILobbyService? _lobbyService;

        /// <summary>
        /// プレイヤー名
        /// </summary>
        private string _playerName = "Player";

        /// <summary>
        /// 最後に作成されたジョインコード
        /// </summary>
        private string? _lastJoinCode;

        #endregion

        #region Initialization

        /// <summary>
        /// 初期化
        /// </summary>
        public void Initialize()
        {
            // ServiceLocatorからLobbyServiceを取得
            _lobbyService = ServiceLocator.Instance.Get<ILobbyService>();

            if (_lobbyService == null)
            {
                Debug.LogError("[LobbyUsageExample] LobbyService not registered in ServiceLocator!");
                return;
            }

            // イベントを購読
            SubscribeToEvents();

            Debug.Log("[LobbyUsageExample] Initialized.");
        }

        /// <summary>
        /// クリーンアップ
        /// </summary>
        public void Dispose()
        {
            // イベントを購読解除
            UnsubscribeFromEvents();

            Debug.Log("[LobbyUsageExample] Disposed.");
        }

        #endregion

        #region Event Subscription

        /// <summary>
        /// イベントを購読
        /// </summary>
        private void SubscribeToEvents()
        {
            if (_lobbyService == null) return;

            _lobbyService.RoomCreated += OnRoomCreated;
            _lobbyService.RoomJoined += OnRoomJoined;
            _lobbyService.RoomLeft += OnRoomLeft;
            _lobbyService.RoomSettingsChanged += OnRoomSettingsChanged;
            _lobbyService.PlayerJoined += OnPlayerJoined;
            _lobbyService.PlayerLeft += OnPlayerLeft;
            _lobbyService.PlayerReadyChanged += OnPlayerReadyChanged;
            _lobbyService.MatchStarting += OnMatchStarting;
            _lobbyService.ErrorOccurred += OnErrorOccurred;
        }

        /// <summary>
        /// イベントを購読解除
        /// </summary>
        private void UnsubscribeFromEvents()
        {
            if (_lobbyService == null) return;

            _lobbyService.RoomCreated -= OnRoomCreated;
            _lobbyService.RoomJoined -= OnRoomJoined;
            _lobbyService.RoomLeft -= OnRoomLeft;
            _lobbyService.RoomSettingsChanged -= OnRoomSettingsChanged;
            _lobbyService.PlayerJoined -= OnPlayerJoined;
            _lobbyService.PlayerLeft -= OnPlayerLeft;
            _lobbyService.PlayerReadyChanged -= OnPlayerReadyChanged;
            _lobbyService.MatchStarting -= OnMatchStarting;
            _lobbyService.ErrorOccurred -= OnErrorOccurred;
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// ルーム作成イベントハンドラ
        /// </summary>
        /// <param name="joinCode">ジョインコード</param>
        private void OnRoomCreated(string joinCode)
        {
            _lastJoinCode = joinCode;
            Debug.Log($"[LobbyUsageExample] Room created! Join Code: {joinCode}");
            Debug.Log($"[LobbyUsageExample] Share this code with other players to join your room.");
        }

        /// <summary>
        /// ルーム参加イベントハンドラ
        /// </summary>
        private void OnRoomJoined()
        {
            Debug.Log("[LobbyUsageExample] Successfully joined room!");
            PrintPlayerSlots();
        }

        /// <summary>
        /// ルーム退出イベントハンドラ
        /// </summary>
        private void OnRoomLeft()
        {
            Debug.Log("[LobbyUsageExample] Left the room.");
            _lastJoinCode = null;
        }

        /// <summary>
        /// ルーム設定変更イベントハンドラ
        /// </summary>
        /// <param name="roomSettings">新しいルーム設定</param>
        private void OnRoomSettingsChanged(RoomSettings roomSettings)
        {
            Debug.Log($"[LobbyUsageExample] Room settings changed:");
            Debug.Log($"  - Room Name: {roomSettings.RoomName}");
            Debug.Log($"  - Game Mode: {roomSettings.GameMode}");
            Debug.Log($"  - Max Players: {roomSettings.MaxPlayers}");
            Debug.Log($"  - Time Limit: {roomSettings.TimeLimit}s");
            Debug.Log($"  - Map: {roomSettings.MapName}");
        }

        /// <summary>
        /// プレイヤー参加イベントハンドラ
        /// </summary>
        /// <param name="playerId">プレイヤーID</param>
        private void OnPlayerJoined(ulong playerId)
        {
            Debug.Log($"[LobbyUsageExample] Player {playerId} joined the room.");
            PrintPlayerSlots();
        }

        /// <summary>
        /// プレイヤー退出イベントハンドラ
        /// </summary>
        /// <param name="playerId">プレイヤーID</param>
        private void OnPlayerLeft(ulong playerId)
        {
            Debug.Log($"[LobbyUsageExample] Player {playerId} left the room.");
            PrintPlayerSlots();
        }

        /// <summary>
        /// プレイヤー準備状態変更イベントハンドラ
        /// </summary>
        /// <param name="playerId">プレイヤーID</param>
        /// <param name="isReady">準備完了かどうか</param>
        private void OnPlayerReadyChanged(ulong playerId, bool isReady)
        {
            Debug.Log($"[LobbyUsageExample] Player {playerId} ready status: {isReady}");
        }

        /// <summary>
        /// マッチ開始イベントハンドラ
        /// </summary>
        private void OnMatchStarting()
        {
            Debug.Log("[LobbyUsageExample] Match is starting!");
            Debug.Log("[LobbyUsageExample] TODO: Load match scene here");
        }

        /// <summary>
        /// エラー発生イベントハンドラ
        /// </summary>
        /// <param name="errorMessage">エラーメッセージ</param>
        private void OnErrorOccurred(string errorMessage)
        {
            Debug.LogError($"[LobbyUsageExample] Error: {errorMessage}");
        }

        #endregion

        #region Host Operations

        /// <summary>
        /// ルームを作成する（ホスト）
        /// </summary>
        public void CreateRoom()
        {
            if (_lobbyService == null)
            {
                Debug.LogError("[LobbyUsageExample] LobbyService not available.");
                return;
            }

            // デフォルト設定でルームを作成
            var roomSettings = new RoomSettings
            {
                RoomName = new FixedString64Bytes("My Cavalry Fight Room"),
                GameMode = GameMode.Arena,
                MaxPlayers = 8,
                Password = new FixedString64Bytes(), // パスワードなし
                IsPublic = false, // プライベートルーム（招待制）
                TimeLimit = 300, // 5分
                ScoreGoal = 100,
                MapName = new FixedString64Bytes("DefaultArena")
            };

            bool success = _lobbyService.CreateRoom(roomSettings, _playerName);

            if (success)
            {
                Debug.Log("[LobbyUsageExample] Creating room...");
            }
            else
            {
                Debug.LogError("[LobbyUsageExample] Failed to create room.");
            }
        }

        /// <summary>
        /// ルーム設定を変更する（ホストのみ）
        /// </summary>
        public void ChangeRoomSettings()
        {
            if (_lobbyService == null || !_lobbyService.IsHost)
            {
                Debug.LogError("[LobbyUsageExample] Only host can change room settings.");
                return;
            }

            var newSettings = _lobbyService.CurrentRoomSettings;
            newSettings.GameMode = GameMode.TeamFight;
            newSettings.TimeLimit = 600; // 10分に変更
            newSettings.MaxPlayers = 6;

            bool success = _lobbyService.UpdateRoomSettings(newSettings);

            if (success)
            {
                Debug.Log("[LobbyUsageExample] Room settings updated.");
            }
        }

        /// <summary>
        /// CPUプレイヤーを追加する（ホストのみ）
        /// </summary>
        public void AddCPUPlayer()
        {
            if (_lobbyService == null || !_lobbyService.IsHost)
            {
                Debug.LogError("[LobbyUsageExample] Only host can add CPU players.");
                return;
            }

            bool success = _lobbyService.AddCPUPlayer(AIDifficulty.Normal, -1);

            if (success)
            {
                Debug.Log("[LobbyUsageExample] CPU player added.");
                PrintPlayerSlots();
            }
        }

        /// <summary>
        /// CPUプレイヤーを削除する（ホストのみ）
        /// </summary>
        /// <param name="slotIndex">スロットインデックス</param>
        public void RemoveCPUPlayer(int slotIndex)
        {
            if (_lobbyService == null || !_lobbyService.IsHost)
            {
                Debug.LogError("[LobbyUsageExample] Only host can remove CPU players.");
                return;
            }

            bool success = _lobbyService.RemoveCPUPlayer(slotIndex);

            if (success)
            {
                Debug.Log($"[LobbyUsageExample] CPU player removed from slot {slotIndex}.");
                PrintPlayerSlots();
            }
        }

        /// <summary>
        /// CPU難易度を変更する（ホストのみ）
        /// </summary>
        /// <param name="slotIndex">スロットインデックス</param>
        public void ChangeCPUDifficulty(int slotIndex)
        {
            if (_lobbyService == null || !_lobbyService.IsHost)
            {
                Debug.LogError("[LobbyUsageExample] Only host can change CPU difficulty.");
                return;
            }

            bool success = _lobbyService.ChangeCPUDifficulty(slotIndex, AIDifficulty.Hard);

            if (success)
            {
                Debug.Log($"[LobbyUsageExample] CPU difficulty changed for slot {slotIndex}.");
            }
        }

        /// <summary>
        /// プレイヤーをキックする（ホストのみ）
        /// </summary>
        /// <param name="playerId">プレイヤーID</param>
        public void KickPlayer(ulong playerId)
        {
            if (_lobbyService == null || !_lobbyService.IsHost)
            {
                Debug.LogError("[LobbyUsageExample] Only host can kick players.");
                return;
            }

            bool success = _lobbyService.KickPlayer(playerId);

            if (success)
            {
                Debug.Log($"[LobbyUsageExample] Player {playerId} kicked.");
            }
        }

        /// <summary>
        /// マッチを開始する（ホストのみ）
        /// </summary>
        public void StartMatch()
        {
            if (_lobbyService == null || !_lobbyService.IsHost)
            {
                Debug.LogError("[LobbyUsageExample] Only host can start the match.");
                return;
            }

            bool success = _lobbyService.StartMatch();

            if (!success)
            {
                Debug.LogError("[LobbyUsageExample] Failed to start match. Not all players are ready.");
            }
        }

        #endregion

        #region Guest Operations

        /// <summary>
        /// ジョインコードを使ってルームに参加する（ゲスト）
        /// </summary>
        /// <param name="joinCode">ジョインコード</param>
        public void JoinRoom(string joinCode)
        {
            if (_lobbyService == null)
            {
                Debug.LogError("[LobbyUsageExample] LobbyService not available.");
                return;
            }

            bool success = _lobbyService.JoinRoom(joinCode, _playerName);

            if (success)
            {
                Debug.Log($"[LobbyUsageExample] Joining room with code: {joinCode}...");
            }
            else
            {
                Debug.LogError("[LobbyUsageExample] Failed to join room.");
            }
        }

        #endregion

        #region Common Operations

        /// <summary>
        /// ルームから退出する
        /// </summary>
        public void LeaveRoom()
        {
            if (_lobbyService == null)
            {
                Debug.LogError("[LobbyUsageExample] LobbyService not available.");
                return;
            }

            _lobbyService.LeaveRoom();
            Debug.Log("[LobbyUsageExample] Leaving room...");
        }

        /// <summary>
        /// 準備状態を切り替える
        /// </summary>
        public void ToggleReady()
        {
            if (_lobbyService == null)
            {
                Debug.LogError("[LobbyUsageExample] LobbyService not available.");
                return;
            }

            if (_lobbyService.LocalPlayerInfo == null)
            {
                Debug.LogError("[LobbyUsageExample] Not in a room.");
                return;
            }

            // 現在のスロット情報を取得して準備状態を反転
            int slotIndex = _lobbyService.GetSlotIndexByPlayerId(_lobbyService.LocalPlayerInfo.PlayerId);
            if (slotIndex != -1)
            {
                var slot = _lobbyService.GetPlayerSlot(slotIndex);
                if (slot != null)
                {
                    bool newReadyState = !slot.Value.IsReady;
                    _lobbyService.SetReady(newReadyState);
                    Debug.Log($"[LobbyUsageExample] Ready status set to: {newReadyState}");
                }
            }
        }

        /// <summary>
        /// カスタマイズプリセットを設定する
        /// </summary>
        /// <param name="presetName">プリセット名</param>
        public void SetCustomization(string presetName)
        {
            if (_lobbyService == null)
            {
                Debug.LogError("[LobbyUsageExample] LobbyService not available.");
                return;
            }

            _lobbyService.SetCustomizationPreset(presetName);
            Debug.Log($"[LobbyUsageExample] Customization preset set to: {presetName}");
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// プレイヤースロット情報を表示する
        /// </summary>
        public void PrintPlayerSlots()
        {
            if (_lobbyService == null)
            {
                Debug.LogError("[LobbyUsageExample] LobbyService not available.");
                return;
            }

            Debug.Log("=== Current Player Slots ===");

            var slots = _lobbyService.PlayerSlots;
            for (int i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];

                if (slot.IsEmpty())
                {
                    Debug.Log($"  Slot {i}: [EMPTY]");
                }
                else if (slot.IsAI)
                {
                    Debug.Log($"  Slot {i}: [CPU] Difficulty: {slot.AIDifficulty}, Team: {GetTeamName(slot.TeamIndex)}, Ready: {slot.IsReady}");
                }
                else
                {
                    Debug.Log($"  Slot {i}: {slot.PlayerName} (ID: {slot.PlayerId}), Team: {GetTeamName(slot.TeamIndex)}, Ready: {slot.IsReady}");
                }
            }

            Debug.Log("============================");
        }

        /// <summary>
        /// チームインデックスからチーム名を取得する
        /// </summary>
        /// <param name="teamIndex">チームインデックス</param>
        /// <returns>チーム名</returns>
        private string GetTeamName(int teamIndex)
        {
            return teamIndex switch
            {
                -1 => "Unassigned",
                0 => "Team A",
                1 => "Team B",
                _ => $"Team {teamIndex}"
            };
        }

        /// <summary>
        /// ルーム情報を表示する
        /// </summary>
        public void PrintRoomInfo()
        {
            if (_lobbyService == null)
            {
                Debug.LogError("[LobbyUsageExample] LobbyService not available.");
                return;
            }

            if (!_lobbyService.IsInRoom)
            {
                Debug.Log("[LobbyUsageExample] Not in a room.");
                return;
            }

            Debug.Log("=== Room Information ===");
            var settings = _lobbyService.CurrentRoomSettings;
            Debug.Log($"  Room Name: {settings.RoomName}");
            Debug.Log($"  Game Mode: {settings.GameMode}");
            Debug.Log($"  Max Players: {settings.MaxPlayers}");
            Debug.Log($"  Is Public: {settings.IsPublic}");
            Debug.Log($"  Has Password: {settings.HasPassword()}");
            Debug.Log($"  Time Limit: {settings.TimeLimit}s");
            Debug.Log($"  Score Goal: {settings.ScoreGoal}");
            Debug.Log($"  Map: {settings.MapName}");
            Debug.Log($"  Join Code: {_lobbyService.CurrentJoinCode ?? "N/A"}");
            Debug.Log($"  You are: {(_lobbyService.IsHost ? "HOST" : "GUEST")}");
            Debug.Log("========================");

            PrintPlayerSlots();
        }

        #endregion

        #region Example Usage Scenarios

        /// <summary>
        /// ホストとして完全なセッションを実行する例
        /// </summary>
        public void ExampleHostSession()
        {
            Debug.Log("=== Example: Host Session ===");

            // 1. ルームを作成
            CreateRoom();

            // 2. (ジョインコードが作成されたら、他のプレイヤーに共有)

            // 3. ルーム設定を変更（オプション）
            // ChangeRoomSettings();

            // 4. CPUプレイヤーを追加（オプション）
            // AddCPUPlayer();

            // 5. ゲストが参加するのを待つ...

            // 6. すべてのプレイヤーが準備完了になったらマッチを開始
            // StartMatch();
        }

        /// <summary>
        /// ゲストとして参加する例
        /// </summary>
        /// <param name="joinCode">ホストから受け取ったジョインコード</param>
        public void ExampleGuestSession(string joinCode)
        {
            Debug.Log("=== Example: Guest Session ===");

            // 1. ジョインコードでルームに参加
            JoinRoom(joinCode);

            // 2. カスタマイズを設定（オプション）
            // SetCustomization("MyPreset");

            // 3. 準備完了にする
            // ToggleReady();

            // 4. ホストがマッチを開始するのを待つ...
        }

        #endregion
    }
}
