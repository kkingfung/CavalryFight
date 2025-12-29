#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using CavalryFight.Core.MVVM;
using CavalryFight.Services.Lobby;
using CavalryFight.Services.Performance;
using CavalryFight.Services.SceneManagement;
using UnityEngine;

namespace CavalryFight.ViewModels
{
    /// <summary>
    /// マッチルーム画面のViewModel
    /// </summary>
    /// <remarks>
    /// プレイヤーの準備状態、チーム割り当て、ルーム設定を管理します。
    /// ホストはゲーム開始とキックの権限を持ちます。
    /// </remarks>
    public class MatchRoomViewModel : ViewModelBase
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
        /// パフォーマンス監視サービス
        /// </summary>
        private readonly IPerformanceMonitor? _performanceMonitor;

        /// <summary>
        /// プレイヤーリスト
        /// </summary>
        private ObservableCollection<PlayerInfo> _players = new ObservableCollection<PlayerInfo>();

        /// <summary>
        /// ローカルプレイヤーがホストかどうか
        /// </summary>
        private bool _isHost = false;

        /// <summary>
        /// ローカルプレイヤーの準備状態
        /// </summary>
        private bool _isReady = false;

        /// <summary>
        /// カウントダウン中かどうか
        /// </summary>
        private bool _isCountingDown = false;

        /// <summary>
        /// カウントダウン残り時間
        /// </summary>
        private int _countdownSeconds = 5;

        /// <summary>
        /// ステータスメッセージ
        /// </summary>
        private string _statusMessage = "Waiting for players to ready up...";

        /// <summary>
        /// ルーム名
        /// </summary>
        private string _roomName = "";

        /// <summary>
        /// ホスト名
        /// </summary>
        private string _hostName = "";

        /// <summary>
        /// ゲームモード
        /// </summary>
        private string _gameMode = "";

        /// <summary>
        /// マップ名
        /// </summary>
        private string _mapName = "";

        /// <summary>
        /// 現在のプレイヤー数
        /// </summary>
        private int _currentPlayers = 0;

        /// <summary>
        /// 最大プレイヤー数
        /// </summary>
        private int _maxPlayers = 8;

        /// <summary>
        /// ジョインコード
        /// </summary>
        private string _joinCode = "";

        /// <summary>
        /// タイムリミット（秒）
        /// </summary>
        private int _timeLimit = 300;

        /// <summary>
        /// 矢の制限数
        /// </summary>
        private int _arrowLimit = 0;

        /// <summary>
        /// カウントダウン用のタイマー累積（秒）
        /// </summary>
        private float _countdownAccumulator = 0f;

        #endregion

        #region Properties

        /// <summary>
        /// プレイヤーリスト
        /// </summary>
        public ObservableCollection<PlayerInfo> Players
        {
            get => _players;
            set => SetProperty(ref _players, value);
        }

        /// <summary>
        /// ローカルプレイヤーがホストかどうか
        /// </summary>
        public bool IsHost
        {
            get => _isHost;
            set => SetProperty(ref _isHost, value);
        }

        /// <summary>
        /// ローカルプレイヤーの準備状態
        /// </summary>
        public bool IsReady
        {
            get => _isReady;
            set => SetProperty(ref _isReady, value);
        }

        /// <summary>
        /// カウントダウン中かどうか
        /// </summary>
        public bool IsCountingDown
        {
            get => _isCountingDown;
            set => SetProperty(ref _isCountingDown, value);
        }

