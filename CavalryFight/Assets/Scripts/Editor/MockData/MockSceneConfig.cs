#nullable enable

#if UNITY_EDITOR

using System;
using CavalryFight.Services.Replay;
using UnityEngine;

namespace CavalryFight.Editor.MockData
{
    /// <summary>
    /// 各シーン用のモックデータ設定
    /// </summary>
    /// <remarks>
    /// シーンごとに異なるモックデータを割り当てるための設定です。
    /// DevBootstrapがこの設定を読み込んで、適切なモックサービスを初期化します。
    /// </remarks>
    [CreateAssetMenu(fileName = "MockSceneConfig", menuName = "CavalryFight/Mock/Scene Config")]
    public class MockSceneConfig : ScriptableObject
    {
        #region Serialized Fields

        [Header("Results Scene")]
        [Tooltip("リザルト画面で使用するモックマッチ結果")]
        /// <summary>リザルト画面で使用するモックマッチ結果設定</summary>
        [SerializeField] private MockMatchResultConfig? _resultsSceneMockData;

        [Header("Replay Scene")]
        [Tooltip("リプレイ画面で使用するモックリプレイデータ")]
        /// <summary>リプレイ画面で使用するモックリプレイ設定</summary>
        [SerializeField] private MockReplayConfig? _replaySceneMockData;

        [Header("Match Scene")]
        [Tooltip("マッチ画面で使用するモック設定")]
        /// <summary>マッチ画面で使用するモック設定</summary>
        [SerializeField] private MockMatchConfig? _matchSceneMockData;

        [Header("Hunting Scene")]
        [Tooltip("ハンティング画面で使用するモック設定")]
        /// <summary>ハンティング画面で使用するモック設定</summary>
        [SerializeField] private MockHuntingConfig? _huntingSceneMockData;

        [Header("Training Scene")]
        [Tooltip("トレーニング画面で使用するモック設定")]
        /// <summary>トレーニング画面で使用するモック設定</summary>
        [SerializeField] private MockTrainingConfig? _trainingSceneMockData;

        #endregion

        #region Properties

        /// <summary>
        /// リザルト画面のモックデータ
        /// </summary>
        public MockMatchResultConfig? ResultsSceneMockData => _resultsSceneMockData;

        /// <summary>
        /// リプレイ画面のモックデータ
        /// </summary>
        public MockReplayConfig? ReplaySceneMockData => _replaySceneMockData;

        /// <summary>
        /// マッチ画面のモックデータ
        /// </summary>
        public MockMatchConfig? MatchSceneMockData => _matchSceneMockData;

        /// <summary>
        /// ハンティング画面のモックデータ
        /// </summary>
        public MockHuntingConfig? HuntingSceneMockData => _huntingSceneMockData;

        /// <summary>
        /// トレーニング画面のモックデータ
        /// </summary>
        public MockTrainingConfig? TrainingSceneMockData => _trainingSceneMockData;

        #endregion
    }

    /// <summary>
    /// モックリプレイ設定
    /// </summary>
    /// <remarks>
    /// リプレイ画面のテスト用モックデータを提供します。
    /// </remarks>
    [CreateAssetMenu(fileName = "MockReplay", menuName = "CavalryFight/Mock/Replay Config")]
    public class MockReplayConfig : ScriptableObject
    {
        #region Serialized Fields

        [Header("Replay Info")]
        /// <summary>リプレイの一意識別子</summary>
        [SerializeField] private string _replayId = "mock-replay-001";

        /// <summary>リプレイの表示名</summary>
        [SerializeField] private string _replayName = "Test Replay";

        /// <summary>リプレイの持続時間（秒）</summary>
        [SerializeField] private float _duration = 300f;

        [Header("Match Data")]
        /// <summary>関連するマッチ結果設定</summary>
        [SerializeField] private MockMatchResultConfig? _matchResult;

        #endregion

        #region Properties

