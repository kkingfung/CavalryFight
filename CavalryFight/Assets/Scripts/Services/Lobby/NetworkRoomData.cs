#nullable enable

using System;
using Unity.Netcode;
using UnityEngine;

namespace CavalryFight.Services.Lobby
{
    /// <summary>
    /// ネットワーク同期されたルームデータ
    /// </summary>
    /// <remarks>
    /// ルーム設定とプレイヤースロットをネットワーク経由で同期します。
    /// サーバー（ホスト）が権限を持ち、クライアント（ゲスト）は読み取り専用です。
    /// </remarks>
    public class NetworkRoomData : NetworkBehaviour
    {
        #region Network Variables

        /// <summary>
        /// ネットワーク同期されたルーム設定
        /// </summary>
        private NetworkVariable<RoomSettings> _roomSettings = new NetworkVariable<RoomSettings>(
            RoomSettings.CreateDefault(),
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        /// <summary>
        /// ネットワーク同期されたプレイヤースロットリスト
        /// </summary>
        private NetworkList<PlayerSlot> _playerSlots = null!;

        #endregion

        #region Events

        /// <summary>
        /// ルーム設定が変更された時に発生します
        /// </summary>
        public event Action<RoomSettings>? RoomSettingsChanged;

        /// <summary>
        /// プレイヤースロットが変更された時に発生します
        /// </summary>
        public event Action<int, PlayerSlot>? PlayerSlotChanged; // slotIndex, slot

        /// <summary>
        /// プレイヤーの準備状態が変更された時に発生します
        /// </summary>
        public event Action<ulong, bool>? PlayerReadyChanged; // playerId, isReady

        #endregion

        #region Properties

        /// <summary>
        /// 現在のルーム設定を取得します
        /// </summary>
        public RoomSettings CurrentRoomSettings => _roomSettings.Value;

        #endregion

        #region Unity Lifecycle

        /// <summary>
        /// Awake時の初期化
        /// </summary>
        private void Awake()
        {
            _playerSlots = new NetworkList<PlayerSlot>();
        }

        /// <summary>
        /// NetworkBehaviourの初期化時に呼ばれます
        /// </summary>
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsServer)
            {
                // サーバー側: 8つの空スロットを初期化
                for (int i = 0; i < 8; i++)
                {
                    _playerSlots.Add(new PlayerSlot(i));
                }
            }

            // クライアント側: 変更イベントを購読
            _roomSettings.OnValueChanged += OnRoomSettingsChanged;
            _playerSlots.OnListChanged += OnPlayerSlotsListChanged;
        }

        /// <summary>
        /// NetworkBehaviourの破棄時に呼ばれます
        /// </summary>
        public override void OnNetworkDespawn()
        {
            _roomSettings.OnValueChanged -= OnRoomSettingsChanged;
            _playerSlots.OnListChanged -= OnPlayerSlotsListChanged;

            base.OnNetworkDespawn();
        }

        #endregion

        #region Server Methods

        /// <summary>
        /// ルーム設定を更新します（サーバーのみ）
        /// </summary>
        /// <param name="roomSettings">新しいルーム設定</param>
        public void UpdateRoomSettings(RoomSettings roomSettings)
        {
            if (!IsServer)
            {
                Debug.LogError("[NetworkRoomData] Only server can update room settings.");
                return;
            }

            _roomSettings.Value = roomSettings;
            Debug.Log("[NetworkRoomData] Room settings updated.");
        }

        /// <summary>
        /// プレイヤースロットを更新します（サーバーのみ）
        /// </summary>
        /// <param name="slotIndex">スロットインデックス</param>
        /// <param name="slot">新しいスロットデータ</param>
        public void UpdatePlayerSlot(int slotIndex, PlayerSlot slot)
        {
            if (!IsServer)
            {
                Debug.LogError("[NetworkRoomData] Only server can update player slots.");
                return;
            }

            if (slotIndex < 0 || slotIndex >= _playerSlots.Count)
            {
                Debug.LogError($"[NetworkRoomData] Invalid slot index: {slotIndex}");
                return;
            }

            _playerSlots[slotIndex] = slot;
            Debug.Log($"[NetworkRoomData] Player slot {slotIndex} updated.");
        }

        /// <summary>
        /// プレイヤーをスロットに追加します（サーバーのみ）
        /// </summary>
        /// <param name="playerId">プレイヤーID</param>
        /// <param name="playerName">プレイヤー名</param>
        /// <returns>割り当てられたスロットインデックス（失敗時は-1）</returns>
        public int AddPlayer(ulong playerId, string playerName)
        {
            if (!IsServer)
            {
                Debug.LogError("[NetworkRoomData] Only server can add players.");
                return -1;
            }

            // 空きスロットを探す
            for (int i = 0; i < _playerSlots.Count; i++)
            {
                if (_playerSlots[i].IsEmpty())
                {
                    var slot = new PlayerSlot(i, playerId, playerName);
                    _playerSlots[i] = slot;
                    Debug.Log($"[NetworkRoomData] Player {playerName} (ID: {playerId}) added to slot {i}");
                    return i;
                }
            }

            Debug.LogError("[NetworkRoomData] No empty slots available.");
            return -1;
        }

        /// <summary>
        /// プレイヤーをスロットから削除します（サーバーのみ）
        /// </summary>
        /// <param name="playerId">プレイヤーID</param>
        public void RemovePlayer(ulong playerId)
        {
            if (!IsServer)
            {
                Debug.LogError("[NetworkRoomData] Only server can remove players.");
                return;
            }

            for (int i = 0; i < _playerSlots.Count; i++)
            {
                if (_playerSlots[i].PlayerId == playerId)
                {
                    var emptySlot = new PlayerSlot(i);
                    _playerSlots[i] = emptySlot;
                    Debug.Log($"[NetworkRoomData] Player (ID: {playerId}) removed from slot {i}");
                    return;
                }
            }

            Debug.LogWarning($"[NetworkRoomData] Player (ID: {playerId}) not found in any slot.");
        }

