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

        // Filter Section
        private DropdownField? _gameModeFilter;
        private Toggle? _hideFullRoomsToggle;
        private Toggle? _hidePasswordRoomsToggle;
        private Toggle? _sortByPlayersToggle;

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
        private Label? _visibilityLabel;
        private Label? _passwordStatusLabel;

        // Player Name Section (top)
        private TextField? _playerNameInput;
        private Button? _applyNameButton;

        // Join by Code
        private TextField? _joinCodeInput;
        private Button? _joinByCodeButton;

        // Password Input (in DetailsContent)
        private VisualElement? _passwordRow;
        private TextField? _passwordInput;
        private Button? _joinRoomButton;

        // Footer
        private Button? _hostRoomButton;
        private Button? _cancelButton;

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

            // Apply buttonの初期状態を設定（変更がないので無効）
            UpdateApplyButtonState();

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
            if (RootVisualElement == null)
            {
                return;
            }

            // Header
            _refreshButton = Q<Button>("RefreshButton");
            _backButton = Q<Button>("BackButton");

            // Filter Section
            _gameModeFilter = Q<DropdownField>("GameModeFilter");
            _hideFullRoomsToggle = Q<Toggle>("HideFullRoomsToggle");
            _hidePasswordRoomsToggle = Q<Toggle>("HidePasswordRoomsToggle");
            _sortByPlayersToggle = Q<Toggle>("SortByPlayersToggle");

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
            _visibilityLabel = Q<Label>("VisibilityLabel");
            _passwordStatusLabel = Q<Label>("PasswordStatusLabel");

            // Player Name Section (top)
            _playerNameInput = Q<TextField>("PlayerNameInput");
            _applyNameButton = Q<Button>("ApplyNameButton");

            // Join by Code
            _joinCodeInput = Q<TextField>("JoinCodeInput");
            _joinByCodeButton = Q<Button>("JoinByCodeButton");

            // Password Input (conditional, in DetailsContent)
            _passwordRow = Q<VisualElement>("PasswordRow");
            _passwordInput = Q<TextField>("PasswordInput");

            // Join Button
            _joinRoomButton = Q<Button>("JoinRoomButton");

            // Footer
            _hostRoomButton = Q<Button>("HostRoomButton");
            _cancelButton = Q<Button>("CancelButton");

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

            // Filter controls
            if (_gameModeFilter != null)
            {
                _gameModeFilter.RegisterValueChangedCallback(OnFilterChanged);
            }

            if (_hideFullRoomsToggle != null)
            {
                _hideFullRoomsToggle.RegisterValueChangedCallback(OnFilterChanged);
            }

            if (_hidePasswordRoomsToggle != null)
            {
                _hidePasswordRoomsToggle.RegisterValueChangedCallback(OnFilterChanged);
            }

            if (_sortByPlayersToggle != null)
            {
                _sortByPlayersToggle.RegisterValueChangedCallback(OnFilterChanged);
            }

            // Apply name button
            if (_applyNameButton != null)
            {
                _applyNameButton.clicked += OnApplyNameButtonClicked;
            }

            // Player name input change
            if (_playerNameInput != null)
            {
                _playerNameInput.RegisterValueChangedCallback(OnPlayerNameInputChanged);
                _playerNameInput.maxLength = MatchLobbyViewModel.MaxPlayerNameLength;
            }

            // Join by code button
            if (_joinByCodeButton != null)
            {
                _joinByCodeButton.clicked += OnJoinByCodeButtonClicked;
            }

            // Password input change
            if (_passwordInput != null)
            {
                _passwordInput.RegisterValueChangedCallback(OnPasswordInputChanged);
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

            if (_cancelButton != null)
            {
                _cancelButton.clicked += OnCancelButtonClicked;
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

            // Filter controls
            if (_gameModeFilter != null)
            {
                _gameModeFilter.UnregisterValueChangedCallback(OnFilterChanged);
            }

            if (_hideFullRoomsToggle != null)
            {
                _hideFullRoomsToggle.UnregisterValueChangedCallback(OnFilterChanged);
            }

            if (_hidePasswordRoomsToggle != null)
            {
                _hidePasswordRoomsToggle.UnregisterValueChangedCallback(OnFilterChanged);
            }

            if (_sortByPlayersToggle != null)
            {
                _sortByPlayersToggle.UnregisterValueChangedCallback(OnFilterChanged);
            }

            // Apply name button
            if (_applyNameButton != null)
            {
                _applyNameButton.clicked -= OnApplyNameButtonClicked;
            }

            // Player name input change
            if (_playerNameInput != null)
            {
                _playerNameInput.UnregisterValueChangedCallback(OnPlayerNameInputChanged);
            }

            // Join by code button
            if (_joinByCodeButton != null)
            {
                _joinByCodeButton.clicked -= OnJoinByCodeButtonClicked;
            }

            // Password input change
            if (_passwordInput != null)
            {
                _passwordInput.UnregisterValueChangedCallback(OnPasswordInputChanged);
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

            if (_cancelButton != null)
            {
                _cancelButton.clicked -= OnCancelButtonClicked;
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

            // Join buttonの状態を更新
            UpdateJoinButtonState();
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

            // 追加の設定情報
            if (_visibilityLabel != null)
            {
                _visibilityLabel.text = selectedRoom.IsPublic ? "Public" : "Private";
            }

            if (_passwordStatusLabel != null)
            {
                _passwordStatusLabel.text = selectedRoom.HasPassword ? "Required" : "None";
            }

            UpdatePasswordVisibility(selectedRoom.HasPassword);

            // Join buttonの状態を更新
            UpdateJoinButtonState();
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
                _hostRoomButton.style.display = isEnabled ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (_joinByCodeButton != null)
            {
                _joinByCodeButton.SetEnabled(isEnabled);
            }

            // JoinRoomButtonは別途UpdateJoinButtonStateで管理
            UpdateJoinButtonState();

            if (_backButton != null)
            {
                _backButton.SetEnabled(isEnabled);
            }

            if (_refreshButton != null)
            {
                _refreshButton.SetEnabled(isEnabled);
            }

            // プレイヤー名入力と適用ボタンを無効化
            if (_playerNameInput != null)
            {
                _playerNameInput.SetEnabled(isEnabled);
            }

            if (_applyNameButton != null)
            {
                _applyNameButton.SetEnabled(isEnabled);
            }

            // ジョインコード入力を無効化
            if (_joinCodeInput != null)
            {
                _joinCodeInput.SetEnabled(isEnabled);
            }

            // パスワード入力を無効化
            if (_passwordInput != null)
            {
                _passwordInput.SetEnabled(isEnabled);
            }

            // フィルターコントロールを無効化
            if (_gameModeFilter != null)
            {
                _gameModeFilter.SetEnabled(isEnabled);
            }

            if (_hideFullRoomsToggle != null)
            {
                _hideFullRoomsToggle.SetEnabled(isEnabled);
            }

            if (_hidePasswordRoomsToggle != null)
            {
                _hidePasswordRoomsToggle.SetEnabled(isEnabled);
            }

            // キャンセルボタンの表示/非表示
            if (_cancelButton != null)
            {
                _cancelButton.style.display = isEnabled ? DisplayStyle.None : DisplayStyle.Flex;
            }

            // Apply buttonの状態も更新
            UpdateApplyButtonState();
        }

        /// <summary>
        /// Apply buttonの有効/無効状態を更新します
        /// </summary>
        private void UpdateApplyButtonState()
        {
            if (_applyNameButton == null || _playerNameInput == null || ViewModel == null)
            {
                return;
            }

            // 処理中は無効
            if (ViewModel.IsProcessing)
            {
                _applyNameButton.SetEnabled(false);
                return;
            }

            // 入力値がViewModelの値と同じ場合は無効
            string inputValue = _playerNameInput.value.Trim();
            bool hasChanged = !string.Equals(inputValue, ViewModel.PlayerName, StringComparison.Ordinal);
            bool isNotEmpty = !string.IsNullOrWhiteSpace(inputValue);

            _applyNameButton.SetEnabled(hasChanged && isNotEmpty);
        }

        /// <summary>
        /// プレイヤー名入力が変更された時の処理
        /// </summary>
        private void OnPlayerNameInputChanged(ChangeEvent<string> evt)
        {
            UpdateApplyButtonState();
        }

        /// <summary>
        /// パスワード入力が変更された時の処理
        /// </summary>
        private void OnPasswordInputChanged(ChangeEvent<string> evt)
        {
            UpdateJoinButtonState();
        }

        /// <summary>
        /// Join Roomボタンの有効/無効状態を更新します
        /// </summary>
        private void UpdateJoinButtonState()
        {
            if (_joinRoomButton == null || ViewModel == null)
            {
                return;
            }

            // 処理中は無効
            if (ViewModel.IsProcessing)
            {
                _joinRoomButton.SetEnabled(false);
                return;
            }

            // ルームが選択されていない場合は無効
            if (ViewModel.SelectedRoom == null)
            {
                _joinRoomButton.SetEnabled(false);
                return;
            }

            var selectedRoom = ViewModel.SelectedRoom;

            // 満員のルームには参加できない
            if (selectedRoom.IsFull)
            {
                _joinRoomButton.SetEnabled(false);
                return;
            }

            // パスワード保護されたルームで、パスワードが入力されていない場合は無効
            if (selectedRoom.HasPassword)
            {
                bool hasPassword = _passwordInput != null && !string.IsNullOrWhiteSpace(_passwordInput.value);
                _joinRoomButton.SetEnabled(hasPassword);
                return;
            }

            // それ以外は有効
            _joinRoomButton.SetEnabled(true);
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
        /// プレイヤー名適用ボタンがクリックされた時の処理
        /// </summary>
        private void OnApplyNameButtonClicked()
        {
            if (ViewModel == null || _playerNameInput == null)
            {
                return;
            }

            PlayButtonClickSfx();

            string playerName = _playerNameInput.value.Trim();
            if (!string.IsNullOrWhiteSpace(playerName))
            {
                ViewModel.PlayerName = playerName;
                ViewModel.StatusMessage = $"Player name set to \"{playerName}\"";
                Debug.Log($"[MatchLobbyView] Player name applied: {playerName}");

                // Apply後はボタンを無効化（変更がなくなるため）
                UpdateApplyButtonState();
            }
            else
            {
                ViewModel.StatusMessage = "Please enter a player name";
            }
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
                    ViewModel.StatusMessage = "Please enter a join code";
                }
            }
        }

        /// <summary>
        /// ルームに参加ボタンがクリックされた時の処理
        /// </summary>
        private void OnJoinRoomButtonClicked()
        {
            if (ViewModel == null)
            {
                return;
            }

            PlayButtonClickSfx();

            // ViewModelに値を設定
            if (_playerNameInput != null)
            {
                ViewModel.PlayerName = _playerNameInput.value;
            }

            // パスワードを設定（パスワード保護されたルームの場合）
            if (_passwordInput != null && ViewModel.SelectedRoom?.HasPassword == true)
            {
                ViewModel.Password = _passwordInput.value;
            }

            // 選択されたルームから参加
            if (ViewModel.SelectedRoom != null)
            {
                ViewModel.JoinCode = ViewModel.SelectedRoom.JoinCode;
                ViewModel.JoinRoom();
                Debug.Log($"[MatchLobbyView] Joining selected room: {ViewModel.SelectedRoom.RoomName}");

                // パスワード入力をクリア（セキュリティのため）
                if (_passwordInput != null)
                {
                    _passwordInput.value = "";
                }
            }
            else
            {
                Debug.LogWarning("[MatchLobbyView] No room selected!");
                if (ViewModel != null)
                {
                    ViewModel.StatusMessage = "Please select a room";
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

            // プレイヤー名を設定してからルーム作成
            if (_playerNameInput != null && !string.IsNullOrWhiteSpace(_playerNameInput.value))
            {
                ViewModel.PlayerName = _playerNameInput.value.Trim();
            }

            // ルームを作成（非同期）
            // RoomCreated イベントが発火したら OnNavigateToRoomRequested でシーンが遷移します
            ViewModel.CreateRoom();
        }

        /// <summary>
        /// キャンセルボタンがクリックされた時の処理
        /// </summary>
        private void OnCancelButtonClicked()
        {
            PlayButtonClickSfx();

            Debug.Log("[MatchLobbyView] Cancel button clicked");

            ViewModel?.CancelOperation();
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
                _statusLabel.text = $"Error: {errorMessage}";
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
        /// フィルターが変更された時の処理
        /// </summary>
        private void OnFilterChanged<T>(ChangeEvent<T> evt)
        {
            RefreshRoomList();
        }

        /// <summary>
        /// ルームがフィルターに一致するかどうかを判定します
        /// </summary>
        /// <param name="room">判定するルーム</param>
        /// <returns>フィルターに一致する場合はtrue</returns>
        private bool PassesFilter(RoomInfo room)
        {
            // ゲームモードフィルター
            if (_gameModeFilter != null && _gameModeFilter.index > 0)
            {
                string selectedMode = _gameModeFilter.value;
                if (!string.Equals(room.GameMode.ToString(), selectedMode, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            // 満員ルームを非表示
            if (_hideFullRoomsToggle != null && _hideFullRoomsToggle.value && room.IsFull)
            {
                return false;
            }

            // パスワード付きルームを非表示
            if (_hidePasswordRoomsToggle != null && _hidePasswordRoomsToggle.value && room.HasPassword)
            {
                return false;
            }

            return true;
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

            int displayedCount = 0;

            // フィルターを適用したルームリストを取得
            var filteredRooms = new List<RoomInfo>();
            foreach (var room in ViewModel.AvailableRooms)
            {
                if (PassesFilter(room))
                {
                    filteredRooms.Add(room);
                }
            }

            // ソートを適用（MaxPlayersの降順）
            if (_sortByPlayersToggle != null && _sortByPlayersToggle.value)
            {
                filteredRooms.Sort((a, b) => b.MaxPlayers.CompareTo(a.MaxPlayers));
            }

            // 新しいルームアイテムを作成
            foreach (var room in filteredRooms)
            {
                var roomItem = CreateRoomListItem(room);
                _roomListScrollView.Add(roomItem);
                _roomItemElements[room.RoomId] = roomItem;
                displayedCount++;
            }

            // 空リスト状態を更新
            UpdateEmptyState();

            Debug.Log($"[MatchLobbyView] Room list refreshed. Displayed: {displayedCount}/{ViewModel.AvailableRooms.Count}");
        }

        /// <summary>
        /// ルームリストアイテムを作成します
        /// </summary>
        private VisualElement CreateRoomListItem(RoomInfo room)
        {
            var item = new VisualElement();
            item.AddToClassList("room-item");

            // ヘッダー行 - ルーム名とステータスバッジ
            var header = new VisualElement();
            header.AddToClassList("room-item-header");
            item.Add(header);

            // ルーム名
            var nameLabel = new Label(room.RoomName);
            nameLabel.AddToClassList("room-item-name");
            header.Add(nameLabel);

            // ステータスバッジコンテナ
            var badges = new VisualElement();
            badges.AddToClassList("room-item-badges");
            header.Add(badges);

            // パスワードバッジ
            if (room.HasPassword)
            {
                var passwordBadge = new Label("LOCKED");
                passwordBadge.AddToClassList("room-item-badge");
                passwordBadge.AddToClassList("badge-password");
                badges.Add(passwordBadge);
            }

            // 満員/空きバッジ
            if (room.IsFull)
            {
                var fullBadge = new Label("FULL");
                fullBadge.AddToClassList("room-item-badge");
                fullBadge.AddToClassList("badge-full");
                badges.Add(fullBadge);
            }
            else
            {
                var openBadge = new Label("OPEN");
                openBadge.AddToClassList("room-item-badge");
                openBadge.AddToClassList("badge-open");
                badges.Add(openBadge);
            }

            // フッター行 - ホスト名とプレイヤー数（モード/マップは詳細パネルで表示）
            var footer = new VisualElement();
            footer.AddToClassList("room-item-footer");
            item.Add(footer);

            var hostLabel = new Label($"Host: {room.HostName}");
            hostLabel.AddToClassList("room-item-host");
            footer.Add(hostLabel);

            var playersLabel = new Label($"{room.CurrentPlayers}/{room.MaxPlayers}");
            playersLabel.AddToClassList("room-item-players");
            if (room.IsFull)
            {
                playersLabel.AddToClassList("room-item-players-full");
            }
            footer.Add(playersLabel);

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
