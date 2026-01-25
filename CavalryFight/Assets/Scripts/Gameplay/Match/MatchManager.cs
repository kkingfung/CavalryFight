#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using CavalryFight.Core.Services;
using CavalryFight.Services.Lobby;
using CavalryFight.Services.Match;
using CavalryFight.Services.SceneManagement;
using ServicesMatch = CavalryFight.Services.Match;
using Unity.Netcode;
using UnityEngine;
using CavalryFight.Gameplay.Player;

namespace CavalryFight.Gameplay.Match
{
    /// <summary>
    /// マッチのメインオーケストレーター
    /// </summary>
    /// <remarks>
    /// ゲームモードに応じた適切なルールハンドラーを選択し、
    /// マッチの進行を管理します。
    /// IMatchDataProviderを実装してMatchServiceにデータを提供します。
    /// </remarks>
    public class MatchManager : NetworkBehaviour, IMatchDataProvider, ServicesMatch.IMatchReadinessProvider
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

        // エンティティ準備状態
        private bool _areAllEntitiesReady;
        private float _entityLoadProgress;
        private string _loadStatusMessage = "Initializing...";
        private bool _playerSpawned;
        private bool _aiSpawned;

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

        /// <summary>
        /// すべてのエンティティの準備が完了した時に発生します（IMatchReadinessProvider実装）
        /// </summary>
        public event Action? AllEntitiesReady;

        #endregion

        #region IMatchReadinessProvider Implementation

        /// <summary>
        /// エンティティの準備が完了しているかどうかを取得します
        /// </summary>
        bool ServicesMatch.IMatchReadinessProvider.AreAllEntitiesReady => _areAllEntitiesReady;

        /// <summary>
        /// 現在のロード進捗を取得します
        /// </summary>
        float ServicesMatch.IMatchReadinessProvider.EntityLoadProgress => _entityLoadProgress;

        /// <summary>
        /// 現在のロードステータスメッセージを取得します
        /// </summary>
        string ServicesMatch.IMatchReadinessProvider.LoadStatusMessage => _loadStatusMessage;

        /// <summary>
        /// ロード進捗を更新します
        /// </summary>
        private void UpdateLoadProgress(float progress, string message)
        {
            _entityLoadProgress = progress;
            _loadStatusMessage = message;
            Debug.Log($"[LOAD-PROGRESS] {progress:P0} - {message}");
        }

        /// <summary>
        /// すべてのエンティティの準備が完了したことを通知します
        /// </summary>
        private void NotifyAllEntitiesReady()
        {
            if (_areAllEntitiesReady)
            {
                return; // 既に通知済み
            }

            _areAllEntitiesReady = true;
            _entityLoadProgress = 1f;
            _loadStatusMessage = "Ready!";

            Debug.Log("[MatchManager] All entities ready! Notifying SceneManagementService...");

            // SceneManagementServiceにゲームプレイ準備完了を通知
            // これによりローディング画面が閉じる
            var sceneService = ServiceLocator.Instance.Get<ISceneManagementService>();
            if (sceneService != null && sceneService.IsWaitingForGameplayReady)
            {
                Debug.Log("[MatchManager] Calling SignalGameplayReady() to close loading screen");
                sceneService.SignalGameplayReady();
            }
            else
            {
                Debug.Log("[MatchManager] SceneManagementService not waiting for gameplay ready (possibly direct scene load)");
            }

            AllEntitiesReady?.Invoke();
        }

        #endregion

        #region IMatchDataProvider Implementation

        /// <summary>
        /// マッチが進行中かどうか（IMatchDataProvider実装）
        /// </summary>
        bool IMatchDataProvider.IsMatchInProgress => _matchState.Value == MatchState.InProgress;

        /// <summary>
        /// 現在のマッチ状態（IMatchDataProvider実装、Services.Match.MatchState型）
        /// </summary>
        ServicesMatch.MatchState IMatchDataProvider.CurrentMatchState => (ServicesMatch.MatchState)(int)_matchState.Value;

        /// <summary>
        /// 残り時間（IMatchDataProvider実装）
        /// </summary>
        float IMatchDataProvider.RemainingTime => RemainingTime;

        /// <summary>
        /// マッチ経過時間（IMatchDataProvider実装）
        /// </summary>
        float IMatchDataProvider.MatchTime => MatchTime;

        /// <summary>
        /// 現在のゲームモード（IMatchDataProvider実装）
        /// </summary>
        GameMode IMatchDataProvider.CurrentGameMode => CurrentGameMode;

        // IMatchDataProvider イベント（Services.Match型を使用）
        private event Action<ServicesMatch.MatchState>? _providerMatchStateChanged;
        private event Action? _providerMatchStarted;
        private event Action<ServicesMatch.MatchEndResult>? _providerMatchEnded;
        private event Action<ulong, int, ServicesMatch.HitLocation>? _providerPlayerScored;

