#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using CavalryFight.Core.Services;
using CavalryFight.Services.Lobby;
using CavalryFight.Services.Match;
using Unity.Netcode;
using UnityEngine;

namespace CavalryFight.Gameplay.Match
{
    /// <summary>
    /// マッチのメインオーケストレーター
    /// </summary>
    /// <remarks>
    /// ゲームモードに応じた適切なルールハンドラーを選択し、
    /// マッチの進行を管理します。
    /// </remarks>
    public class MatchManager : NetworkBehaviour
    {
        #region Singleton

        private static MatchManager? _instance;

        /// <summary>
        /// シングルトンインスタンス
        /// </summary>
        public static MatchManager? Instance => _instance;

        #endregion

        #region Serialized Fields

        [Header("Mode Handlers")]
        [SerializeField] private ArenaRulesHandler? _arenaHandler;
        [SerializeField] private ScoreMatchRulesHandler? _scoreMatchHandler;
        [SerializeField] private TeamFightRulesHandler? _teamFightHandler;
        [SerializeField] private DeathmatchRulesHandler? _deathmatchHandler;
        [SerializeField] private HuntingRulesHandler? _huntingHandler;

        [Header("Match Settings")]
        [SerializeField] private float _countdownDuration = 3f;

        #endregion

        #region Network Variables

