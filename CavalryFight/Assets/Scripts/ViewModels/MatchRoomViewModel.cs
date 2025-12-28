#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using CavalryFight.Core.MVVM;
using CavalryFight.Services.Lobby;
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
        /// スコアゴール
        /// </summary>
        private int _scoreGoal = 100;

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
            set => SetProperty(ref _gameMode, value);
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
        /// スコアゴール
        /// </summary>
        public int ScoreGoal
        {
            get => _scoreGoal;
            set => SetProperty(ref _scoreGoal, value);
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
        public MatchRoomViewModel(ILobbyService lobbyService, ISceneManagementService sceneManagementService)
        {
            _lobbyService = lobbyService ?? throw new ArgumentNullException(nameof(lobbyService));
            _sceneManagementService = sceneManagementService ?? throw new ArgumentNullException(nameof(sceneManagementService));

            // 初期化
            InitializeRoomData();

            Debug.Log("[MatchRoomViewModel] Initialized.");
        }

        #endregion

        #region Initialization

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
                ScoreGoal = settings.ScoreGoal;
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

                Debug.Log($"[MatchRoomViewModel] Room data initialized from LobbyService: {RoomName}, Host: {IsHost}");
            }
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
        /// プレイヤーのチームを変更します
        /// </summary>
        /// <param name="playerId">プレイヤーID</param>
        /// <param name="newTeam">新しいチーム</param>
        public void ChangePlayerTeam(string playerId, PlayerTeam newTeam)
        {
            var player = Players.FirstOrDefault(p => p.PlayerId == playerId);
            if (player != null)
            {
                player.Team = newTeam;

                // TODO: ネットワーク経由でチーム変更を送信
                Debug.Log($"[MatchRoomViewModel] Player {player.PlayerName} team changed to {newTeam}");

                OnPropertyChanged(nameof(Players));
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
            StatusMessage = "Game starting...";

            GameStarting?.Invoke(this, EventArgs.Empty);

            Debug.Log("[MatchRoomViewModel] Starting countdown...");

            // TODO: ネットワーク経由でカウントダウン開始を同期
            // 注: カウントダウンはホストが管理し、ゲスト側でも同期表示する必要がある
        }

        /// <summary>
        /// カウントダウンをキャンセルします
        /// </summary>
        public void CancelCountdown()
        {
            IsCountingDown = false;
            CountdownSeconds = 5;

            // TODO: ネットワーク経由でキャンセルを同期
            // 注: カウントダウンキャンセルもホストが管理し、ゲスト側で同期する必要がある
            Debug.Log("[MatchRoomViewModel] Countdown cancelled.");

            UpdateStatusMessage();
        }

        /// <summary>
        /// カウントダウンを更新します
        /// </summary>
        public void UpdateCountdown()
        {
            if (!IsCountingDown) return;

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
            settings.TimeLimit = TimeLimit;
            settings.ScoreGoal = ScoreGoal;

            // ネットワーク経由で設定を送信
            if (_lobbyService.UpdateRoomSettings(settings))
            {
                Debug.Log($"[MatchRoomViewModel] Room settings updated: TimeLimit={TimeLimit}, ScoreGoal={ScoreGoal}");
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