        /// <summary>
        /// カウントダウン残り時間
        /// </summary>
        public int CountdownSeconds
        {
            get => _countdownSeconds;
            set => SetProperty(ref _countdownSeconds, value);
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
        /// ルーム名
        /// </summary>
        public string RoomName
        {
            get => _roomName;
            set => SetProperty(ref _roomName, value);
        }

        /// <summary>
        /// ホスト名
        /// </summary>
        public string HostName
        {
            get => _hostName;
            set => SetProperty(ref _hostName, value);
        }

        /// <summary>
        /// ゲームモード
        /// </summary>
        public string GameMode
        {
            get => _gameMode;
            set
            {
                if (SetProperty(ref _gameMode, value))
                {
                    // ゲームモード変更時に適切なデフォルト値を設定
                    ApplyGameModeRules();
                }
            }
        }

        /// <summary>
        /// マップ名
        /// </summary>
        public string MapName
        {
            get => _mapName;
            set => SetProperty(ref _mapName, value);
        }

        /// <summary>
        /// 現在のプレイヤー数
        /// </summary>
        public int CurrentPlayers
        {
            get => _currentPlayers;
            set => SetProperty(ref _currentPlayers, value);
        }

        /// <summary>
        /// 最大プレイヤー数
        /// </summary>
        public int MaxPlayers
        {
            get => _maxPlayers;
            set => SetProperty(ref _maxPlayers, value);
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
        /// タイムリミット（秒）
        /// </summary>
        public int TimeLimit
        {
            get => _timeLimit;
            set => SetProperty(ref _timeLimit, value);
        }

        /// <summary>
        /// 矢の制限数
        /// </summary>
        public int ArrowLimit
        {
            get => _arrowLimit;
            set => SetProperty(ref _arrowLimit, value);
        }

        /// <summary>
        /// 全てのゲストが準備完了しているかどうか
        /// </summary>
        public bool AllGuestsReady
        {
            get
            {
                var guests = Players.Where(p => !p.IsHost);
                return guests.Any() && guests.All(p => p.IsReady);
            }
        }

        /// <summary>
        /// スタートゲームボタンが有効かどうか
        /// </summary>
        public bool CanStartGame => IsHost && AllGuestsReady && !IsCountingDown;

        #endregion

        #region Events

        /// <summary>
        /// ゲーム開始イベント
        /// </summary>
        public event EventHandler? GameStarting;

        /// <summary>
        /// カウントダウン更新イベント
        /// </summary>
        public event EventHandler<int>? CountdownUpdated;

        /// <summary>
        /// ローディングシーン遷移要求イベント
        /// </summary>
        public event EventHandler? LoadingSceneRequested;

        /// <summary>
        /// エラー発生イベント
        /// </summary>
        public event EventHandler<string>? ErrorOccurred;

        #endregion

        #region Constructors

        /// <summary>
        /// MatchRoomViewModelの新しいインスタンスを初期化します
        /// </summary>
        /// <param name="lobbyService">ロビーサービス</param>
        /// <param name="sceneManagementService">シーン管理サービス</param>
        /// <param name="performanceMonitor">パフォーマンス監視サービス（オプション）</param>
        public MatchRoomViewModel(ILobbyService lobbyService, ISceneManagementService sceneManagementService, IPerformanceMonitor? performanceMonitor = null)
        {
            _lobbyService = lobbyService ?? throw new ArgumentNullException(nameof(lobbyService));
            _sceneManagementService = sceneManagementService ?? throw new ArgumentNullException(nameof(sceneManagementService));
            _performanceMonitor = performanceMonitor;

            // ロビーイベントを購読
            SubscribeToLobbyEvents();

            // パフォーマンス監視イベントを購読
            if (_performanceMonitor != null)
            {
                _performanceMonitor.FPSUpdated += OnFPSUpdated;
                Debug.Log("[MatchRoomViewModel] Subscribed to PerformanceMonitor FPS updates.");
            }

            // 初期化
            InitializeRoomData();

            Debug.Log("[MatchRoomViewModel] Initialized.");
        }

        #endregion

        #region Initialization

        /// <summary>
        /// ロビーサービスのイベントを購読します
        /// </summary>
        private void SubscribeToLobbyEvents()
        {
            _lobbyService.HostDisconnected += OnHostDisconnected;
            _lobbyService.PlayerJoined += OnPlayerJoined;
            _lobbyService.PlayerLeft += OnPlayerLeft;
        }

        /// <summary>
        /// ロビーサービスのイベント購読を解除します
        /// </summary>
        private void UnsubscribeFromLobbyEvents()
        {
            _lobbyService.HostDisconnected -= OnHostDisconnected;
            _lobbyService.PlayerJoined -= OnPlayerJoined;
            _lobbyService.PlayerLeft -= OnPlayerLeft;
        }

        /// <summary>
        /// ホスト切断イベントハンドラ
        /// </summary>
        private void OnHostDisconnected()
        {
            Debug.LogWarning("[MatchRoomViewModel] Host has disconnected.");

            // エラーメッセージを発火
            ErrorOccurred?.Invoke(this, "Host has left the room.");

            // ロビーに戻る
            _sceneManagementService.LoadLobby();
        }

        /// <summary>
        /// FPS更新イベントハンドラ
        /// </summary>
        /// <param name="fps">新しいFPS値</param>
        private void OnFPSUpdated(int fps)
        {
            // ローカルプレイヤーのFPSを更新
            var localPlayer = Players.FirstOrDefault(p => p.IsHost == IsHost &&
                                                          p.PlayerId == _lobbyService.LocalPlayerInfo?.PlayerId.ToString());
            if (localPlayer != null)
            {
                localPlayer.Fps = fps;
            }
        }

        /// <summary>
        /// プレイヤー参加イベントハンドラ
        /// </summary>
        /// <param name="playerId">参加したプレイヤーID</param>
        private void OnPlayerJoined(ulong playerId)
        {
            Debug.Log($"[MatchRoomViewModel] Player joined: {playerId}");

            // PlayerSlotsから該当プレイヤーの情報を取得
            var slot = _lobbyService.PlayerSlots.FirstOrDefault(s => s.PlayerId == playerId);
            if (slot.IsEmpty())
            {
                Debug.LogWarning($"[MatchRoomViewModel] Joined player slot not found: {playerId}");
                return;
            }

            // Playersコレクションに追加
            var playerInfo = new PlayerInfo
            {
                PlayerId = slot.PlayerId.ToString(),
                PlayerName = slot.PlayerName.ToString(),
                IsHost = slot.PlayerId == _lobbyService.LocalPlayerInfo?.PlayerId,
                IsReady = slot.IsReady,
                Team = slot.TeamIndex switch
                {
                    0 => PlayerTeam.TeamA,
                    1 => PlayerTeam.TeamB,
                    _ => PlayerTeam.None
                },
                IsNPC = slot.IsAI,
                Difficulty = slot.IsAI ? slot.AIDifficulty.ToString() : "Normal",
                Fps = (slot.PlayerId == _lobbyService.LocalPlayerInfo?.PlayerId) ? (_performanceMonitor?.CurrentFPS ?? 0) : 0
            };

            Players.Add(playerInfo);
            CurrentPlayers = Players.Count;
            OnPropertyChanged(nameof(Players));
            UpdateStatusMessage();

            Debug.Log($"[MatchRoomViewModel] Player added to collection: {playerInfo.PlayerName}");
        }

        /// <summary>
        /// プレイヤー退出イベントハンドラ
        /// </summary>
        /// <param name="playerId">退出したプレイヤーID</param>
        private void OnPlayerLeft(ulong playerId)
        {
            Debug.Log($"[MatchRoomViewModel] Player left: {playerId}");

            // Playersコレクションから削除
            var player = Players.FirstOrDefault(p => p.PlayerId == playerId.ToString());
            if (player != null)
            {
                Players.Remove(player);
                CurrentPlayers = Players.Count;
                OnPropertyChanged(nameof(Players));
                UpdateStatusMessage();

                Debug.Log($"[MatchRoomViewModel] Player removed from collection: {player.PlayerName}");
            }
            else
            {
                Debug.LogWarning($"[MatchRoomViewModel] Left player not found in collection: {playerId}");
            }
        }

        /// <summary>
        /// ルームデータを初期化します
        /// </summary>
        private void InitializeRoomData()
        {
            // ロビーサービスから現在のルームデータを取得
            if (_lobbyService.IsInRoom)
            {
                var settings = _lobbyService.CurrentRoomSettings;
                RoomName = settings.RoomName.ToString();
                GameMode = settings.GameMode.ToString();
                MapName = settings.MapName.ToString();
                MaxPlayers = settings.MaxPlayers;
                TimeLimit = settings.TimeLimit;
                ArrowLimit = settings.ArrowLimit;
                JoinCode = _lobbyService.CurrentJoinCode ?? "";
                IsHost = _lobbyService.IsHost;

                // ホスト名を設定（ローカルプレイヤーがホストの場合）
                if (_lobbyService.LocalPlayerInfo != null)
                {
                    if (IsHost)
                    {
                        HostName = _lobbyService.LocalPlayerInfo.PlayerName.ToString();
                    }
                }

                // プレイヤースロットから現在のプレイヤー数を取得
                CurrentPlayers = _lobbyService.PlayerSlots.Count(s => !s.IsEmpty());

                // Playersコレクションに実際のプレイヤー情報を追加
                Players.Clear();
                foreach (var slot in _lobbyService.PlayerSlots)
                {
                    if (!slot.IsEmpty())
                    {
                        var playerInfo = new PlayerInfo
                        {
                            PlayerId = slot.PlayerId.ToString(),
                            PlayerName = slot.PlayerName.ToString(),
                            IsHost = slot.PlayerId == _lobbyService.LocalPlayerInfo?.PlayerId, // ホスト判定
                            IsReady = slot.IsReady,
                            Team = slot.TeamIndex switch
                            {
                                0 => PlayerTeam.TeamA,
                                1 => PlayerTeam.TeamB,
                                _ => PlayerTeam.None
                            },
                            IsNPC = slot.IsAI,
                            Difficulty = slot.IsAI ? slot.AIDifficulty.ToString() : "Normal",
                            Fps = (slot.PlayerId == _lobbyService.LocalPlayerInfo?.PlayerId) ? (_performanceMonitor?.CurrentFPS ?? 0) : 0
                        };

                        Players.Add(playerInfo);
                    }
                }

                Debug.Log($"[MatchRoomViewModel] Room data initialized from LobbyService: {RoomName}, Host: {IsHost}, Players: {Players.Count}");
            }
#if DEBUG
            else
            {
                // ルームに参加していない場合は仮データで初期化（テスト用）
                Debug.LogWarning("[MatchRoomViewModel] Not in room, using dummy data for testing.");
                RoomName = "Test Room";
                HostName = "Player1";
                GameMode = "Arena";
                MapName = "DefaultArena";
                CurrentPlayers = 1;
                MaxPlayers = 8;
                JoinCode = "ABC123";
                IsHost = true;

                // テスト用プレイヤーを追加
                Players.Add(new PlayerInfo
                {
                    PlayerId = "host",
                    PlayerName = "Player1",
                    IsHost = true,
                    IsReady = false,
                    Team = PlayerTeam.None,
                    Fps = 60
                });
            }
#endif
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// プレイヤーの準備状態を切り替えます
        /// </summary>
        public void ToggleReady()
        {
            if (IsHost)
            {
                Debug.LogWarning("[MatchRoomViewModel] Host cannot ready up.");
                return;
            }

            IsReady = !IsReady;

            // ネットワーク経由で準備状態を送信
            _lobbyService.SetReady(IsReady);
            Debug.Log($"[MatchRoomViewModel] Player ready state: {IsReady}");

            UpdateStatusMessage();
        }

        /// <summary>
        /// プレイヤーのチームを変更します（ホストのみ）
        /// </summary>
        /// <param name="playerId">プレイヤーID</param>
        /// <param name="newTeam">新しいチーム</param>
        public void ChangePlayerTeam(string playerId, PlayerTeam newTeam)
        {
            if (!IsHost)
            {
                Debug.LogWarning("[MatchRoomViewModel] Only host can change player teams.");
                return;
            }

            // PlayerId文字列をulongに変換
            if (!ulong.TryParse(playerId, out ulong playerIdUlong))
            {
                Debug.LogWarning($"[MatchRoomViewModel] Invalid player ID format: {playerId}");
                return;
            }

            // PlayerTeam enumをteamIndexに変換
            int teamIndex = newTeam switch
            {
                PlayerTeam.TeamA => 0,
                PlayerTeam.TeamB => 1,
                PlayerTeam.None => -1,
                _ => -1
            };

            // LobbyServiceを使用してネットワーク経由でチーム変更を送信
            bool success = _lobbyService.SetPlayerTeam(playerIdUlong, teamIndex);

            if (success)
            {
                Debug.Log($"[MatchRoomViewModel] Player {playerId} team changed to {newTeam}");
                // チーム変更はネットワーク同期により、PlayerSlotsが更新され、
                // 変更イベント経由でPlayersコレクションも更新されます
                OnPropertyChanged(nameof(Players));
            }
            else
            {
                Debug.LogError($"[MatchRoomViewModel] Failed to change player team for {playerId}");
                ErrorOccurred?.Invoke(this, "Failed to change player team");
            }
        }

        /// <summary>
        /// プレイヤーをキックします（ホストのみ）
        /// </summary>
        /// <param name="playerId">プレイヤーID</param>
        public void KickPlayer(string playerId)
        {
            if (!IsHost)
            {
                Debug.LogWarning("[MatchRoomViewModel] Only host can kick players.");
                ErrorOccurred?.Invoke(this, "Only host can kick players.");
                return;
            }

            var player = Players.FirstOrDefault(p => p.PlayerId == playerId);
            if (player != null && !player.IsHost)
            {
                // ネットワーク経由でキック命令を送信
                if (ulong.TryParse(playerId, out ulong networkPlayerId))
                {
                    if (_lobbyService.KickPlayer(networkPlayerId))
                    {
                        Debug.Log($"[MatchRoomViewModel] Kicked player: {player.PlayerName}");
                        Players.Remove(player);
                        CurrentPlayers = Players.Count;
                        UpdateStatusMessage();
                    }
                    else
                    {
                        Debug.LogError($"[MatchRoomViewModel] Failed to kick player: {player.PlayerName}");
                        ErrorOccurred?.Invoke(this, $"Failed to kick player: {player.PlayerName}");
                    }
                }
                else
                {
                    Debug.LogError($"[MatchRoomViewModel] Invalid player ID format: {playerId}");
                    ErrorOccurred?.Invoke(this, "Invalid player ID format");
                }
            }
        }

        /// <summary>
        /// NPCを追加します（ホストのみ）
        /// </summary>
        /// <param name="slotIndex">スロットインデックス</param>
        public void AddNPC(int slotIndex)
        {
            if (!IsHost)
            {
                Debug.LogWarning("[MatchRoomViewModel] Only host can add NPCs.");
                return;
            }

            if (Players.Count >= MaxPlayers)
            {
                Debug.LogWarning("[MatchRoomViewModel] Cannot add NPC: room is full.");
                return;
            }

            // LobbyServiceを使用してネットワーク経由でCPUプレイヤーを追加
            // スロット位置を指定して追加（-1の場合は自動配置）
            bool success = _lobbyService.AddCPUPlayer(AIDifficulty.Normal, slotIndex);

            if (success)
            {
                Debug.Log($"[MatchRoomViewModel] Successfully added NPC at slot {slotIndex}");
                // NPCはPlayerJoinedイベント経由でPlayersコレクションに自動的に追加されます
            }
            else
            {
                Debug.LogError($"[MatchRoomViewModel] Failed to add NPC at slot {slotIndex}");
                ErrorOccurred?.Invoke(this, "Failed to add NPC");
            }
        }

        /// <summary>
        /// NPCを削除します（ホストのみ）
        /// </summary>
        /// <param name="npcId">NPC ID</param>
        public void RemoveNPC(string npcId)
        {
            if (!IsHost)
            {
                Debug.LogWarning("[MatchRoomViewModel] Only host can remove NPCs.");
                return;
            }

            // PlayerId から該当NPCのスロットインデックスを検索
            if (!ulong.TryParse(npcId, out ulong playerId))
            {
                Debug.LogWarning($"[MatchRoomViewModel] Invalid NPC ID format: {npcId}");
                return;
            }

            // PlayerSlotsから該当するNPCのスロットを探す
            var slot = _lobbyService.PlayerSlots.FirstOrDefault(s => s.PlayerId == playerId && s.IsAI);
            if (slot.IsEmpty())
            {
                Debug.LogWarning($"[MatchRoomViewModel] NPC not found in PlayerSlots: {npcId}");
                return;
            }

            // LobbyServiceを使用してネットワーク経由でCPUプレイヤーを削除
            bool success = _lobbyService.RemoveCPUPlayer(slot.SlotIndex);

            if (success)
            {
                Debug.Log($"[MatchRoomViewModel] Successfully removed NPC from slot {slot.SlotIndex}");
                // NPCはPlayerLeftイベント経由でPlayersコレクションから自動的に削除されます
            }
            else
            {
                Debug.LogError($"[MatchRoomViewModel] Failed to remove NPC from slot {slot.SlotIndex}");
                ErrorOccurred?.Invoke(this, "Failed to remove NPC");
            }
        }

        /// <summary>
        /// NPC難易度を変更します（ホストのみ）
        /// </summary>
        /// <param name="npcId">NPC ID</param>
        /// <param name="difficulty">難易度（文字列）</param>
        public void ChangeNPCDifficulty(string npcId, string difficulty)
        {
            if (!IsHost)
            {
                Debug.LogWarning("[MatchRoomViewModel] Only host can change NPC difficulty.");
                return;
            }

            // 難易度文字列をAIDifficulty enumに変換
            if (!System.Enum.TryParse<AIDifficulty>(difficulty, ignoreCase: true, out AIDifficulty aiDifficulty))
            {
                Debug.LogWarning($"[MatchRoomViewModel] Invalid difficulty value: {difficulty}");
                ErrorOccurred?.Invoke(this, $"Invalid difficulty: {difficulty}");
                return;
            }

            // PlayerId から該当NPCのスロットインデックスを検索
            if (!ulong.TryParse(npcId, out ulong playerId))
            {
                Debug.LogWarning($"[MatchRoomViewModel] Invalid NPC ID format: {npcId}");
                return;
            }

            // PlayerSlotsから該当するNPCのスロットを探す
            var slot = _lobbyService.PlayerSlots.FirstOrDefault(s => s.PlayerId == playerId && s.IsAI);
            if (slot.IsEmpty())
            {
                Debug.LogWarning($"[MatchRoomViewModel] NPC not found in PlayerSlots: {npcId}");
                return;
            }

            // LobbyServiceを使用してネットワーク経由でNPC難易度を変更
            bool success = _lobbyService.ChangeCPUDifficulty(slot.SlotIndex, aiDifficulty);

            if (success)
            {
                Debug.Log($"[MatchRoomViewModel] Successfully changed NPC difficulty at slot {slot.SlotIndex} to {aiDifficulty}");
                // 難易度変更はネットワーク同期により、各クライアントのPlayerSlotsが更新されます
                // Playersコレクションは次のOnPropertyChangedで更新されます
                OnPropertyChanged(nameof(Players));
            }
            else
            {
                Debug.LogError($"[MatchRoomViewModel] Failed to change NPC difficulty at slot {slot.SlotIndex}");
                ErrorOccurred?.Invoke(this, "Failed to change NPC difficulty");
            }
        }

        /// <summary>
        /// ゲームを開始します（ホストのみ）
        /// </summary>
        public void StartGame()
        {
            if (!CanStartGame)
            {
                Debug.LogWarning("[MatchRoomViewModel] Cannot start game. Not all guests are ready.");
                StatusMessage = "All players must be ready!";
                return;
            }

            // カウントダウン開始
            IsCountingDown = true;
            CountdownSeconds = 5;
            _countdownAccumulator = 0f; // タイマーリセット
            StatusMessage = "Game starting...";

            // LobbyServiceを使用してネットワーク経由でカウントダウン開始を同期
            bool success = _lobbyService.StartGameCountdown(CountdownSeconds);

            if (success)
            {
                GameStarting?.Invoke(this, EventArgs.Empty);
                Debug.Log("[MatchRoomViewModel] Starting countdown...");
            }
            else
            {
                // カウントダウン開始に失敗した場合はリセット
                IsCountingDown = false;
                CountdownSeconds = 5;
                _countdownAccumulator = 0f;
                Debug.LogError("[MatchRoomViewModel] Failed to start countdown");
                ErrorOccurred?.Invoke(this, "Failed to start countdown");
            }
        }

        /// <summary>
        /// カウントダウンをキャンセルします
        /// </summary>
        public void CancelCountdown()
        {
            // LobbyServiceを使用してネットワーク経由でキャンセルを同期
            bool success = _lobbyService.CancelGameCountdown();

            if (success)
            {
                IsCountingDown = false;
                CountdownSeconds = 5;
                _countdownAccumulator = 0f; // タイマーリセット

                Debug.Log("[MatchRoomViewModel] Countdown cancelled.");
                UpdateStatusMessage();
            }
            else
            {
                Debug.LogError("[MatchRoomViewModel] Failed to cancel countdown");
                ErrorOccurred?.Invoke(this, "Failed to cancel countdown");
            }
        }

        /// <summary>
        /// ViewModelの更新処理（カウントダウンタイマーを進める）
        /// </summary>
        /// <param name="deltaTime">前フレームからの経過時間（秒）</param>
        /// <remarks>
        /// MatchRoomViewのUpdate()から毎フレーム呼び出されます。
        /// カウントダウン中のみタイマーを進め、1秒ごとにカウントダウンを減算します。
        /// </remarks>
        public void Tick(float deltaTime)
        {
            if (!IsCountingDown)
            {
                return;
            }

            _countdownAccumulator += deltaTime;

            // 1秒経過したら
            if (_countdownAccumulator >= 1f)
            {
                _countdownAccumulator -= 1f; // 余りを保持
                UpdateCountdown();
            }
        }

        /// <summary>
        /// カウントダウンを1秒進めます（内部用）
        /// </summary>
        private void UpdateCountdown()
        {
            if (!IsCountingDown)
            {
                return;
            }

            CountdownSeconds--;
            CountdownUpdated?.Invoke(this, CountdownSeconds);

            if (CountdownSeconds <= 0)
            {
                // カウントダウン終了、ゲーム準備
                PrepareGame();
            }
        }

        /// <summary>
        /// ルームから退出します
        /// </summary>
        public void LeaveRoom()
        {
            if (IsReady)
            {
                Debug.LogWarning("[MatchRoomViewModel] Cannot leave while ready. Cancel ready first.");
                StatusMessage = "Cancel ready before leaving!";
                return;
            }

            // ロビーサービスに退出を通知
            _lobbyService.LeaveRoom();
            Debug.Log("[MatchRoomViewModel] Left room.");

            _sceneManagementService.LoadLobby();
        }

        /// <summary>
        /// ルーム設定を更新します（ホストのみ）
        /// </summary>
        public void UpdateRoomSettings()
        {
            if (!IsHost)
            {
                Debug.LogWarning("[MatchRoomViewModel] Only host can update room settings.");
                return;
            }

            // 現在の設定を取得して更新
            var settings = _lobbyService.CurrentRoomSettings;

            // すべての変更可能な設定を更新
            settings.RoomName = new Unity.Collections.FixedString64Bytes(RoomName);

            // GameMode文字列をenumに変換
            if (System.Enum.TryParse<GameMode>(GameMode, out var parsedGameMode))
            {
                settings.GameMode = parsedGameMode;
            }

            settings.MapName = new Unity.Collections.FixedString64Bytes(MapName);
            settings.TimeLimit = TimeLimit;
            settings.ArrowLimit = ArrowLimit;

            // ネットワーク経由で設定を送信
            if (_lobbyService.UpdateRoomSettings(settings))
            {
                Debug.Log($"[MatchRoomViewModel] Room settings updated: RoomName={RoomName}, GameMode={GameMode}, MapName={MapName}, TimeLimit={TimeLimit}, ArrowLimit={ArrowLimit}");
            }
            else
            {
                Debug.LogError("[MatchRoomViewModel] Failed to update room settings.");
                ErrorOccurred?.Invoke(this, "Failed to update room settings");
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// ゲームモードに応じたルールを適用します
        /// </summary>
        private void ApplyGameModeRules()
        {
            // GameMode文字列をenumに変換
            if (!System.Enum.TryParse<GameMode>(GameMode, out var parsedMode))
            {
                return;
            }

            // タイムリミットのルール適用
            if (GameModeRules.IsTimeLimitLockedToNoLimit(parsedMode))
            {
                // Deathmatch: 常に"No Limit"（0）に固定
                TimeLimit = 0;
            }
            else if (GameModeRules.IsTimeLimitRequired(parsedMode))
            {
                // Arena/ScoreMatch/TeamFight: 必須（0にできない）
                if (TimeLimit == 0)
                {
                    TimeLimit = GameModeRules.GetDefaultTimeLimit(parsedMode);
                }
            }
            else
            {
                // Versus: 自由設定（変更なし）
                // 現在の値をそのまま保持
            }

            // 矢の制限のルール適用
            if (GameModeRules.IsArrowLimitLockedToNoLimit(parsedMode))
            {
                // Arena/TeamFight/Deathmatch: 常に"No Limit"（0）に固定
                ArrowLimit = 0;
            }
            else if (GameModeRules.IsArrowLimitRequired(parsedMode))
            {
                // ScoreMatch: 必須（0にできない）
                if (ArrowLimit == 0)
                {
                    ArrowLimit = GameModeRules.GetDefaultArrowLimit(parsedMode);
                }
            }
            else
            {
                // Versus: 自由設定（変更なし）
                // 現在の値をそのまま保持
            }

            Debug.Log($"[MatchRoomViewModel] Game mode rules applied for {GameMode}: TimeLimit={TimeLimit}, ArrowLimit={ArrowLimit}");
        }

        /// <summary>
        /// ゲームの準備をします
        /// </summary>
        private void PrepareGame()
        {
            IsCountingDown = false;
            StatusMessage = "Preparing game...";

            // ネットワーク経由でマッチ開始を通知
            if (_lobbyService.StartMatch())
            {
                Debug.Log("[MatchRoomViewModel] Match started. Transitioning to loading scene...");
                // ローディングシーンに遷移
                LoadingSceneRequested?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                Debug.LogError("[MatchRoomViewModel] Failed to start match.");
                ErrorOccurred?.Invoke(this, "Failed to start match");
                IsCountingDown = false;
                UpdateStatusMessage();
            }
        }

        /// <summary>
        /// ステータスメッセージを更新します
        /// </summary>
        private void UpdateStatusMessage()
        {
            if (IsHost)
            {
                if (AllGuestsReady)
                {
                    StatusMessage = "All players ready! You can start the game.";
                }
                else
                {
                    StatusMessage = "Waiting for players to ready up...";
                }
            }
            else
            {
                if (IsReady)
                {
                    StatusMessage = "Waiting for host to start the game...";
                }
                else
                {
                    StatusMessage = "Press Ready when you're prepared.";
                }
            }
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// リソースを解放します
        /// </summary>
        protected override void OnDispose()
        {
            // ロビーイベントの購読を解除
            UnsubscribeFromLobbyEvents();

            // パフォーマンス監視イベントの購読を解除
            if (_performanceMonitor != null)
            {
                _performanceMonitor.FPSUpdated -= OnFPSUpdated;
            }

            // クリーンアップ
            Players.Clear();

            base.OnDispose();
        }

        #endregion
    }

    /// <summary>
    /// プレイヤー情報
    /// </summary>
    public class PlayerInfo : INotifyPropertyChanged
    {
        private string _playerId = "";
        private string _playerName = "";
        private bool _isHost = false;
        private bool _isReady = false;
        private PlayerTeam _team = PlayerTeam.None;
        private int _fps = 0;
        private bool _isNPC = false;
        private string _difficulty = "Normal";

        /// <summary>
        /// プレイヤーID
        /// </summary>
        public string PlayerId
        {
            get => _playerId;
            set
            {
                _playerId = value;
                OnPropertyChanged(nameof(PlayerId));
            }
        }

        /// <summary>
        /// プレイヤー名
        /// </summary>
        public string PlayerName
        {
            get => _playerName;
            set
            {
                _playerName = value;
                OnPropertyChanged(nameof(PlayerName));
            }
        }

        /// <summary>
        /// ホストかどうか
        /// </summary>
        public bool IsHost
        {
            get => _isHost;
            set
            {
                _isHost = value;
                OnPropertyChanged(nameof(IsHost));
            }
        }

        /// <summary>
        /// 準備完了かどうか
        /// </summary>
        public bool IsReady
        {
            get => _isReady;
            set
            {
                _isReady = value;
                OnPropertyChanged(nameof(IsReady));
            }
        }

        /// <summary>
        /// チーム
        /// </summary>
        public PlayerTeam Team
        {
            get => _team;
            set
            {
                _team = value;
                OnPropertyChanged(nameof(Team));
            }
        }

        /// <summary>
        /// FPS
        /// </summary>
        public int Fps
        {
            get => _fps;
            set
            {
                _fps = value;
                OnPropertyChanged(nameof(Fps));
            }
        }

        /// <summary>
        /// NPCかどうか
        /// </summary>
        public bool IsNPC
        {
            get => _isNPC;
            set
            {
                _isNPC = value;
                OnPropertyChanged(nameof(IsNPC));
            }
        }

        /// <summary>
        /// NPC難易度（Easy, Normal, Hard, Expert）
        /// </summary>
        public string Difficulty
        {
            get => _difficulty;
            set
            {
                _difficulty = value;
                OnPropertyChanged(nameof(Difficulty));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// プレイヤーチーム
    /// </summary>
    public enum PlayerTeam
    {
        None,
        TeamA,
        TeamB
    }
}
