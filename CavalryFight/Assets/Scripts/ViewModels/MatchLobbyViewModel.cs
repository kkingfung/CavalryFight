#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using CavalryFight.Core.MVVM;
using CavalryFight.Core.Services;
using CavalryFight.Services.Lobby;
using CavalryFight.Services.SceneManagement;
using Unity.Collections;
using UnityEngine;

namespace CavalryFight.ViewModels
{
    /// <summary>
    /// マッチロビー画面のViewModel
    /// </summary>
    /// <remarks>
    /// ルームの作成、参加、管理を行います。
    /// ILobbyServiceを使用してマルチプレイヤーロビーを管理します。
    /// </remarks>
    public class MatchLobbyViewModel : ViewModelBase
    {
        #region Fields

        /// <summary>
        /// ロビーサービス
        /// </summary>
        private readonly ILobbyService _lobbyService;

        /// <summary>
        /// シーン管理サービス
        /// </summary>
        private readonly ISceneManagementService _sceneManagementService;

        /// <summary>
        /// プレイヤー名
        /// </summary>
        private string _playerName = "Player";

        /// <summary>
        /// ルーム名
        /// </summary>
        private string _roomName = "My Room";

        /// <summary>
        /// ジョインコード
        /// </summary>
        private string _joinCode = "";

        /// <summary>
        /// パスワード（パスワード保護されたルームに参加する場合）
        /// </summary>
        private string _password = "";

        /// <summary>
        /// 選択されたゲームモード
        /// </summary>
        private GameMode _selectedGameMode = GameMode.Arena;

        /// <summary>
        /// 選択された最大プレイヤー数
        /// </summary>
        private int _selectedMaxPlayers = 8;

        /// <summary>
        /// 選択されたマップ（フィールドプレハブ名と一致）
        /// </summary>
        private string _selectedMap = "Arena";

        /// <summary>
        /// ステータスメッセージ
        /// </summary>
        private string _statusMessage = "Host or join a room";

        /// <summary>
        /// ルームに参加しているかどうか
        /// </summary>
        private bool _isInRoom = false;

        /// <summary>
        /// ホストダイアログを表示するかどうか
        /// </summary>
        private bool _showHostDialog = false;

        /// <summary>
        /// 参加ダイアログを表示するかどうか
        /// </summary>
        private bool _showJoinDialog = false;

        /// <summary>
        /// 選択されたルーム
        /// </summary>
        private RoomInfo? _selectedRoom = null;

        /// <summary>
        /// ルームシーンに遷移中かどうか
        /// </summary>
        private bool _isNavigatingToRoom = false;

        /// <summary>
        /// ルーム作成/参加処理中かどうか
        /// </summary>
        private bool _isProcessing = false;

        /// <summary>
        /// 操作がキャンセルされたかどうか
        /// </summary>
        private bool _isCancelled = false;

        #endregion

        #region Properties

        /// <summary>
        /// プレイヤー名の最大文字数
        /// </summary>
        public const int MaxPlayerNameLength = 12;

        /// <summary>
        /// プレイヤー名
        /// </summary>
        public string PlayerName
        {
            get => _playerName;
            set
            {
                // 12文字を超える場合は切り詰める
                string truncated = value?.Length > MaxPlayerNameLength
                    ? value.Substring(0, MaxPlayerNameLength)
                    : value ?? "";
                SetProperty(ref _playerName, truncated);
            }
        }

        /// <summary>
        /// ルーム名
        /// </summary>
        public string RoomName
        {
            get => _roomName;
            set => SetProperty(ref _roomName, value);
        }

        /// <summary>
        /// ジョインコード
        /// </summary>
        public string JoinCode
        {
            get => _joinCode;
            set => SetProperty(ref _joinCode, value);
        }

