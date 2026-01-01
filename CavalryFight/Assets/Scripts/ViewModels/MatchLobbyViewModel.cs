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
        private string _statusMessage = "ルームをホストするか、参加してください";

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

        #endregion

        #region Properties

        /// <summary>
        /// プレイヤー名
        /// </summary>
        public string PlayerName
        {
            get => _playerName;
            set => SetProperty(ref _playerName, value);
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

            IsInRoom = true;
            IsProcessing = false;
            _isNavigatingToRoom = true;
            StatusMessage = $"ルームをホスト中: {_roomName}";
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

            IsInRoom = true;
            IsProcessing = false;
            _isNavigatingToRoom = true;
            StatusMessage = "ルームに参加しました";
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
            StatusMessage = "ルームをホストするか、参加してください";
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

            StatusMessage = $"エラー: {errorMessage}";
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
                StatusMessage = "既にルームに参加しています";
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
                StatusMessage = "既にルームに参加しています";
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
                StatusMessage = "ルーム名を入力してください";
                ErrorOccurred?.Invoke(this, "ルーム名を入力してください");
                return;
            }

            if (string.IsNullOrWhiteSpace(PlayerName))
            {
                StatusMessage = "プレイヤー名を入力してください";
                ErrorOccurred?.Invoke(this, "プレイヤー名を入力してください");
                return;
            }

            // FixedString64Bytesのバイト数制限をチェック（UTF-8で64バイトまで）
            if (System.Text.Encoding.UTF8.GetByteCount(PlayerName) > 64)
            {
                StatusMessage = "プレイヤー名が長すぎます（64バイトまで）";
                ErrorOccurred?.Invoke(this, "プレイヤー名が長すぎます。日本語の場合は約21文字までです。");
                Debug.LogWarning($"[MatchLobbyViewModel] Player name too long: {PlayerName} ({System.Text.Encoding.UTF8.GetByteCount(PlayerName)} bytes)");
                return;
            }

            if (System.Text.Encoding.UTF8.GetByteCount(RoomName) > 64)
            {
                StatusMessage = "ルーム名が長すぎます（64バイトまで）";
                ErrorOccurred?.Invoke(this, "ルーム名が長すぎます。日本語の場合は約21文字までです。");
                Debug.LogWarning($"[MatchLobbyViewModel] Room name too long: {RoomName} ({System.Text.Encoding.UTF8.GetByteCount(RoomName)} bytes)");
                return;
            }

            if (System.Text.Encoding.UTF8.GetByteCount(SelectedMap) > 64)
            {
                StatusMessage = "マップ名が長すぎます（64バイトまで）";
                ErrorOccurred?.Invoke(this, "マップ名が長すぎます。");
                Debug.LogWarning($"[MatchLobbyViewModel] Map name too long: {SelectedMap} ({System.Text.Encoding.UTF8.GetByteCount(SelectedMap)} bytes)");
                return;
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
                MapName = new FixedString64Bytes(SelectedMap)
            };

            IsProcessing = true;
            bool success = _lobbyService.CreateRoom(roomSettings, PlayerName);

            if (success)
            {
                StatusMessage = "ルームを作成中...";
                Debug.Log("[MatchLobbyViewModel] Creating room...");
            }
            else
            {
                StatusMessage = "ルーム作成に失敗しました";
                ErrorOccurred?.Invoke(this, "ルーム作成に失敗しました");
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
                StatusMessage = "ジョインコードを入力してください";
                ErrorOccurred?.Invoke(this, "ジョインコードを入力してください");
                return;
            }

            if (string.IsNullOrWhiteSpace(PlayerName))
            {
                StatusMessage = "プレイヤー名を入力してください";
                ErrorOccurred?.Invoke(this, "プレイヤー名を入力してください");
                return;
            }

            // FixedString64Bytesのバイト数制限をチェック（UTF-8で64バイトまで）
            if (System.Text.Encoding.UTF8.GetByteCount(PlayerName) > 64)
            {
                StatusMessage = "プレイヤー名が長すぎます（64バイトまで）";
                ErrorOccurred?.Invoke(this, "プレイヤー名が長すぎます。日本語の場合は約21文字までです。");
                Debug.LogWarning($"[MatchLobbyViewModel] Player name too long: {PlayerName} ({System.Text.Encoding.UTF8.GetByteCount(PlayerName)} bytes)");
                return;
            }

            IsProcessing = true;
            bool success = _lobbyService.JoinRoom(JoinCode, PlayerName);

            if (success)
            {
                StatusMessage = "ルームに参加中...";
                Debug.Log("[MatchLobbyViewModel] Joining room...");
            }
            else
            {
                StatusMessage = "ルーム参加に失敗しました";
                ErrorOccurred?.Invoke(this, "ルーム参加に失敗しました");
                IsProcessing = false;
            }
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
            StatusMessage = "ルームリストを更新中...";
            _lobbyService.RefreshAvailableRooms();
            Debug.Log("[MatchLobbyViewModel] Room list refresh requested.");
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
