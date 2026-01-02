#nullable enable

#if UNITY_EDITOR

using CavalryFight.Core.Services;
using CavalryFight.Development.MockData.MockServices;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CavalryFight.Development.MockData
{
    /// <summary>
    /// 開発用ブートストラップ
    /// </summary>
    /// <remarks>
    /// エディタでシーンを直接開いた時に、ServiceLocatorが初期化されていない場合に
    /// モックサービスを自動的に登録します。
    /// これにより、Startupシーンを経由せずに個別シーンをテストできます。
    /// </remarks>
    public static class DevBootstrap
    {
        #region Fields

        /// <summary>初期化済みフラグ</summary>
        private static bool _isInitialized;

        /// <summary>モック設定への参照</summary>
        private static MockSceneConfig? _mockConfig;

        #endregion

        #region Properties

        /// <summary>
        /// モック設定を取得します
        /// </summary>
        public static MockSceneConfig? MockConfig => _mockConfig;

        /// <summary>
        /// 初期化されているかどうか
        /// </summary>
        public static bool IsInitialized => _isInitialized;

        /// <summary>
        /// DevBootstrapで初期化されたかどうか（本番のGameBootstrapではなく）
        /// </summary>
        public static bool IsDevMode { get; private set; }

        #endregion

        #region Initialization

        /// <summary>
        /// シーンロード前に自動実行されます
        /// </summary>
        /// <remarks>
        /// RuntimeInitializeOnLoadMethod属性により、シーンがロードされる前に呼び出されます。
        /// Startupシーン以外から開始された場合にモックサービスを初期化します。
        /// </remarks>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitializeBeforeSceneLoad()
        {
            // 既に初期化済みならスキップ
            if (_isInitialized)
            {
                return;
            }

            // Startupシーンから開始された場合はスキップ（GameBootstrapに任せる）
            string currentSceneName = SceneManager.GetActiveScene().name;
            if (currentSceneName == "Startup" || currentSceneName == "Bootstrap")
            {
                Debug.Log("[DevBootstrap] Startup scene detected. Skipping mock initialization.");
                return;
            }

            // ServiceLocatorが既に初期化されているかチェック
            if (ServiceLocator.Instance != null && HasAnyService())
            {
                Debug.Log("[DevBootstrap] ServiceLocator already initialized. Skipping mock initialization.");
                return;
            }

            // モック設定を読み込み
            LoadMockConfig();

            // モックサービスを初期化
            InitializeMockServices();

            _isInitialized = true;
            IsDevMode = true;

            Debug.Log($"[DevBootstrap] Mock services initialized for scene: {currentSceneName}");
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 何らかのサービスが登録されているかチェックします
        /// </summary>
        /// <returns>サービスが登録されている場合はtrue</returns>
        private static bool HasAnyService()
        {
            // 主要なサービスが登録されているかチェック（例外をスローしないTryGetを使用）
            return ServiceLocator.Instance.IsRegistered<CavalryFight.Services.SceneManagement.ISceneManagementService>();
        }

        /// <summary>
        /// モック設定をResourcesフォルダから読み込みます
        /// </summary>
        private static void LoadMockConfig()
        {
            // Resources/MockDataフォルダから設定を読み込み
            _mockConfig = Resources.Load<MockSceneConfig>("MockData/MockSceneConfig");

            if (_mockConfig == null)
            {
                Debug.LogWarning("[DevBootstrap] MockSceneConfig not found in Resources/MockData/. Using default mock data.");
            }
        }

        /// <summary>
        /// モックサービスをServiceLocatorに登録します
        /// </summary>
        private static void InitializeMockServices()
        {
            var serviceLocator = ServiceLocator.Instance;

            // モックシーン管理サービス
            serviceLocator.Register<CavalryFight.Services.SceneManagement.ISceneManagementService>(
                new MockSceneManagementService());

            // モックマッチサービス
            var mockMatchService = new MockMatchService(_mockConfig);
            serviceLocator.Register<CavalryFight.Services.Match.IMatchService>(mockMatchService);

            // モックオーディオサービス
            serviceLocator.Register<CavalryFight.Services.Audio.IAudioService>(
                new MockAudioService());

            // モック入力サービス
            serviceLocator.Register<CavalryFight.Services.Input.IInputService>(
                new MockInputService());

            // モックリプレイサービス
            serviceLocator.Register<CavalryFight.Services.Replay.IReplayService>(
                new MockReplayService(_mockConfig));

            Debug.Log("[DevBootstrap] All mock services registered.");
        }

        /// <summary>
        /// デフォルトのモックマッチ結果を作成します
        /// </summary>
        /// <returns>デフォルト値で初期化されたマッチ結果</returns>
        private static CavalryFight.Services.Match.MatchResult CreateDefaultMockMatchResult()
        {
            var result = new CavalryFight.Services.Match.MatchResult
            {
                MapName = "Training Grounds",
                GameMode = "Arena",
                MatchDuration = 300f,
                MaxPlayers = 4,
                TimeLimit = 300,
                ArrowLimit = 30,
                IsTeamMatch = false,
                PlayerScore = 1500,
                EnemyScore = 1200,
                HasReplayData = true,
                IsMultiplayerMatch = false,
                CurrentPlayerCount = 4
            };

            // ローカルプレイヤー
            var localPlayer = new CavalryFight.Services.Match.PlayerStatistics
            {
                PlayerId = "local-player",
                PlayerName = "You",
                IsLocalPlayer = true,
                IsHost = true,
                Rank = 1,
                Score = 1500,
                ArrowsFired = 45,
                Hits = 30,
                Kills = 8,
                Deaths = 3,
                Headshots = 5,
                LongestKillStreak = 4
            };
            result.LocalPlayerStats = localPlayer;
            result.AllPlayerStats.Add(localPlayer);

            // AI opponents
            for (int i = 1; i <= 3; i++)
            {
                var aiPlayer = new CavalryFight.Services.Match.PlayerStatistics
                {
                    PlayerId = $"ai-{i}",
                    PlayerName = $"AI Player {i}",
                    IsNPC = true,
                    Rank = i + 1,
                    Score = 1200 - (i * 100),
                    ArrowsFired = 40,
                    Hits = 20 - (i * 2),
                    Kills = 5 - i,
                    Deaths = 4 + i
                };
                result.AllPlayerStats.Add(aiPlayer);
            }

            return result;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 現在のシーン用のモックマッチ結果を取得します
        /// </summary>
        /// <returns>モックマッチ結果、設定がない場合はデフォルト値</returns>
        public static CavalryFight.Services.Match.MatchResult? GetMockMatchResult()
        {
            if (_mockConfig?.ResultsSceneMockData != null)
            {
                return _mockConfig.ResultsSceneMockData.CreateMatchResult();
            }

            // デフォルトのモック結果を作成
            return CreateDefaultMockMatchResult();
        }

        #endregion
    }
}

#endif
