#nullable enable

using System;
using System.Collections.Generic;
using CavalryFight.Core.MVVM;
using CavalryFight.Core.Services;
using CavalryFight.Services.Audio;
using CavalryFight.Services.Lobby;
using CavalryFight.Services.SceneManagement;
using CavalryFight.ViewModels;
using UnityEngine;
using UnityEngine.UIElements;

namespace CavalryFight.Views
{
    /// <summary>
    /// マッチロビー画面のView
    /// </summary>
    /// <remarks>
    /// ルームの作成と参加を管理する画面です。
    /// MatchLobbyViewModelと連携して動作します。
    /// </remarks>
    [RequireComponent(typeof(UIDocument))]
    public class MatchLobbyView : UIToolkitViewBase<MatchLobbyViewModel>
    {
        #region Serialized Fields

        [Header("Audio")]
        [SerializeField] private AudioClip? _bgmClip;
        [SerializeField] private AudioClip? _buttonClickSfx;

        #endregion

        #region UI Elements

        // Header
        private Button? _refreshButton;
        private Button? _backButton;

        // Left Panel - Room List
        private VisualElement? _roomListContainer;
        private VisualElement? _emptyState;
        private ScrollView? _roomListScrollView;

        // Right Panel - Room Details
        private VisualElement? _noSelectionState;
        private VisualElement? _detailsContent;
        private Label? _roomNameLabel;
        private Label? _hostNameLabel;
        private Label? _gameModeLabel;
        private Label? _mapLabel;
        private Label? _playersLabel;

        // Join by Code
        private TextField? _joinCodeInput;
        private Button? _joinByCodeButton;

        // Join Form
        private TextField? _playerNameInput;
        private VisualElement? _passwordRow;
        private TextField? _passwordInput;
        private Button? _joinRoomButton;

        // Footer
        private Button? _hostRoomButton;

        // Status
        private Label? _statusLabel;

        #endregion

        #region Fields

        private readonly Dictionary<string, VisualElement> _roomItemElements = new Dictionary<string, VisualElement>();
        private VisualElement? _currentSelectedElement;

        #endregion

        #region Unity Lifecycle

        /// <summary>
        /// 初期化処理
        /// </summary>
        protected override void Awake()
        {
            base.Awake();

            // サービスを取得（例外を回避するためTryGetを使用）
            var lobbyService = ServiceLocator.Instance.TryGet<ILobbyService>();
            var sceneService = ServiceLocator.Instance.TryGet<ISceneManagementService>();

            if (lobbyService == null || sceneService == null)
            {
                Debug.LogError("[MatchLobbyView] Required services not found! Disabling component.");
                enabled = false;
                return;
            }

            // ViewModelを作成して設定
            ViewModel = new MatchLobbyViewModel(lobbyService, sceneService);
        }

        /// <summary>
        /// 有効化時の処理
        /// </summary>
        protected override void OnEnable()
        {
            base.OnEnable();

            // BGMを再生
            if (_bgmClip != null)
            {
                var audioService = ServiceLocator.Instance.Get<IAudioService>();
                if (audioService != null)
                {
                    audioService.PlayBgm(_bgmClip, loop: true, fadeInDuration: 2f);
                }
            }
        }

