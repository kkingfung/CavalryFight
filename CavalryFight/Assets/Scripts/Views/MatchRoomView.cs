#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using CavalryFight.Core.MVVM;
using CavalryFight.Core.Services;
using CavalryFight.Services.Audio;
using CavalryFight.Services.Lobby;
using CavalryFight.Services.Performance;
using CavalryFight.Services.SceneManagement;
using CavalryFight.ViewModels;
using UnityEngine;
using UnityEngine.UIElements;

namespace CavalryFight.Views
{
    /// <summary>
    /// マッチルーム画面のView
    /// </summary>
    /// <remarks>
    /// プレイヤーリスト、ルーム設定、準備状態を表示します。
    /// MatchRoomViewModelと連携して動作します。
    /// </remarks>
    [RequireComponent(typeof(UIDocument))]
    public class MatchRoomView : UIToolkitViewBase<MatchRoomViewModel>
    {
        #region Serialized Fields

        [Header("Audio")]
        [SerializeField] private AudioClip? _bgmClip;
        [SerializeField] private AudioClip? _buttonClickSfx;
        [SerializeField] private AudioClip? _countdownTickSfx;

        #endregion

        #region UI Elements

        // Header
        private Label? _joinCodeLabel;

        // Left Panel - Player List
        private VisualElement? _playerListContainer;
        private ScrollView? _playerListScrollView;

        // Right Panel - Room Settings
        private Label? _roomNameLabel;
        private TextField? _roomNameField;
        private Label? _hostNameLabel;
        private Label? _passwordLabel;
        private TextField? _passwordField;
        private Label? _publicLabel;
        private Toggle? _publicToggle;
        private Label? _maxPlayersLabel;
        private DropdownField? _maxPlayersDropdown;
        private Label? _gameModeLabel;
        private Label? _mapLabel;
        private Label? _playersLabel;
        private Label? _statusLabel;

        // Room Info Dropdowns (Host Only)
        private DropdownField? _gameModeDropdown;
        private DropdownField? _mapDropdown;

        // Game Settings (Host Only)
        private VisualElement? _gameSettingsSection;
        private Label? _timeLimitLabel;
        private DropdownField? _timeLimitDropdown;
        private Label? _arrowLimitLabel;
        private DropdownField? _arrowLimitDropdown;

        // Change/Apply Settings Button (Host Only)
        private VisualElement? _changeSettingsButtonSection;
        private Button? _changeSettingsButton;

        // Footer Buttons
        private Button? _leaveRoomButton;
        private Button? _startGameButton;
        private Button? _readyButton;
        private Button? _cancelReadyButton;

        // Countdown Dialog
        private VisualElement? _countdownDialog;
        private Label? _countdownLabel;
        private Button? _cancelCountdownButton;

        #endregion

        #region Fields

        private readonly Dictionary<string, VisualElement> _playerItemElements = new Dictionary<string, VisualElement>();
        private bool _isCountdownActive = false;
        private bool _isEditMode = false; // ホストの設定編集モード

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
            var performanceMonitor = ServiceLocator.Instance.TryGet<IPerformanceMonitor>();

            if (lobbyService == null || sceneService == null)
            {
                Debug.LogError("[MatchRoomView] Required services not found! Disabling component.", this);
                enabled = false;
                return;
            }

            // ViewModelを作成して設定（PerformanceMonitorはオプション）
            ViewModel = new MatchRoomViewModel(lobbyService, sceneService, performanceMonitor);
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
            base.OnDisable();
        }

        /// <summary>
        /// 更新処理
        /// </summary>
        private void Update()
        {
            // ViewModelのカウントダウンタイマーを更新
            // タイマーロジックはViewModel内で管理されます
            if (_isCountdownActive && ViewModel != null)
            {
                ViewModel.Tick(Time.deltaTime);
            }
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

            // ドロップダウンの選択肢を設定
            SetupDropdowns();

            // イベントハンドラを登録
            RegisterEventHandlers();

            // プレイヤーリストを初期化（UI要素が準備できた後）
            PopulatePlayerList();

            // 初期状態を設定
            UpdateUI();

            // ゲーム設定の有効/無効を初期化
            UpdateGameSettingsAvailability();

            Debug.Log("[MatchRoomView] UI initialized.");
        }

        /// <summary>
        /// ViewModelとのバインディングを設定します
        /// </summary>
        /// <param name="viewModel">バインドするViewModel</param>
        protected override void BindViewModel(MatchRoomViewModel viewModel)
        {
            base.BindViewModel(viewModel);

            // ViewModelのイベントを購読
            viewModel.PropertyChanged += OnViewModelPropertyChanged;
            viewModel.GameStarting += OnGameStarting;
            viewModel.CountdownUpdated += OnCountdownUpdated;
            viewModel.LoadingSceneRequested += OnLoadingSceneRequested;
            viewModel.ErrorOccurred += OnErrorOccurred;

            // プレイヤーリストの変更を購読
            viewModel.Players.CollectionChanged += OnPlayersCollectionChanged;

            // プレイヤーリストを初期化
            PopulatePlayerList();
        }

