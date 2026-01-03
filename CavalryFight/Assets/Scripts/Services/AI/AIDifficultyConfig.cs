#nullable enable

using CavalryFight.Services.Lobby;
using UnityEngine;

namespace CavalryFight.Services.AI
{
    /// <summary>
    /// AI難易度ごとの設定を定義するScriptableObject
    /// </summary>
    /// <remarks>
    /// BlazeAIのパラメータを難易度に応じて調整します。
    /// Assets/Settings/AIDifficultyConfig.asset として保存してください。
    /// </remarks>
    [CreateAssetMenu(fileName = "AIDifficultyConfig", menuName = "CavalryFight/AI/Difficulty Config")]
    public class AIDifficultyConfig : ScriptableObject
    {
        #region Serialized Fields

        [Header("難易度別設定")]
        [SerializeField] private DifficultySettings _easySettings = new DifficultySettings
        {
            ReactionTime = 1.5f,
            AimAccuracy = 0.3f,
            AttackInterval = new Vector2(3f, 5f),
            VisionRange = 15f,
            VisionAngle = 60f,
            MoveSpeed = 3f,
            TurnSpeed = 3f,
            ChargeTimeMultiplier = 0.5f,
            MissChance = 0.4f,
            StrafeChance = 0.2f
        };

        [SerializeField] private DifficultySettings _normalSettings = new DifficultySettings
        {
            ReactionTime = 1.0f,
            AimAccuracy = 0.5f,
            AttackInterval = new Vector2(2f, 4f),
            VisionRange = 20f,
            VisionAngle = 80f,
            MoveSpeed = 4f,
            TurnSpeed = 4f,
            ChargeTimeMultiplier = 0.7f,
            MissChance = 0.25f,
            StrafeChance = 0.4f
        };

        [SerializeField] private DifficultySettings _hardSettings = new DifficultySettings
        {
            ReactionTime = 0.5f,
            AimAccuracy = 0.75f,
            AttackInterval = new Vector2(1f, 3f),
            VisionRange = 25f,
            VisionAngle = 100f,
            MoveSpeed = 5f,
            TurnSpeed = 5f,
            ChargeTimeMultiplier = 0.85f,
            MissChance = 0.1f,
            StrafeChance = 0.6f
        };

        [SerializeField] private DifficultySettings _expertSettings = new DifficultySettings
        {
            ReactionTime = 0.2f,
            AimAccuracy = 0.95f,
            AttackInterval = new Vector2(0.5f, 2f),
            VisionRange = 30f,
            VisionAngle = 120f,
            MoveSpeed = 6f,
            TurnSpeed = 6f,
            ChargeTimeMultiplier = 1.0f,
            MissChance = 0.02f,
            StrafeChance = 0.8f
        };

        #endregion

        #region Public Methods

        /// <summary>
        /// 指定した難易度の設定を取得します
        /// </summary>
        /// <param name="difficulty">AI難易度</param>
        /// <returns>難易度設定</returns>
        public DifficultySettings GetSettings(AIDifficulty difficulty)
        {
            return difficulty switch
            {
                AIDifficulty.Easy => _easySettings,
                AIDifficulty.Normal => _normalSettings,
                AIDifficulty.Hard => _hardSettings,
                AIDifficulty.Expert => _expertSettings,
                _ => _normalSettings
            };
        }

        #endregion
    }

    /// <summary>
    /// 難易度ごとの詳細設定
    /// </summary>
    [System.Serializable]
    public struct DifficultySettings
    {
        #region Fields

        [Header("反応・精度")]
        [Tooltip("敵を発見してから行動を開始するまでの時間（秒）")]
        [Range(0.1f, 3f)]
        public float ReactionTime;

        [Tooltip("照準の精度（0.0～1.0）")]
        [Range(0f, 1f)]
        public float AimAccuracy;

        [Tooltip("攻撃間隔の範囲（秒）")]
        public Vector2 AttackInterval;

        [Header("視界")]
        [Tooltip("視界範囲（メートル）")]
        [Range(5f, 50f)]
        public float VisionRange;

        [Tooltip("視野角（度）")]
        [Range(30f, 180f)]
        public float VisionAngle;

        [Header("移動")]
        [Tooltip("移動速度")]
        [Range(1f, 10f)]
        public float MoveSpeed;

        [Tooltip("旋回速度")]
        [Range(1f, 10f)]
        public float TurnSpeed;

        [Header("攻撃")]
        [Tooltip("チャージ時間の倍率（1.0で最大チャージ）")]
        [Range(0.1f, 1f)]
        public float ChargeTimeMultiplier;

        [Tooltip("ミス確率（0.0～1.0）")]
        [Range(0f, 1f)]
        public float MissChance;

        [Header("行動パターン")]
        [Tooltip("ストレイフ（横移動）を行う確率")]
        [Range(0f, 1f)]
        public float StrafeChance;

        #endregion
    }
}