        /// <summary>
        /// 無効化時の処理
        /// </summary>
        protected override void OnDisable()
        {
            // BGMは停止しない（シーン遷移時の継続再生のため）
            // 次のシーンが異なるBGMを要求する場合は、そのシーンのOnEnable()で自動的に切り替わる
            base.OnDisable();
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// ルートビジュアル要素が準備できたときの処理
        /// </summary>
        /// <param name="root">ルートビジュアル要素</param>
        protected override void OnRootVisualElementReady(VisualElement root)
        {
            base.OnRootVisualElementReady(root);

            // UI要素を取得
            GetUIElements();

            // UI要素の検証
            ValidateUIElements();

            // イベントハンドラを登録
            RegisterEventHandlers();

            // 初期状態を設定
            UpdateEmptyState();
            UpdateDetailsVisibility();

            // ViewModelの保存されたプレイヤー名をUIに反映
            if (_playerNameInput != null && ViewModel != null && !string.IsNullOrWhiteSpace(ViewModel.PlayerName))
            {
                _playerNameInput.value = ViewModel.PlayerName;
            }

            Debug.Log("[MatchLobbyView] UI initialized.");
        }

        /// <summary>
        /// ViewModelとのバインディングを設定します
        /// </summary>
        /// <param name="viewModel">バインドするViewModel</param>
        protected override void BindViewModel(MatchLobbyViewModel viewModel)
        {
            base.BindViewModel(viewModel);

            // ViewModelのイベントを購読
            viewModel.PropertyChanged += OnViewModelPropertyChanged;
            viewModel.NavigateToRoomRequested += OnNavigateToRoomRequested;
            viewModel.ErrorOccurred += OnErrorOccurred;

            // ルームリストを初期化
            viewModel.PropertyChanged += OnRoomListPropertyChanged;

            // 初回のルームリスト更新
            RefreshRoomList();
        }

        /// <summary>
        /// ViewModelとのバインディングを解除します
        /// </summary>
        protected override void UnbindViewModel()
        {
            if (ViewModel != null)
            {
                ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
                ViewModel.NavigateToRoomRequested -= OnNavigateToRoomRequested;
                ViewModel.ErrorOccurred -= OnErrorOccurred;
            }

            UnregisterEventHandlers();
            base.UnbindViewModel();
        }

        #endregion

        #region UI Element Setup

        /// <summary>
        /// UI要素を取得します
        /// </summary>
        private void GetUIElements()
        {
            if (RootVisualElement == null) return;

            // Header
            _refreshButton = Q<Button>("RefreshButton");
            _backButton = Q<Button>("BackButton");

            // Left Panel - Room List
            _roomListContainer = Q<VisualElement>("RoomListContainer");
            _emptyState = Q<VisualElement>("EmptyState");
            _roomListScrollView = Q<ScrollView>("RoomListScrollView");

            // Right Panel - Room Details
            _noSelectionState = Q<VisualElement>("NoSelectionState");
            _detailsContent = Q<VisualElement>("DetailsContent");
            _roomNameLabel = Q<Label>("RoomNameLabel");
            _hostNameLabel = Q<Label>("HostNameLabel");
            _gameModeLabel = Q<Label>("GameModeLabel");
            _mapLabel = Q<Label>("MapLabel");
            _playersLabel = Q<Label>("PlayersLabel");

            // Join by Code
            _joinCodeInput = Q<TextField>("JoinCodeInput");
            _joinByCodeButton = Q<Button>("JoinByCodeButton");

            // Join Form
            _playerNameInput = Q<TextField>("PlayerNameInput");
            _passwordRow = Q<VisualElement>("PasswordRow");
            _passwordInput = Q<TextField>("PasswordInput");
            _joinRoomButton = Q<Button>("JoinRoomButton");

            // Footer
            _hostRoomButton = Q<Button>("HostRoomButton");

            // Status
            _statusLabel = Q<Label>("StatusLabel");
        }

        /// <summary>
        /// UI要素が正しく取得できているか検証します
        /// </summary>
        private void ValidateUIElements()
        {
            if (_refreshButton == null)
            {
                Debug.LogWarning("[MatchLobbyView] RefreshButton not found!", this);
            }

            if (_roomListContainer == null)
            {
                Debug.LogWarning("[MatchLobbyView] RoomListContainer not found!", this);
            }

            if (_emptyState == null)
            {
                Debug.LogWarning("[MatchLobbyView] EmptyState not found!", this);
            }

            if (_noSelectionState == null)
            {
                Debug.LogWarning("[MatchLobbyView] NoSelectionState not found!", this);
            }

            if (_detailsContent == null)
            {
                Debug.LogWarning("[MatchLobbyView] DetailsContent not found!", this);
            }

            if (_backButton == null)
            {
                Debug.LogWarning("[MatchLobbyView] BackButton not found!", this);
            }

            if (_hostRoomButton == null)
            {
                Debug.LogWarning("[MatchLobbyView] HostRoomButton not found!", this);
            }
        }

        #endregion

        #region Event Handlers Registration

        /// <summary>
        /// イベントハンドラを登録します
        /// </summary>
        private void RegisterEventHandlers()
        {
            // Header buttons
            if (_refreshButton != null)
            {
                _refreshButton.clicked += OnRefreshButtonClicked;
            }

            if (_backButton != null)
            {
                _backButton.clicked += OnBackButtonClicked;
            }

            // Join by code button
            if (_joinByCodeButton != null)
            {
                _joinByCodeButton.clicked += OnJoinByCodeButtonClicked;
            }

            // Join form buttons
            if (_joinRoomButton != null)
            {
                _joinRoomButton.clicked += OnJoinRoomButtonClicked;
            }

            // Footer buttons
            if (_hostRoomButton != null)
            {
                _hostRoomButton.clicked += OnHostRoomButtonClicked;
            }
        }

        /// <summary>
        /// イベントハンドラの登録を解除します
        /// </summary>
        private void UnregisterEventHandlers()
        {
            // Header buttons
            if (_refreshButton != null)
            {
                _refreshButton.clicked -= OnRefreshButtonClicked;
            }

            if (_backButton != null)
            {
                _backButton.clicked -= OnBackButtonClicked;
            }

            // Join by code button
            if (_joinByCodeButton != null)
            {
                _joinByCodeButton.clicked -= OnJoinByCodeButtonClicked;
            }

            // Join form buttons
            if (_joinRoomButton != null)
            {
                _joinRoomButton.clicked -= OnJoinRoomButtonClicked;
            }

            // Footer buttons
            if (_hostRoomButton != null)
            {
                _hostRoomButton.clicked -= OnHostRoomButtonClicked;
            }
        }

        #endregion

        #region UI Updates

        /// <summary>
        /// 空リスト状態の表示/非表示を更新します
        /// </summary>
        private void UpdateEmptyState()
        {
            if (_emptyState == null || _roomListScrollView == null)
            {
                return;
            }

            bool isEmpty = _roomItemElements.Count == 0;

            if (isEmpty)
            {
                _emptyState.style.display = DisplayStyle.Flex;
                _roomListScrollView.style.display = DisplayStyle.None;
            }
            else
            {
                _emptyState.style.display = DisplayStyle.None;
                _roomListScrollView.style.display = DisplayStyle.Flex;
            }
        }

        /// <summary>
        /// 詳細パネルの表示/非表示を更新します
        /// </summary>
        private void UpdateDetailsVisibility()
        {
            if (_noSelectionState == null || _detailsContent == null)
            {
                return;
            }

            bool hasSelection = _currentSelectedElement != null && ViewModel?.SelectedRoom != null;

            if (hasSelection)
            {
                _noSelectionState.style.display = DisplayStyle.None;
                _detailsContent.style.display = DisplayStyle.Flex;
            }
            else
            {
                _noSelectionState.style.display = DisplayStyle.Flex;
                _detailsContent.style.display = DisplayStyle.None;
            }
        }

        /// <summary>
        /// 詳細パネルの内容を更新します
        /// </summary>
        private void UpdateDetailsContent()
        {
            if (ViewModel == null || ViewModel.SelectedRoom == null)
            {
                return;
            }

            var selectedRoom = ViewModel.SelectedRoom;

            if (_roomNameLabel != null)
            {
                _roomNameLabel.text = selectedRoom.RoomName;
            }

            if (_hostNameLabel != null)
            {
                _hostNameLabel.text = selectedRoom.HostName;
            }

            if (_gameModeLabel != null)
            {
                _gameModeLabel.text = selectedRoom.GameMode.ToString();
            }

            if (_mapLabel != null)
            {
                _mapLabel.text = selectedRoom.MapName;
            }

            if (_playersLabel != null)
            {
                _playersLabel.text = $"{selectedRoom.CurrentPlayers}/{selectedRoom.MaxPlayers}";
            }

            UpdatePasswordVisibility(selectedRoom.HasPassword);
        }

        /// <summary>
        /// パスワード入力欄の表示/非表示を更新します
        /// </summary>
        /// <param name="hasPassword">パスワード付きルームかどうか</param>
        private void UpdatePasswordVisibility(bool hasPassword)
        {
            if (_passwordRow != null)
            {
                _passwordRow.style.display = hasPassword ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        /// <summary>
        /// ViewModelのプロパティ変更イベントハンドラ
        /// </summary>
        /// <param name="sender">送信元</param>
        /// <param name="e">イベント引数</param>
        private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (ViewModel == null)
            {
                return;
            }

            switch (e.PropertyName)
            {
                case nameof(MatchLobbyViewModel.StatusMessage):
                    UpdateStatusMessage();
                    break;
                case nameof(MatchLobbyViewModel.IsProcessing):
                    UpdateButtonStates();
                    break;
                case nameof(MatchLobbyViewModel.PlayerName):
                    // プレイヤー名が変更されたらUIに反映
                    if (_playerNameInput != null && !string.IsNullOrWhiteSpace(ViewModel.PlayerName))
                    {
                        _playerNameInput.SetValueWithoutNotify(ViewModel.PlayerName);
                    }
                    break;
            }
        }

        /// <summary>
        /// ステータスメッセージを更新します
        /// </summary>
        private void UpdateStatusMessage()
        {
            if (_statusLabel != null && ViewModel != null)
            {
                _statusLabel.text = ViewModel.StatusMessage;
            }
        }

        /// <summary>
        /// ボタンの有効/無効状態を更新します
        /// </summary>
        private void UpdateButtonStates()
        {
            if (ViewModel == null)
            {
                return;
            }

            bool isEnabled = !ViewModel.IsProcessing;

            // ホスト/参加/戻るボタンを無効化
            if (_hostRoomButton != null)
            {
                _hostRoomButton.SetEnabled(isEnabled);
            }

            if (_joinByCodeButton != null)
            {
                _joinByCodeButton.SetEnabled(isEnabled);
            }

            if (_joinRoomButton != null)
            {
                _joinRoomButton.SetEnabled(isEnabled);
            }

            if (_backButton != null)
            {
                _backButton.SetEnabled(isEnabled);
            }

            if (_refreshButton != null)
            {
                _refreshButton.SetEnabled(isEnabled);
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// リフレッシュボタンがクリックされた時の処理
        /// </summary>
        private void OnRefreshButtonClicked()
        {
            PlayButtonClickSfx();

            ViewModel?.RefreshRooms();
            Debug.Log("[MatchLobbyView] Refresh button clicked");
        }

        /// <summary>
        /// 戻るボタンがクリックされた時の処理
        /// </summary>
        private void OnBackButtonClicked()
        {
            PlayButtonClickSfx();
            ViewModel?.BackToMainMenu();
        }

        /// <summary>
        /// Join by Codeボタンがクリックされた時の処理
        /// </summary>
        private void OnJoinByCodeButtonClicked()
        {
            if (ViewModel == null)
            {
                return;
            }

            PlayButtonClickSfx();

            // ジョインコードを取得
            if (_joinCodeInput != null && !string.IsNullOrWhiteSpace(_joinCodeInput.value))
            {
                string joinCode = _joinCodeInput.value.Trim();
                Debug.Log($"[MatchLobbyView] Join by code clicked: {joinCode}");

                // プレイヤー名を取得
                if (_playerNameInput != null && !string.IsNullOrWhiteSpace(_playerNameInput.value))
                {
                    ViewModel.PlayerName = _playerNameInput.value.Trim();
                }

                // ViewModelのJoinCodeプロパティとJoinRoomメソッドを使用
                ViewModel.JoinCode = joinCode;
                ViewModel.JoinRoom();
            }
            else
            {
                Debug.LogWarning("[MatchLobbyView] Join code is empty!");
                if (ViewModel != null)
                {
                    ViewModel.StatusMessage = "ジョインコードを入力してください";
                }
            }
        }

        /// <summary>
        /// ルームに参加ボタンがクリックされた時の処理
        /// </summary>
        private void OnJoinRoomButtonClicked()
        {
            if (ViewModel == null) return;

            PlayButtonClickSfx();

            // ViewModelに値を設定
            if (_playerNameInput != null)
            {
                ViewModel.PlayerName = _playerNameInput.value;
            }

            // 選択されたルームから参加
            if (ViewModel.SelectedRoom != null)
            {
                ViewModel.JoinCode = ViewModel.SelectedRoom.JoinCode;
                ViewModel.JoinRoom();
                Debug.Log($"[MatchLobbyView] Joining selected room: {ViewModel.SelectedRoom.RoomName}");
            }
            else
            {
                Debug.LogWarning("[MatchLobbyView] No room selected!");
                if (ViewModel != null)
                {
                    ViewModel.StatusMessage = "ルームを選択してください";
                }
            }
        }

        /// <summary>
        /// ルームをホストボタンがクリックされた時の処理
        /// </summary>
        private void OnHostRoomButtonClicked()
        {
            PlayButtonClickSfx();

            Debug.Log("[MatchLobbyView] Host room button clicked - creating room");

            if (ViewModel == null)
            {
                Debug.LogError("[MatchLobbyView] ViewModel is null!");
                return;
            }

            // ルームを作成（非同期）
            // RoomCreated イベントが発火したら OnNavigateToRoomRequested でシーンが遷移します
            ViewModel.CreateRoom();
        }

        /// <summary>
        /// ルームシーン遷移要求イベントハンドラ
        /// </summary>
        /// <param name="sender">送信元</param>
        /// <param name="e">イベント引数</param>
        private void OnNavigateToRoomRequested(object? sender, EventArgs e)
        {
            Debug.Log("[MatchLobbyView] Navigating to match room scene...");

            var sceneService = ServiceLocator.Instance.Get<ISceneManagementService>();
            sceneService?.LoadMatchRoom();
        }

        /// <summary>
        /// エラー発生イベントハンドラ
        /// </summary>
        /// <param name="sender">送信元</param>
        /// <param name="errorMessage">エラーメッセージ</param>
        private void OnErrorOccurred(object? sender, string errorMessage)
        {
            Debug.LogError($"[MatchLobbyView] Error: {errorMessage}");

            // ステータスラベルにエラーメッセージを表示
            if (_statusLabel != null)
            {
                _statusLabel.text = $"エラー: {errorMessage}";
            }
        }

        #endregion

        #region Private Methods - Room List

        /// <summary>
        /// ルームリストPropertyChangedイベントハンドラ
        /// </summary>
        private void OnRoomListPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MatchLobbyViewModel.AvailableRooms))
            {
                RefreshRoomList();
            }
            else if (e.PropertyName == nameof(MatchLobbyViewModel.SelectedRoom))
            {
                UpdateDetailsVisibility();
                UpdateDetailsContent();
            }
        }

        /// <summary>
        /// ルームリストUIを更新します
        /// </summary>
        private void RefreshRoomList()
        {
            if (ViewModel == null || _roomListScrollView == null)
            {
                return;
            }

            // 既存のルームアイテムをクリア
            _roomItemElements.Clear();
            _roomListScrollView.Clear();

            // 新しいルームアイテムを作成
            foreach (var room in ViewModel.AvailableRooms)
            {
                var roomItem = CreateRoomListItem(room);
                _roomListScrollView.Add(roomItem);
                _roomItemElements[room.RoomId] = roomItem;
            }

            // 空リスト状態を更新
            UpdateEmptyState();

            Debug.Log($"[MatchLobbyView] Room list refreshed. Count: {ViewModel.AvailableRooms.Count}");
        }

        /// <summary>
        /// ルームリストアイテムを作成します
        /// </summary>
        private VisualElement CreateRoomListItem(RoomInfo room)
        {
            var item = new VisualElement();
            item.AddToClassList("room-item");

            // ルーム名
            var nameLabel = new Label(room.RoomName);
            nameLabel.AddToClassList("room-name");
            item.Add(nameLabel);

            // ホスト名
            var hostLabel = new Label($"Host: {room.HostName}");
            hostLabel.AddToClassList("room-host");
            item.Add(hostLabel);

            // プレイヤー数
            var playersLabel = new Label($"{room.CurrentPlayers}/{room.MaxPlayers}");
            playersLabel.AddToClassList("room-players");
            item.Add(playersLabel);

            // クリックイベント
            item.RegisterCallback<ClickEvent>(evt =>
            {
                OnRoomItemClicked(room, item);
            });

            return item;
        }

        /// <summary>
        /// ルームアイテムがクリックされた時の処理
        /// </summary>
        private void OnRoomItemClicked(RoomInfo room, VisualElement item)
        {
            PlayButtonClickSfx();

            // 前の選択を解除
            if (_currentSelectedElement != null)
            {
                _currentSelectedElement.RemoveFromClassList("selected");
            }

            // 新しい選択を設定
            _currentSelectedElement = item;
            _currentSelectedElement.AddToClassList("selected");

            // ViewModelに通知
            ViewModel?.SelectRoom(room);

            Debug.Log($"[MatchLobbyView] Room item clicked: {room.RoomName}");
        }

        #endregion

        #region Private Methods - Audio

        /// <summary>
        /// ボタンクリック効果音を再生します
        /// </summary>
        private void PlayButtonClickSfx()
        {
            if (_buttonClickSfx != null)
            {
                var audioService = ServiceLocator.Instance.Get<IAudioService>();
                if (audioService != null)
                {
                    audioService.PlaySfx(_buttonClickSfx);
                }
            }
        }

        #endregion
    }
}
