#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
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
        private Label? _hostNameLabel;
        private Label? _gameModeLabel;
        private Label? _mapLabel;
        private Label? _playersLabel;
        private Label? _statusLabel;

        // Room Info Dropdowns (Host Only)
        private DropdownField? _gameModeDropdown;
        private DropdownField? _mapDropdown;

        // Game Settings (Host Only)
        private VisualElement? _gameSettingsSection;
        private DropdownField? _timeLimitDropdown;
        private DropdownField? _scoreGoalDropdown;

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
        private float _countdownTimer = 0f;
        private bool _isCountdownActive = false;

        #endregion

        #region Unity Lifecycle

        /// <summary>
        /// 初期化処理
        /// </summary>
        protected override void Awake()
        {
            base.Awake();

            // サービスを取得
            var lobbyService = ServiceLocator.Instance.Get<ILobbyService>();
            var sceneService = ServiceLocator.Instance.Get<ISceneManagementService>();

            if (lobbyService == null || sceneService == null)
            {
                Debug.LogError("[MatchRoomView] Required services not found!", this);
                return;
            }

            // ViewModelを作成して設定
            ViewModel = new MatchRoomViewModel(lobbyService, sceneService);
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
            if (_isCountdownActive && ViewModel != null)
            {
                _countdownTimer += Time.deltaTime;

                if (_countdownTimer >= 1f)
                {
                    _countdownTimer = 0f;
                    ViewModel.UpdateCountdown();
                }
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

            // 初期状態を設定
            UpdateUI();

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
            _hostNameLabel = Q<Label>("HostNameLabel");
            _gameModeLabel = Q<Label>("GameModeLabel");
            _mapLabel = Q<Label>("MapLabel");
            _playersLabel = Q<Label>("PlayersLabel");
            _statusLabel = Q<Label>("StatusLabel");

            // Room Info Dropdowns (Host Only)
            _gameModeDropdown = Q<DropdownField>("GameModeDropdown");
            _mapDropdown = Q<DropdownField>("MapDropdown");

            // Game Settings (Host Only)
            _gameSettingsSection = Q<VisualElement>("GameSettingsSection");
            _timeLimitDropdown = Q<DropdownField>("TimeLimitDropdown");
            _scoreGoalDropdown = Q<DropdownField>("ScoreGoalDropdown");

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
                    "Arena Mode", "Score Match", "Team Fight", "Deathmatch", "PvE Mode"
                };
                _gameModeDropdown.value = "Arena Mode";
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

            // Time Limit dropdown
            if (_timeLimitDropdown != null)
            {
                _timeLimitDropdown.choices = new List<string>
                {
                    "3:00", "5:00", "10:00", "15:00", "No Limit"
                };
                _timeLimitDropdown.value = "5:00";
            }

            // Score Goal dropdown
            if (_scoreGoalDropdown != null)
            {
                _scoreGoalDropdown.choices = new List<string>
                {
                    "50", "100", "200", "500", "No Limit"
                };
                _scoreGoalDropdown.value = "100";
            }
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

            if (_scoreGoalDropdown != null)
            {
                _scoreGoalDropdown.RegisterValueChangedCallback(OnScoreGoalChanged);
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

            if (_scoreGoalDropdown != null)
            {
                _scoreGoalDropdown.UnregisterValueChangedCallback(OnScoreGoalChanged);
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

            // プレイヤーアイテムを作成
            foreach (var player in ViewModel.Players)
            {
                var playerItem = CreatePlayerListItem(player);
                _playerListContainer.Add(playerItem);
                _playerItemElements[player.PlayerId] = playerItem;
            }
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

            // ステータス行（FPS, Team, Ready）
            var statsRow = new VisualElement();
            statsRow.AddToClassList("player-item-stats");

            // FPS
            var fpsLabel = new Label($"{player.Fps} FPS");
            fpsLabel.AddToClassList("player-item-fps");
            statsRow.Add(fpsLabel);

            // ホストバッジ
            if (player.IsHost)
            {
                var hostBadge = new Label("HOST");
                hostBadge.AddToClassList("host-badge");
                statsRow.Add(hostBadge);
            }

            // チームバッジ
            var teamBadge = new Label(GetTeamLabel(player.Team));
            teamBadge.AddToClassList("team-badge");
            teamBadge.AddToClassList(GetTeamClass(player.Team));
            statsRow.Add(teamBadge);

            // 準備状態バッジ（ゲストのみ）
            if (!player.IsHost)
            {
                var readyBadge = new Label(player.IsReady ? "READY" : "NOT READY");
                readyBadge.AddToClassList("ready-badge");
                readyBadge.AddToClassList(player.IsReady ? "ready-true" : "ready-false");
                statsRow.Add(readyBadge);
            }

            infoSection.Add(statsRow);
            container.Add(infoSection);

            // アクションセクション（チーム変更、キック）
            if (ViewModel != null)
            {
                var actionsSection = new VisualElement();
                actionsSection.AddToClassList("player-item-actions");

                // チーム変更ボタン
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

            // Game Settings Section (Host Only)
            if (_gameSettingsSection != null)
            {
                _gameSettingsSection.style.display = ViewModel.IsHost ? DisplayStyle.Flex : DisplayStyle.None;
            }

            // Room Info Dropdowns (Host shows dropdowns, Guest shows labels)
            if (_gameModeLabel != null && _gameModeDropdown != null)
            {
                _gameModeLabel.style.display = ViewModel.IsHost ? DisplayStyle.None : DisplayStyle.Flex;
                _gameModeDropdown.style.display = ViewModel.IsHost ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (_mapLabel != null && _mapDropdown != null)
            {
                _mapLabel.style.display = ViewModel.IsHost ? DisplayStyle.None : DisplayStyle.Flex;
                _mapDropdown.style.display = ViewModel.IsHost ? DisplayStyle.Flex : DisplayStyle.None;
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
            _countdownTimer = 0f;

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
        /// スコアゴール変更イベント
        /// </summary>
        private void OnScoreGoalChanged(ChangeEvent<string> evt)
        {
            if (ViewModel == null)
            {
                return;
            }

            // スコアゴール文字列を数値に変換
            int score = evt.newValue switch
            {
                "50" => 50,
                "100" => 100,
                "200" => 200,
                "500" => 500,
                "No Limit" => 0,
                _ => 100
            };

            ViewModel.ScoreGoal = score;
            ViewModel.UpdateRoomSettings();

            Debug.Log($"[MatchRoomView] Score goal changed to: {evt.newValue} ({score} points)");
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