        event Action<ServicesMatch.MatchState>? IMatchDataProvider.MatchStateChanged
        {
            add => _providerMatchStateChanged += value;
            remove => _providerMatchStateChanged -= value;
        }

        event Action? IMatchDataProvider.MatchStarted
        {
            add => _providerMatchStarted += value;
            remove => _providerMatchStarted -= value;
        }

        event Action<ServicesMatch.MatchEndResult>? IMatchDataProvider.MatchEnded
        {
            add => _providerMatchEnded += value;
            remove => _providerMatchEnded -= value;
        }

        event Action<ulong, int, ServicesMatch.HitLocation>? IMatchDataProvider.PlayerScored
        {
            add => _providerPlayerScored += value;
            remove => _providerPlayerScored -= value;
        }

        private event Action<int>? _providerCountdownUpdated;

        event Action<int>? IMatchDataProvider.CountdownUpdated
        {
            add => _providerCountdownUpdated += value;
            remove => _providerCountdownUpdated -= value;
        }

        /// <summary>
        /// プレイヤースコア取得（IMatchDataProvider実装）
        /// </summary>
        ServicesMatch.PlayerScore? IMatchDataProvider.GetPlayerScore(ulong clientId) => GetPlayerScore(clientId);

        /// <summary>
        /// 全プレイヤースコア取得（IMatchDataProvider実装）
        /// </summary>
        ServicesMatch.PlayerScore[] IMatchDataProvider.GetAllPlayerScores() => GetAllPlayerScores();

        /// <summary>
        /// チームスコア取得（IMatchDataProvider実装）
        /// </summary>
        int IMatchDataProvider.GetTeamScore(int teamIndex) => GetTeamScore(teamIndex);

        /// <summary>
        /// プロバイダーイベントを発火します（内部で呼び出し）
        /// </summary>
        private void RaiseProviderMatchStateChanged(MatchState state)
        {
            _providerMatchStateChanged?.Invoke((ServicesMatch.MatchState)(int)state);
        }

        private void RaiseProviderMatchStarted()
        {
            _providerMatchStarted?.Invoke();
        }

        private void RaiseProviderMatchEnded(MatchEndResult result)
        {
            var servicesResult = new ServicesMatch.MatchEndResult
            {
                WinnerId = result.WinnerId,
                MatchDuration = result.MatchDuration,
                GameMode = result.GameMode,
                IsTeamMode = result.IsTeamMode
            };
            _providerMatchEnded?.Invoke(servicesResult);
        }

        private void RaiseProviderPlayerScored(ulong clientId, int score, HitLocation hitLocation)
        {
            _providerPlayerScored?.Invoke(clientId, score, (ServicesMatch.HitLocation)(int)hitLocation);
        }

        private void RaiseProviderCountdownUpdated(int seconds)
        {
            _providerCountdownUpdated?.Invoke(seconds);
        }

        /// <summary>
        /// MatchServiceにデータプロバイダーとして登録します
        /// </summary>
        private void RegisterWithMatchService()
        {
            var matchService = ServiceLocator.Instance.Get<IMatchService>();
            if (matchService != null)
            {
                Debug.Log($"[SCORE-DEBUG] MatchManager.RegisterWithMatchService: MatchService instance: {matchService.GetHashCode()}");
                matchService.RegisterMatchDataProvider(this);
                Debug.Log("[MatchManager] Registered as IMatchDataProvider with MatchService.");
            }
            else
            {
                Debug.LogWarning("[MatchManager] MatchService not found. Cannot register as data provider.");
            }
        }

        /// <summary>
        /// MatchServiceからデータプロバイダー登録を解除します
        /// </summary>
        private void UnregisterFromMatchService()
        {
            var matchService = ServiceLocator.Instance.Get<IMatchService>();
            if (matchService != null)
            {
                matchService.UnregisterMatchDataProvider();
                Debug.Log("[MatchManager] Unregistered from MatchService.");
            }
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            Debug.Log($"[SPAWN-DEBUG] MatchManager.Awake() called on GameObject: {gameObject.name}");

            if (_instance != null && _instance != this)
            {
                Debug.LogWarning("[SPAWN-DEBUG] MatchManager: Instance already exists. Destroying duplicate.");
                Destroy(gameObject);
                return;
            }
            _instance = this;

            _playerSlots = new NetworkList<PlayerSlot>();
            _playerScores = new NetworkList<Services.Match.PlayerScore>();

            Debug.Log("[SPAWN-DEBUG] MatchManager.Awake() completed. Instance set.");
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            _matchState.OnValueChanged += OnMatchStateValueChanged;
            _roomSettings.OnValueChanged += OnRoomSettingsValueChanged;

            // MatchServiceにデータプロバイダーとして登録
            RegisterWithMatchService();

            Debug.Log($"[SPAWN-DEBUG] MatchManager.OnNetworkSpawn() called. IsServer: {IsServer}, IsClient: {IsClient}");

            // サーバーの場合、LobbyServiceからデータを取得してマッチを初期化
            if (IsServer)
            {
                Debug.Log("[SPAWN-DEBUG] MatchManager: IsServer=true, calling InitializeFromLobbyService()");
                InitializeFromLobbyService();
            }
            else
            {
                Debug.Log("[SPAWN-DEBUG] MatchManager: IsServer=false, skipping InitializeFromLobbyService()");
            }
        }

