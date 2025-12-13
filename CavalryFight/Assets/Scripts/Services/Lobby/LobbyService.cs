#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace CavalryFight.Services.Lobby
{
    /// <summary>
    /// ロビーサービスの実装
    /// </summary>
    /// <remarks>
    /// マルチプレイヤーロビーの管理を行います。
    /// NetworkLobbyManagerと連携して動作します。
    /// </remarks>
    public class LobbyService : ILobbyService
    {
        #region Fields

        /// <summary>
        /// 初期化済みフラグ
        /// </summary>
        private bool _initialized = false;

        /// <summary>
        /// 現在のルーム設定
        /// </summary>
        private RoomSettings _currentRoomSettings;

        /// <summary>
        /// プレイヤースロットリスト
        /// </summary>
        private List<PlayerSlot> _playerSlots = new List<PlayerSlot>();

        /// <summary>
        /// ローカルプレイヤー情報
        /// </summary>
        private LobbyPlayerInfo? _localPlayerInfo;

        /// <summary>
        /// Relayマネージャー
        /// </summary>
        private RelayManager _relayManager = new RelayManager();

        /// <summary>
        /// ルームに参加しているかどうか
        /// </summary>
        private bool _isInRoom = false;

        #endregion

        #region Events

        public event Action<string>? RoomCreated;
        public event Action? RoomJoined;
        public event Action? RoomLeft;
        public event Action<RoomSettings>? RoomSettingsChanged;
        public event Action<ulong>? PlayerJoined;
        public event Action<ulong>? PlayerLeft;
        public event Action<ulong, bool>? PlayerReadyChanged;
        public event Action? MatchStarting;
        public event Action<string>? ErrorOccurred;

        #endregion

        #region Properties

        public RoomSettings CurrentRoomSettings => _currentRoomSettings;
        public IReadOnlyList<PlayerSlot> PlayerSlots => _playerSlots.AsReadOnly();
        public LobbyPlayerInfo? LocalPlayerInfo => _localPlayerInfo;
        public bool IsHost => NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost;
        public bool IsInRoom => _isInRoom;
        public string? CurrentJoinCode => _relayManager.CurrentJoinCode;

        #endregion

        #region IService Implementation

        /// <summary>
        /// サービスを初期化します
        /// </summary>
        public void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            Debug.Log("[LobbyService] Initializing...");

            // Initialize relay manager (async operation will happen when creating/joining room)
            _ = _relayManager.InitializeAsync();

            // プレイヤースロットを初期化（最大8スロット）
            _playerSlots = new List<PlayerSlot>();
            for (int i = 0; i < 8; i++)
            {
                _playerSlots.Add(new PlayerSlot(i));
            }

            _initialized = true;
            Debug.Log("[LobbyService] Initialization complete.");
        }

        public void Dispose()
        {
            Debug.Log("[LobbyService] Disposing...");

            LeaveRoom();

            // イベントハンドラをクリア
            RoomCreated = null;
            RoomJoined = null;
            RoomLeft = null;
            RoomSettingsChanged = null;
            PlayerJoined = null;
            PlayerLeft = null;
            PlayerReadyChanged = null;
            MatchStarting = null;
            ErrorOccurred = null;

            _initialized = false;
        }

        #endregion

        #region Host Methods

        public bool CreateRoom(RoomSettings roomSettings, string playerName)
        {
            if (!_initialized)
            {
                Debug.LogError("[LobbyService] Service not initialized.");
                return false;
            }

            if (_isInRoom)
            {
                Debug.LogError("[LobbyService] Already in a room.");
                return false;
            }

            try
            {
                _currentRoomSettings = roomSettings;

                // Relayを開始してジョインコードを取得（非同期）
                _ = StartHostRelayAsync(playerName);

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LobbyService] Failed to create room: {ex.Message}");
                ErrorOccurred?.Invoke($"Failed to create room: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Relayホストを非同期で開始します
        /// </summary>
        /// <param name="playerName">プレイヤー名</param>
        private async System.Threading.Tasks.Task StartHostRelayAsync(string playerName)
        {
            string? joinCode = await _relayManager.StartHostAsync();

            if (joinCode == null)
            {
                ErrorOccurred?.Invoke("Failed to create relay allocation.");
                return;
            }

            // NetworkManagerを開始
            if (!NetworkManager.Singleton.StartHost())
            {
                ErrorOccurred?.Invoke("Failed to start host.");
                return;
            }

            _isInRoom = true;

            // ローカルプレイヤー情報を設定
            _localPlayerInfo = new LobbyPlayerInfo(
                NetworkManager.Singleton.LocalClientId,
                playerName,
                true
            );
            _localPlayerInfo.IsLocalPlayer = true;

            // 最初のスロットにホストを配置
            _playerSlots[0] = new PlayerSlot(0, _localPlayerInfo.PlayerId, playerName);

            RoomCreated?.Invoke(joinCode);
            Debug.Log($"[LobbyService] Room created with join code: {joinCode}");
        }

        public bool UpdateRoomSettings(RoomSettings roomSettings)
        {
            if (!IsHost)
            {
                Debug.LogError("[LobbyService] Only host can update room settings.");
                return false;
            }

            _currentRoomSettings = roomSettings;
            RoomSettingsChanged?.Invoke(_currentRoomSettings);

            Debug.Log("[LobbyService] Room settings updated.");
            return true;
        }

        public bool AddCPUPlayer(AIDifficulty difficulty, int teamIndex = -1)
        {
            if (!IsHost)
            {
                Debug.LogError("[LobbyService] Only host can add CPU players.");
                return false;
            }

            // 空きスロットを探す
            int emptySlotIndex = -1;
            for (int i = 0; i < _playerSlots.Count; i++)
            {
                if (_playerSlots[i].IsEmpty())
                {
                    emptySlotIndex = i;
                    break;
                }
            }

            if (emptySlotIndex == -1)
            {
                Debug.LogError("[LobbyService] No empty slots available.");
                return false;
            }

            // CPUプレイヤーを追加（負のインデックスを使用）
            int aiIndex = -(CountCPUPlayers() + 1);
            PlayerSlot cpuSlot = new PlayerSlot(emptySlotIndex, aiIndex, difficulty);
            cpuSlot.TeamIndex = teamIndex;

            _playerSlots[emptySlotIndex] = cpuSlot;

            Debug.Log($"[LobbyService] CPU player added at slot {emptySlotIndex} with difficulty {difficulty}");
            return true;
        }

        public bool RemoveCPUPlayer(int slotIndex)
        {
            if (!IsHost)
            {
                Debug.LogError("[LobbyService] Only host can remove CPU players.");
                return false;
            }

            if (slotIndex < 0 || slotIndex >= _playerSlots.Count)
            {
                Debug.LogError($"[LobbyService] Invalid slot index: {slotIndex}");
                return false;
            }

            if (!_playerSlots[slotIndex].IsAI)
            {
                Debug.LogError($"[LobbyService] Slot {slotIndex} is not a CPU player.");
                return false;
            }

            _playerSlots[slotIndex].Clear();

            Debug.Log($"[LobbyService] CPU player removed from slot {slotIndex}");
            return true;
        }

        public bool ChangeCPUDifficulty(int slotIndex, AIDifficulty difficulty)
        {
            if (!IsHost)
            {
                Debug.LogError("[LobbyService] Only host can change CPU difficulty.");
                return false;
            }

            if (slotIndex < 0 || slotIndex >= _playerSlots.Count)
            {
                Debug.LogError($"[LobbyService] Invalid slot index: {slotIndex}");
                return false;
            }

            if (!_playerSlots[slotIndex].IsAI)
            {
                Debug.LogError($"[LobbyService] Slot {slotIndex} is not a CPU player.");
                return false;
            }

            var slot = _playerSlots[slotIndex];
            slot.AIDifficulty = difficulty;
            _playerSlots[slotIndex] = slot;

            Debug.Log($"[LobbyService] CPU difficulty changed to {difficulty} for slot {slotIndex}");
            return true;
        }

        public bool KickPlayer(ulong playerId)
        {
            if (!IsHost)
            {
                Debug.LogError("[LobbyService] Only host can kick players.");
                return false;
            }

            if (playerId == NetworkManager.Singleton.LocalClientId)
            {
                Debug.LogError("[LobbyService] Cannot kick yourself.");
                return false;
            }

            NetworkManager.Singleton.DisconnectClient(playerId);

            Debug.Log($"[LobbyService] Player {playerId} kicked.");
            return true;
        }

        public bool StartMatch()
        {
            if (!IsHost)
            {
                Debug.LogError("[LobbyService] Only host can start the match.");
                return false;
            }

            // すべての人間プレイヤーが準備完了か確認
            bool allReady = _playerSlots
                .Where(s => !s.IsEmpty() && !s.IsAI)
                .All(s => s.IsReady);

            if (!allReady)
            {
                Debug.LogError("[LobbyService] Not all players are ready.");
                ErrorOccurred?.Invoke("Not all players are ready.");
                return false;
            }

            MatchStarting?.Invoke();

            Debug.Log("[LobbyService] Match starting...");
            // TODO: Load match scene
            return true;
        }

        #endregion

        #region Guest Methods

        public bool JoinRoom(string joinCode, string playerName, string password = "")
        {
            if (!_initialized)
            {
                Debug.LogError("[LobbyService] Service not initialized.");
                return false;
            }

            if (_isInRoom)
            {
                Debug.LogError("[LobbyService] Already in a room.");
                return false;
            }

            // Relayに参加（非同期）
            _ = JoinRelayAsync(joinCode, playerName, password);

            return true;
        }

        /// <summary>
        /// Relayに非同期で参加します
        /// </summary>
        /// <param name="joinCode">ジョインコード</param>
        /// <param name="playerName">プレイヤー名</param>
        /// <param name="password">パスワード</param>
        private async System.Threading.Tasks.Task JoinRelayAsync(string joinCode, string playerName, string password)
        {
            bool relayJoined = await _relayManager.JoinRelayAsync(joinCode);

            if (!relayJoined)
            {
                ErrorOccurred?.Invoke("Failed to join relay.");
                return;
            }

            // NetworkManagerを開始
            if (!NetworkManager.Singleton.StartClient())
            {
                ErrorOccurred?.Invoke("Failed to start client.");
                return;
            }

            _isInRoom = true;

            // ローカルプレイヤー情報を設定
            _localPlayerInfo = new LobbyPlayerInfo(
                NetworkManager.Singleton.LocalClientId,
                playerName,
                false
            );
            _localPlayerInfo.IsLocalPlayer = true;

            RoomJoined?.Invoke();
            Debug.Log($"[LobbyService] Joined room with code: {joinCode}");
        }

        #endregion

        #region Common Methods

        public void LeaveRoom()
        {
            if (!_isInRoom)
            {
                return;
            }

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                NetworkManager.Singleton.Shutdown();
            }

            _relayManager.Cleanup();

            _isInRoom = false;
            _localPlayerInfo = null;

            // スロットをクリア
            for (int i = 0; i < _playerSlots.Count; i++)
            {
                _playerSlots[i].Clear();
            }

            RoomLeft?.Invoke();
            Debug.Log("[LobbyService] Left room.");
        }

        public void SetReady(bool isReady)
        {
            if (_localPlayerInfo == null)
            {
                Debug.LogError("[LobbyService] No local player info.");
                return;
            }

            int slotIndex = GetSlotIndexByPlayerId(_localPlayerInfo.PlayerId);
            if (slotIndex == -1)
            {
                Debug.LogError("[LobbyService] Local player not found in slots.");
                return;
            }

            var slot = _playerSlots[slotIndex];
            slot.IsReady = isReady;
            _playerSlots[slotIndex] = slot;

            PlayerReadyChanged?.Invoke(_localPlayerInfo.PlayerId, isReady);

            Debug.Log($"[LobbyService] Ready status set to: {isReady}");
        }

        public void SetCustomizationPreset(string presetName)
        {
            if (_localPlayerInfo == null)
            {
                Debug.LogError("[LobbyService] No local player info.");
                return;
            }

            _localPlayerInfo.CustomizationPresetName = presetName;

            int slotIndex = GetSlotIndexByPlayerId(_localPlayerInfo.PlayerId);
            if (slotIndex != -1)
            {
                var slot = _playerSlots[slotIndex];
                slot.CustomizationPresetName = presetName;
                _playerSlots[slotIndex] = slot;
            }

            Debug.Log($"[LobbyService] Customization preset set to: {presetName}");
        }

        public PlayerSlot? GetPlayerSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _playerSlots.Count)
            {
                return null;
            }

            return _playerSlots[slotIndex];
        }

        public int GetSlotIndexByPlayerId(ulong playerId)
        {
            for (int i = 0; i < _playerSlots.Count; i++)
            {
                if (_playerSlots[i].PlayerId == playerId)
                {
                    return i;
                }
            }

            return -1;
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// CPUプレイヤーの数をカウントします
        /// </summary>
        /// <returns>CPUプレイヤーの数</returns>
        private int CountCPUPlayers()
        {
            return _playerSlots.Count(s => s.IsAI);
        }

        #endregion
    }
}