        /// <summary>
        /// ViewModelとのバインディングを解除します
        /// </summary>
        protected override void UnbindViewModel()
        {
            if (ViewModel != null)
            {
                ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
                ViewModel.GameStarting -= OnGameStarting;
                ViewModel.CountdownUpdated -= OnCountdownUpdated;
                ViewModel.LoadingSceneRequested -= OnLoadingSceneRequested;
                ViewModel.ErrorOccurred -= OnErrorOccurred;
                ViewModel.Players.CollectionChanged -= OnPlayersCollectionChanged;
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
            _joinCodeLabel = Q<Label>("JoinCodeLabel");

            // Left Panel - Player List
            _playerListContainer = Q<VisualElement>("PlayerListContainer");
            _playerListScrollView = Q<ScrollView>("PlayerListScrollView");

            // Right Panel - Room Settings
            _roomNameLabel = Q<Label>("RoomNameLabel");
            _roomNameField = Q<TextField>("RoomNameField");
            _hostNameLabel = Q<Label>("HostNameLabel");
            _passwordLabel = Q<Label>("PasswordLabel");
            _passwordField = Q<TextField>("PasswordField");
            _publicLabel = Q<Label>("PublicLabel");
            _publicToggle = Q<Toggle>("PublicToggle");
            _maxPlayersLabel = Q<Label>("MaxPlayersLabel");
            _maxPlayersDropdown = Q<DropdownField>("MaxPlayersDropdown");
            _gameModeLabel = Q<Label>("GameModeLabel");
            _mapLabel = Q<Label>("MapLabel");
            _playersLabel = Q<Label>("PlayersLabel");
            _statusLabel = Q<Label>("StatusLabel");

            // Room Info Dropdowns (Host Only)
            _gameModeDropdown = Q<DropdownField>("GameModeDropdown");
            _mapDropdown = Q<DropdownField>("MapDropdown");

            // Game Settings (Host Only)
            _gameSettingsSection = Q<VisualElement>("GameSettingsSection");
            _timeLimitLabel = Q<Label>("TimeLimitLabel");
            _timeLimitDropdown = Q<DropdownField>("TimeLimitDropdown");
            _arrowLimitLabel = Q<Label>("ArrowLimitLabel");
            _arrowLimitDropdown = Q<DropdownField>("ArrowLimitDropdown");

            // Change/Apply Settings Button (Host Only)
            _changeSettingsButtonSection = Q<VisualElement>("ChangeSettingsButtonSection");
            _changeSettingsButton = Q<Button>("ChangeSettingsButton");

            // Footer Buttons
            _leaveRoomButton = Q<Button>("LeaveRoomButton");
            _startGameButton = Q<Button>("StartGameButton");
            _readyButton = Q<Button>("ReadyButton");
            _cancelReadyButton = Q<Button>("CancelReadyButton");

            // Countdown Dialog
            _countdownDialog = Q<VisualElement>("CountdownDialog");
            _countdownLabel = Q<Label>("CountdownLabel");
            _cancelCountdownButton = Q<Button>("CancelCountdownButton");
        }

        /// <summary>
        /// UI要素が正しく取得できているか検証します
        /// </summary>
        private void ValidateUIElements()
        {
            if (_playerListContainer == null)
            {
                Debug.LogWarning("[MatchRoomView] PlayerListContainer not found!", this);
            }

            if (_joinCodeLabel == null)
            {
                Debug.LogWarning("[MatchRoomView] JoinCodeLabel not found!", this);
            }

            if (_statusLabel == null)
            {
                Debug.LogWarning("[MatchRoomView] StatusLabel not found!", this);
            }
        }

        /// <summary>
        /// ドロップダウンの選択肢を設定します
        /// </summary>
        private void SetupDropdowns()
        {
            // Game Mode dropdown (Room Info - Host only)
            if (_gameModeDropdown != null)
            {
                _gameModeDropdown.choices = new List<string>
                {
                    "Arena", "ScoreMatch", "TeamFight", "Deathmatch", "Versus"
                };
                _gameModeDropdown.value = "Arena";
            }

            // Map dropdown (Room Info - Host only)
            if (_mapDropdown != null)
            {
                _mapDropdown.choices = new List<string>
                {
                    "DefaultArena", "Desert Arena", "Forest Arena", "Snow Arena"
                };
                _mapDropdown.value = "DefaultArena";
            }

            // Max Players dropdown (Room Info - Host only)
            if (_maxPlayersDropdown != null)
            {
                _maxPlayersDropdown.choices = new List<string>
                {
                    "2", "4", "6", "8", "10", "12", "16"
                };
                _maxPlayersDropdown.value = "8";
            }

            // Time Limit dropdown
            if (_timeLimitDropdown != null)
            {
                _timeLimitDropdown.choices = new List<string>
                {
                    "3:00", "5:00", "10:00", "15:00", "No Limit"
                };
                _timeLimitDropdown.value = "5:00";
            }

            // Arrow Limit dropdown
            if (_arrowLimitDropdown != null)
            {
                _arrowLimitDropdown.choices = new List<string>
                {
                    "5", "10", "15", "20", "No Limit"
                };
                _arrowLimitDropdown.value = "No Limit";
            }

            // NPC Difficulty dropdowns are now created dynamically in player cells
        }

        #endregion

        #region Event Handlers Registration

        /// <summary>
        /// イベントハンドラを登録します
        /// </summary>
        private void RegisterEventHandlers()
        {
            // Footer buttons
            if (_leaveRoomButton != null)
            {
                _leaveRoomButton.clicked += OnLeaveRoomButtonClicked;
            }

            if (_startGameButton != null)
            {
                _startGameButton.clicked += OnStartGameButtonClicked;
            }

            if (_readyButton != null)
            {
                _readyButton.clicked += OnReadyButtonClicked;
            }

            if (_cancelReadyButton != null)
            {
                _cancelReadyButton.clicked += OnCancelReadyButtonClicked;
            }

            // Countdown dialog
            if (_cancelCountdownButton != null)
            {
                _cancelCountdownButton.clicked += OnCancelCountdownButtonClicked;
            }

            // Change Settings button (Host only)
            if (_changeSettingsButton != null)
            {
                _changeSettingsButton.clicked += OnChangeSettingsButtonClicked;
            }

            // Room Info dropdown change events (Host only)
            if (_gameModeDropdown != null)
            {
                _gameModeDropdown.RegisterValueChangedCallback(OnGameModeChanged);
            }

            if (_mapDropdown != null)
            {
                _mapDropdown.RegisterValueChangedCallback(OnMapChanged);
            }

            // Dropdown change events
            if (_timeLimitDropdown != null)
            {
                _timeLimitDropdown.RegisterValueChangedCallback(OnTimeLimitChanged);
            }

            if (_arrowLimitDropdown != null)
            {
                _arrowLimitDropdown.RegisterValueChangedCallback(OnArrowLimitChanged);
            }
        }

        /// <summary>
        /// イベントハンドラを解除します
        /// </summary>
        private void UnregisterEventHandlers()
        {
            // Footer buttons
            if (_leaveRoomButton != null)
            {
                _leaveRoomButton.clicked -= OnLeaveRoomButtonClicked;
            }

            if (_startGameButton != null)
            {
                _startGameButton.clicked -= OnStartGameButtonClicked;
            }

            if (_readyButton != null)
            {
                _readyButton.clicked -= OnReadyButtonClicked;
            }

            if (_cancelReadyButton != null)
            {
                _cancelReadyButton.clicked -= OnCancelReadyButtonClicked;
            }

            // Countdown dialog
            if (_cancelCountdownButton != null)
            {
                _cancelCountdownButton.clicked -= OnCancelCountdownButtonClicked;
            }

            // Change Settings button
            if (_changeSettingsButton != null)
            {
                _changeSettingsButton.clicked -= OnChangeSettingsButtonClicked;
            }

            // Room Info dropdown change events
            if (_gameModeDropdown != null)
            {
                _gameModeDropdown.UnregisterValueChangedCallback(OnGameModeChanged);
            }

            if (_mapDropdown != null)
            {
                _mapDropdown.UnregisterValueChangedCallback(OnMapChanged);
            }

            // Dropdown change events
            if (_timeLimitDropdown != null)
            {
                _timeLimitDropdown.UnregisterValueChangedCallback(OnTimeLimitChanged);
            }

            if (_arrowLimitDropdown != null)
            {
                _arrowLimitDropdown.UnregisterValueChangedCallback(OnArrowLimitChanged);
            }
        }

        #endregion

        #region Player List Population

        /// <summary>
        /// プレイヤーリストを生成します
        /// </summary>
        private void PopulatePlayerList()
        {
            if (ViewModel == null || _playerListContainer == null)
            {
                return;
            }

            // 既存のリストをクリア
            _playerListContainer.Clear();
            _playerItemElements.Clear();

            // 固定スロット数を作成（MaxPlayers分）
            int maxPlayers = ViewModel.MaxPlayers;
            var playersList = ViewModel.Players.ToList();

            for (int i = 0; i < maxPlayers; i++)
            {
                VisualElement playerItem;

                if (i < playersList.Count)
                {
                    // プレイヤーが存在する場合
                    var player = playersList[i];
                    playerItem = CreatePlayerListItem(player);
                    _playerItemElements[player.PlayerId] = playerItem;
                }
                else
                {
                    // 空スロットの場合
                    playerItem = CreateEmptySlot(i);
                }

                _playerListContainer.Add(playerItem);
            }
        }

        /// <summary>
        /// 空スロットのUI要素を作成します
        /// </summary>
        /// <param name="slotIndex">スロットインデックス</param>
        /// <returns>作成されたVisualElement</returns>
        private VisualElement CreateEmptySlot(int slotIndex)
        {
            var container = new VisualElement();
            container.AddToClassList("player-item");
            container.AddToClassList("empty");
            container.name = $"EmptySlot_{slotIndex}";

            // プレイヤー情報セクション
            var infoSection = new VisualElement();
            infoSection.AddToClassList("player-item-info");

            // "Empty" ラベル
            var nameLabel = new Label("Empty");
            nameLabel.AddToClassList("player-item-name");
            nameLabel.AddToClassList("empty");
            infoSection.Add(nameLabel);

            container.Add(infoSection);

            // ホストの場合: Add NPC ボタンを表示
            if (ViewModel != null && ViewModel.IsHost)
            {
                var actionsSection = new VisualElement();
                actionsSection.AddToClassList("player-item-actions");

                var addNpcButton = new Button(() => OnAddNPCToSlot(slotIndex));
                addNpcButton.text = "Add NPC";
                addNpcButton.AddToClassList("add-npc-button");
                actionsSection.Add(addNpcButton);

                container.Add(actionsSection);
            }

            return container;
        }

        /// <summary>
        /// プレイヤーリストアイテムのUI要素を作成します
        /// </summary>
        /// <param name="player">プレイヤー情報</param>
        /// <returns>作成されたVisualElement</returns>
        private VisualElement CreatePlayerListItem(PlayerInfo player)
        {
            var container = new VisualElement();
            container.AddToClassList("player-item");
            container.name = $"PlayerItem_{player.PlayerId}";

            // プレイヤー情報セクション
            var infoSection = new VisualElement();
            infoSection.AddToClassList("player-item-info");

            // プレイヤー名
            var nameLabel = new Label(player.PlayerName);
            nameLabel.AddToClassList("player-item-name");
            infoSection.Add(nameLabel);

            // ステータス行（HOST, Team, FPS/Difficulty, Ready）
            var statsRow = new VisualElement();
            statsRow.AddToClassList("player-item-stats");

            // 1. ホストバッジ（ホストの場合、名前の直後）またはスペーサー
            if (player.IsHost)
            {
                var hostBadge = new Label("HOST");
                hostBadge.AddToClassList("host-badge");
                statsRow.Add(hostBadge);
            }
            else
            {
                // ホストでない場合、スペーサーを追加して位置を揃える
                var spacer = new VisualElement();
                spacer.AddToClassList("host-badge");
                spacer.style.visibility = Visibility.Hidden;
                statsRow.Add(spacer);
            }

            // 2. チームバッジ
            var teamBadge = new Label(GetTeamLabel(player.Team));
            teamBadge.AddToClassList("team-badge");
            teamBadge.AddToClassList(GetTeamClass(player.Team));
            statsRow.Add(teamBadge);

            // 3. FPS（通常プレイヤー）or Difficulty（NPC）
            if (player.IsNPC)
            {
                // NPC難易度ドロップダウン（ホストのみ編集可能）
                if (ViewModel != null && ViewModel.IsHost)
                {
                    var difficultyDropdown = new DropdownField();
                    difficultyDropdown.choices = new List<string> { "Easy", "Normal", "Hard", "Expert" };
                    difficultyDropdown.value = player.Difficulty;
                    difficultyDropdown.AddToClassList("npc-difficulty-dropdown");
                    difficultyDropdown.RegisterValueChangedCallback(evt => OnNPCDifficultyChanged(player.PlayerId, evt.newValue));
                    statsRow.Add(difficultyDropdown);
                }
                else
                {
                    var difficultyLabel = new Label($"NPC ({player.Difficulty})");
                    difficultyLabel.AddToClassList("player-item-fps");
                    statsRow.Add(difficultyLabel);
                }
            }
            else
            {
                // 通常プレイヤー: FPS
                var fpsLabel = new Label($"{player.Fps} FPS");
                fpsLabel.AddToClassList("player-item-fps");
                statsRow.Add(fpsLabel);
            }

            // 準備状態バッジ（ゲストのみ、NPCは除外）
            if (!player.IsHost && !player.IsNPC)
            {
                var readyBadge = new Label(player.IsReady ? "READY" : "NOT READY");
                readyBadge.AddToClassList("ready-badge");
                readyBadge.AddToClassList(player.IsReady ? "ready-true" : "ready-false");
                statsRow.Add(readyBadge);
            }

            infoSection.Add(statsRow);
            container.Add(infoSection);

            // アクションセクション（チーム変更、キック、Remove NPC）
            if (ViewModel != null)
            {
                var actionsSection = new VisualElement();
                actionsSection.AddToClassList("player-item-actions");

                if (player.IsNPC)
                {
                    // NPCの場合: チーム変更ボタン（ホストのみ）
                    if (ViewModel.IsHost)
                    {
                        var teamButton = new Button(() => OnTeamButtonClicked(player.PlayerId));
                        teamButton.text = "Team";
                        teamButton.AddToClassList("team-button");
                        actionsSection.Add(teamButton);

                        var removeNpcButton = new Button(() => OnRemoveNPCClicked(player.PlayerId));
                        removeNpcButton.text = "Remove";
                        removeNpcButton.AddToClassList("kick-button");
                        actionsSection.Add(removeNpcButton);
                    }
                }
                else
                {
                    // 通常プレイヤーの場合: チーム変更ボタン
                    var teamButton = new Button(() => OnTeamButtonClicked(player.PlayerId));
                    teamButton.text = "Team";
                    teamButton.AddToClassList("team-button");
                    actionsSection.Add(teamButton);

                    // キックボタン（ホストのみ、自分以外）
                    if (ViewModel.IsHost && !player.IsHost)
                    {
                        var kickButton = new Button(() => OnKickButtonClicked(player.PlayerId));
                        kickButton.text = "Kick";
                        kickButton.AddToClassList("kick-button");
                        actionsSection.Add(kickButton);
                    }
                }

                container.Add(actionsSection);
            }

            return container;
        }

        /// <summary>
        /// チームラベルを取得します
        /// </summary>
        private string GetTeamLabel(PlayerTeam team)
        {
            return team switch
            {
                PlayerTeam.TeamA => "Team A",
                PlayerTeam.TeamB => "Team B",
                _ => "No Team"
            };
        }

        /// <summary>
        /// チームクラスを取得します
        /// </summary>
        private string GetTeamClass(PlayerTeam team)
        {
            return team switch
            {
                PlayerTeam.TeamA => "team-a",
                PlayerTeam.TeamB => "team-b",
                _ => "team-none"
            };
        }

        #endregion

        #region UI Updates

        /// <summary>
        /// UIを更新します
        /// </summary>
        private void UpdateUI()
        {
            if (ViewModel == null) return;

            // Join Code
            if (_joinCodeLabel != null)
            {
                _joinCodeLabel.text = $"Join Code: {ViewModel.JoinCode}";
            }

            // Room Info
            if (_roomNameLabel != null)
            {
                _roomNameLabel.text = ViewModel.RoomName;
            }

            if (_hostNameLabel != null)
            {
                _hostNameLabel.text = ViewModel.HostName;
            }

            if (_gameModeLabel != null)
            {
                _gameModeLabel.text = ViewModel.GameMode;
            }

            if (_mapLabel != null)
            {
                _mapLabel.text = ViewModel.MapName;
            }

            if (_playersLabel != null)
            {
                _playersLabel.text = $"{ViewModel.CurrentPlayers} / {ViewModel.MaxPlayers}";
            }

            // Status
            if (_statusLabel != null)
            {
                _statusLabel.text = ViewModel.StatusMessage;
            }

            // Game Settings Section (Always visible for host)
            if (_gameSettingsSection != null)
            {
                _gameSettingsSection.style.display = ViewModel.IsHost ? DisplayStyle.Flex : DisplayStyle.None;
            }

            // Game Settings - Time Limit (Toggle between label and dropdown)
            if (_timeLimitLabel != null && _timeLimitDropdown != null)
            {
                _timeLimitLabel.style.display = (ViewModel.IsHost && _isEditMode) ? DisplayStyle.None : DisplayStyle.Flex;
                _timeLimitDropdown.style.display = (ViewModel.IsHost && _isEditMode) ? DisplayStyle.Flex : DisplayStyle.None;
            }

            // Game Settings - Arrow Limit (Toggle between label and dropdown)
            if (_arrowLimitLabel != null && _arrowLimitDropdown != null)
            {
                _arrowLimitLabel.style.display = (ViewModel.IsHost && _isEditMode) ? DisplayStyle.None : DisplayStyle.Flex;
                _arrowLimitDropdown.style.display = (ViewModel.IsHost && _isEditMode) ? DisplayStyle.Flex : DisplayStyle.None;
            }

            // Change Settings Button Section (Host Only)
            if (_changeSettingsButtonSection != null)
            {
                _changeSettingsButtonSection.style.display = ViewModel.IsHost ? DisplayStyle.Flex : DisplayStyle.None;
            }

            // Room Info - Room Name (Edit Mode shows TextField, otherwise Label)
            if (_roomNameLabel != null && _roomNameField != null)
            {
                _roomNameLabel.style.display = (ViewModel.IsHost && _isEditMode) ? DisplayStyle.None : DisplayStyle.Flex;
                _roomNameField.style.display = (ViewModel.IsHost && _isEditMode) ? DisplayStyle.Flex : DisplayStyle.None;
                if (ViewModel.IsHost && _isEditMode && _roomNameField.value != ViewModel.RoomName)
                {
                    _roomNameField.value = ViewModel.RoomName;
                }
            }

            // Room Info - Password (Edit Mode shows TextField, otherwise Label)
            if (_passwordLabel != null && _passwordField != null)
            {
                _passwordLabel.style.display = (ViewModel.IsHost && _isEditMode) ? DisplayStyle.None : DisplayStyle.Flex;
                _passwordField.style.display = (ViewModel.IsHost && _isEditMode) ? DisplayStyle.Flex : DisplayStyle.None;

                // Show "******" if password is set, otherwise "None"
                if (_passwordField.value != null && !string.IsNullOrEmpty(_passwordField.value))
                {
                    _passwordLabel.text = "******";
                }
                else
                {
                    _passwordLabel.text = "None";
                }
            }

            // Room Info - Public (Edit Mode shows Toggle, otherwise Label)
            if (_publicLabel != null && _publicToggle != null)
            {
                _publicLabel.style.display = (ViewModel.IsHost && _isEditMode) ? DisplayStyle.None : DisplayStyle.Flex;
                _publicToggle.style.display = (ViewModel.IsHost && _isEditMode) ? DisplayStyle.Flex : DisplayStyle.None;
            }

            // Room Info - Max Players (Edit Mode shows Dropdown, otherwise Label)
            if (_maxPlayersLabel != null && _maxPlayersDropdown != null)
            {
                _maxPlayersLabel.style.display = (ViewModel.IsHost && _isEditMode) ? DisplayStyle.None : DisplayStyle.Flex;
                _maxPlayersDropdown.style.display = (ViewModel.IsHost && _isEditMode) ? DisplayStyle.Flex : DisplayStyle.None;
            }

            // Room Info Dropdowns (Edit Mode shows dropdowns, otherwise labels)
            if (_gameModeLabel != null && _gameModeDropdown != null)
            {
                _gameModeLabel.style.display = (ViewModel.IsHost && _isEditMode) ? DisplayStyle.None : DisplayStyle.Flex;
                _gameModeDropdown.style.display = (ViewModel.IsHost && _isEditMode) ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (_mapLabel != null && _mapDropdown != null)
            {
                _mapLabel.style.display = (ViewModel.IsHost && _isEditMode) ? DisplayStyle.None : DisplayStyle.Flex;
                _mapDropdown.style.display = (ViewModel.IsHost && _isEditMode) ? DisplayStyle.Flex : DisplayStyle.None;
            }

            // Buttons visibility
            UpdateButtonVisibility();
        }

        /// <summary>
        /// ボタンの表示状態を更新します
        /// </summary>
        private void UpdateButtonVisibility()
        {
            if (ViewModel == null) return;

            // Leave Room Button - 準備中は無効
            if (_leaveRoomButton != null)
            {
                _leaveRoomButton.SetEnabled(!ViewModel.IsReady || ViewModel.IsHost);
            }

            if (ViewModel.IsHost)
            {
                // ホストの場合: Start Game ボタンのみ表示
                if (_startGameButton != null)
                {
                    _startGameButton.style.display = DisplayStyle.Flex;
                    _startGameButton.SetEnabled(ViewModel.CanStartGame);
                }

                if (_readyButton != null)
                {
                    _readyButton.style.display = DisplayStyle.None;
                }

                if (_cancelReadyButton != null)
                {
                    _cancelReadyButton.style.display = DisplayStyle.None;
                }
            }
            else
            {
                // ゲストの場合: Ready / Cancel Ready ボタンを切り替え
                if (_startGameButton != null)
                {
                    _startGameButton.style.display = DisplayStyle.None;
                }

                if (_readyButton != null)
                {
                    _readyButton.style.display = ViewModel.IsReady ? DisplayStyle.None : DisplayStyle.Flex;
                }

                if (_cancelReadyButton != null)
                {
                    _cancelReadyButton.style.display = ViewModel.IsReady ? DisplayStyle.Flex : DisplayStyle.None;
                }
            }
        }

        /// <summary>
        /// カウントダウンダイアログの表示状態を更新します
        /// </summary>
        private void UpdateCountdownDialog()
        {
            if (ViewModel == null || _countdownDialog == null) return;

            _countdownDialog.style.display = ViewModel.IsCountingDown ? DisplayStyle.Flex : DisplayStyle.None;

            if (ViewModel.IsCountingDown && _countdownLabel != null)
            {
                _countdownLabel.text = ViewModel.CountdownSeconds.ToString();
            }
        }

        /// <summary>
        /// ゲームモードに応じて設定の有効/無効を更新します
        /// </summary>
        private void UpdateGameSettingsAvailability()
        {
            if (ViewModel == null) return;

            // GameMode文字列をenumに変換
            if (!System.Enum.TryParse<GameMode>(ViewModel.GameMode, out var parsedMode))
            {
                return;
            }

            // タイムリミットの有効/無効
            bool isTimeLimitEditable = GameModeRules.IsTimeLimitEditable(parsedMode);
            if (_timeLimitDropdown != null)
            {
                _timeLimitDropdown.SetEnabled(isTimeLimitEditable && ViewModel.IsHost && _isEditMode);
            }

            // 矢の制限の有効/無効
            bool isArrowLimitEditable = GameModeRules.IsArrowLimitEditable(parsedMode);
            if (_arrowLimitDropdown != null)
            {
                _arrowLimitDropdown.SetEnabled(isArrowLimitEditable && ViewModel.IsHost && _isEditMode);
            }

            // ドロップダウンの値を更新
            UpdateGameSettingsDropdownValues();

            Debug.Log($"[MatchRoomView] Game settings availability updated for mode {ViewModel.GameMode}: TimeLimitEditable={isTimeLimitEditable}, ArrowLimitEditable={isArrowLimitEditable}");
        }

        /// <summary>
        /// ゲーム設定ドロップダウンの表示値を更新します
        /// </summary>
        private void UpdateGameSettingsDropdownValues()
        {
            if (ViewModel == null) return;

            // タイムリミットの表示を更新
            if (_timeLimitDropdown != null)
            {
                string timeLimitValue = ViewModel.TimeLimit switch
                {
                    180 => "3:00",
                    300 => "5:00",
                    600 => "10:00",
                    900 => "15:00",
                    0 => "No Limit",
                    _ => "No Limit"
                };
                _timeLimitDropdown.value = timeLimitValue;
            }

            // タイムリミットラベルの表示を更新
            if (_timeLimitLabel != null)
            {
                _timeLimitLabel.text = ViewModel.TimeLimit == 0 ? "No Limit" : $"{ViewModel.TimeLimit / 60}:00";
            }

            // 矢の制限の表示を更新
            if (_arrowLimitDropdown != null)
            {
                string arrowLimitValue = ViewModel.ArrowLimit switch
                {
                    5 => "5",
                    10 => "10",
                    15 => "15",
                    20 => "20",
                    0 => "No Limit",
                    _ => "No Limit"
                };
                _arrowLimitDropdown.value = arrowLimitValue;
            }

            // 矢の制限ラベルの表示を更新
            if (_arrowLimitLabel != null)
            {
                _arrowLimitLabel.text = ViewModel.ArrowLimit == 0 ? "No Limit" : ViewModel.ArrowLimit.ToString();
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// ViewModelのプロパティ変更イベントを処理します
        /// </summary>
        private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (ViewModel == null)
            {
                return;
            }

            switch (e.PropertyName)
            {
                case nameof(MatchRoomViewModel.IsCountingDown):
                    UpdateCountdownDialog();
                    break;

                case nameof(MatchRoomViewModel.IsReady):
                case nameof(MatchRoomViewModel.IsHost):
                case nameof(MatchRoomViewModel.CanStartGame):
                    UpdateButtonVisibility();
                    break;

                case nameof(MatchRoomViewModel.Players):
                    // プレイヤー情報の変更（チーム変更など）
                    PopulatePlayerList();
                    UpdateUI();
                    break;

                case nameof(MatchRoomViewModel.GameMode):
                    // ゲームモード変更時にドロップダウンの有効/無効を更新
                    UpdateGameSettingsAvailability();
                    UpdateUI();
                    break;

                case nameof(MatchRoomViewModel.TimeLimit):
                case nameof(MatchRoomViewModel.ArrowLimit):
                    // 設定値が変更された場合にドロップダウン表示を更新
                    UpdateGameSettingsDropdownValues();
                    break;

                default:
                    UpdateUI();
                    break;
            }
        }

        /// <summary>
        /// プレイヤーリストの変更イベントを処理します
        /// </summary>
        private void OnPlayersCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            PopulatePlayerList();
            UpdateUI();
        }

        /// <summary>
        /// ゲーム開始イベントを処理します
        /// </summary>
        private void OnGameStarting(object? sender, EventArgs e)
        {
            _isCountdownActive = true;

            UpdateCountdownDialog();
        }

        /// <summary>
        /// カウントダウン更新イベントを処理します
        /// </summary>
        private void OnCountdownUpdated(object? sender, int secondsRemaining)
        {
            if (_countdownLabel != null)
            {
                _countdownLabel.text = secondsRemaining.ToString();
            }

            // カウントダウンティックSE
            if (secondsRemaining > 0 && _countdownTickSfx != null)
            {
                var audioService = ServiceLocator.Instance.Get<IAudioService>();
                audioService?.PlaySfx(_countdownTickSfx);
            }

            if (secondsRemaining <= 0)
            {
                _isCountdownActive = false;
            }
        }

        /// <summary>
        /// ローディングシーン遷移要求イベントを処理します
        /// </summary>
        private void OnLoadingSceneRequested(object? sender, EventArgs e)
        {
            Debug.Log("[MatchRoomView] Loading scene requested. (Handled by ASM automatically)");
            // Loading screens are handled automatically by Advanced Scene Manager
        }

        /// <summary>
        /// エラー発生イベントを処理します
        /// </summary>
        private void OnErrorOccurred(object? sender, string errorMessage)
        {
            Debug.LogError($"[MatchRoomView] Error: {errorMessage}");
        }

        /// <summary>
        /// 退出ボタンがクリックされた時の処理
        /// </summary>
        private void OnLeaveRoomButtonClicked()
        {
            PlayButtonClickSfx();
            ViewModel?.LeaveRoom();
        }

        /// <summary>
        /// ゲーム開始ボタンがクリックされた時の処理
        /// </summary>
        private void OnStartGameButtonClicked()
        {
            PlayButtonClickSfx();
            ViewModel?.StartGame();
        }

        /// <summary>
        /// 準備完了ボタンがクリックされた時の処理
        /// </summary>
        private void OnReadyButtonClicked()
        {
            PlayButtonClickSfx();
            ViewModel?.ToggleReady();
        }

        /// <summary>
        /// 準備キャンセルボタンがクリックされた時の処理
        /// </summary>
        private void OnCancelReadyButtonClicked()
        {
            PlayButtonClickSfx();
            ViewModel?.ToggleReady();
        }

        /// <summary>
        /// カウントダウンキャンセルボタンがクリックされた時の処理
        /// </summary>
        private void OnCancelCountdownButtonClicked()
        {
            PlayButtonClickSfx();
            ViewModel?.CancelCountdown();
            _isCountdownActive = false;
        }

        /// <summary>
        /// チーム変更ボタンがクリックされた時の処理
        /// </summary>
        private void OnTeamButtonClicked(string playerId)
        {
            if (ViewModel == null) return;

            PlayButtonClickSfx();

            var player = ViewModel.Players.FirstOrDefault(p => p.PlayerId == playerId);
            if (player != null)
            {
                // チームをローテーション
                var newTeam = player.Team switch
                {
                    PlayerTeam.None => PlayerTeam.TeamA,
                    PlayerTeam.TeamA => PlayerTeam.TeamB,
                    PlayerTeam.TeamB => PlayerTeam.None,
                    _ => PlayerTeam.None
                };

                ViewModel.ChangePlayerTeam(playerId, newTeam);
            }
        }

        /// <summary>
        /// キックボタンがクリックされた時の処理
        /// </summary>
        private void OnKickButtonClicked(string playerId)
        {
            PlayButtonClickSfx();
            ViewModel?.KickPlayer(playerId);
        }

        /// <summary>
        /// タイムリミット変更イベント
        /// </summary>
        private void OnTimeLimitChanged(ChangeEvent<string> evt)
        {
            if (ViewModel == null)
            {
                return;
            }

            // タイムリミット文字列を秒数に変換
            int seconds = evt.newValue switch
            {
                "3:00" => 180,
                "5:00" => 300,
                "10:00" => 600,
                "15:00" => 900,
                "No Limit" => 0,
                _ => 300
            };

            ViewModel.TimeLimit = seconds;
            ViewModel.UpdateRoomSettings();

            Debug.Log($"[MatchRoomView] Time limit changed to: {evt.newValue} ({seconds} seconds)");
        }

        /// <summary>
        /// 矢の制限変更イベント
        /// </summary>
        private void OnArrowLimitChanged(ChangeEvent<string> evt)
        {
            if (ViewModel == null)
            {
                return;
            }

            // 矢の制限文字列を数値に変換
            int arrows = evt.newValue switch
            {
                "5" => 5,
                "10" => 10,
                "15" => 15,
                "20" => 20,
                "No Limit" => 0,
                _ => 0
            };

            ViewModel.ArrowLimit = arrows;
            ViewModel.UpdateRoomSettings();

            Debug.Log($"[MatchRoomView] Arrow limit changed to: {evt.newValue} ({arrows} arrows)");
        }

        /// <summary>
        /// ゲームモード変更イベント
        /// </summary>
        private void OnGameModeChanged(ChangeEvent<string> evt)
        {
            if (ViewModel == null)
            {
                return;
            }

            ViewModel.GameMode = evt.newValue;
            ViewModel.UpdateRoomSettings();

            Debug.Log($"[MatchRoomView] Game mode changed to: {evt.newValue}");
        }

        /// <summary>
        /// マップ変更イベント
        /// </summary>
        private void OnMapChanged(ChangeEvent<string> evt)
        {
            if (ViewModel == null)
            {
                return;
            }

            ViewModel.MapName = evt.newValue;
            ViewModel.UpdateRoomSettings();

            Debug.Log($"[MatchRoomView] Map changed to: {evt.newValue}");
        }

        /// <summary>
        /// スロットにNPCを追加する処理
        /// </summary>
        private void OnAddNPCToSlot(int slotIndex)
        {
            PlayButtonClickSfx();
            ViewModel?.AddNPC(slotIndex);
        }

        /// <summary>
        /// NPCを削除する処理
        /// </summary>
        private void OnRemoveNPCClicked(string npcId)
        {
            PlayButtonClickSfx();
            ViewModel?.RemoveNPC(npcId);
        }

        /// <summary>
        /// NPC難易度変更イベント
        /// </summary>
        private void OnNPCDifficultyChanged(string npcId, string difficulty)
        {
            ViewModel?.ChangeNPCDifficulty(npcId, difficulty);
        }

        /// <summary>
        /// Change Settings / Apply Settings ボタンがクリックされた時の処理
        /// </summary>
        private void OnChangeSettingsButtonClicked()
        {
            PlayButtonClickSfx();

            if (_isEditMode)
            {
                // Apply Settings: 設定を適用
                ApplySettings();
                _isEditMode = false;
                if (_changeSettingsButton != null)
                {
                    _changeSettingsButton.text = "Change Settings";
                }
                Debug.Log("[MatchRoomView] Settings applied, returning to read-only mode");
            }
            else
            {
                // Change Settings: 編集モードに切り替え
                _isEditMode = true;
                if (_changeSettingsButton != null)
                {
                    _changeSettingsButton.text = "Apply Settings";
                }
                Debug.Log("[MatchRoomView] Entering edit mode");
            }

            // ゲーム設定の有効/無効を更新
            UpdateGameSettingsAvailability();

            // UIを更新
            UpdateUI();
        }

        /// <summary>
        /// 設定を適用します
        /// </summary>
        private void ApplySettings()
        {
            if (ViewModel == null)
            {
                return;
            }

            bool hasChanges = false;

            // ルーム名の変更を検出
            if (_roomNameField != null && _roomNameField.value != ViewModel.RoomName)
            {
                ViewModel.RoomName = _roomNameField.value;
                hasChanges = true;
                Debug.Log($"[MatchRoomView] Room name changed to: {_roomNameField.value}");
            }

            // ゲームモードの変更を検出
            if (_gameModeDropdown != null && _gameModeDropdown.value != ViewModel.GameMode)
            {
                ViewModel.GameMode = _gameModeDropdown.value;
                hasChanges = true;
                Debug.Log($"[MatchRoomView] Game mode changed to: {_gameModeDropdown.value}");
            }

            // マップの変更を検出
            if (_mapDropdown != null && _mapDropdown.value != ViewModel.Map)
            {
                ViewModel.Map = _mapDropdown.value;
                hasChanges = true;
                Debug.Log($"[MatchRoomView] Map changed to: {_mapDropdown.value}");
            }

            // 変更があった場合のみサーバーに通知
            if (hasChanges)
            {
                ViewModel.UpdateRoomSettings();
                Debug.Log("[MatchRoomView] Room settings applied and synced to server");
            }
            else
            {
                Debug.Log("[MatchRoomView] No settings changes detected");
            }
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