        public override void OnNetworkDespawn()
        {
            // MatchServiceからデータプロバイダー登録を解除
            UnregisterFromMatchService();

            _matchState.OnValueChanged -= OnMatchStateValueChanged;
            _roomSettings.OnValueChanged -= OnRoomSettingsValueChanged;

            _activeHandler?.Cleanup();
            _activeHandler = null;

            base.OnNetworkDespawn();
        }

        public override void OnDestroy()
        {
            Debug.Log("[MATCH-DEBUG] MatchManager.OnDestroy() called");
            Debug.Log($"[MATCH-DEBUG] OnDestroy StackTrace:\n{System.Environment.StackTrace}");

            // ローカルテストモードの場合、ここで登録解除
            if (!IsSpawned)
            {
                UnregisterFromMatchService();
            }

            if (_instance == this)
            {
                _instance = null;
            }
            base.OnDestroy();
        }

        private void OnDisable()
        {
            Debug.Log("[MATCH-DEBUG] MatchManager.OnDisable() called");
            Debug.Log($"[MATCH-DEBUG] OnDisable StackTrace:\n{System.Environment.StackTrace}");
        }

        private void Start()
        {
            Debug.Log($"[AI-SPAWN-DEBUG] MatchManager.Start() called");
            Debug.Log($"[AI-SPAWN-DEBUG] NetworkManager.Singleton={(NetworkManager.Singleton != null ? "exists" : "NULL")}");
            Debug.Log($"[AI-SPAWN-DEBUG] NetworkManager.IsListening={(NetworkManager.Singleton?.IsListening ?? false)}");
            Debug.Log($"[AI-SPAWN-DEBUG] MatchManager.IsSpawned={IsSpawned}");

            // LobbyServiceにデータがあるかチェック（Lobbyから遷移した場合）
            var lobbyService = ServiceLocator.Instance.Get<ILobbyService>();
            bool hasLobbyData = lobbyService != null && lobbyService.PlayerSlots.Count > 0;
            Debug.Log($"[AI-SPAWN-DEBUG] LobbyService hasData={hasLobbyData}");

            // NetworkManagerが起動していない場合、またはこのNetworkBehaviourがスポーンされていない場合
            // 開発テスト用にローカルモードで初期化
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
            {
                Debug.Log("[AI-SPAWN-DEBUG] >>> Taking LOCAL TEST MODE path (NetworkManager not running)");
                StartCoroutine(InitializeLocalTestMode());
            }
            else if (!IsSpawned)
            {
                // NetworkManagerは起動しているが、このオブジェクトがネットワークスポーンされていない場合
                // LobbyServiceにデータがある場合は、すぐにローカルモードで初期化（3秒待たない）
                if (hasLobbyData)
                {
                    Debug.Log("[AI-SPAWN-DEBUG] >>> Taking LOCAL TEST MODE path immediately (has LobbyService data, skip 3s wait)");
                    StartCoroutine(InitializeLocalTestMode());
                }
                else
                {
                    Debug.Log("[AI-SPAWN-DEBUG] >>> Taking WAIT FOR NETWORK SPAWN path (no LobbyService data)");
                    // 少し待ってからもう一度チェック
                    StartCoroutine(WaitForNetworkSpawnOrFallback());
                }
            }
            else
            {
                Debug.Log("[AI-SPAWN-DEBUG] >>> MatchManager already spawned via network, skipping local init");
            }
        }

        /// <summary>
        /// ネットワークスポーンを待つか、ローカルモードにフォールバックするコルーチン
        /// </summary>
        private System.Collections.IEnumerator WaitForNetworkSpawnOrFallback()
        {
            Debug.Log("[AI-SPAWN-DEBUG] WaitForNetworkSpawnOrFallback() started - waiting up to 3s for network spawn");
            float waitTime = 0f;
            const float maxWaitTime = 3f; // 最大3秒待機

            while (!IsSpawned && waitTime < maxWaitTime)
            {
                yield return new WaitForSeconds(0.1f);
                waitTime += 0.1f;
            }

            Debug.Log($"[AI-SPAWN-DEBUG] WaitForNetworkSpawnOrFallback() finished waiting. IsSpawned={IsSpawned}, waitTime={waitTime:F1}s");

            if (!IsSpawned)
            {
                Debug.Log("[AI-SPAWN-DEBUG] MatchManager still not spawned after waiting. >>> Falling back to LOCAL TEST MODE");
                StartCoroutine(InitializeLocalTestMode());
            }
            else
            {
                Debug.Log("[AI-SPAWN-DEBUG] MatchManager spawned successfully during wait - AI will spawn via OnNetworkSpawn path");
            }
        }