        /// <summary>
        /// リプレイID
        /// </summary>
        public string ReplayId => _replayId;

        /// <summary>
        /// リプレイ名
        /// </summary>
        public string ReplayName => _replayName;

        /// <summary>
        /// 持続時間（秒）
        /// </summary>
        public float Duration => _duration;

        /// <summary>
        /// マッチ結果設定
        /// </summary>
        public MockMatchResultConfig? MatchResult => _matchResult;

        #endregion

        #region Methods

        /// <summary>
        /// ReplayDataを作成します
        /// </summary>
        /// <returns>生成されたリプレイデータ</returns>
        public ReplayData CreateReplayData()
        {
            var replay = new ReplayData
            {
                ReplayId = _replayId,
                RecordedAt = DateTime.UtcNow.ToString("o"),
                MatchDuration = _duration,
                MapName = _matchResult?.MapName ?? "Training Grounds",
                GameMode = _matchResult?.GameMode ?? "Arena",
                PlayerName = "You"
            };

            if (_matchResult != null)
            {
                replay.FinalPlayerScore = _matchResult.PlayerScore;
                replay.FinalEnemyScore = _matchResult.EnemyScore;
            }

            // モックフレームを追加（再生テスト用）
            int frameCount = Mathf.CeilToInt(_duration);
            for (int i = 0; i <= frameCount; i++)
            {
                replay.AddFrame(new ReplayFrame
                {
                    Timestamp = i
                });
            }

            // モックイベントを追加
            replay.AddEvent(new ReplayEvent
            {
                Timestamp = _duration * 0.1f,
                EventType = ReplayEventType.Score,
                SubjectEntityId = "player-local",
                Description = "Headshot! +100"
            });

            replay.AddEvent(new ReplayEvent
            {
                Timestamp = _duration * 0.25f,
                EventType = ReplayEventType.Death,
                SubjectEntityId = "enemy-1",
                TargetEntityId = "player-local",
                Description = "First Blood!"
            });

            replay.AddEvent(new ReplayEvent
            {
                Timestamp = _duration * 0.5f,
                EventType = ReplayEventType.Score,
                SubjectEntityId = "player-local",
                Description = "Heart Shot! +200"
            });

            // ハイライトを生成
            replay.GenerateHighlightsFromScoreEvents();

            return replay;
        }

        #endregion
    }

    /// <summary>
    /// モックマッチ設定（進行中のマッチ用）
    /// </summary>
    /// <remarks>
    /// マッチ画面のテスト用モックデータを提供します。
    /// </remarks>
    [CreateAssetMenu(fileName = "MockMatch", menuName = "CavalryFight/Mock/Match Config")]
    public class MockMatchConfig : ScriptableObject
    {
        #region Serialized Fields

        [Header("Match Settings")]
        /// <summary>ゲームモード名</summary>
        [SerializeField] private string _gameMode = "Arena";

        /// <summary>制限時間（秒）</summary>
        [SerializeField] private float _timeLimit = 300f;

        /// <summary>プレイヤーあたりの矢数</summary>
        [SerializeField] private int _arrowsPerPlayer = 30;

        /// <summary>チームモードかどうか</summary>
        [SerializeField] private bool _isTeamMode = false;

        [Header("Initial State")]
        /// <summary>ローカルプレイヤーの初期スコア</summary>
        [SerializeField] private int _localPlayerScore = 0;

        /// <summary>チーム0の初期スコア</summary>
        [SerializeField] private int _team0Score = 0;

        /// <summary>チーム1の初期スコア</summary>
        [SerializeField] private int _team1Score = 0;

        /// <summary>初期残り時間（秒）</summary>
        [SerializeField] private float _remainingTime = 300f;

        #endregion

        #region Properties

        /// <summary>
        /// ゲームモード
        /// </summary>
        public string GameMode => _gameMode;

        /// <summary>
        /// 制限時間
        /// </summary>
        public float TimeLimit => _timeLimit;