        #endregion

        #region Client RPC Methods

        /// <summary>
        /// プレイヤーの準備状態変更をサーバーに要求します
        /// </summary>
        /// <param name="isReady">準備完了かどうか</param>
        /// <param name="serverRpcParams">RPCパラメータ</param>
        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        public void SetPlayerReadyServerRpc(bool isReady, RpcParams serverRpcParams = default)
        {
            ulong senderId = serverRpcParams.Receive.SenderClientId;

            // プレイヤーのスロットを探す
            for (int i = 0; i < _playerSlots.Count; i++)
            {
                if (_playerSlots[i].PlayerId == senderId)
                {
                    var slot = _playerSlots[i];
                    slot.IsReady = isReady;
                    _playerSlots[i] = slot;

                    Debug.Log($"[NetworkRoomData] Player (ID: {senderId}) ready status set to: {isReady}");

                    // クライアントに通知
                    NotifyPlayerReadyChangedClientRpc(senderId, isReady);
                    return;
                }
            }

            Debug.LogWarning($"[NetworkRoomData] Player (ID: {senderId}) not found in any slot.");
        }

        /// <summary>
        /// カスタマイズプリセット変更をサーバーに要求します
        /// </summary>
        /// <param name="presetName">プリセット名</param>
        /// <param name="serverRpcParams">RPCパラメータ</param>
        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        public void SetCustomizationPresetServerRpc(string presetName, RpcParams serverRpcParams = default)
        {
            ulong senderId = serverRpcParams.Receive.SenderClientId;

            // プレイヤーのスロットを探す
            for (int i = 0; i < _playerSlots.Count; i++)
            {
                if (_playerSlots[i].PlayerId == senderId)
                {
                    var slot = _playerSlots[i];
                    slot.CustomizationPresetName = presetName;
                    _playerSlots[i] = slot;

                    Debug.Log($"[NetworkRoomData] Player (ID: {senderId}) customization preset set to: {presetName}");
                    return;
                }
            }

            Debug.LogWarning($"[NetworkRoomData] Player (ID: {senderId}) not found in any slot.");
        }

        /// <summary>
        /// プレイヤーの準備状態変更を全クライアントに通知します
        /// </summary>
        /// <param name="playerId">プレイヤーID</param>
        /// <param name="isReady">準備完了かどうか</param>
        [Rpc(SendTo.Everyone)]
        private void NotifyPlayerReadyChangedClientRpc(ulong playerId, bool isReady)
        {
            PlayerReadyChanged?.Invoke(playerId, isReady);
        }

        #endregion

        #region Query Methods

        /// <summary>
        /// プレイヤースロットを取得します
        /// </summary>
        /// <param name="slotIndex">スロットインデックス</param>
        /// <returns>プレイヤースロット</returns>
        public PlayerSlot? GetPlayerSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _playerSlots.Count)
            {
                return null;
            }

            return _playerSlots[slotIndex];
        }

        /// <summary>
        /// プレイヤーIDからスロットインデックスを取得します
        /// </summary>
        /// <param name="playerId">プレイヤーID</param>
        /// <returns>スロットインデックス（見つからない場合は-1）</returns>
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

        /// <summary>
        /// すべてのプレイヤースロットを取得します
        /// </summary>
        /// <returns>プレイヤースロット配列</returns>
        public PlayerSlot[] GetAllPlayerSlots()
        {
            var slots = new PlayerSlot[_playerSlots.Count];
            for (int i = 0; i < _playerSlots.Count; i++)
            {
                slots[i] = _playerSlots[i];
            }
            return slots;
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// ルーム設定変更時のハンドラ
        /// </summary>
        /// <param name="previousValue">変更前の値</param>
        /// <param name="newValue">変更後の値</param>
        private void OnRoomSettingsChanged(RoomSettings previousValue, RoomSettings newValue)
        {
            Debug.Log("[NetworkRoomData] Room settings changed notification received.");
            RoomSettingsChanged?.Invoke(newValue);
        }

        /// <summary>
        /// プレイヤースロットリスト変更時のハンドラ
        /// </summary>
        /// <param name="changeEvent">変更イベント</param>
        private void OnPlayerSlotsListChanged(NetworkListEvent<PlayerSlot> changeEvent)
        {
            Debug.Log($"[NetworkRoomData] Player slots list changed: {changeEvent.Type} at index {changeEvent.Index}");

            // スロット変更イベントを発火
            if (changeEvent.Type == NetworkListEvent<PlayerSlot>.EventType.Value)
            {
                PlayerSlotChanged?.Invoke(changeEvent.Index, changeEvent.Value);
            }
            else if (changeEvent.Type == NetworkListEvent<PlayerSlot>.EventType.Add)
            {
                // 新しいスロットが追加された時（初期同期時など）
                PlayerSlotChanged?.Invoke(changeEvent.Index, changeEvent.Value);
            }
            else if (changeEvent.Type == NetworkListEvent<PlayerSlot>.EventType.Full)
            {
                // リスト全体が初期化された時（クライアント側の初期同期）
                for (int i = 0; i < _playerSlots.Count; i++)
                {
                    PlayerSlotChanged?.Invoke(i, _playerSlots[i]);
                }
            }
        }

        #endregion
    }
}