        /// <summary>
        /// ローカルテストモードでの初期化（シーン直接再生用、またはネットワーク非対応マッチ用）
        /// </summary>
        private System.Collections.IEnumerator InitializeLocalTestMode()
        {
            Debug.Log("[AI-SPAWN-DEBUG] ========================================");
            Debug.Log("[AI-SPAWN-DEBUG] InitializeLocalTestMode() STARTED");
            Debug.Log("[AI-SPAWN-DEBUG] ========================================");
            UpdateLoadProgress(0.1f, "Initializing...");

            // LobbyServiceからルーム設定とプレイヤースロットを取得（Lobbyから遷移した場合）
            var lobbyService = ServiceLocator.Instance.Get<ILobbyService>();
            Debug.Log($"[AI-SPAWN-DEBUG] LobbyService={(lobbyService != null ? "exists" : "NULL")}, PlayerSlots.Count={(lobbyService?.PlayerSlots?.Count ?? 0)}");

            RoomSettings settings;
            PlayerSlot[] slots;

            if (lobbyService != null && lobbyService.PlayerSlots.Count > 0)
            {
                // Lobbyから遷移した場合
                settings = lobbyService.CurrentRoomSettings;
                slots = lobbyService.PlayerSlots.ToArray();
                Debug.Log($"[AI-SPAWN-DEBUG] Using LobbyService data. Mode: {settings.GameMode}, Players: {slots.Length}");

                // スロットの詳細をログ出力
                for (int i = 0; i < slots.Length; i++)
                {
                    var slot = slots[i];
                    Debug.Log($"[AI-SPAWN-DEBUG] PlayerSlot[{i}] - Name: {slot.PlayerName}, IsAI: {slot.IsAI}, Team: {slot.TeamIndex}, Difficulty: {slot.AIDifficulty}");
                }
            }
            else
            {
                // シーン直接再生の場合（デフォルト設定）
                settings = RoomSettings.CreateDefault();
                var localPlayerSlot = new PlayerSlot(0, 0UL, "LocalPlayer");
                localPlayerSlot.TeamIndex = 0;
                slots = new PlayerSlot[] { localPlayerSlot };
                Debug.Log("[AI-SPAWN-DEBUG] Using default settings (no LobbyService data)");
            }

            // マップをロード（OnNetworkSpawn経由でない場合は手動でロード）
            UpdateLoadProgress(0.2f, "Loading field...");
            Debug.Log($"[AI-SPAWN-DEBUG] Loading map: {settings.MapName}");
            LoadMap(settings.MapName);

            // フィールドロード完了を待つ
            Debug.Log($"[AI-SPAWN-DEBUG] Waiting for FieldLoader... FieldLoader.Instance={(FieldLoader.Instance != null ? "exists" : "NULL")}");
            float fieldLoadWaitTime = 0f;
            const float maxFieldLoadWaitTime = 10f;
            while ((FieldLoader.Instance == null || !FieldLoader.Instance.IsLoaded) && fieldLoadWaitTime < maxFieldLoadWaitTime)
            {
                yield return new WaitForSeconds(0.1f);
                fieldLoadWaitTime += 0.1f;
            }

            if (FieldLoader.Instance == null || !FieldLoader.Instance.IsLoaded)
            {
                Debug.LogError($"[AI-SPAWN-DEBUG] FieldLoader failed to load after {fieldLoadWaitTime:F1}s. Aborting local test mode.");
                yield break;
            }

            Debug.Log("[AI-SPAWN-DEBUG] Field loaded, initializing match...");
            UpdateLoadProgress(0.4f, "Preparing match...");

            // RulesHandlersを自動検索
            AutoFindRulesHandlers();

            // ルーム設定を適用（ローカルモードでもRoomSettingsプロパティで参照できるように）
            _roomSettings.Value = settings;
            Debug.Log($"[AI-SPAWN-DEBUG] Applied RoomSettings - TimeLimit: {settings.TimeLimit}s, GameMode: {settings.GameMode}, ArrowLimit: {settings.ArrowLimit}");

            // プレイヤースコアを初期化（ローカルテストモード用）
            Debug.Log($"[AI-SPAWN-DEBUG] Initializing player scores for {slots.Length} players...");
            _playerScores?.Clear();
            foreach (var slot in slots)
            {
                var playerScore = new Services.Match.PlayerScore
                {
                    ClientId = slot.PlayerId,
                    PlayerName = slot.PlayerName,
                    Score = 0,
                    RemainingArrows = settings.ArrowLimit,
                    HitCount = 0,
                    ShotCount = 0,
                    TeamIndex = slot.TeamIndex
                };
                _playerScores?.Add(playerScore);
            }
            Debug.Log($"[AI-SPAWN-DEBUG] Player scores initialized. Total: {_playerScores?.Count ?? 0}");

            // ゲームモードハンドラーを初期化
            SelectHandler(settings.GameMode);
            _activeHandler?.Initialize(this, settings);

            // プレイヤーのスポーン完了を待つ
            UpdateLoadProgress(0.5f, "Spawning player...");
            yield return StartCoroutine(WaitForPlayerSpawn());

            // AIプレイヤーをスポーン（スロットにAIがいる場合）
            // ★重要: AIはここでスポーンするが、有効化はカウントダウン終了後
            UpdateLoadProgress(0.7f, "Spawning opponents...");
            Debug.Log($"[AI-SPAWN-DEBUG] ========== AI SPAWN PHASE START ==========");
            Debug.Log($"[AI-SPAWN-DEBUG] AISpawner.Instance={(AISpawner.Instance != null ? "exists" : "NULL")}");
            Debug.Log($"[AI-SPAWN-DEBUG] Total slots to check: {slots.Length}");
            int aiCount = 0;
            if (AISpawner.Instance != null)
            {
                foreach (var slot in slots)
                {
                    Debug.Log($"[AI-SPAWN-DEBUG] Checking slot: {slot.PlayerName}, IsAI={slot.IsAI}, Team={slot.TeamIndex}");
                    if (slot.IsAI)
                    {
                        Debug.Log($"[AI-SPAWN-DEBUG] >>> Calling AISpawner.SpawnAIPlayers for: {slot.PlayerName}");
                        AISpawner.Instance.Initialize(settings.GameMode, slot.AIDifficulty);
                        var spawnedIds = AISpawner.Instance.SpawnAIPlayers(1, slot.TeamIndex);

                        if (spawnedIds.Count > 0)
                        {
                            aiCount++;
                            Debug.Log($"[AI-SPAWN-DEBUG] Spawned AI successfully. ID: {spawnedIds[0]}");
                        }
                        else
                        {
                            Debug.LogWarning($"[AI-SPAWN-DEBUG] FAILED to spawn AI for slot: {slot.PlayerName}");
                        }
                    }
                }

                // AIがスロットにいなかった場合、テスト用に1体スポーン
                if (aiCount == 0)
                {
                    Debug.Log("[AI-SPAWN-DEBUG] No AI in slots found, spawning 1 test AI...");
                    AISpawner.Instance.Initialize(settings.GameMode, AIDifficulty.Normal);
                    Debug.Log("[AI-SPAWN-DEBUG] >>> Calling AISpawner.SpawnAIPlayers for test AI");
                    var spawnedIds = AISpawner.Instance.SpawnAIPlayers(1, -1);
                    aiCount = spawnedIds.Count;
                    Debug.Log($"[AI-SPAWN-DEBUG] Test AI spawn result: {aiCount} spawned");
                }

                Debug.Log($"[AI-SPAWN-DEBUG] ========== AI SPAWN PHASE END: Total spawned={aiCount} ==========");
            }
            else
            {
                Debug.LogError("[AI-SPAWN-DEBUG] AISpawner.Instance is NULL! Cannot spawn AI.");
            }

            _aiSpawned = true;

            // ★重要: AIは有効化しない（カウントダウン終了後に有効化）
            // AISpawner.Instance.EnableAllAI() はここでは呼ばない
            Debug.Log("[AI-SPAWN-DEBUG] AI spawned but NOT enabled yet (will enable after countdown)");

            // MatchServiceにデータプロバイダーとして登録（ローカルテストモード用）
            Debug.Log("[AI-SPAWN-DEBUG] About to register with MatchService...");
            try
            {
                RegisterWithMatchService();
                Debug.Log("[AI-SPAWN-DEBUG] Registered with MatchService successfully");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[AI-SPAWN-DEBUG] Exception in RegisterWithMatchService: {ex.Message}\n{ex.StackTrace}");
            }

            // すべてのエンティティの準備完了を通知
            UpdateLoadProgress(0.95f, "Finalizing...");
            yield return new WaitForSeconds(0.2f); // 少し待ってからUIが更新される時間を確保

            Debug.Log("[AI-SPAWN-DEBUG] Local test mode initialized complete - NotifyAllEntitiesReady()");
            NotifyAllEntitiesReady();

            // ローカルテストモードでカウントダウンを開始
            Debug.Log("[AI-SPAWN-DEBUG] Starting countdown coroutine...");
            StartCoroutine(RunLocalCountdown());
        }