        /// <summary>
        /// パスワード（パスワード保護されたルームに参加する場合）
        /// </summary>
        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        /// <summary>
        /// 選択されたゲームモード
        /// </summary>
        public GameMode SelectedGameMode
        {
            get => _selectedGameMode;
            set => SetProperty(ref _selectedGameMode, value);
        }

        /// <summary>
        /// 選択された最大プレイヤー数
        /// </summary>
        public int SelectedMaxPlayers
        {
            get => _selectedMaxPlayers;
            set => SetProperty(ref _selectedMaxPlayers, value);
        }

        /// <summary>
        /// 選択されたマップ
        /// </summary>
        public string SelectedMap
        {
            get => _selectedMap;
            set => SetProperty(ref _selectedMap, value);
        }

        /// <summary>
        /// ステータスメッセージ
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        /// <summary>
        /// ルームに参加しているかどうか
        /// </summary>
        public bool IsInRoom
        {
            get => _isInRoom;
            set => SetProperty(ref _isInRoom, value);
        }

        /// <summary>
        /// ホストダイアログを表示するかどうか
        /// </summary>
        public bool ShowHostDialog
        {
            get => _showHostDialog;
            set => SetProperty(ref _showHostDialog, value);
        }

        /// <summary>
        /// 参加ダイアログを表示するかどうか
        /// </summary>
        public bool ShowJoinDialog
        {
            get => _showJoinDialog;
            set => SetProperty(ref _showJoinDialog, value);
        }

        /// <summary>
        /// 現在のジョインコード（ホスト時のみ）
        /// </summary>
        public string? CurrentJoinCode => _lobbyService.CurrentJoinCode;

        /// <summary>
        /// 利用可能なルームリスト
        /// </summary>
        public IReadOnlyList<RoomInfo> AvailableRooms => _lobbyService.AvailableRooms;

        /// <summary>
        /// 選択されたルーム
        /// </summary>
        public RoomInfo? SelectedRoom
        {
            get => _selectedRoom;
            set => SetProperty(ref _selectedRoom, value);
        }

        /// <summary>
        /// ルーム作成/参加処理中かどうか
        /// </summary>
        public bool IsProcessing
        {
            get => _isProcessing;
            private set => SetProperty(ref _isProcessing, value);
        }

        #endregion

        #region Events

        /// <summary>
        /// ルームシーンへの遷移を要求するイベント
        /// </summary>
        public event EventHandler? NavigateToRoomRequested;

        /// <summary>
        /// エラーが発生したときのイベント
        /// </summary>
        public event EventHandler<string>? ErrorOccurred;

        #endregion

        #region Constructors

        /// <summary>
        /// MatchLobbyViewModelの新しいインスタンスを初期化します
        /// </summary>
        /// <param name="lobbyService">ロビーサービス</param>
        /// <param name="sceneManagementService">シーン管理サービス</param>
        public MatchLobbyViewModel(ILobbyService lobbyService, ISceneManagementService sceneManagementService)
        {
            _lobbyService = lobbyService ?? throw new ArgumentNullException(nameof(lobbyService));
            _sceneManagementService = sceneManagementService ?? throw new ArgumentNullException(nameof(sceneManagementService));

            // 保存されたプレイヤー名を読み込む
            string? savedPlayerName = _lobbyService.LoadPlayerName();
            if (!string.IsNullOrWhiteSpace(savedPlayerName))
            {
                PlayerName = savedPlayerName;
                Debug.Log($"[MatchLobbyViewModel] Loaded saved player name: {savedPlayerName}");
            }

            // ロビーサービスのイベントを購読
            SubscribeToLobbyEvents();

            Debug.Log("[MatchLobbyViewModel] Initialized.");
        }

        #endregion

        #region Initialization

        /// <summary>
        /// ロビーサービスのイベントを購読します
        /// </summary>
        private void SubscribeToLobbyEvents()
        {
            _lobbyService.RoomCreated += OnRoomCreated;
            _lobbyService.RoomJoined += OnRoomJoined;
            _lobbyService.RoomLeft += OnRoomLeft;
            _lobbyService.ErrorOccurred += OnLobbyError;
            _lobbyService.AvailableRoomsUpdated += OnAvailableRoomsUpdated;
        }

