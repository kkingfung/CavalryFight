#nullable enable

using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace CavalryFight.Services.Lobby
{
    /// <summary>
    /// ネットワークロビーマネージャー
    /// </summary>
    /// <remarks>
    /// Netcodeのコネクション管理とLobbyServiceの統合を行います。
    /// プレイヤーの接続・切断イベントを処理し、NetworkRoomDataと連携します。
    /// </remarks>
    [RequireComponent(typeof(NetworkRoomData))]
    public class NetworkLobbyManager : NetworkBehaviour
    {
        #region Singleton

        /// <summary>
        /// シングルトンインスタンス
        /// </summary>
        public static NetworkLobbyManager? Instance { get; private set; }

        #endregion

        #region Fields

        /// <summary>
        /// NetworkRoomDataへの参照
        /// </summary>
        private NetworkRoomData? _networkRoomData;

        /// <summary>
        /// プレイヤー名のマッピング（ClientId -> PlayerName）
        /// </summary>
        private Dictionary<ulong, string> _playerNames = new Dictionary<ulong, string>();

        #endregion

        #region Events

        /// <summary>
        /// プレイヤーが参加した時に発生します
        /// </summary>
        public event Action<ulong>? PlayerJoined;

        /// <summary>
        /// プレイヤーが退出した時に発生します
        /// </summary>
        public event Action<ulong>? PlayerLeft;

        #endregion

        #region Properties

        /// <summary>
        /// NetworkRoomDataへの参照を取得します
        /// </summary>
        public NetworkRoomData? NetworkRoomData => _networkRoomData;

        #endregion

        #region Unity Lifecycle

        /// <summary>
        /// Awake時の初期化
        /// </summary>
        private void Awake()
        {
            // シングルトン設定
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            _networkRoomData = GetComponent<NetworkRoomData>();
        }

        /// <summary>
        /// OnDestroy時のクリーンアップ
        /// </summary>
        public override void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            base.OnDestroy();
        }

        #endregion

        #region NetworkBehaviour Overrides

        /// <summary>
        /// NetworkBehaviourの初期化時に呼ばれます
        /// </summary>
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsServer)
            {
                // サーバー側: 接続イベントを購読
                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

                Debug.Log("[NetworkLobbyManager] Server started - listening for connections.");
            }
            else
            {
                Debug.Log("[NetworkLobbyManager] Client started.");
            }
        }

        /// <summary>
        /// NetworkBehaviourの破棄時に呼ばれます
        /// </summary>
        public override void OnNetworkDespawn()
        {
            if (IsServer && NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
            }

            base.OnNetworkDespawn();
        }

        #endregion

        #region Server Callbacks

        /// <summary>
        /// クライアント接続時のコールバック（サーバーのみ）
        /// </summary>
        /// <param name="clientId">クライアントID</param>
        private void OnClientConnected(ulong clientId)
        {
            Debug.Log($"[NetworkLobbyManager] Client connected: {clientId}");

            // ホストの場合は特別な処理（既にLobbyServiceで処理済み）
            if (clientId == NetworkManager.ServerClientId)
            {
                return;
            }

            // ゲストの場合はプレイヤー名の送信を待つ
            // プレイヤー名はSubmitPlayerNameServerRpcで受信される
        }

        /// <summary>
        /// クライアント切断時のコールバック（サーバーのみ）
        /// </summary>
        /// <param name="clientId">クライアントID</param>
        private void OnClientDisconnected(ulong clientId)
        {
            Debug.Log($"[NetworkLobbyManager] Client disconnected: {clientId}");

            // プレイヤーをスロットから削除
            _networkRoomData?.RemovePlayer(clientId);

            // プレイヤー名マッピングから削除
            if (_playerNames.ContainsKey(clientId))
            {
                _playerNames.Remove(clientId);
            }

            // イベント発火
            PlayerLeft?.Invoke(clientId);
        }

        #endregion

        #region Server RPC Methods

        /// <summary>
        /// プレイヤー名をサーバーに送信します
        /// </summary>
        /// <param name="playerName">プレイヤー名</param>
        /// <param name="serverRpcParams">RPCパラメータ</param>
        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        public void SubmitPlayerNameServerRpc(string playerName, RpcParams serverRpcParams = default)
        {
            ulong clientId = serverRpcParams.Receive.SenderClientId;

            Debug.Log($"[NetworkLobbyManager] Received player name '{playerName}' from client {clientId}");

            // プレイヤー名を保存
            _playerNames[clientId] = playerName;

            // プレイヤーをスロットに追加
            int slotIndex = _networkRoomData?.AddPlayer(clientId, playerName) ?? -1;

            if (slotIndex != -1)
            {
                // 成功通知をクライアントに送信
                NotifyPlayerJoinedClientRpc(clientId);

                // イベント発火
                PlayerJoined?.Invoke(clientId);
            }
            else
            {
                Debug.LogError($"[NetworkLobbyManager] Failed to add player {playerName} (ID: {clientId}) to slot.");
                // TODO: クライアントに失敗を通知してキックする
            }
        }

        #endregion

        #region Client RPC Methods

        /// <summary>
        /// プレイヤー参加を全クライアントに通知します
        /// </summary>
        /// <param name="clientId">参加したクライアントID</param>
        [Rpc(SendTo.Everyone)]
        private void NotifyPlayerJoinedClientRpc(ulong clientId)
        {
            // クライアント側でもイベントを発火
            if (!IsServer)
            {
                PlayerJoined?.Invoke(clientId);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// プレイヤーをキックします（サーバーのみ）
        /// </summary>
        /// <param name="clientId">キックするクライアントID</param>
        public void KickPlayer(ulong clientId)
        {
            if (!IsServer)
            {
                Debug.LogError("[NetworkLobbyManager] Only server can kick players.");
                return;
            }

            if (clientId == NetworkManager.ServerClientId)
            {
                Debug.LogError("[NetworkLobbyManager] Cannot kick the host.");
                return;
            }

            Debug.Log($"[NetworkLobbyManager] Kicking player: {clientId}");
            NetworkManager.Singleton.DisconnectClient(clientId);
        }

        /// <summary>
        /// プレイヤー名を取得します
        /// </summary>
        /// <param name="clientId">クライアントID</param>
        /// <returns>プレイヤー名（見つからない場合はnull）</returns>
        public string? GetPlayerName(ulong clientId)
        {
            if (_playerNames.TryGetValue(clientId, out string? playerName))
            {
                return playerName;
            }

            return null;
        }

        /// <summary>
        /// クライアント側からプレイヤー名を登録します
        /// </summary>
        /// <param name="playerName">プレイヤー名</param>
        public void RegisterPlayerName(string playerName)
        {
            if (IsServer)
            {
                // サーバー（ホスト）の場合は直接登録
                _playerNames[NetworkManager.Singleton.LocalClientId] = playerName;
            }
            else
            {
                // クライアント（ゲスト）の場合はServerRPCを使用
                SubmitPlayerNameServerRpc(playerName);
            }
        }

        #endregion
    }
}