        /// <summary>
        /// プレイヤーのスポーン完了を待ちます
        /// </summary>
        private System.Collections.IEnumerator WaitForPlayerSpawn()
        {
            Debug.Log("[AI-SPAWN-DEBUG] WaitForPlayerSpawn() started");

            float waitTime = 0f;
            const float maxWaitTime = 10f;

            while (!_playerSpawned && waitTime < maxWaitTime)
            {
                // PlayerSpawnerのスポーン完了をチェック
                if (PlayerSpawner.Instance != null && PlayerSpawner.Instance.IsSpawned)
                {
                    _playerSpawned = true;
                    Debug.Log("[AI-SPAWN-DEBUG] Player spawned successfully!");
                    yield break;
                }

                yield return new WaitForSeconds(0.1f);
                waitTime += 0.1f;
            }

            if (!_playerSpawned)
            {
                Debug.LogWarning($"[AI-SPAWN-DEBUG] Player spawn timeout after {waitTime:F1}s. Continuing anyway...");
                _playerSpawned = true; // タイムアウトでも続行
            }
        }

        /// <summary>
        /// ローカルテストモード用のカウントダウン処理
        /// </summary>
        private System.Collections.IEnumerator RunLocalCountdown()
        {
            Debug.Log("[MATCH-DEBUG] RunLocalCountdown: Starting countdown...");

            // カウントダウン状態に遷移
            _matchState.Value = MatchState.Countdown;

            float countdownTimer = _countdownDuration;
            int lastSeconds = -1;

            while (countdownTimer > 0)
            {
                int seconds = Mathf.CeilToInt(countdownTimer);

                // 秒数が変わった時だけ通知（毎フレーム通知するとログが多すぎる）
                if (seconds != lastSeconds)
                {
                    Debug.Log($"[COUNTDOWN-DEBUG] RunLocalCountdown: seconds={seconds}");

                    // イベントを発火
                    CountdownUpdated?.Invoke(seconds);
                    RaiseProviderCountdownUpdated(seconds);

                    lastSeconds = seconds;
                }

                countdownTimer -= Time.deltaTime;
                yield return null;
            }

            Debug.Log("[MATCH-DEBUG] RunLocalCountdown: Countdown finished, starting match...");

            // マッチ開始
            _matchState.Value = MatchState.InProgress;
            _matchTime.Value = 0f;

            _activeHandler?.OnMatchStart();
            MatchStarted?.Invoke();
            RaiseProviderMatchStarted();

            // AIを有効化（まだ有効化されていない場合）
            AISpawner.Instance?.EnableAllAI();

            Debug.Log("[MATCH-DEBUG] RunLocalCountdown: Match started!");

            // ローカルモード用のマッチ時間更新を開始
            StartCoroutine(RunLocalMatchTimer());
        }

