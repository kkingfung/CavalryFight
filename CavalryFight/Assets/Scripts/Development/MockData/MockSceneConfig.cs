#nullable enable

using UnityEngine;

namespace CavalryFight.Development.MockData
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

        [Header("Lobby Scene")]
        [Tooltip("ロビー画面で使用するモック設定")]
        /// <summary>ロビー画面で使用するモック設定</summary>
        [SerializeField] private MockLobbyConfig? _lobbySceneMockData;

        [Header("Room Scene")]
        [Tooltip("ルーム画面で使用するモック設定")]
        /// <summary>ルーム画面で使用するモック設定</summary>
        [SerializeField] private MockRoomConfig? _roomSceneMockData;

        [Header("History Scene")]
        [Tooltip("履歴画面で使用するモック設定")]
        /// <summary>履歴画面で使用するモック設定</summary>
        [SerializeField] private MockHistoryConfig? _historySceneMockData;

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
        /// ロビー画面のモックデータ
        /// </summary>
        public MockLobbyConfig? LobbySceneMockData => _lobbySceneMockData;

        /// <summary>
        /// ルーム画面のモックデータ
        /// </summary>
        public MockRoomConfig? RoomSceneMockData => _roomSceneMockData;

        /// <summary>
        /// 履歴画面のモックデータ
        /// </summary>
        public MockHistoryConfig? HistorySceneMockData => _historySceneMockData;

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
}