        /// <summary>
        /// プレイヤーあたりの矢数
        /// </summary>
        public int ArrowsPerPlayer => _arrowsPerPlayer;

        /// <summary>
        /// チームモードかどうか
        /// </summary>
        public bool IsTeamMode => _isTeamMode;

        /// <summary>
        /// ローカルプレイヤーのスコア
        /// </summary>
        public int LocalPlayerScore => _localPlayerScore;

        /// <summary>
        /// チーム0のスコア
        /// </summary>
        public int Team0Score => _team0Score;

        /// <summary>
        /// チーム1のスコア
        /// </summary>
        public int Team1Score => _team1Score;

        /// <summary>
        /// 残り時間
        /// </summary>
        public float RemainingTime => _remainingTime;

        #endregion
    }

    /// <summary>
    /// モックハンティング設定
    /// </summary>
    /// <remarks>
    /// ハンティング画面のテスト用モックデータを提供します。
    /// </remarks>
    [CreateAssetMenu(fileName = "MockHunting", menuName = "CavalryFight/Mock/Hunting Config")]
    public class MockHuntingConfig : ScriptableObject
    {
        #region Serialized Fields

        [Header("Hunting Settings")]
        /// <summary>ローカルプレイヤーがハンター役かどうか</summary>
        [SerializeField] private bool _isLocalPlayerHunter = true;

        /// <summary>ローカルプレイヤーのチームインデックス</summary>
        [SerializeField] private int _localPlayerTeamIndex = 0;

        /// <summary>制限時間（秒）</summary>
        [SerializeField] private float _timeLimit = 300f;

        [Header("Initial State")]
        /// <summary>チーム0の初期スコア</summary>
        [SerializeField] private int _team0Score = 0;

        /// <summary>チーム1の初期スコア</summary>
        [SerializeField] private int _team1Score = 0;

        /// <summary>初期残り時間（秒）</summary>
        [SerializeField] private float _remainingTime = 300f;

        #endregion

        #region Properties

        /// <summary>
        /// ローカルプレイヤーがハンターかどうか
        /// </summary>
        public bool IsLocalPlayerHunter => _isLocalPlayerHunter;

        /// <summary>
        /// ローカルプレイヤーのチームインデックス
        /// </summary>
        public int LocalPlayerTeamIndex => _localPlayerTeamIndex;

        /// <summary>
        /// 制限時間
        /// </summary>
        public float TimeLimit => _timeLimit;

        /// <summary>
        /// チーム0のスコア
        /// </summary>
        public int Team0Score => _team0Score;

        /// <summary>
        /// チーム1のスコア
        /// </summary>
        public int Team1Score => _team1Score;

        /// <summary>
        /// 残り時間
        /// </summary>
        public float RemainingTime => _remainingTime;

        #endregion
    }

    /// <summary>
    /// モックトレーニング設定
    /// </summary>
    /// <remarks>
    /// トレーニング画面のテスト用モックデータを提供します。
    /// </remarks>
    [CreateAssetMenu(fileName = "MockTraining", menuName = "CavalryFight/Mock/Training Config")]
    public class MockTrainingConfig : ScriptableObject
    {
        #region Serialized Fields

        [Header("Training Settings")]
        /// <summary>ターゲットの数</summary>
        [SerializeField] private int _targetCount = 5;

        /// <summary>セッションの持続時間（秒）、0は無制限</summary>
        [SerializeField] private float _sessionDuration = 0f;

        /// <summary>ヒントを表示するかどうか</summary>
        [SerializeField] private bool _showHints = true;

        #endregion

        #region Properties

        /// <summary>
        /// ターゲット数
        /// </summary>
        public int TargetCount => _targetCount;

        /// <summary>
        /// セッション時間（秒）、0は無制限
        /// </summary>
        public float SessionDuration => _sessionDuration;

        /// <summary>
        /// ヒントを表示するかどうか
        /// </summary>
        public bool ShowHints => _showHints;

        #endregion
    }
}

#endif
