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

#if UNITY_EDITOR
        [Header("Debug Settings")]
        [SerializeField] private KeyCode _debugEndMatchKey = KeyCode.F12;
#endif

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
        /// <remarks>
        /// パラメータ: clientId, score, hitLocation, hitPosition
        /// </remarks>
        public event Action<ulong, int, HitLocation, Vector3>? PlayerScored;

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

            // SceneManagementServiceにゲームプレイ準備完了を通知
            // これによりローディング画面が閉じる
            var sceneService = ServiceLocator.Instance.Get<ISceneManagementService>();
            if (sceneService != null && sceneService.IsWaitingForGameplayReady)
            {
                sceneService.SignalGameplayReady();
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
        private event Action<ulong, int, ServicesMatch.HitLocation, Vector3>? _providerPlayerScored;

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

        event Action<ulong, int, ServicesMatch.HitLocation, Vector3>? IMatchDataProvider.PlayerScored
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

        private void RaiseProviderPlayerScored(ulong clientId, int score, HitLocation hitLocation, Vector3 hitPosition)
        {
            _providerPlayerScored?.Invoke(clientId, score, (ServicesMatch.HitLocation)(int)hitLocation, hitPosition);
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
                matchService.RegisterMatchDataProvider(this);
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
            }
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;

            _playerSlots = new NetworkList<PlayerSlot>();
            _playerScores = new NetworkList<Services.Match.PlayerScore>();

            // ServiceLocatorにIMatchReadinessProviderとして登録
            ServiceLocator.Instance.Register<ServicesMatch.IMatchReadinessProvider>(this);
        }

        /// <summary>
        /// IService.Initialize の実装（MonoBehaviourのためAwakeで初期化済み）
        /// </summary>
        public void Initialize()
        {
            // MonoBehaviourのため、Awake()で初期化済み
        }

        /// <summary>
        /// IService.Dispose の実装（MonoBehaviourのためOnDestroyで解放）
        /// </summary>
        public void Dispose()
        {
            // MonoBehaviourのため、OnDestroy()で解放される
            // 手動でDisposeが呼ばれた場合は何もしない
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            _matchState.OnValueChanged += OnMatchStateValueChanged;
            _roomSettings.OnValueChanged += OnRoomSettingsValueChanged;

            // MatchServiceにデータプロバイダーとして登録
            RegisterWithMatchService();

            // サーバーの場合、LobbyServiceからデータを取得してマッチを初期化
            if (IsServer)
            {
                InitializeFromLobbyService();
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
            // ServiceLocatorから解除
            ServiceLocator.Instance.Unregister<ServicesMatch.IMatchReadinessProvider>();

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

        private void Start()
        {
            // LobbyServiceにデータがあるかチェック（Lobbyから遷移した場合）
            var lobbyService = ServiceLocator.Instance.Get<ILobbyService>();
            bool hasLobbyData = lobbyService != null && lobbyService.PlayerSlots.Count > 0;

            // NetworkManagerが起動していない場合、またはこのNetworkBehaviourがスポーンされていない場合
            // 開発テスト用にローカルモードで初期化
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
            {
                StartCoroutine(InitializeLocalTestMode());
            }
            else if (!IsSpawned)
            {
                // NetworkManagerは起動しているが、このオブジェクトがネットワークスポーンされていない場合
                // LobbyServiceにデータがある場合は、すぐにローカルモードで初期化（3秒待たない）
                if (hasLobbyData)
                {
                    StartCoroutine(InitializeLocalTestMode());
                }
                else
                {
                    // 少し待ってからもう一度チェック
                    StartCoroutine(WaitForNetworkSpawnOrFallback());
                }
            }
        }

        /// <summary>
        /// ネットワークスポーンを待つか、ローカルモードにフォールバックするコルーチン
        /// </summary>
        private System.Collections.IEnumerator WaitForNetworkSpawnOrFallback()
        {
            float waitTime = 0f;
            const float maxWaitTime = 3f; // 最大3秒待機

            while (!IsSpawned && waitTime < maxWaitTime)
            {
                yield return new WaitForSeconds(0.1f);
                waitTime += 0.1f;
            }

            if (!IsSpawned)
            {
                StartCoroutine(InitializeLocalTestMode());
            }
        }

        /// <summary>
        /// ローカルテストモードでの初期化（シーン直接再生用、またはネットワーク非対応マッチ用）
        /// </summary>
        private System.Collections.IEnumerator InitializeLocalTestMode()
        {
            UpdateLoadProgress(0.1f, "Initializing...");

            // LobbyServiceからルーム設定とプレイヤースロットを取得（Lobbyから遷移した場合）
            var lobbyService = ServiceLocator.Instance.Get<ILobbyService>();

            RoomSettings settings;
            PlayerSlot[] slots;

            if (lobbyService != null && lobbyService.PlayerSlots.Count > 0)
            {
                // Lobbyから遷移した場合
                settings = lobbyService.CurrentRoomSettings;
                slots = lobbyService.PlayerSlots.ToArray();
            }
            else
            {
                // シーン直接再生の場合（デフォルト設定）
                settings = RoomSettings.CreateDefault();
                var localPlayerSlot = new PlayerSlot(0, 0UL, "LocalPlayer");
                localPlayerSlot.TeamIndex = 0;
                slots = new PlayerSlot[] { localPlayerSlot };
            }

            // マップをロード（OnNetworkSpawn経由でない場合は手動でロード）
            UpdateLoadProgress(0.2f, "Loading field...");
            LoadMap(settings.MapName);

            // フィールドロード完了を待つ
            float fieldLoadWaitTime = 0f;
            const float maxFieldLoadWaitTime = 10f;
            while ((FieldLoader.Instance == null || !FieldLoader.Instance.IsLoaded) && fieldLoadWaitTime < maxFieldLoadWaitTime)
            {
                yield return new WaitForSeconds(0.1f);
                fieldLoadWaitTime += 0.1f;
            }

            if (FieldLoader.Instance == null || !FieldLoader.Instance.IsLoaded)
            {
                Debug.LogError("[MatchManager] FieldLoader failed to load. Aborting local test mode.");
                yield break;
            }

            UpdateLoadProgress(0.4f, "Preparing match...");

            // RulesHandlersを自動検索
            AutoFindRulesHandlers();

            // ルーム設定を適用（ローカルモードでもRoomSettingsプロパティで参照できるように）
            _roomSettings.Value = settings;

            // プレイヤースコアを初期化（ローカルテストモード用）
            _playerScores?.Clear();
            foreach (var slot in slots)
            {
                var playerScore = new Services.Match.PlayerScore
                {
                    ClientId = slot.PlayerId,
                    PlayerName = slot.PlayerName,
                    Score = 0,
                    RemainingArrows = settings.ArrowLimit == 0 ? -1 : settings.ArrowLimit,
                    HitCount = 0,
                    ShotCount = 0,
                    TeamIndex = slot.TeamIndex
                };
                _playerScores?.Add(playerScore);
            }

            // ゲームモードハンドラーを初期化
            SelectHandler(settings.GameMode);
            _activeHandler?.Initialize(this, settings);

            // プレイヤーのスポーン完了を待つ
            UpdateLoadProgress(0.5f, "Spawning player...");
            yield return StartCoroutine(WaitForPlayerSpawn());

            // AIプレイヤーをスポーン（スロットにAIがいる場合）
            // ★重要: AIはここでスポーンするが、有効化はカウントダウン終了後
            UpdateLoadProgress(0.7f, "Spawning opponents...");
            int aiCount = 0;
            if (AISpawner.Instance != null && _playerScores != null)
            {
                int slotIndex = 0;
                foreach (var slot in slots)
                {
                    if (slot.IsAI)
                    {
                        AISpawner.Instance.Initialize(settings.GameMode, slot.AIDifficulty);
                        var spawnedIds = AISpawner.Instance.SpawnAIPlayers(1, slot.TeamIndex);

                        // スポーンされたAIのClientIdでスコアエントリを更新
                        if (spawnedIds.Count > 0)
                        {
                            ulong realAIId = spawnedIds[0];

                            // _playerScores内の対応するエントリを探してClientIdを更新
                            for (int i = 0; i < _playerScores.Count; i++)
                            {
                                if (_playerScores[i].ClientId == slot.PlayerId)
                                {
                                    var scoreEntry = _playerScores[i];
                                    scoreEntry.ClientId = realAIId; // プレースホルダーIDを実際のAI IDに置き換え
                                    _playerScores[i] = scoreEntry;
                                    break;
                                }
                            }

                            aiCount++;
                        }
                    }
                    slotIndex++;
                }

                // AIがスロットにいなかった場合、テスト用に1体スポーン
                if (aiCount == 0)
                {
                    AISpawner.Instance.Initialize(settings.GameMode, AIDifficulty.Normal);
                    AISpawner.Instance.SpawnAIPlayers(1, -1);
                }
            }

            _aiSpawned = true;

            // MatchServiceにデータプロバイダーとして登録（ローカルテストモード用）
            RegisterWithMatchService();

            // すべてのエンティティの準備完了を通知
            UpdateLoadProgress(0.95f, "Finalizing...");
            yield return new WaitForSeconds(0.2f); // 少し待ってからUIが更新される時間を確保

            NotifyAllEntitiesReady();

            // ローカルテストモードでカウントダウンを開始
            StartCoroutine(RunLocalCountdown());
        }

        /// <summary>
        /// プレイヤーのスポーン完了を待ちます
        /// </summary>
        private System.Collections.IEnumerator WaitForPlayerSpawn()
        {
            float waitTime = 0f;
            const float maxWaitTime = 10f;

            while (!_playerSpawned && waitTime < maxWaitTime)
            {
                // PlayerSpawnerのスポーン完了をチェック
                if (PlayerSpawner.Instance != null && PlayerSpawner.Instance.IsSpawned)
                {
                    _playerSpawned = true;
                    yield break;
                }

                yield return new WaitForSeconds(0.1f);
                waitTime += 0.1f;
            }

            if (!_playerSpawned)
            {
                _playerSpawned = true; // タイムアウトでも続行
            }
        }

        /// <summary>
        /// ローカルテストモード用のカウントダウン処理
        /// </summary>
        private System.Collections.IEnumerator RunLocalCountdown()
        {
            // カウントダウン状態に遷移
            _matchState.Value = MatchState.Countdown;

            float countdownTimer = _countdownDuration;
            int lastSeconds = -1;

            while (countdownTimer > 0)
            {
                int seconds = Mathf.CeilToInt(countdownTimer);

                // 秒数が変わった時だけ通知
                if (seconds != lastSeconds)
                {
                    // イベントを発火
                    CountdownUpdated?.Invoke(seconds);
                    RaiseProviderCountdownUpdated(seconds);

                    lastSeconds = seconds;
                }

                countdownTimer -= Time.deltaTime;
                yield return null;
            }

            // マッチ開始
            _matchState.Value = MatchState.InProgress;
            _matchTime.Value = 0f;

            _activeHandler?.OnMatchStart();
            MatchStarted?.Invoke();
            RaiseProviderMatchStarted();

            // AIを有効化（まだ有効化されていない場合）
            AISpawner.Instance?.EnableAllAI();

            // ローカルモード用のマッチ時間更新を開始
            StartCoroutine(RunLocalMatchTimer());
        }

        /// <summary>
        /// ローカルテストモード用のマッチタイマー更新
        /// </summary>
        private System.Collections.IEnumerator RunLocalMatchTimer()
        {
            while (_matchState.Value == MatchState.InProgress)
            {
                _matchTime.Value += Time.deltaTime;
                _activeHandler?.OnUpdate(Time.deltaTime);

                // 時間制限チェック
                if (RoomSettings.TimeLimit > 0 && _matchTime.Value >= RoomSettings.TimeLimit)
                {
                    // マッチ終了処理（ローカルモード用）
                    _matchState.Value = MatchState.Ended;
                    _activeHandler?.OnMatchEnd();
                    break;
                }

                yield return null;
            }
        }

        private void Update()
        {
#if UNITY_EDITOR
            // デバッグキーでマッチを強制終了
            if (Input.GetKeyDown(_debugEndMatchKey))
            {
                Debug.Log($"[MatchManager] DEBUG: {_debugEndMatchKey} key pressed! MatchState={_matchState.Value}, IsServer={IsServer}, IsHost={IsHost}");

                if (_matchState.Value == MatchState.InProgress)
                {
                    Debug.Log($"[MatchManager] DEBUG: Calling DebugForceEndMatch()...");
                    DebugForceEndMatch();
                }
                else
                {
                    Debug.LogWarning($"[MatchManager] DEBUG: Cannot end match - state is {_matchState.Value}, not InProgress");
                }
            }
#endif

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
                    RemainingArrows = settings.ArrowLimit == 0 ? -1 : settings.ArrowLimit,
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
        /// <param name="clientId">クライアントID</param>
        /// <param name="score">追加するスコア</param>
        /// <param name="hitLocation">命中部位</param>
        /// <param name="hitPosition">ヒット位置（ワールド座標）</param>
        [Rpc(SendTo.Server)]
        public void AddPlayerScoreRpc(ulong clientId, int score, HitLocation hitLocation, Vector3 hitPosition)
        {
            if (!IsServer || _playerScores == null)
            {
                return;
            }

            bool found = false;
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
                    NotifyPlayerScoredClientRpc(clientId, score, hitLocation, hitPosition);
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                Debug.LogError($"[MatchManager] AddPlayerScoreRpc: ClientId={clientId} NOT FOUND in _playerScores!");
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
                return;
            }

            RecordArrowFiredInternal(clientId);
        }

        /// <summary>
        /// プレイヤーのスコアを追加します（ローカルモード用）
        /// </summary>
        /// <param name="clientId">クライアントID</param>
        /// <param name="score">追加するスコア</param>
        /// <param name="hitLocation">命中部位</param>
        /// <param name="hitPosition">ヒット位置（ワールド座標）</param>
        public void AddPlayerScoreLocal(ulong clientId, int score, HitLocation hitLocation, Vector3 hitPosition)
        {
            if (_playerScores == null)
            {
                Debug.LogWarning($"[MatchManager] AddPlayerScoreLocal - _playerScores is NULL!");
                return;
            }

            bool found = false;
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

                    // ローカルモードではイベントを直接発火
                    PlayerScored?.Invoke(clientId, score, hitLocation, hitPosition);
                    RaiseProviderPlayerScored(clientId, score, hitLocation, hitPosition);
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                Debug.LogError($"[MatchManager] AddPlayerScoreLocal: ClientId={clientId} NOT FOUND in _playerScores!");
            }
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
        private void NotifyPlayerScoredClientRpc(ulong clientId, int score, HitLocation hitLocation, Vector3 hitPosition)
        {
            PlayerScored?.Invoke(clientId, score, hitLocation, hitPosition);
            RaiseProviderPlayerScored(clientId, score, hitLocation, hitPosition);
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
            var lobbyService = ServiceLocator.Instance.Get<ILobbyService>();
            if (lobbyService == null)
            {
                Debug.LogError("[MatchManager] ILobbyService not found. Cannot initialize match.");
                return;
            }

            var roomSettings = lobbyService.CurrentRoomSettings;
            var playerSlots = lobbyService.PlayerSlots.ToArray();

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
                FieldLoader.Instance.LoadField(mapName);
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
                    RemainingArrows = settings.ArrowLimit == 0 ? -1 : settings.ArrowLimit,
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

            if (AISpawner.Instance != null && _playerScores != null)
            {
                int slotIndex = 0;
                foreach (var slot in slots)
                {
                    if (slot.IsAI)
                    {
                        AISpawner.Instance.Initialize(settings.GameMode, slot.AIDifficulty);
                        var spawnedIds = AISpawner.Instance.SpawnAIPlayers(1, slot.TeamIndex);

                        // スポーンされたAIのClientIdでスコアエントリを更新
                        if (spawnedIds.Count > 0)
                        {
                            ulong realAIId = spawnedIds[0];

                            // _playerScores内の対応するエントリを探してClientIdを更新
                            for (int i = 0; i < _playerScores.Count; i++)
                            {
                                if (_playerScores[i].ClientId == slot.PlayerId)
                                {
                                    var scoreEntry = _playerScores[i];
                                    scoreEntry.ClientId = realAIId; // プレースホルダーIDを実際のAI IDに置き換え
                                    _playerScores[i] = scoreEntry;
                                    break;
                                }
                            }
                        }
                    }
                    slotIndex++;
                }
            }

            _aiSpawned = true;

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

            // AIがまだスポーンされていない場合のみスポーン（フォールバック）
            if (!_aiSpawned)
            {
                SpawnAIPlayers();
                _aiSpawned = true;
            }

            _activeHandler?.OnMatchStart();
            NotifyMatchStartedClientRpc();

            // AIを有効化
            AISpawner.Instance?.EnableAllAI();
        }

        /// <summary>
        /// PlayerSlotsに基づいてAIプレイヤーをスポーンします
        /// </summary>
        private void SpawnAIPlayers()
        {
            if (AISpawner.Instance == null || _playerSlots == null || _playerScores == null)
            {
                return;
            }

            int slotIndex = 0;
            foreach (var slot in _playerSlots)
            {
                if (slot.IsAI)
                {
                    AISpawner.Instance.Initialize(RoomSettings.GameMode, slot.AIDifficulty);
                    var spawnedIds = AISpawner.Instance.SpawnAIPlayers(1, slot.TeamIndex);

                    // スポーンされたAIのClientIdでスコアエントリを更新
                    if (spawnedIds.Count > 0)
                    {
                        ulong realAIId = spawnedIds[0];

                        // _playerScores内の対応するエントリを探してClientIdを更新
                        for (int i = 0; i < _playerScores.Count; i++)
                        {
                            if (_playerScores[i].ClientId == slot.PlayerId)
                            {
                                var scoreEntry = _playerScores[i];
                                scoreEntry.ClientId = realAIId; // プレースホルダーIDを実際のAI IDに置き換え
                                _playerScores[i] = scoreEntry;
                                break;
                            }
                        }
                    }
                }
                slotIndex++;
            }
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

            // ネットワークモードの場合はRPCで通知、ローカルモードの場合は直接イベント発火
            if (IsSpawned)
            {
                try
                {
                    NotifyMatchEndedClientRpc(result);
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[MatchManager] Failed to send NotifyMatchEndedClientRpc: {ex.Message}");
                    Debug.LogError($"[MatchManager] Falling back to local event invocation");
                    // RPC失敗時はローカルで直接イベントを発火
                    MatchEnded?.Invoke(result);
                    RaiseProviderMatchEnded(result);
                }
            }
            else
            {
                // ネットワーク未使用時はローカルでイベント発火
                MatchEnded?.Invoke(result);
                RaiseProviderMatchEnded(result);
            }
        }

        private void OnMatchStateValueChanged(MatchState previousValue, MatchState newValue)
        {
            MatchStateChanged?.Invoke(newValue);
            RaiseProviderMatchStateChanged(newValue);
        }

        private void OnRoomSettingsValueChanged(RoomSettings previousValue, RoomSettings newValue)
        {
            // 必要に応じて処理
        }

        private void OnHandlerMatchEndTriggered(ulong winnerId)
        {
            if (IsServer)
            {
                EndMatch(winnerId);
            }
        }

        #endregion

#if UNITY_EDITOR
        #region Debug Methods

        /// <summary>
        /// デバッグ用：マッチを強制終了します（最高スコアのプレイヤーを勝者とする）
        /// </summary>
        [ContextMenu("Debug: Force End Match (Highest Score Wins)")]
        public void DebugForceEndMatch()
        {
            Debug.Log($"[MatchManager] DEBUG: DebugForceEndMatch called. MatchState={_matchState.Value}, IsSpawned={IsSpawned}, IsServer={IsServer}, IsHost={IsHost}");

            if (_matchState.Value != MatchState.InProgress)
            {
                Debug.LogWarning($"[MatchManager] DEBUG: Cannot force end match - match state is {_matchState.Value}, not InProgress");
                return;
            }

            // ネットワークモードの場合、サーバーである必要がある
            if (IsSpawned && !IsServer)
            {
                Debug.LogWarning("[MatchManager] DEBUG: Cannot force end match - not server in network mode");
                return;
            }

            // 最高スコアのプレイヤーを取得
            ulong winnerId = _activeHandler?.GetHighestScoringPlayer() ?? 0;

            // マッチを終了
            EndMatch(winnerId);
        }

        /// <summary>
        /// デバッグ用：マッチを強制終了します（引き分けとして）
        /// </summary>
        [ContextMenu("Debug: Force End Match (Draw)")]
        public void DebugForceEndMatchAsDraw()
        {
            if (_matchState.Value != MatchState.InProgress)
            {
                Debug.LogWarning($"[MatchManager] DEBUG: Cannot force end match - match state is {_matchState.Value}, not InProgress");
                return;
            }

            // ネットワークモードの場合、サーバーである必要がある
            if (IsSpawned && !IsServer)
            {
                Debug.LogWarning("[MatchManager] DEBUG: Cannot force end match - not server in network mode");
                return;
            }

            Debug.Log("[MatchManager] DEBUG: Force ending match as DRAW...");

            // 引き分けとして終了
            EndMatch(ulong.MaxValue);
        }

        /// <summary>
        /// デバッグ用：指定したプレイヤーを勝者としてマッチを強制終了します
        /// </summary>
        /// <param name="winnerId">勝者のClientId</param>
        public void DebugForceEndMatchWithWinner(ulong winnerId)
        {
            if (_matchState.Value != MatchState.InProgress)
            {
                Debug.LogWarning($"[MatchManager] DEBUG: Cannot force end match - match state is {_matchState.Value}, not InProgress");
                return;
            }

            // ネットワークモードの場合、サーバーである必要がある
            if (IsSpawned && !IsServer)
            {
                Debug.LogWarning("[MatchManager] DEBUG: Cannot force end match - not server in network mode");
                return;
            }

            Debug.Log($"[MatchManager] DEBUG: Force ending match with winner ClientId={winnerId}");

            // 指定したプレイヤーを勝者として終了
            EndMatch(winnerId);
        }

        #endregion
#endif
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
