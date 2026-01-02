#nullable enable

using UnityEngine;

namespace CavalryFight.Development.MockData
{
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