        /// <summary>
        /// ローカルテストモード用のマッチタイマー更新
        /// </summary>
        private System.Collections.IEnumerator RunLocalMatchTimer()
        {
            Debug.Log("[MATCH-DEBUG] RunLocalMatchTimer: Started");

            while (_matchState.Value == MatchState.InProgress)
            {
                _matchTime.Value += Time.deltaTime;
                _activeHandler?.OnUpdate(Time.deltaTime);

                // 時間制限チェック
                if (RoomSettings.TimeLimit > 0 && _matchTime.Value >= RoomSettings.TimeLimit)
                {
                    Debug.Log("[MATCH-DEBUG] RunLocalMatchTimer: Time limit reached!");
                    // マッチ終了処理（ローカルモード用）
                    _matchState.Value = MatchState.Ended;
                    _activeHandler?.OnMatchEnd();
                    break;
                }

                yield return null;
            }

            Debug.Log("[MATCH-DEBUG] RunLocalMatchTimer: Ended");
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

            RecordArrowFiredInternal(clientId);
        }

        /// <summary>
        /// プレイヤーの矢発射を記録します（ローカルモード用）
        /// </summary>
        /// <param name="clientId">クライアントID</param>
        public void RecordArrowFiredLocal(ulong clientId)
        {
            if (_playerScores == null)
            {
                Debug.LogWarning("[MatchManager] RecordArrowFiredLocal: _playerScores is null");
                return;
            }

            RecordArrowFiredInternal(clientId);
        }

        /// <summary>
        /// 矢発射記録の内部実装
        /// </summary>
        private void RecordArrowFiredInternal(ulong clientId)
        {
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

                    Debug.Log($"[MatchManager] Arrow fired by {clientId}. Remaining: {playerScore.RemainingArrows}, Shots: {playerScore.ShotCount}");

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
            Debug.Log($"[COUNTDOWN-DEBUG] NotifyCountdownClientRpc: seconds={seconds}");
            CountdownUpdated?.Invoke(seconds);
            RaiseProviderCountdownUpdated(seconds);
        }

        [ClientRpc]
        private void NotifyMatchStartedClientRpc()
        {
            MatchStarted?.Invoke();
            RaiseProviderMatchStarted();
        }

        [ClientRpc]
        private void NotifyMatchEndedClientRpc(MatchEndResult result)
        {
            MatchEnded?.Invoke(result);
            RaiseProviderMatchEnded(result);
        }

