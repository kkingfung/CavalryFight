#nullable enable

using UnityEngine;

namespace CavalryFight.Services.AI
{
    /// <summary>
    /// AIサービスの設定を保持するScriptableObject
    /// </summary>
    /// <remarks>
    /// AIプレハブや設定アセットへの参照を保持します。
    /// Resources/Settings/AIServiceConfig.assetに配置してください。
    /// </remarks>
    [CreateAssetMenu(fileName = "AIServiceConfig", menuName = "CavalryFight/AI/Service Config")]
    public class AIServiceConfig : ScriptableObject
    {
        #region Prefabs

        [Header("AI Prefabs")]
        [Tooltip("AI騎手のプレハブ")]
        [SerializeField] private GameObject? _aiRiderPrefab;

        [Tooltip("AI馬のプレハブ")]
        [SerializeField] private GameObject? _aiMountPrefab;

        #endregion

        #region Config Assets

        [Header("Configuration")]
        [Tooltip("難易度設定")]
        [SerializeField] private AIDifficultyConfig? _difficultyConfig;

        [Tooltip("ゲームモード行動設定")]
        [SerializeField] private AIGameModeBehavior? _gameModeBehavior;

        #endregion

        #region Properties

        /// <summary>
        /// AI騎手のプレハブ
        /// </summary>
        public GameObject? AIRiderPrefab => _aiRiderPrefab;

        /// <summary>
        /// AI馬のプレハブ
        /// </summary>
        public GameObject? AIMountPrefab => _aiMountPrefab;

        /// <summary>
        /// 難易度設定
        /// </summary>
        public AIDifficultyConfig? DifficultyConfig => _difficultyConfig;

        /// <summary>
        /// ゲームモード行動設定
        /// </summary>
        public AIGameModeBehavior? GameModeBehavior => _gameModeBehavior;

        #endregion

        #region Validation

        /// <summary>
        /// 設定が有効かどうかを確認します
        /// </summary>
        public bool IsValid => _aiRiderPrefab != null && _aiMountPrefab != null;

        /// <summary>
        /// 設定を検証してログを出力します
        /// </summary>
        public void Validate()
        {
            if (_aiRiderPrefab == null)
            {
                Debug.LogWarning("[AIServiceConfig] AI Rider Prefab is not assigned!");
            }

            if (_aiMountPrefab == null)
            {
                Debug.LogWarning("[AIServiceConfig] AI Mount Prefab is not assigned!");
            }

            if (_difficultyConfig == null)
            {
                Debug.LogWarning("[AIServiceConfig] Difficulty Config is not assigned!");
            }

            if (_gameModeBehavior == null)
            {
                Debug.LogWarning("[AIServiceConfig] Game Mode Behavior is not assigned!");
            }
        }

        #endregion
    }
}
