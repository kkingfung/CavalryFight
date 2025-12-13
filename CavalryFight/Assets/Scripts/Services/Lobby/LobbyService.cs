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

        /// <summary>
        /// NetworkLobbyManagerへの参照
        /// </summary>
        private NetworkLobbyManager? _networkLobbyManager;

        /// <summary>
        /// NetworkRoomDataへの参照
        /// </summary>
        private NetworkRoomData? _networkRoomData;

        #endregion

        #region Events

        /// <summary>
        /// ルームが作成された時に発生します
        /// </summary>
        public event Action<string>? RoomCreated;

        /// <summary>
        /// ルームに参加した時に発生します
        /// </summary>
        public event Action? RoomJoined;

        /// <summary>
        /// ルームから退出した時に発生します
        /// </summary>
        public event Action? RoomLeft;

        /// <summary>
        /// ルーム設定が変更された時に発生します
        /// </summary>
        public event Action<RoomSettings>? RoomSettingsChanged;

        /// <summary>
        /// プレイヤーがルームに参加した時に発生します
        /// </summary>
        public event Action<ulong>? PlayerJoined;

        /// <summary>
        /// プレイヤーがルームから退出した時に発生します
        /// </summary>
        public event Action<ulong>? PlayerLeft;

        /// <summary>
        /// プレイヤーの準備状態が変更された時に発生します
        /// </summary>
        public event Action<ulong, bool>? PlayerReadyChanged;

        /// <summary>
        /// マッチが開始される時に発生します
        /// </summary>
        public event Action? MatchStarting;

        /// <summary>
        /// エラーが発生した時に発生します
        /// </summary>
        public event Action<string>? ErrorOccurred;

        #endregion

        #region Properties

        /// <summary>
        /// 現在のルーム設定を取得します
        /// </summary>
        public RoomSettings CurrentRoomSettings => _networkRoomData?.CurrentRoomSettings ?? _currentRoomSettings;

        /// <summary>
        /// 現在のプレイヤースロットリストを取得します
        /// </summary>
        public IReadOnlyList<PlayerSlot> PlayerSlots
        {
            get
            {
                if (_networkRoomData != null)
                {
                    return _networkRoomData.GetAllPlayerSlots();
                }
                return Array.Empty<PlayerSlot>();
            }
        }

        /// <summary>
        /// ローカルプレイヤー情報を取得します
        /// </summary>
        public LobbyPlayerInfo? LocalPlayerInfo => _localPlayerInfo;

        /// <summary>
        /// ホストかどうかを取得します
        /// </summary>
        public bool IsHost => NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost;

        /// <summary>
        /// ルームに参加しているかどうかを取得します
        /// </summary>
        public bool IsInRoom => _isInRoom;

        /// <summary>
        /// 現在のジョインコードを取得します
        /// </summary>
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

            _initialized = true;
            Debug.Log("[LobbyService] Initialization complete.");
        }

        /// <summary>
        /// NetworkLobbyManagerの参照を設定します
        /// </summary>
        /// <param name="networkLobbyManager">NetworkLobbyManagerインスタンス</param>
        public void SetNetworkLobbyManager(NetworkLobbyManager networkLobbyManager)
        {
            if (_networkLobbyManager != null)
            {
                // 既存の参照をクリーンアップ
                UnsubscribeFromNetworkEvents();
            }

            _networkLobbyManager = networkLobbyManager;
            _networkRoomData = networkLobbyManager.NetworkRoomData;

            // ネットワークイベントを購読
            SubscribeToNetworkEvents();

            Debug.Log("[LobbyService] NetworkLobbyManager reference set.");
        }

        /// <summary>
        /// サービスを破棄します
        /// </summary>
        public void Dispose()
        {
            Debug.Log("[LobbyService] Disposing...");

            LeaveRoom();

            // ネットワークイベントを購読解除
            UnsubscribeFromNetworkEvents();

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

        /// <summary>
        /// ネットワークイベントを購読します
        /// </summary>
        private void SubscribeToNetworkEvents()
        {
            if (_networkLobbyManager != null)
            {
                _networkLobbyManager.PlayerJoined += OnNetworkPlayerJoined;
                _networkLobbyManager.PlayerLeft += OnNetworkPlayerLeft;
            }

            if (_networkRoomData != null)
            {
                _networkRoomData.RoomSettingsChanged += OnNetworkRoomSettingsChanged;
                _networkRoomData.PlayerReadyChanged += OnNetworkPlayerReadyChanged;
            }
        }

        /// <summary>
        /// ネットワークイベントを購読解除します
        /// </summary>
        private void UnsubscribeFromNetworkEvents()
        {
            if (_networkLobbyManager != null)
            {
                _networkLobbyManager.PlayerJoined -= OnNetworkPlayerJoined;
                _networkLobbyManager.PlayerLeft -= OnNetworkPlayerLeft;
            }

            if (_networkRoomData != null)
            {
                _networkRoomData.RoomSettingsChanged -= OnNetworkRoomSettingsChanged;
                _networkRoomData.PlayerReadyChanged -= OnNetworkPlayerReadyChanged;
            }
        }

        /// <summary>
        /// ネットワークプレイヤー参加イベントハンドラ
        /// </summary>
        /// <param name="playerId">プレイヤーID</param>
        private void OnNetworkPlayerJoined(ulong playerId)
        {
            PlayerJoined?.Invoke(playerId);
        }

        /// <summary>
        /// ネットワークプレイヤー退出イベントハンドラ
        /// </summary>
        /// <param name="playerId">プレイヤーID</param>
        private void OnNetworkPlayerLeft(ulong playerId)
        {
            PlayerLeft?.Invoke(playerId);
        }

        /// <summary>
        /// ネットワークルーム設定変更イベントハンドラ
        /// </summary>
        /// <param name="roomSettings">新しいルーム設定</param>
        private void OnNetworkRoomSettingsChanged(RoomSettings roomSettings)
        {
            _currentRoomSettings = roomSettings;
            RoomSettingsChanged?.Invoke(roomSettings);
        }

        /// <summary>
        /// ネットワークプレイヤー準備状態変更イベントハンドラ
        /// </summary>
        /// <param name="playerId">プレイヤーID</param>
        /// <param name="isReady">準備完了かどうか</param>
        private void OnNetworkPlayerReadyChanged(ulong playerId, bool isReady)
        {
            PlayerReadyChanged?.Invoke(playerId, isReady);
        }

        #endregion

        #region Host Methods

        /// <summary>
        /// ルームを作成します（ホスト）
        /// </summary>
        /// <param name="roomSettings">ルーム設定</param>
        /// <param name="playerName">プレイヤー名</param>
        /// <returns>成功した場合はtrue</returns>
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

            // NetworkLobbyManagerにプレイヤー名を登録
            if (_networkLobbyManager != null)
            {
                _networkLobbyManager.RegisterPlayerName(playerName);
            }

            // NetworkRoomDataの初期設定
            if (_networkRoomData != null)
            {
                _networkRoomData.UpdateRoomSettings(_currentRoomSettings);
                // ホストを最初のスロットに追加
                _networkRoomData.AddPlayer(NetworkManager.Singleton.LocalClientId, playerName);
            }

            RoomCreated?.Invoke(joinCode);
            Debug.Log($"[LobbyService] Room created with join code: {joinCode}");
        }

        /// <summary>
        /// ルーム設定を変更します（ホストのみ）
        /// </summary>
        /// <param name="roomSettings">新しいルーム設定</param>
        /// <returns>成功した場合はtrue</returns>
        public bool UpdateRoomSettings(RoomSettings roomSettings)
        {
            if (!IsHost)
            {
                Debug.LogError("[LobbyService] Only host can update room settings.");
                return false;
            }

            if (_networkRoomData == null)
            {
                Debug.LogError("[LobbyService] NetworkRoomData not available.");
                return false;
            }

            _currentRoomSettings = roomSettings;
            _networkRoomData.UpdateRoomSettings(roomSettings);

            Debug.Log("[LobbyService] Room settings updated.");
            return true;
        }

        /// <summary>
        /// CPUプレイヤーを追加します（ホストのみ）
        /// </summary>
        /// <param name="difficulty">AI難易度</param>
        /// <param name="teamIndex">チームインデックス（-1の場合は未割り当て）</param>
        /// <returns>成功した場合はtrue</returns>
        public bool AddCPUPlayer(AIDifficulty difficulty, int teamIndex = -1)
        {
            if (!IsHost)
            {
                Debug.LogError("[LobbyService] Only host can add CPU players.");
                return false;
            }

            if (_networkRoomData == null)
            {
                Debug.LogError("[LobbyService] NetworkRoomData not available.");
                return false;
            }

            var slots = _networkRoomData.GetAllPlayerSlots();

            // 空きスロットを探す
            int emptySlotIndex = -1;
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].IsEmpty())
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

            _networkRoomData.UpdatePlayerSlot(emptySlotIndex, cpuSlot);

            Debug.Log($"[LobbyService] CPU player added at slot {emptySlotIndex} with difficulty {difficulty}");
            return true;
        }

        /// <summary>
        /// CPUプレイヤーを削除します（ホストのみ）
        /// </summary>
        /// <param name="slotIndex">スロットインデックス</param>
        /// <returns>成功した場合はtrue</returns>
        public bool RemoveCPUPlayer(int slotIndex)
        {
            if (!IsHost)
            {
                Debug.LogError("[LobbyService] Only host can remove CPU players.");
                return false;
            }

            if (_networkRoomData == null)
            {
                Debug.LogError("[LobbyService] NetworkRoomData not available.");
                return false;
            }

            var slots = _networkRoomData.GetAllPlayerSlots();

            if (slotIndex < 0 || slotIndex >= slots.Length)
            {
                Debug.LogError($"[LobbyService] Invalid slot index: {slotIndex}");
                return false;
            }

            if (!slots[slotIndex].IsAI)
            {
                Debug.LogError($"[LobbyService] Slot {slotIndex} is not a CPU player.");
                return false;
            }

            var emptySlot = new PlayerSlot(slotIndex);
            _networkRoomData.UpdatePlayerSlot(slotIndex, emptySlot);

            Debug.Log($"[LobbyService] CPU player removed from slot {slotIndex}");
            return true;
        }

        /// <summary>
        /// CPU難易度を変更します（ホストのみ）
        /// </summary>
        /// <param name="slotIndex">スロットインデックス</param>
        /// <param name="difficulty">新しい難易度</param>
        /// <returns>成功した場合はtrue</returns>
        public bool ChangeCPUDifficulty(int slotIndex, AIDifficulty difficulty)
        {
            if (!IsHost)
            {
                Debug.LogError("[LobbyService] Only host can change CPU difficulty.");
                return false;
            }

            if (_networkRoomData == null)
            {
                Debug.LogError("[LobbyService] NetworkRoomData not available.");
                return false;
            }

            var slots = _networkRoomData.GetAllPlayerSlots();

            if (slotIndex < 0 || slotIndex >= slots.Length)
            {
                Debug.LogError($"[LobbyService] Invalid slot index: {slotIndex}");
                return false;
            }

            if (!slots[slotIndex].IsAI)
            {
                Debug.LogError($"[LobbyService] Slot {slotIndex} is not a CPU player.");
                return false;
            }

            var slot = slots[slotIndex];
            slot.AIDifficulty = difficulty;
            _networkRoomData.UpdatePlayerSlot(slotIndex, slot);

            Debug.Log($"[LobbyService] CPU difficulty changed to {difficulty} for slot {slotIndex}");
            return true;
        }

        /// <summary>
        /// プレイヤーをキックします（ホストのみ）
        /// </summary>
        /// <param name="playerId">プレイヤーID</param>
        /// <returns>成功した場合はtrue</returns>
        public bool KickPlayer(ulong playerId)
        {
            if (!IsHost)
            {
                Debug.LogError("[LobbyService] Only host can kick players.");
                return false;
            }

            if (_networkLobbyManager == null)
            {
                Debug.LogError("[LobbyService] NetworkLobbyManager not available.");
                return false;
            }

            if (playerId == NetworkManager.Singleton.LocalClientId)
            {
                Debug.LogError("[LobbyService] Cannot kick yourself.");
                return false;
            }

            _networkLobbyManager.KickPlayer(playerId);

            Debug.Log($"[LobbyService] Player {playerId} kicked.");
            return true;
        }

        /// <summary>
        /// マッチを開始します（ホストのみ）
        /// </summary>
        /// <returns>成功した場合はtrue</returns>
        public bool StartMatch()
        {
            if (!IsHost)
            {
                Debug.LogError("[LobbyService] Only host can start the match.");
                return false;
            }

            if (_networkRoomData == null)
            {
                Debug.LogError("[LobbyService] NetworkRoomData not available.");
                return false;
            }

            var slots = _networkRoomData.GetAllPlayerSlots();

            // すべての人間プレイヤーが準備完了か確認
            bool allReady = slots
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

        /// <summary>
        /// ジョインコードを使用してルームに参加します（ゲスト）
        /// </summary>
        /// <param name="joinCode">ジョインコード</param>
        /// <param name="playerName">プレイヤー名</param>
        /// <param name="password">パスワード（必要な場合）</param>
        /// <returns>成功した場合はtrue</returns>
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

            // NetworkLobbyManagerにプレイヤー名を登録
            if (_networkLobbyManager != null)
            {
                _networkLobbyManager.RegisterPlayerName(playerName);
            }

            RoomJoined?.Invoke();
            Debug.Log($"[LobbyService] Joined room with code: {joinCode}");
        }

        #endregion

        #region Common Methods

        /// <summary>
        /// ルームから退出します
        /// </summary>
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

            RoomLeft?.Invoke();
            Debug.Log("[LobbyService] Left room.");
        }

        /// <summary>
        /// 準備状態を切り替えます
        /// </summary>
        /// <param name="isReady">準備完了かどうか</param>
        public void SetReady(bool isReady)
        {
            if (_localPlayerInfo == null)
            {
                Debug.LogError("[LobbyService] No local player info.");
                return;
            }

            if (_networkRoomData == null)
            {
                Debug.LogError("[LobbyService] NetworkRoomData not available.");
                return;
            }

            // NetworkRoomDataのServerRPCを呼び出して準備状態を変更
            _networkRoomData.SetPlayerReadyServerRpc(isReady);

            Debug.Log($"[LobbyService] Ready status set to: {isReady}");
        }

        /// <summary>
        /// カスタマイズプリセットを設定します
        /// </summary>
        /// <param name="presetName">プリセット名</param>
        public void SetCustomizationPreset(string presetName)
        {
            if (_localPlayerInfo == null)
            {
                Debug.LogError("[LobbyService] No local player info.");
                return;
            }

            if (_networkRoomData == null)
            {
                Debug.LogError("[LobbyService] NetworkRoomData not available.");
                return;
            }

            _localPlayerInfo.CustomizationPresetName = presetName;

            // NetworkRoomDataのServerRPCを呼び出してカスタマイズプリセットを変更
            _networkRoomData.SetCustomizationPresetServerRpc(presetName);

            Debug.Log($"[LobbyService] Customization preset set to: {presetName}");
        }

        /// <summary>
        /// プレイヤースロット情報を取得します
        /// </summary>
        /// <param name="slotIndex">スロットインデックス</param>
        /// <returns>プレイヤースロット</returns>
        public PlayerSlot? GetPlayerSlot(int slotIndex)
        {
            if (_networkRoomData == null)
            {
                return null;
            }

            return _networkRoomData.GetPlayerSlot(slotIndex);
        }

        /// <summary>
        /// プレイヤーIDからスロットインデックスを取得します
        /// </summary>
        /// <param name="playerId">プレイヤーID</param>
        /// <returns>スロットインデックス（見つからない場合は-1）</returns>
        public int GetSlotIndexByPlayerId(ulong playerId)
        {
            if (_networkRoomData == null)
            {
                return -1;
            }

            return _networkRoomData.GetSlotIndexByPlayerId(playerId);
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// CPUプレイヤーの数をカウントします
        /// </summary>
        /// <returns>CPUプレイヤーの数</returns>
        private int CountCPUPlayers()
        {
            if (_networkRoomData == null)
            {
                return 0;
            }

            var slots = _networkRoomData.GetAllPlayerSlots();
            return slots.Count(s => s.IsAI);
        }

        #endregion
    }
}