        private NetworkVariable<RoomSettings> _roomSettings = new NetworkVariable<RoomSettings>(
            default,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        private NetworkVariable<MatchState> _matchState = new NetworkVariable<MatchState>(
            MatchState.WaitingForPlayers,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        private NetworkVariable<float> _matchTime = new NetworkVariable<float>(
            0f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        private NetworkList<PlayerSlot>? _playerSlots;
        private NetworkList<Services.Match.PlayerScore>? _playerScores;

        #endregion

        #region Private Fields

        private IGameModeRulesHandler? _activeHandler;
        private float _countdownTimer;

        #endregion

        #region Properties

        /// <summary>
        /// 現在のルーム設定
        /// </summary>
        public RoomSettings RoomSettings => _roomSettings.Value;

        /// <summary>
        /// 現在のマッチ状態
        /// </summary>
        public MatchState CurrentState => _matchState.Value;

        /// <summary>
        /// マッチ経過時間
        /// </summary>
        public float MatchTime => _matchTime.Value;

        /// <summary>
        /// 残り時間（時間制限がある場合）
        /// </summary>
        public float RemainingTime => RoomSettings.TimeLimit > 0
            ? Mathf.Max(0, RoomSettings.TimeLimit - _matchTime.Value)
            : 0f;

        /// <summary>
        /// 現在のゲームモード
        /// </summary>
        public GameMode CurrentGameMode => RoomSettings.GameMode;

        /// <summary>
        /// アクティブなルールハンドラー
        /// </summary>
        public IGameModeRulesHandler? ActiveHandler => _activeHandler;

        /// <summary>
        /// プレイヤースロット一覧
        /// </summary>
        public IReadOnlyList<PlayerSlot> PlayerSlots
        {
            get
            {
                if (_playerSlots == null)
                {
                    return Array.Empty<PlayerSlot>();
                }
                var list = new List<PlayerSlot>();
                foreach (var slot in _playerSlots)
                {
                    list.Add(slot);
                }
                return list;
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// マッチ状態が変更された時に発生します
        /// </summary>
        public event Action<MatchState>? MatchStateChanged;

        /// <summary>
        /// カウントダウンが更新された時に発生します
        /// </summary>
        public event Action<int>? CountdownUpdated;

        /// <summary>
        /// マッチが開始された時に発生します
        /// </summary>
        public event Action? MatchStarted;

        /// <summary>
        /// マッチが終了した時に発生します
        /// </summary>
        public event Action<MatchEndResult>? MatchEnded;

        /// <summary>
        /// プレイヤーがスコアを獲得した時に発生します
        /// </summary>
        public event Action<ulong, int, HitLocation>? PlayerScored;

        /// <summary>
        /// プレイヤーが死亡した時に発生します
        /// </summary>
        public event Action<ulong, ulong>? PlayerDied;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning("[MatchManager] Instance already exists. Destroying duplicate.");
                Destroy(gameObject);
                return;
            }
            _instance = this;

            _playerSlots = new NetworkList<PlayerSlot>();
            _playerScores = new NetworkList<Services.Match.PlayerScore>();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            _matchState.OnValueChanged += OnMatchStateValueChanged;
            _roomSettings.OnValueChanged += OnRoomSettingsValueChanged;

            Debug.Log($"[MatchManager] Network spawned. IsServer: {IsServer}, IsClient: {IsClient}");

            // サーバーの場合、LobbyServiceからデータを取得してマッチを初期化
            if (IsServer)
            {
                InitializeFromLobbyService();
            }
        }

        public override void OnNetworkDespawn()
        {
            _matchState.OnValueChanged -= OnMatchStateValueChanged;
            _roomSettings.OnValueChanged -= OnRoomSettingsValueChanged;

            _activeHandler?.Cleanup();
            _activeHandler = null;

            base.OnNetworkDespawn();
        }

        public override void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
            base.OnDestroy();
        }

        private void Update()
        {
            if (!IsServer)
            {
                return;
            }

            switch (_matchState.Value)
            {
                case MatchState.Countdown:
                    UpdateCountdown();
                    break;

                case MatchState.InProgress:
                    UpdateMatch();
                    break;
            }
        }

        #endregion

        #region Server Methods

        /// <summary>
        /// マッチを初期化します（サーバーのみ）
        /// </summary>
        /// <param name="settings">ルーム設定</param>
        /// <param name="slots">プレイヤースロット</param>
        [Rpc(SendTo.Server)]
        public void InitializeMatchRpc(RoomSettings settings, PlayerSlot[] slots)
        {
            if (!IsServer)
            {
                return;
            }

            _roomSettings.Value = settings;

            // プレイヤースロットを設定
            _playerSlots?.Clear();
            foreach (var slot in slots)
            {
                _playerSlots?.Add(slot);
            }

            // プレイヤースコアを初期化
            _playerScores?.Clear();
            foreach (var slot in slots)
            {
                _playerScores?.Add(new Services.Match.PlayerScore
                {
                    ClientId = slot.PlayerId,
                    PlayerName = slot.PlayerName,
                    Score = 0,
                    RemainingArrows = settings.ArrowLimit,
                    HitCount = 0,
                    ShotCount = 0,
                    TeamIndex = slot.TeamIndex
                });
            }

            // ゲームモードに応じたハンドラーを選択
            SelectHandler(settings.GameMode);

            // ハンドラーを初期化
            _activeHandler?.Initialize(this, settings);

            // 状態を更新
            _matchState.Value = MatchState.WaitingForPlayers;

            Debug.Log($"[MatchManager] Match initialized. Mode: {settings.GameMode}, Players: {slots.Length}");
        }

        /// <summary>
        /// カウントダウンを開始します（サーバーのみ）
        /// </summary>
        [Rpc(SendTo.Server)]
        public void StartCountdownRpc()
        {
            if (!IsServer)
            {
                return;
            }
            if (_matchState.Value != MatchState.WaitingForPlayers)
            {
                return;
            }

            _countdownTimer = _countdownDuration;
            _matchState.Value = MatchState.Countdown;

            Debug.Log("[MatchManager] Countdown started");
        }

        /// <summary>
        /// マッチを強制終了します（サーバーのみ）
        /// </summary>
        /// <param name="winnerId">勝者のクライアントID</param>
        [Rpc(SendTo.Server)]
        public void ForceEndMatchRpc(ulong winnerId)
        {
            if (!IsServer)
            {
                return;
            }

            EndMatch(winnerId);
        }

        /// <summary>
        /// プレイヤーのスコアを追加します（サーバーのみ）
        /// </summary>
        [Rpc(SendTo.Server)]
        public void AddPlayerScoreRpc(ulong clientId, int score, HitLocation hitLocation)
        {
            if (!IsServer || _playerScores == null)
            {
                return;
            }

            for (int i = 0; i < _playerScores.Count; i++)
            {
                if (_playerScores[i].ClientId == clientId)
                {
                    var playerScore = _playerScores[i];
                    playerScore.Score += score;
                    playerScore.HitCount++;
                    _playerScores[i] = playerScore;

                    // ハンドラーに通知
                    _activeHandler?.OnPlayerScored(clientId, score, hitLocation);

                    // イベント発火
                    NotifyPlayerScoredClientRpc(clientId, score, hitLocation);

                    Debug.Log($"[MatchManager] Player {clientId} scored {score} (total: {playerScore.Score})");
                    break;
                }
            }
        }

        /// <summary>
        /// プレイヤーの矢発射を記録します（サーバーのみ）
        /// </summary>
        [Rpc(SendTo.Server)]
        public void RecordArrowFiredRpc(ulong clientId)
        {
            if (!IsServer || _playerScores == null)
            {
                return;
            }

            for (int i = 0; i < _playerScores.Count; i++)
            {
                if (_playerScores[i].ClientId == clientId)
                {
                    var playerScore = _playerScores[i];
                    playerScore.ShotCount++;
                    if (playerScore.RemainingArrows > 0)
                    {
                        playerScore.RemainingArrows--;
                    }
                    _playerScores[i] = playerScore;

                    // ハンドラーに通知
                    _activeHandler?.OnArrowFired(clientId);
                    break;
                }
            }
        }

        /// <summary>
        /// プレイヤーの死亡を記録します（サーバーのみ）
        /// </summary>
        [Rpc(SendTo.Server)]
        public void RecordPlayerDeathRpc(ulong clientId, ulong killerId)
        {
            if (!IsServer)
            {
                return;
            }

            _activeHandler?.OnPlayerDeath(clientId, killerId);
            NotifyPlayerDiedClientRpc(clientId, killerId);
        }

        #endregion

        #region Client RPCs

        [ClientRpc]
        private void NotifyCountdownClientRpc(int seconds)
        {
            CountdownUpdated?.Invoke(seconds);
        }

        [ClientRpc]
        private void NotifyMatchStartedClientRpc()
        {
            MatchStarted?.Invoke();
        }

        [ClientRpc]
        private void NotifyMatchEndedClientRpc(MatchEndResult result)
        {
            MatchEnded?.Invoke(result);
        }

        [ClientRpc]
        private void NotifyPlayerScoredClientRpc(ulong clientId, int score, HitLocation hitLocation)
        {
            PlayerScored?.Invoke(clientId, score, hitLocation);
        }

        [ClientRpc]
        private void NotifyPlayerDiedClientRpc(ulong clientId, ulong killerId)
        {
            PlayerDied?.Invoke(clientId, killerId);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// プレイヤーのスコア情報を取得します
        /// </summary>
        public Services.Match.PlayerScore? GetPlayerScore(ulong clientId)
        {
            if (_playerScores == null)
            {
                return null;
            }

            foreach (var score in _playerScores)
            {
                if (score.ClientId == clientId)
                {
                    return score;
                }
            }
            return null;
        }

        /// <summary>
        /// すべてのプレイヤーのスコア情報を取得します
        /// </summary>
        public Services.Match.PlayerScore[] GetAllPlayerScores()
        {
            if (_playerScores == null)
            {
                return Array.Empty<Services.Match.PlayerScore>();
            }

            var list = new List<Services.Match.PlayerScore>();
            foreach (var score in _playerScores)
            {
                list.Add(score);
            }
            return list.ToArray();
        }

        /// <summary>
        /// チームスコアを取得します
        /// </summary>
        public int GetTeamScore(int teamIndex)
        {
            return _activeHandler?.GetTeamScore(teamIndex) ?? 0;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// LobbyServiceからマッチ情報を取得して初期化します
        /// </summary>
        private void InitializeFromLobbyService()
        {
            var lobbyService = ServiceLocator.Instance.Get<ILobbyService>();
            if (lobbyService == null)
            {
                Debug.LogError("[MatchManager] ILobbyService not found. Cannot initialize match.");
                return;
            }

            var roomSettings = lobbyService.CurrentRoomSettings;
            var playerSlots = lobbyService.PlayerSlots.ToArray();

            Debug.Log($"[MatchManager] Initializing from LobbyService. Map: {roomSettings.MapName}, GameMode: {roomSettings.GameMode}, Players: {playerSlots.Length}");

            // マップをロード
            LoadMap(roomSettings.MapName);

            // マッチを初期化
            InitializeMatchInternal(roomSettings, playerSlots);
        }

        /// <summary>
        /// マップをロードします
        /// </summary>
        /// <param name="mapName">マップ名</param>
        private void LoadMap(MapName mapName)
        {
            if (FieldLoader.Instance != null)
            {
                bool success = FieldLoader.Instance.LoadField(mapName);
                Debug.Log($"[MatchManager] LoadMap: {mapName} - {(success ? "Success" : "Failed")}");
            }
            else
            {
                Debug.LogWarning("[MatchManager] FieldLoader.Instance not found. Map will not be loaded.");
            }
        }

        /// <summary>
        /// マッチを内部的に初期化します
        /// </summary>
        /// <param name="settings">ルーム設定</param>
        /// <param name="slots">プレイヤースロット</param>
        private void InitializeMatchInternal(RoomSettings settings, PlayerSlot[] slots)
        {
            _roomSettings.Value = settings;

            // プレイヤースロットを設定
            _playerSlots?.Clear();
            foreach (var slot in slots)
            {
                _playerSlots?.Add(slot);
            }

            // プレイヤースコアを初期化
            _playerScores?.Clear();
            foreach (var slot in slots)
            {
                _playerScores?.Add(new Services.Match.PlayerScore
                {
                    ClientId = slot.PlayerId,
                    PlayerName = slot.PlayerName,
                    Score = 0,
                    RemainingArrows = settings.ArrowLimit,
                    HitCount = 0,
                    ShotCount = 0,
                    TeamIndex = slot.TeamIndex
                });
            }

            // RulesHandlersを自動検索
            AutoFindRulesHandlers();

            // ゲームモードに応じたハンドラーを選択
            SelectHandler(settings.GameMode);

            // ハンドラーを初期化
            _activeHandler?.Initialize(this, settings);

            // 状態を更新
            _matchState.Value = MatchState.WaitingForPlayers;

            Debug.Log($"[MatchManager] Match initialized. Mode: {settings.GameMode}, Players: {slots.Length}");

            // カウントダウンを開始
            StartCountdownRpc();
        }

        /// <summary>
        /// RulesHandlerを自動的に検索して設定します
        /// </summary>
        private void AutoFindRulesHandlers()
        {
            if (_arenaHandler == null)
            {
                _arenaHandler = GetComponentInChildren<ArenaRulesHandler>();
            }
            if (_scoreMatchHandler == null)
            {
                _scoreMatchHandler = GetComponentInChildren<ScoreMatchRulesHandler>();
            }
            if (_teamFightHandler == null)
            {
                _teamFightHandler = GetComponentInChildren<TeamFightRulesHandler>();
            }
            if (_deathmatchHandler == null)
            {
                _deathmatchHandler = GetComponentInChildren<DeathmatchRulesHandler>();
            }
            if (_huntingHandler == null)
            {
                _huntingHandler = GetComponentInChildren<HuntingRulesHandler>();
            }

            Debug.Log($"[MatchManager] AutoFindRulesHandlers - Arena: {_arenaHandler != null}, ScoreMatch: {_scoreMatchHandler != null}, TeamFight: {_teamFightHandler != null}, Deathmatch: {_deathmatchHandler != null}, Hunting: {_huntingHandler != null}");
        }

        private void SelectHandler(GameMode mode)
        {
            _activeHandler = mode switch
            {
                GameMode.Arena => _arenaHandler,
                GameMode.ScoreMatch => _scoreMatchHandler,
                GameMode.TeamFight => _teamFightHandler,
                GameMode.Deathmatch => _deathmatchHandler,
                GameMode.Hunting => _huntingHandler,
                _ => throw new ArgumentException($"Unsupported game mode: {mode}")
            };

            if (_activeHandler == null)
            {
                Debug.LogError($"[MatchManager] Handler for mode {mode} is not assigned!");
            }
            else
            {
                // ハンドラーのイベントを購読
                _activeHandler.MatchEndTriggered += OnHandlerMatchEndTriggered;
            }
        }

        private void UpdateCountdown()
        {
            _countdownTimer -= Time.deltaTime;

            int seconds = Mathf.CeilToInt(_countdownTimer);
            NotifyCountdownClientRpc(seconds);

            if (_countdownTimer <= 0)
            {
                StartMatch();
            }
        }

        private void UpdateMatch()
        {
            _matchTime.Value += Time.deltaTime;
            _activeHandler?.OnUpdate(Time.deltaTime);
        }

        private void StartMatch()
        {
            _matchState.Value = MatchState.InProgress;
            _matchTime.Value = 0f;

            // AIプレイヤーをスポーン
            SpawnAIPlayers();

            _activeHandler?.OnMatchStart();
            NotifyMatchStartedClientRpc();

            // AIを有効化
            AISpawner.Instance?.EnableAllAI();

            Debug.Log("[MatchManager] Match started!");
        }

        /// <summary>
        /// PlayerSlotsに基づいてAIプレイヤーをスポーンします
        /// </summary>
        private void SpawnAIPlayers()
        {
            if (AISpawner.Instance == null)
            {
                Debug.LogWarning("[MatchManager] AISpawner not found in scene. AI players will not spawn.");
                return;
            }

            if (_playerSlots == null)
            {
                return;
            }

            // AIをスポーン（各スロットの難易度を使用）
            foreach (var slot in _playerSlots)
            {
                if (slot.IsAI)
                {
                    // そのAIの難易度でAISpawnerを初期化
                    AISpawner.Instance.Initialize(RoomSettings.GameMode, slot.AIDifficulty);

                    // AIをスポーン（チームを指定）
                    var spawnedIds = AISpawner.Instance.SpawnAIPlayers(1, slot.TeamIndex);

                    if (spawnedIds.Count > 0)
                    {
                        Debug.Log($"[MatchManager] Spawned AI for slot. PlayerName: {slot.PlayerName}, Team: {slot.TeamIndex}, Difficulty: {slot.AIDifficulty}");
                    }
                }
            }

            Debug.Log($"[MatchManager] Total AI spawned: {AISpawner.Instance.SpawnedAICount}");
        }

        private void EndMatch(ulong winnerId)
        {
            _matchState.Value = MatchState.Ended;
            _activeHandler?.OnMatchEnd();

            // AIを無効化・削除
            AISpawner.Instance?.DisableAllAI();
            AISpawner.Instance?.DespawnAllAI();

            var result = new MatchEndResult
            {
                WinnerId = winnerId,
                MatchDuration = _matchTime.Value,
                GameMode = RoomSettings.GameMode,
                IsTeamMode = _activeHandler?.IsTeamMode ?? false
            };

            NotifyMatchEndedClientRpc(result);

            Debug.Log($"[MatchManager] Match ended! Winner: {winnerId}, Duration: {_matchTime.Value:F1}s");
        }

        private void OnMatchStateValueChanged(MatchState previousValue, MatchState newValue)
        {
            MatchStateChanged?.Invoke(newValue);
            Debug.Log($"[MatchManager] State changed: {previousValue} -> {newValue}");
        }

        private void OnRoomSettingsValueChanged(RoomSettings previousValue, RoomSettings newValue)
        {
            Debug.Log($"[MatchManager] Room settings updated. Mode: {newValue.GameMode}");
        }

        private void OnHandlerMatchEndTriggered(ulong winnerId)
        {
            if (IsServer)
            {
                EndMatch(winnerId);
            }
        }

        #endregion
    }

    /// <summary>
    /// マッチ状態
    /// </summary>
    public enum MatchState
    {
        /// <summary>プレイヤー待機中</summary>
        WaitingForPlayers = 0,

        /// <summary>カウントダウン中</summary>
        Countdown = 1,

        /// <summary>マッチ進行中</summary>
        InProgress = 2,

        /// <summary>一時停止中</summary>
        Paused = 3,

        /// <summary>マッチ終了</summary>
        Ended = 4
    }

    /// <summary>
    /// マッチ終了結果
    /// </summary>
    [Serializable]
    public struct MatchEndResult : INetworkSerializable
    {
        /// <summary>勝者のクライアントID（チームモードの場合はチームインデックス）</summary>
        public ulong WinnerId;

        /// <summary>マッチ時間（秒）</summary>
        public float MatchDuration;

        /// <summary>ゲームモード</summary>
        public GameMode GameMode;

        /// <summary>チームモードかどうか</summary>
        public bool IsTeamMode;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref WinnerId);
            serializer.SerializeValue(ref MatchDuration);
            serializer.SerializeValue(ref GameMode);
            serializer.SerializeValue(ref IsTeamMode);
        }
    }
}