        [ClientRpc]
        private void NotifyPlayerScoredClientRpc(ulong clientId, int score, HitLocation hitLocation)
        {
            PlayerScored?.Invoke(clientId, score, hitLocation);
            RaiseProviderPlayerScored(clientId, score, hitLocation);
        }

        [ClientRpc]
        private void NotifyPlayerDiedClientRpc(ulong clientId, ulong killerId)
        {
            PlayerDied?.Invoke(clientId, killerId);
        }

        #endregion

        #region Public Methods

        // GetPlayerScoreのデバッグカウンター
        private int _getPlayerScoreDebugCounter = 0;

        /// <summary>
        /// プレイヤーのスコア情報を取得します
        /// </summary>
        public Services.Match.PlayerScore? GetPlayerScore(ulong clientId)
        {
            _getPlayerScoreDebugCounter++;
            bool shouldLog = _getPlayerScoreDebugCounter <= 10;

            if (_playerScores == null)
            {
                if (shouldLog)
                {
                    Debug.Log($"[SCORE-DEBUG] MatchManager.GetPlayerScore({clientId}): _playerScores is NULL!");
                }
                return null;
            }

            if (shouldLog)
            {
                Debug.Log($"[SCORE-DEBUG] MatchManager.GetPlayerScore({clientId}): _playerScores.Count={_playerScores.Count}");
                foreach (var s in _playerScores)
                {
                    Debug.Log($"[SCORE-DEBUG]   - ClientId={s.ClientId}, Name={s.PlayerName}, Arrows={s.RemainingArrows}");
                }
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

        /// <summary>
        /// 指定したプレイヤー/AIがリスポーン可能かどうかを取得します
        /// </summary>
        /// <param name="clientId">プレイヤーまたはAIのID</param>
        /// <returns>リスポーン可能な場合はtrue</returns>
        public bool CanRespawn(ulong clientId)
        {
            // マッチが進行中でない場合はリスポーン不可
            if (_matchState.Value != MatchState.InProgress)
            {
                return false;
            }

            // ゲームモードハンドラーに確認
            return _activeHandler?.CanPlayerRespawn(clientId) ?? true;
        }

        /// <summary>
        /// リスポーン遅延時間を取得します
        /// </summary>
        /// <returns>リスポーン遅延（秒）</returns>
        public float GetRespawnDelay()
        {
            // ルーム設定からリスポーン遅延を取得（デフォルト3秒）
            // TODO: RoomSettingsにRespawnDelayを追加
            return 3f;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// LobbyServiceからマッチ情報を取得して初期化します
        /// </summary>
        private void InitializeFromLobbyService()
        {
            Debug.Log("[SPAWN-DEBUG] MatchManager.InitializeFromLobbyService() called");

            var lobbyService = ServiceLocator.Instance.Get<ILobbyService>();
            if (lobbyService == null)
            {
                Debug.LogError("[SPAWN-DEBUG] MatchManager: ILobbyService not found. Cannot initialize match.");
                return;
            }

            var roomSettings = lobbyService.CurrentRoomSettings;
            var playerSlots = lobbyService.PlayerSlots.ToArray();

            Debug.Log($"[SPAWN-DEBUG] MatchManager.InitializeFromLobbyService() - Map: {roomSettings.MapName}, GameMode: {roomSettings.GameMode}, PlayerSlots: {playerSlots.Length}");

            // ログに各スロットの詳細を出力
            for (int i = 0; i < playerSlots.Length; i++)
            {
                var slot = playerSlots[i];
                Debug.Log($"[SPAWN-DEBUG] MatchManager: PlayerSlot[{i}] - Name: {slot.PlayerName}, IsAI: {slot.IsAI}, Team: {slot.TeamIndex}, Difficulty: {slot.AIDifficulty}");
            }

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

            // エンティティスポーンのコルーチンを開始
            StartCoroutine(SpawnEntitiesAndStartCountdown(settings, slots));
        }

        /// <summary>
        /// エンティティをスポーンしてからカウントダウンを開始します（ネットワークモード用）
        /// </summary>
        private System.Collections.IEnumerator SpawnEntitiesAndStartCountdown(RoomSettings settings, PlayerSlot[] slots)
        {
            UpdateLoadProgress(0.4f, "Preparing match...");

            // プレイヤーのスポーン完了を待つ
            UpdateLoadProgress(0.5f, "Spawning player...");
            yield return StartCoroutine(WaitForPlayerSpawn());

            // AIプレイヤーをスポーン（スロットにAIがいる場合）
            // ★重要: AIはここでスポーンするが、有効化はカウントダウン終了後
            UpdateLoadProgress(0.7f, "Spawning opponents...");
            Debug.Log($"[SPAWN-DEBUG] MatchManager: Spawning AI during initialization (network mode)");

            if (AISpawner.Instance != null)
            {
                int aiCount = 0;
                foreach (var slot in slots)
                {
                    if (slot.IsAI)
                    {
                        Debug.Log($"[SPAWN-DEBUG] MatchManager: Spawning AI - Name: {slot.PlayerName}, Team: {slot.TeamIndex}, Difficulty: {slot.AIDifficulty}");
                        AISpawner.Instance.Initialize(settings.GameMode, slot.AIDifficulty);
                        var spawnedIds = AISpawner.Instance.SpawnAIPlayers(1, slot.TeamIndex);
                        if (spawnedIds.Count > 0)
                        {
                            aiCount++;
                            Debug.Log($"[SPAWN-DEBUG] MatchManager: AI spawned successfully. ID: {spawnedIds[0]}");
                        }
                    }
                }
                Debug.Log($"[SPAWN-DEBUG] MatchManager: Total AI spawned during initialization: {aiCount}");
            }

            _aiSpawned = true;
            Debug.Log("[SPAWN-DEBUG] MatchManager: AI spawned but NOT enabled yet (will enable after countdown)");

            // すべてのエンティティの準備完了を通知
            UpdateLoadProgress(0.95f, "Finalizing...");
            yield return new WaitForSeconds(0.2f); // UIが更新される時間を確保

            NotifyAllEntitiesReady();

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
                Debug.Log("[SPAWN-DEBUG] MatchManager.UpdateCountdown(): Countdown finished, calling StartMatch()");
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
            Debug.Log("[SPAWN-DEBUG] MatchManager.StartMatch() called");

            _matchState.Value = MatchState.InProgress;
            _matchTime.Value = 0f;

            // ★重要: AIはInitializeMatchInternal()で既にスポーン済み
            // ここではスポーンしない（SpawnAIPlayers()は呼ばない）
            // AIがまだスポーンされていない場合のみスポーン（フォールバック）
            if (!_aiSpawned)
            {
                Debug.Log("[SPAWN-DEBUG] MatchManager: AI not spawned yet, spawning now as fallback...");
                SpawnAIPlayers();
                _aiSpawned = true;
            }
            else
            {
                Debug.Log("[SPAWN-DEBUG] MatchManager: AI already spawned during initialization");
            }

            _activeHandler?.OnMatchStart();
            NotifyMatchStartedClientRpc();

            // AIを有効化（ここで初めて有効化）
            Debug.Log("[SPAWN-DEBUG] MatchManager: Enabling AI now...");
            AISpawner.Instance?.EnableAllAI();

            Debug.Log("[SPAWN-DEBUG] MatchManager.StartMatch() completed");
        }

        /// <summary>
        /// PlayerSlotsに基づいてAIプレイヤーをスポーンします
        /// </summary>
        private void SpawnAIPlayers()
        {
            Debug.Log($"[SPAWN-DEBUG] MatchManager.SpawnAIPlayers() called");

            if (AISpawner.Instance == null)
            {
                Debug.LogWarning("[SPAWN-DEBUG] MatchManager: AISpawner.Instance is NULL! AI players will not spawn.");
                return;
            }

            if (_playerSlots == null)
            {
                Debug.LogWarning("[SPAWN-DEBUG] MatchManager: _playerSlots is NULL!");
                return;
            }

            Debug.Log($"[SPAWN-DEBUG] MatchManager: _playerSlots.Count={_playerSlots.Count}");

            int aiCount = 0;
            foreach (var slot in _playerSlots)
            {
                Debug.Log($"[SPAWN-DEBUG] MatchManager: Checking slot - PlayerName: {slot.PlayerName}, IsAI: {slot.IsAI}, Team: {slot.TeamIndex}");

                if (slot.IsAI)
                {
                    aiCount++;
                    // そのAIの難易度でAISpawnerを初期化
                    Debug.Log($"[SPAWN-DEBUG] MatchManager: Initializing AISpawner for slot. Difficulty: {slot.AIDifficulty}");
                    AISpawner.Instance.Initialize(RoomSettings.GameMode, slot.AIDifficulty);

                    // AIをスポーン（チームを指定）
                    var spawnedIds = AISpawner.Instance.SpawnAIPlayers(1, slot.TeamIndex);

                    if (spawnedIds.Count > 0)
                    {
                        Debug.Log($"[SPAWN-DEBUG] MatchManager: Spawned AI for slot. PlayerName: {slot.PlayerName}, Team: {slot.TeamIndex}, Difficulty: {slot.AIDifficulty}");
                    }
                    else
                    {
                        Debug.LogWarning($"[SPAWN-DEBUG] MatchManager: FAILED to spawn AI for slot: {slot.PlayerName}");
                    }
                }
            }

            Debug.Log($"[SPAWN-DEBUG] MatchManager.SpawnAIPlayers() completed. AI slots found: {aiCount}, Total AI spawned: {AISpawner.Instance.SpawnedAICount}");
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
            RaiseProviderMatchStateChanged(newValue);
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
