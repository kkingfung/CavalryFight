#nullable enable

using UnityEngine;

namespace CavalryFight.Development.MockData
{
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
}
