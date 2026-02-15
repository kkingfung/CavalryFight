#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using CavalryFight.Core.Services;
using CavalryFight.Services.SceneManagement;

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
        #region Constants

        /// <summary>
        /// PlayerPrefsキー: プレイヤー名
        /// </summary>
        private const string PlayerNamePrefKey = "CavalryFight_PlayerName";

        #endregion

        #region Fields

        /// <summary>
        /// インスタンスID（デバッグ用）
        /// </summary>
        private readonly string _instanceId = System.Guid.NewGuid().ToString().Substring(0, 8);

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
        /// ローカルプレイヤーがホストかどうか（内部追跡）
        /// </summary>
        private bool _isLocalHost = false;

        /// <summary>
        /// ホスト開始処理中かどうか（重複呼び出し防止用）
        /// </summary>
        private bool _isStartingHost = false;

        /// <summary>
        /// ルーム参加処理中かどうか（重複呼び出し防止用）
        /// </summary>
        private bool _isJoiningRoom = false;

        /// <summary>
        /// NetworkLobbyManagerへの参照
        /// </summary>
        private NetworkLobbyManager? _networkLobbyManager;

        /// <summary>
        /// NetworkRoomDataへの参照
        /// </summary>
        private NetworkRoomData? _networkRoomData;

        /// <summary>
        /// 利用可能なルームリスト
        /// </summary>
        private List<RoomInfo> _availableRooms = new List<RoomInfo>();

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
        /// プレイヤースロット情報が変更された時に発生します
        /// </summary>
        public event Action<int, PlayerSlot>? PlayerSlotChanged;

        /// <summary>
        /// プレイヤーの準備状態が変更された時に発生します
        /// </summary>
        public event Action<ulong, bool>? PlayerReadyChanged;

        /// <summary>
        /// マッチが開始される時に発生します
        /// </summary>
        public event Action? MatchStarting;

        /// <summary>
        /// カウントダウンが開始された時に発生します
        /// </summary>
        public event Action<int>? CountdownStarted;

        /// <summary>
        /// カウントダウンが更新された時に発生します
        /// </summary>
        public event Action<int>? CountdownUpdated;

        /// <summary>
        /// カウントダウンがキャンセルされた時に発生します
        /// </summary>
        public event Action? CountdownCancelled;

        /// <summary>
        /// エラーが発生した時に発生します
        /// </summary>
        public event Action<string>? ErrorOccurred;

        /// <summary>
        /// ホストが切断された時に発生します（ゲストのみ）
        /// </summary>
        public event Action? HostDisconnected;

        /// <summary>
        /// 利用可能なルームリストが更新された時に発生します
        /// </summary>
        public event Action<IReadOnlyList<RoomInfo>>? AvailableRoomsUpdated;

        /// <summary>
        /// マッチからルームに戻る時に発生します
        /// </summary>
        public event Action? ReturnToRoomRequested;

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
        public bool IsHost => _isLocalHost;

        /// <summary>
        /// ルームに参加しているかどうかを取得します
        /// </summary>
        public bool IsInRoom => _isInRoom;

        /// <summary>
        /// 現在のジョインコードを取得します
        /// </summary>
        public string? CurrentJoinCode => _relayManager.CurrentJoinCode;

        /// <summary>
        /// 利用可能なルームリストを取得します
        /// </summary>
        public IReadOnlyList<RoomInfo> AvailableRooms => _availableRooms.AsReadOnly();

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

            // RelayManager は CreateRoom/JoinRoom 時に非同期で初期化されます

            // NetworkManager の切断イベントを購読
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
            }

            // NetworkLobbyManagerを探して登録（Startupシーンに配置されている前提）
            var networkLobbyManager = NetworkLobbyManager.Instance;
            if (networkLobbyManager == null)
            {
                // シングルトンがnullの場合はシーンから検索
                networkLobbyManager = UnityEngine.Object.FindFirstObjectByType<NetworkLobbyManager>();
            }

            if (networkLobbyManager != null)
            {
                SetNetworkLobbyManager(networkLobbyManager);
                Debug.Log("[LobbyService] NetworkLobbyManager found and registered from Startup scene.");
            }
            else
            {
                Debug.LogWarning("[LobbyService] NetworkLobbyManager not found. Please ensure NetworkLobbyManager is in the Startup scene.");
            }

            // NetworkManagerのステータスと同期
            SyncHostStatusWithNetwork();

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

            // NetworkManager の切断イベントを購読解除
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
            }

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
            HostDisconnected = null;

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
                _networkLobbyManager.CountdownStarted += OnNetworkCountdownStarted;
                _networkLobbyManager.CountdownUpdated += OnNetworkCountdownUpdated;
                _networkLobbyManager.CountdownCancelled += OnNetworkCountdownCancelled;
                _networkLobbyManager.ReturnToRoomRequested += OnNetworkReturnToRoomRequested;
            }

            if (_networkRoomData != null)
            {
                _networkRoomData.RoomSettingsChanged += OnNetworkRoomSettingsChanged;
                _networkRoomData.PlayerReadyChanged += OnNetworkPlayerReadyChanged;
                _networkRoomData.PlayerSlotChanged += OnNetworkPlayerSlotChanged;
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
                _networkLobbyManager.CountdownStarted -= OnNetworkCountdownStarted;
                _networkLobbyManager.CountdownUpdated -= OnNetworkCountdownUpdated;
                _networkLobbyManager.CountdownCancelled -= OnNetworkCountdownCancelled;
                _networkLobbyManager.ReturnToRoomRequested -= OnNetworkReturnToRoomRequested;
            }

            if (_networkRoomData != null)
            {
                _networkRoomData.RoomSettingsChanged -= OnNetworkRoomSettingsChanged;
                _networkRoomData.PlayerReadyChanged -= OnNetworkPlayerReadyChanged;
                _networkRoomData.PlayerSlotChanged -= OnNetworkPlayerSlotChanged;
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

        /// <summary>
        /// ネットワークプレイヤースロット変更イベントハンドラ
        /// </summary>
        /// <param name="slotIndex">変更されたスロットのインデックス</param>
        /// <param name="slot">新しいスロットデータ</param>
        private void OnNetworkPlayerSlotChanged(int slotIndex, PlayerSlot slot)
        {
            Debug.Log($"[LobbyService:{_instanceId}] Player slot changed: Index={slotIndex}, PlayerId={slot.PlayerId}, Name={slot.PlayerName}");

            // プレイヤースロット変更イベントを発火
            PlayerSlotChanged?.Invoke(slotIndex, slot);
        }

        /// <summary>
        /// ネットワークカウントダウン開始イベントハンドラ
        /// </summary>
        /// <param name="initialSeconds">カウントダウン初期秒数</param>
        private void OnNetworkCountdownStarted(int initialSeconds)
        {
            CountdownStarted?.Invoke(initialSeconds);
        }

        /// <summary>
        /// ネットワークカウントダウン更新イベントハンドラ
        /// </summary>
        /// <param name="secondsRemaining">残り秒数</param>
        private void OnNetworkCountdownUpdated(int secondsRemaining)
        {
            CountdownUpdated?.Invoke(secondsRemaining);
        }

        /// <summary>
        /// ネットワークカウントダウンキャンセルイベントハンドラ
        /// </summary>
        private void OnNetworkCountdownCancelled()
        {
            CountdownCancelled?.Invoke();
        }

        /// <summary>
        /// ネットワークルームへの復帰要求イベントハンドラ
        /// </summary>
        private void OnNetworkReturnToRoomRequested()
        {
            Debug.Log("[LobbyService] Return to room requested");

            // NetworkManagerの状態と同期
            SyncHostStatusWithNetwork();

            ReturnToRoomRequested?.Invoke();
        }

        /// <summary>
        /// クライアント切断イベントハンドラ
        /// </summary>
        /// <param name="clientId">切断されたクライアントID</param>
        private void OnClientDisconnected(ulong clientId)
        {
            // ゲストの場合、サーバー（ホスト）が切断されたか確認
            if (!IsHost && clientId == NetworkManager.ServerClientId)
            {
                Debug.LogWarning("[LobbyService] Host disconnected.");
                _isInRoom = false;
                _isLocalHost = false;
                _localPlayerInfo = null;

                // ホスト切断イベントを発火
                HostDisconnected?.Invoke();
            }
        }

        /// <summary>
        /// NetworkManagerのホスト/サーバー状態と_isLocalHostフラグを同期します
        /// </summary>
        /// <remarks>
        /// マッチから戻った際など、NetworkManagerは接続状態を維持しているが
        /// LobbyServiceの_isLocalHostフラグが同期していない場合に使用します。
        /// </remarks>
        private void SyncHostStatusWithNetwork()
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                bool wasHost = _isLocalHost;
                bool isNetworkHost = NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsHost;

                // ルームに参加中でネットワークがホスト/サーバーの場合
                if (_isInRoom && isNetworkHost && !_isLocalHost)
                {
                    _isLocalHost = true;
                    Debug.Log($"[LobbyService] Synced host status with NetworkManager: was={wasHost}, now={_isLocalHost}, IsServer={NetworkManager.Singleton.IsServer}");
                }
                // ルームに参加中でネットワークがクライアントの場合
                else if (_isInRoom && !isNetworkHost && _isLocalHost)
                {
                    _isLocalHost = false;
                    Debug.Log($"[LobbyService] Synced guest status with NetworkManager: was={wasHost}, now={_isLocalHost}, IsServer={NetworkManager.Singleton.IsServer}");
                }
            }
        }

        /// <summary>
        /// ホスト/ゲストステータスを手動で同期します
        /// </summary>
        /// <remarks>
        /// ルームシーンに戻った際など、NetworkManagerとの同期が必要な場合に呼び出します。
        /// </remarks>
        public void RefreshHostStatus()
        {
            SyncHostStatusWithNetwork();
            Debug.Log($"[LobbyService] Host status refreshed: IsHost={IsHost}, IsInRoom={IsInRoom}");
        }

        #endregion

        #region Host Methods

        /// <summary>
        /// ルームを作成します（ホスト）
        /// </summary>
        /// <remarks>
        /// このメソッドは非同期処理を開始し、即座に制御を返します。
        /// 実際の成功/失敗は RoomCreated イベントまたは ErrorOccurred イベントで通知されます。
        /// </remarks>
        /// <param name="roomSettings">ルーム設定</param>
        /// <param name="playerName">プレイヤー名</param>
        /// <returns>リクエストが受け付けられた場合はtrue（初期化エラーや既にルームに参加している場合はfalse）</returns>
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

            // 既にホスト開始処理中の場合は重複呼び出しを防止
            if (_isStartingHost)
            {
                Debug.LogWarning("[LobbyService] Host operation already in progress.");
                return false;
            }

            // NetworkManagerが既に稼働中の場合は新規ホスト開始を防止
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                Debug.LogWarning("[LobbyService] NetworkManager is already running.");
                return false;
            }

            try
            {
                _isStartingHost = true;
                _currentRoomSettings = roomSettings;

                // Relayを開始してジョインコードを取得（非同期）
                _ = StartHostRelayAsync(playerName);

                Debug.Log($"[LobbyService] Creating room: {roomSettings.RoomName}");
                return true;
            }
            catch (Exception ex)
            {
                _isStartingHost = false;
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
            try
            {
                // RelayManager の初期化を確実に待つ
                bool initialized = await _relayManager.InitializeAsync();
                if (!initialized)
                {
                    ErrorOccurred?.Invoke("Failed to initialize RelayManager.");
                    return;
                }

                string? joinCode = await _relayManager.StartHostAsync();

                if (joinCode == null)
                {
                    ErrorOccurred?.Invoke("Failed to create relay allocation.");
                    return;
                }

                // 念のためNetworkManagerが既に稼働中かチェック
                if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
                {
                    Debug.LogWarning("[LobbyService] NetworkManager is already listening. Skipping StartHost.");
                    ErrorOccurred?.Invoke("NetworkManager is already running.");
                    return;
                }

                // NetworkManagerを開始
                if (!NetworkManager.Singleton.StartHost())
                {
                    ErrorOccurred?.Invoke("Failed to start host.");
                    return;
                }

                _isInRoom = true;
                _isLocalHost = true;

                // ローカルプレイヤー情報を設定
                _localPlayerInfo = new LobbyPlayerInfo(
                    NetworkManager.Singleton.LocalClientId,
                    playerName,
                    true
                );
                _localPlayerInfo.IsLocalPlayer = true;

                // プレイヤー名を保存
                SavePlayerName(playerName);

                // NetworkLobbyManagerの登録を確認
                if (_networkLobbyManager == null)
                {
                    Debug.LogError("[LobbyService] NetworkLobbyManager is not registered! Please ensure it is in the Startup scene.");
                    ErrorOccurred?.Invoke("NetworkLobbyManager not found. Cannot create room.");
                    return;
                }

                // NetworkLobbyManagerにプレイヤー名を登録
                if (_networkLobbyManager != null)
                {
                    _networkLobbyManager.RegisterPlayerName(playerName);
                }

                // NetworkRoomDataのスポーンを待機（タイムアウト付きポーリング）
                const int maxWaitTimeMs = 5000; // 5秒でタイムアウト
                const int checkIntervalMs = 50; // 50ms毎にチェック
                int elapsedMs = 0;

                while (elapsedMs < maxWaitTimeMs)
                {
                    if (_networkLobbyManager != null)
                    {
                        _networkRoomData = _networkLobbyManager.NetworkRoomData;

                        if (_networkRoomData != null && _networkRoomData.IsSpawned)
                        {
                            // スポーン完了
                            break;
                        }
                    }

                    await System.Threading.Tasks.Task.Delay(checkIntervalMs);
                    elapsedMs += checkIntervalMs;
                }

                // NetworkRoomDataの初期設定
                if (_networkRoomData == null || !_networkRoomData.IsSpawned)
                {
                    Debug.LogError("[LobbyService] NetworkRoomData failed to spawn within timeout.");
                    ErrorOccurred?.Invoke("Failed to initialize room data.");
                    return;
                }

                // ルーム設定を適用
                _networkRoomData.UpdateRoomSettings(_currentRoomSettings);

                // ホストを最初のスロットに追加
                _networkRoomData.AddPlayer(NetworkManager.Singleton.LocalClientId, playerName);

                RoomCreated?.Invoke(joinCode);
                Debug.Log($"[LobbyService] Room created with join code: {joinCode}");
            }
            finally
            {
                // 成功・失敗に関わらずフラグをリセット
                _isStartingHost = false;
            }
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

            // MaxPlayersが減少した場合、超過プレイヤーを削除
            if (roomSettings.MaxPlayers < _currentRoomSettings.MaxPlayers)
            {
                var slots = _networkRoomData.GetAllPlayerSlots();
                for (int i = roomSettings.MaxPlayers; i < slots.Length; i++)
                {
                    if (!slots[i].IsEmpty())
                    {
                        if (slots[i].IsAI)
                        {
                            // AI（NPC）を削除 - スロットを空にする
                            _networkRoomData.UpdatePlayerSlot(i, new PlayerSlot(i));
                            Debug.Log($"[LobbyService] Removed NPC from slot {i} due to MaxPlayers reduction");
                        }
                        else
                        {
                            // 人間プレイヤーをキック
                            KickPlayer(slots[i].PlayerId);
                            Debug.Log($"[LobbyService] Kicked player {slots[i].PlayerId} from slot {i} due to MaxPlayers reduction");
                        }
                    }
                }
            }

            // TeamFightモードに変更された場合、チーム未割り当てのプレイヤーを自動割り当て
            bool wasTeamFight = _currentRoomSettings.GameMode == GameMode.TeamFight;
            bool isTeamFight = roomSettings.GameMode == GameMode.TeamFight;

            _currentRoomSettings = roomSettings;
            _networkRoomData.UpdateRoomSettings(roomSettings);

            // TeamFightモードに新しく変更された場合
            if (isTeamFight && !wasTeamFight)
            {
                AutoAssignTeamsForTeamFight();
            }

            Debug.Log("[LobbyService] Room settings updated.");
            return true;
        }

        /// <summary>
        /// TeamFightモード用にチーム未割り当てのプレイヤーを自動割り当てします
        /// </summary>
        private void AutoAssignTeamsForTeamFight()
        {
            if (_networkRoomData == null)
            {
                return;
            }

            var slots = _networkRoomData.GetAllPlayerSlots();

            // チーム未割り当て（TeamIndex == -1）のプレイヤーを交互にチームに割り当て
            bool assignToTeamA = true;
            for (int i = 0; i < slots.Length; i++)
            {
                if (!slots[i].IsEmpty() && slots[i].TeamIndex == -1)
                {
                    var slot = slots[i];
                    slot.TeamIndex = assignToTeamA ? 0 : 1; // 0=TeamA, 1=TeamB
                    _networkRoomData.UpdatePlayerSlot(i, slot);

                    Debug.Log($"[LobbyService] Auto-assigned player {slot.PlayerId} to Team{(assignToTeamA ? "A" : "B")} for TeamFight mode");

                    assignToTeamA = !assignToTeamA; // 次のプレイヤーは別チームへ
                }
            }
        }

        /// <summary>
        /// CPUプレイヤーを追加します（ホストのみ）
        /// </summary>
        /// <param name="difficulty">AI難易度</param>
        /// <param name="teamIndex">チームインデックス（-1の場合は未割り当て）</param>
        /// <returns>成功した場合はtrue</returns>
        public bool AddCPUPlayer(AIDifficulty difficulty, int teamIndex = -1)
        {
            Debug.Log($"[LobbyService:{_instanceId}] AddCPUPlayer called. IsHost={IsHost}, _isLocalHost={_isLocalHost}, _isInRoom={_isInRoom}");

            if (!IsHost)
            {
                Debug.LogError($"[LobbyService:{_instanceId}] Only host can add CPU players. IsHost={IsHost}, _isLocalHost={_isLocalHost}, _isInRoom={_isInRoom}");
                return false;
            }

            if (_networkRoomData == null)
            {
                Debug.LogError("[LobbyService] NetworkRoomData not available.");
                return false;
            }

            var slots = _networkRoomData.GetAllPlayerSlots();
            int maxPlayers = _currentRoomSettings.MaxPlayers;

            // 空きスロットを探す（MaxPlayers範囲内のみ）
            int emptySlotIndex = -1;
            for (int i = 0; i < maxPlayers && i < slots.Length; i++)
            {
                if (slots[i].IsEmpty())
                {
                    emptySlotIndex = i;
                    break;
                }
            }

            if (emptySlotIndex == -1)
            {
                Debug.LogError($"[LobbyService] No empty slots available within MaxPlayers limit ({maxPlayers}).");
                return false;
            }

            // CPUプレイヤーを追加（負のインデックスを使用）
            int aiIndex = -(CountCPUPlayers() + 1);
            PlayerSlot cpuSlot = new PlayerSlot(emptySlotIndex, aiIndex, difficulty);

            // TeamFightモードの場合、チームを自動割り当て
            if (CurrentRoomSettings.GameMode == GameMode.TeamFight && teamIndex == -1)
            {
                // 人数の少ないチームに割り当て
                int teamACount = CountPlayersInTeam(0);
                int teamBCount = CountPlayersInTeam(1);
                teamIndex = teamACount <= teamBCount ? 0 : 1;
                Debug.Log($"[LobbyService] Auto-assigning CPU to Team{(teamIndex == 0 ? "A" : "B")} (TeamA: {teamACount}, TeamB: {teamBCount})");
            }

            cpuSlot.TeamIndex = teamIndex;

            _networkRoomData.UpdatePlayerSlot(emptySlotIndex, cpuSlot);

            Debug.Log($"[LobbyService] CPU player added at slot {emptySlotIndex} with difficulty {difficulty}, team {teamIndex}");
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
        /// プレイヤーのチームを変更します（ホストのみ）
        /// </summary>
        /// <param name="playerId">プレイヤーID</param>
        /// <param name="teamIndex">チームインデックス（0=TeamA, 1=TeamB, -1=None）</param>
        /// <returns>成功した場合はtrue</returns>
        public bool SetPlayerTeam(ulong playerId, int teamIndex)
        {
            if (!IsHost)
            {
                Debug.LogError("[LobbyService] Only host can change player teams.");
                return false;
            }

            if (_networkRoomData == null)
            {
                Debug.LogError("[LobbyService] NetworkRoomData not available.");
                return false;
            }

            if (teamIndex < -1 || teamIndex > 1)
            {
                Debug.LogError($"[LobbyService] Invalid team index: {teamIndex}. Must be -1, 0, or 1.");
                return false;
            }

            // TeamFightモードの場合、チームバランスを検証
            if (CurrentRoomSettings.GameMode == GameMode.TeamFight)
            {
                // TeamFightモードではNone（-1）は許可しない
                if (teamIndex == -1)
                {
                    Debug.LogError("[LobbyService] TeamFight mode requires all players to be in a team.");
                    ErrorOccurred?.Invoke("TeamFight mode requires all players to be in a team.");
                    return false;
                }

                // 変更後も両チームにプレイヤーがいることを確認
                if (!WouldTeamChangeBeValid(playerId, teamIndex))
                {
                    Debug.LogError("[LobbyService] Cannot change team: would leave one team empty in TeamFight mode.");
                    ErrorOccurred?.Invoke("Cannot leave a team empty in TeamFight mode.");
                    return false;
                }
            }

            // プレイヤーのスロットインデックスを取得
            int slotIndex = GetSlotIndexByPlayerId(playerId);
            if (slotIndex == -1)
            {
                Debug.LogError($"[LobbyService] Player {playerId} not found in any slot.");
                return false;
            }

            var slots = _networkRoomData.GetAllPlayerSlots();
            var slot = slots[slotIndex];
            slot.TeamIndex = teamIndex;
            _networkRoomData.UpdatePlayerSlot(slotIndex, slot);

            Debug.Log($"[LobbyService] Player {playerId} team changed to {teamIndex} (slot {slotIndex})");
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

            // すべての人間プレイヤー（ホスト以外）が準備完了か確認
            // ホストは常に準備完了とみなす
            ulong hostClientId = Unity.Netcode.NetworkManager.Singleton?.LocalClientId ?? 0;
            bool allReady = slots
                .Where(s => !s.IsEmpty() && !s.IsAI)
                .All(s => s.IsReady || s.PlayerId == hostClientId);

            if (!allReady)
            {
                Debug.LogError("[LobbyService] Not all players are ready.");
                ErrorOccurred?.Invoke("Not all players are ready.");
                return false;
            }

            // TeamFightモードの場合、チームバランスを検証
            if (CurrentRoomSettings.GameMode == GameMode.TeamFight)
            {
                if (!IsTeamBalanceValid())
                {
                    Debug.LogError("[LobbyService] Cannot start TeamFight: both teams must have at least one player.");
                    ErrorOccurred?.Invoke("Both teams must have at least one player to start TeamFight.");
                    return false;
                }
            }

            MatchStarting?.Invoke();

            Debug.Log("[LobbyService] Match starting. Loading match scene...");

            // マッチシーンをロード
            var sceneService = ServiceLocator.Instance.Get<ISceneManagementService>();
            if (sceneService != null)
            {
                sceneService.LoadMatch();
            }
            else
            {
                Debug.LogError("[LobbyService] ISceneManagementService not available. Cannot load match scene.");
                ErrorOccurred?.Invoke("Failed to load match scene");
                return false;
            }

            return true;
        }

        /// <summary>
        /// ゲーム開始カウントダウンを開始します（ホストのみ）
        /// </summary>
        /// <param name="initialSeconds">カウントダウン初期秒数</param>
        /// <returns>成功した場合はtrue</returns>
        public bool StartGameCountdown(int initialSeconds)
        {
            if (!IsHost)
            {
                Debug.LogError("[LobbyService] Only host can start countdown.");
                return false;
            }

            if (_networkLobbyManager == null)
            {
                Debug.LogError("[LobbyService] NetworkLobbyManager not available.");
                return false;
            }

            // NetworkLobbyManager の ServerRpc を呼び出してすべてのクライアントに通知
            _networkLobbyManager.StartCountdownServerRpc(initialSeconds);

            Debug.Log($"[LobbyService] Countdown started: {initialSeconds} seconds");
            return true;
        }

        /// <summary>
        /// ゲーム開始カウントダウンをキャンセルします（ホストのみ）
        /// </summary>
        /// <returns>成功した場合はtrue</returns>
        public bool CancelGameCountdown()
        {
            if (!IsHost)
            {
                Debug.LogError("[LobbyService] Only host can cancel countdown.");
                return false;
            }

            if (_networkLobbyManager == null)
            {
                Debug.LogError("[LobbyService] NetworkLobbyManager not available.");
                return false;
            }

            // NetworkLobbyManager の ServerRpc を呼び出してすべてのクライアントに通知
            _networkLobbyManager.CancelCountdownServerRpc();

            Debug.Log("[LobbyService] Countdown cancelled");
            return true;
        }

        /// <summary>
        /// マッチを終了してルームに戻ることを要求します（ホストのみ）
        /// </summary>
        /// <remarks>
        /// このメソッドを呼び出すと、すべてのクライアントに通知が送られ、
        /// 全員がMatchRoomシーンに戻ります。
        /// </remarks>
        /// <returns>成功した場合はtrue</returns>
        public bool RequestReturnToRoom()
        {
            if (!IsHost)
            {
                Debug.LogError("[LobbyService] Only host can request return to room.");
                return false;
            }

            if (_networkLobbyManager == null)
            {
                Debug.LogError("[LobbyService] NetworkLobbyManager not available.");
                return false;
            }

            // NetworkLobbyManager の ServerRpc を呼び出してすべてのクライアントに通知
            _networkLobbyManager.RequestReturnToRoomServerRpc();

            Debug.Log("[LobbyService] Return to room requested");
            return true;
        }

        #endregion

        #region Guest Methods

        /// <summary>
        /// ジョインコードを使用してルームに参加します（ゲスト）
        /// </summary>
        /// <remarks>
        /// このメソッドは非同期処理を開始し、即座に制御を返します。
        /// 実際の成功/失敗は RoomJoined イベントまたは ErrorOccurred イベントで通知されます。
        /// </remarks>
        /// <param name="joinCode">ジョインコード</param>
        /// <param name="playerName">プレイヤー名</param>
        /// <param name="password">パスワード（必要な場合）</param>
        /// <returns>リクエストが受け付けられた場合はtrue（初期化エラーや既にルームに参加している場合はfalse）</returns>
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

            // 既に参加処理中の場合は重複呼び出しを防止
            if (_isJoiningRoom)
            {
                Debug.LogWarning("[LobbyService] Join operation already in progress.");
                return false;
            }

            // NetworkManagerが既に稼働中の場合は参加を防止
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                Debug.LogWarning("[LobbyService] NetworkManager is already running.");
                return false;
            }

            _isJoiningRoom = true;

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
            try
            {
                // RelayManager の初期化を確実に待つ
                bool initialized = await _relayManager.InitializeAsync();
                if (!initialized)
                {
                    ErrorOccurred?.Invoke("Failed to initialize RelayManager.");
                    return;
                }

                bool relayJoined = await _relayManager.JoinRelayAsync(joinCode);

                if (!relayJoined)
                {
                    ErrorOccurred?.Invoke("Failed to join relay.");
                    return;
                }

                // 念のためNetworkManagerが既に稼働中かチェック
                if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
                {
                    Debug.LogWarning("[LobbyService] NetworkManager is already listening. Skipping StartClient.");
                    ErrorOccurred?.Invoke("NetworkManager is already running.");
                    return;
                }

                // NetworkManagerを開始
                if (!NetworkManager.Singleton.StartClient())
                {
                    ErrorOccurred?.Invoke("Failed to start client.");
                    return;
                }

                _isInRoom = true;
                _isLocalHost = false; // ゲストフラグを設定

                // ローカルプレイヤー情報を設定
                _localPlayerInfo = new LobbyPlayerInfo(
                    NetworkManager.Singleton.LocalClientId,
                    playerName,
                    false
                );
                _localPlayerInfo.IsLocalPlayer = true;

                // プレイヤー名を保存
                SavePlayerName(playerName);

                // NetworkLobbyManagerにプレイヤー名を登録
                if (_networkLobbyManager != null)
                {
                    _networkLobbyManager.RegisterPlayerName(playerName);
                }

                RoomJoined?.Invoke();
                Debug.Log($"[LobbyService] Joined room with code: {joinCode}");
            }
            finally
            {
                // 成功・失敗に関わらずフラグをリセット
                _isJoiningRoom = false;
            }
        }

        #endregion

        #region Common Methods

        /// <summary>
        /// ルームから退出します
        /// </summary>
        /// <remarks>
        /// ホストが退出する場合、NetworkManager.Shutdown()によってサーバーがシャットダウンされます。
        /// これにより、すべてのゲストクライアントでOnClientDisconnectedコールバックが発火し、
        /// HostDisconnectedイベントが通知されます。ゲストはこのイベントを受けてロビーに戻ります。
        /// </remarks>
        public void LeaveRoom()
        {
            if (!_isInRoom)
            {
                return;
            }

            // まずRelay/Transport接続をクリーンアップ（NetworkManager.Shutdownの前に）
            _relayManager.Cleanup();

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                // ホストの場合はサーバーをシャットダウン（すべてのクライアントが切断される）
                // ゲストの場合はクライアントのみシャットダウン
                NetworkManager.Singleton.Shutdown(true); // discardMessageQueue: true で即座にシャットダウン
            }

            _isInRoom = false;
            _isLocalHost = false;
            _isStartingHost = false; // 重複呼び出し防止フラグもリセット
            _isJoiningRoom = false; // 参加処理中フラグもリセット
            _localPlayerInfo = null;
            _networkRoomData = null; // Clear reference to old NetworkRoomData

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
        /// プレイヤー名を設定します
        /// </summary>
        /// <param name="playerName">新しいプレイヤー名</param>
        public void SetPlayerName(string playerName)
        {
            if (string.IsNullOrWhiteSpace(playerName))
            {
                Debug.LogError("[LobbyService] Player name cannot be empty.");
                return;
            }

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

            // プレイヤー名を保存（次回使用のため）
            SavePlayerName(playerName);

            // ローカルプレイヤー情報を更新
            _localPlayerInfo.PlayerName = playerName;

            // NetworkRoomDataのServerRPCを呼び出してプレイヤー名を変更
            _networkRoomData.SetPlayerNameServerRpc(playerName);

            Debug.Log($"[LobbyService] Player name set to: {playerName}");
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

        /// <summary>
        /// 利用可能なルームリストを更新します
        /// </summary>
        public void RefreshAvailableRooms()
        {
            // プレースホルダー: 現在のルーム情報をリストに追加（ホストの場合のみ）
            _availableRooms.Clear();

            if (IsHost && IsInRoom)
            {
                var roomInfo = new RoomInfo
                {
                    RoomId = CurrentJoinCode ?? "",
                    RoomName = CurrentRoomSettings.RoomName.ToString(),
                    HostName = _localPlayerInfo?.PlayerName ?? "Unknown",
                    GameMode = CurrentRoomSettings.GameMode,
                    MapName = CurrentRoomSettings.MapName.ToString(),
                    CurrentPlayers = PlayerSlots.Count(s => !s.IsEmpty()),
                    MaxPlayers = CurrentRoomSettings.MaxPlayers,
                    HasPassword = !string.IsNullOrEmpty(CurrentRoomSettings.Password.ToString()),
                    JoinCode = CurrentJoinCode ?? "",
                    IsPublic = CurrentRoomSettings.IsPublic
                };

                _availableRooms.Add(roomInfo);
            }

            // イベント発火
            AvailableRoomsUpdated?.Invoke(AvailableRooms);

            Debug.Log($"[LobbyService] Available rooms refreshed. Count: {_availableRooms.Count}");
        }

        #endregion

        #region Player Name Persistence

        /// <summary>
        /// プレイヤー名を保存します
        /// </summary>
        /// <param name="playerName">保存するプレイヤー名</param>
        public void SavePlayerName(string playerName)
        {
            if (string.IsNullOrWhiteSpace(playerName))
            {
                Debug.LogWarning("[LobbyService] Cannot save empty player name.");
                return;
            }

            PlayerPrefs.SetString(PlayerNamePrefKey, playerName);
            PlayerPrefs.Save();
            Debug.Log($"[LobbyService] Player name saved: {playerName}");
        }

        /// <summary>
        /// 保存されたプレイヤー名を読み込みます
        /// </summary>
        /// <returns>保存されたプレイヤー名（存在しない場合はnull）</returns>
        public string? LoadPlayerName()
        {
            if (PlayerPrefs.HasKey(PlayerNamePrefKey))
            {
                string playerName = PlayerPrefs.GetString(PlayerNamePrefKey);
                Debug.Log($"[LobbyService] Player name loaded: {playerName}");
                return playerName;
            }

            Debug.Log("[LobbyService] No saved player name found.");
            return null;
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

        /// <summary>
        /// 指定されたチームのプレイヤー数をカウントします
        /// </summary>
        /// <param name="teamIndex">チームインデックス（0=TeamA, 1=TeamB）</param>
        /// <returns>チームのプレイヤー数</returns>
        private int CountPlayersInTeam(int teamIndex)
        {
            if (_networkRoomData == null)
            {
                return 0;
            }

            var slots = _networkRoomData.GetAllPlayerSlots();
            return slots.Count(s => !s.IsEmpty() && s.TeamIndex == teamIndex);
        }

        /// <summary>
        /// チームバランスが有効かどうかを検証します（TeamFightモード用）
        /// </summary>
        /// <returns>両チームに少なくとも1人ずつプレイヤーがいる場合はtrue</returns>
        private bool IsTeamBalanceValid()
        {
            int teamACount = CountPlayersInTeam(0); // TeamA
            int teamBCount = CountPlayersInTeam(1); // TeamB
            return teamACount > 0 && teamBCount > 0;
        }

        /// <summary>
        /// チーム変更後のバランスを検証します
        /// </summary>
        /// <param name="playerId">チームを変更するプレイヤーID</param>
        /// <param name="newTeamIndex">新しいチームインデックス</param>
        /// <returns>変更後も両チームにプレイヤーがいる場合はtrue</returns>
        private bool WouldTeamChangeBeValid(ulong playerId, int newTeamIndex)
        {
            if (_networkRoomData == null)
            {
                return false;
            }

            var slots = _networkRoomData.GetAllPlayerSlots();

            // 現在のプレイヤーのチームを取得
            int currentTeamIndex = -1;
            foreach (var slot in slots)
            {
                if (!slot.IsEmpty() && slot.PlayerId == playerId)
                {
                    currentTeamIndex = slot.TeamIndex;
                    break;
                }
            }

            // 同じチームへの変更なら問題なし
            if (currentTeamIndex == newTeamIndex)
            {
                return true;
            }

            // 変更後の各チームの人数をシミュレート
            int teamACount = 0;
            int teamBCount = 0;

            foreach (var slot in slots)
            {
                if (slot.IsEmpty())
                {
                    continue;
                }

                int teamIndex = slot.PlayerId == playerId ? newTeamIndex : slot.TeamIndex;

                if (teamIndex == 0)
                {
                    teamACount++;
                }
                else if (teamIndex == 1)
                {
                    teamBCount++;
                }
            }

            return teamACount > 0 && teamBCount > 0;
        }

        #endregion
    }
}