        /// <summary>
        /// ロビーサービスのイベント購読を解除します
        /// </summary>
        private void UnsubscribeFromLobbyEvents()
        {
            _lobbyService.RoomCreated -= OnRoomCreated;
            _lobbyService.RoomJoined -= OnRoomJoined;
            _lobbyService.RoomLeft -= OnRoomLeft;
            _lobbyService.ErrorOccurred -= OnLobbyError;
            _lobbyService.AvailableRoomsUpdated -= OnAvailableRoomsUpdated;
        }

        #endregion

        #region Lobby Event Handlers

        /// <summary>
        /// ルーム作成イベントハンドラ
        /// </summary>
        /// <param name="joinCode">ジョインコード</param>
        private void OnRoomCreated(string joinCode)
        {
            Debug.Log($"[MatchLobbyViewModel] Room created with join code: {joinCode}");

            IsProcessing = false;

            // キャンセルされた場合はルームから退出して遷移しない
            if (_isCancelled)
            {
                Debug.Log("[MatchLobbyViewModel] Operation was cancelled, leaving room.");
                _lobbyService.LeaveRoom();
                _isCancelled = false;
                return;
            }

            IsInRoom = true;
            _isNavigatingToRoom = true;
            StatusMessage = $"Hosting room: {_roomName}";
            ShowHostDialog = false;

            OnPropertyChanged(nameof(CurrentJoinCode));

            // ルームシーンに遷移
            NavigateToRoomRequested?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// ルーム参加イベントハンドラ
        /// </summary>
        private void OnRoomJoined()
        {
            Debug.Log("[MatchLobbyViewModel] Successfully joined room.");

            IsProcessing = false;

            // キャンセルされた場合はルームから退出して遷移しない
            if (_isCancelled)
            {
                Debug.Log("[MatchLobbyViewModel] Operation was cancelled, leaving room.");
                _lobbyService.LeaveRoom();
                _isCancelled = false;
                return;
            }

            IsInRoom = true;
            _isNavigatingToRoom = true;
            StatusMessage = "Successfully joined room";
            ShowJoinDialog = false;

            // ルームシーンに遷移
            NavigateToRoomRequested?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// ルーム退出イベントハンドラ
        /// </summary>
        private void OnRoomLeft()
        {
            Debug.Log("[MatchLobbyViewModel] Left room.");

            IsInRoom = false;
            StatusMessage = "Host or join a room";
            JoinCode = "";

            OnPropertyChanged(nameof(CurrentJoinCode));
        }

        /// <summary>
        /// ロビーエラーイベントハンドラ
        /// </summary>
        /// <param name="errorMessage">エラーメッセージ</param>
        private void OnLobbyError(string errorMessage)
        {
            Debug.LogError($"[MatchLobbyViewModel] Lobby error: {errorMessage}");

            StatusMessage = $"Error: {errorMessage}";
            IsProcessing = false;
            ErrorOccurred?.Invoke(this, errorMessage);
        }

        /// <summary>
        /// 利用可能なルームリスト更新イベントハンドラ
        /// </summary>
        /// <param name="rooms">ルームリスト</param>
        private void OnAvailableRoomsUpdated(IReadOnlyList<RoomInfo> rooms)
        {
            Debug.Log($"[MatchLobbyViewModel] Available rooms updated. Count: {rooms.Count}");

            // AvailableRoomsプロパティの変更を通知
            OnPropertyChanged(nameof(AvailableRooms));
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// ホストダイアログを表示します
        /// </summary>
        public void OpenHostDialog()
        {
            if (IsInRoom)
            {
                StatusMessage = "Already in a room";
                return;
            }

            ShowHostDialog = true;
            Debug.Log("[MatchLobbyViewModel] Host dialog opened.");
        }

        /// <summary>
        /// ホストダイアログを閉じます
        /// </summary>
        public void CloseHostDialog()
        {
            ShowHostDialog = false;
            Debug.Log("[MatchLobbyViewModel] Host dialog closed.");
        }

        /// <summary>
        /// 参加ダイアログを表示します
        /// </summary>
        public void OpenJoinDialog()
        {
            if (IsInRoom)
            {
                StatusMessage = "Already in a room";
                return;
            }

            ShowJoinDialog = true;
            Debug.Log("[MatchLobbyViewModel] Join dialog opened.");
        }

        /// <summary>
        /// 参加ダイアログを閉じます
        /// </summary>
        public void CloseJoinDialog()
        {
            ShowJoinDialog = false;
            Debug.Log("[MatchLobbyViewModel] Join dialog closed.");
        }

        /// <summary>
        /// ルームを作成します
        /// </summary>
        public void CreateRoom()
        {
            if (string.IsNullOrWhiteSpace(RoomName))
            {
                StatusMessage = "Please enter a room name";
                ErrorOccurred?.Invoke(this, "Please enter a room name");
                return;
            }

            if (string.IsNullOrWhiteSpace(PlayerName))
            {
                StatusMessage = "Please enter a player name";
                ErrorOccurred?.Invoke(this, "Please enter a player name");
                return;
            }

            // プレイヤー名の長さチェック（12文字まで）
            if (PlayerName.Length > MaxPlayerNameLength)
            {
                StatusMessage = $"Player name is too long (max {MaxPlayerNameLength} characters)";
                ErrorOccurred?.Invoke(this, $"Player name is too long (max {MaxPlayerNameLength} characters)");
                Debug.LogWarning($"[MatchLobbyViewModel] Player name too long: {PlayerName} ({PlayerName.Length} characters)");
                return;
            }

            if (System.Text.Encoding.UTF8.GetByteCount(RoomName) > 64)
            {
                StatusMessage = "Room name is too long (max 64 bytes)";
                ErrorOccurred?.Invoke(this, "Room name is too long (max 64 bytes)");
                Debug.LogWarning($"[MatchLobbyViewModel] Room name too long: {RoomName} ({System.Text.Encoding.UTF8.GetByteCount(RoomName)} bytes)");
                return;
            }

            if (System.Text.Encoding.UTF8.GetByteCount(SelectedMap) > 64)
            {
                StatusMessage = "Map name is too long (max 64 bytes)";
                ErrorOccurred?.Invoke(this, "Map name is too long (max 64 bytes)");
                Debug.LogWarning($"[MatchLobbyViewModel] Map name too long: {SelectedMap} ({System.Text.Encoding.UTF8.GetByteCount(SelectedMap)} bytes)");
                return;
            }

            // マップ名を解析
            MapName parsedMapName = MapName.Arena;
            if (System.Enum.TryParse<MapName>(SelectedMap, true, out var mapNameResult))
            {
                parsedMapName = mapNameResult;
            }

            var roomSettings = new RoomSettings
            {
                RoomName = new FixedString64Bytes(RoomName),
                GameMode = SelectedGameMode,
                MaxPlayers = SelectedMaxPlayers,
                Password = new FixedString64Bytes(),
                IsPublic = false,
                TimeLimit = 300,
                ArrowLimit = 0, // デフォルトは無制限（ScoreMatchの場合は後で設定変更可能）
                MapName = parsedMapName
            };

            _isCancelled = false;
            IsProcessing = true;
            bool success = _lobbyService.CreateRoom(roomSettings, PlayerName);

            if (success)
            {
                StatusMessage = "Creating room...";
                Debug.Log("[MatchLobbyViewModel] Creating room...");
            }
            else
            {
                StatusMessage = "Failed to create room";
                ErrorOccurred?.Invoke(this, "Failed to create room");
                IsProcessing = false;
            }
        }

        /// <summary>
        /// ルームに参加します
        /// </summary>
        public void JoinRoom()
        {
            if (string.IsNullOrWhiteSpace(JoinCode))
            {
                StatusMessage = "Please enter a join code";
                ErrorOccurred?.Invoke(this, "Please enter a join code");
                return;
            }

            if (string.IsNullOrWhiteSpace(PlayerName))
            {
                StatusMessage = "Please enter a player name";
                ErrorOccurred?.Invoke(this, "Please enter a player name");
                return;
            }

            // プレイヤー名の長さチェック（12文字まで）
            if (PlayerName.Length > MaxPlayerNameLength)
            {
                StatusMessage = $"Player name is too long (max {MaxPlayerNameLength} characters)";
                ErrorOccurred?.Invoke(this, $"Player name is too long (max {MaxPlayerNameLength} characters)");
                Debug.LogWarning($"[MatchLobbyViewModel] Player name too long: {PlayerName} ({PlayerName.Length} characters)");
                return;
            }

            // パスワード保護されたルームに参加する場合のチェック
            if (SelectedRoom != null && SelectedRoom.HasPassword && string.IsNullOrWhiteSpace(Password))
            {
                StatusMessage = "Please enter the room password";
                ErrorOccurred?.Invoke(this, "This room requires a password");
                return;
            }

            _isCancelled = false;
            IsProcessing = true;
            bool success = _lobbyService.JoinRoom(JoinCode, PlayerName, Password);

            if (success)
            {
                StatusMessage = "Joining room...";
                Debug.Log("[MatchLobbyViewModel] Joining room...");
            }
            else
            {
                StatusMessage = "Failed to join room";
                ErrorOccurred?.Invoke(this, "Failed to join room");
                IsProcessing = false;
            }

            // パスワードをクリア（セキュリティのため）
            Password = "";
        }

        /// <summary>
        /// メインメニューに戻ります
        /// </summary>
        public void BackToMainMenu()
        {
            // ルームに参加している場合は退出
            if (IsInRoom)
            {
                _lobbyService.LeaveRoom();
            }

            _sceneManagementService.LoadMainMenu();
            Debug.Log("[MatchLobbyViewModel] Returning to main menu.");
        }

        /// <summary>
        /// 利用可能なルームリストを更新します
        /// </summary>
        public void RefreshRooms()
        {
            StatusMessage = "Refreshing room list...";
            _lobbyService.RefreshAvailableRooms();
            Debug.Log("[MatchLobbyViewModel] Room list refresh requested.");
        }

        /// <summary>
        /// 現在の操作をキャンセルします
        /// </summary>
        public void CancelOperation()
        {
            if (!IsProcessing)
            {
                return;
            }

            _isCancelled = true;
            IsProcessing = false;
            StatusMessage = "Operation cancelled";
            Debug.Log("[MatchLobbyViewModel] Operation cancelled.");
        }

        /// <summary>
        /// ルームを選択します
        /// </summary>
        /// <param name="room">選択するルーム</param>
        public void SelectRoom(RoomInfo? room)
        {
            SelectedRoom = room;

            if (room != null)
            {
                Debug.Log($"[MatchLobbyViewModel] Room selected: {room.RoomName}");
            }
            else
            {
                Debug.Log("[MatchLobbyViewModel] Room selection cleared.");
            }
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// リソースを解放します
        /// </summary>
        protected override void OnDispose()
        {
            UnsubscribeFromLobbyEvents();

            // ルームシーンに遷移する場合は退出しない（MatchRoomViewModelで引き続き使用するため）
            // ユーザーがバックボタンなどで明示的に退出する場合のみLeaveRoom()を呼ぶ
            if (IsInRoom && !_isNavigatingToRoom)
            {
                Debug.Log("[MatchLobbyViewModel] Leaving room on dispose (not navigating to room).");
                _lobbyService.LeaveRoom();
            }

            base.OnDispose();
        }

        #endregion
    }
}
