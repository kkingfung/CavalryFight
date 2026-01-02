#nullable enable

using UnityEngine;

namespace CavalryFight.Development.MockData
{
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
}
