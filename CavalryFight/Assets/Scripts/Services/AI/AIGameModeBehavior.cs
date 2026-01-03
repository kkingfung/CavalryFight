#nullable enable

using CavalryFight.Services.Lobby;
using UnityEngine;

namespace CavalryFight.Services.AI
{
    /// <summary>
    /// ゲームモードごとのAI行動設定を定義するScriptableObject
    /// </summary>
    /// <remarks>
    /// 各ゲームモードに応じたAIの戦術や優先順位を設定します。
    /// </remarks>
    [CreateAssetMenu(fileName = "AIGameModeBehavior", menuName = "CavalryFight/AI/Game Mode Behavior")]
    public class AIGameModeBehavior : ScriptableObject
    {
        #region Serialized Fields

        [Header("アリーナモード設定")]
        [SerializeField] private ArenaModeBehavior _arenaBehavior = new ArenaModeBehavior();

        [Header("スコアマッチ設定")]
        [SerializeField] private ScoreMatchBehavior _scoreMatchBehavior = new ScoreMatchBehavior();

        [Header("チームファイト設定")]
        [SerializeField] private TeamFightBehavior _teamFightBehavior = new TeamFightBehavior();

        [Header("デスマッチ設定")]
        [SerializeField] private DeathmatchBehavior _deathmatchBehavior = new DeathmatchBehavior();

        #endregion

        #region Public Methods

        /// <summary>
        /// 指定したゲームモードの行動設定を取得します
        /// </summary>
        public IModeBehavior GetBehavior(GameMode mode)
        {
            return mode switch
            {
                GameMode.Arena => _arenaBehavior,
                GameMode.ScoreMatch => _scoreMatchBehavior,
                GameMode.TeamFight => _teamFightBehavior,
                GameMode.Deathmatch => _deathmatchBehavior,
                _ => _arenaBehavior
            };
        }

        #endregion
    }

    /// <summary>
    /// モード行動インターフェース
    /// </summary>
    public interface IModeBehavior
    {
        /// <summary>ターゲット選択の優先度設定</summary>
        TargetPriority TargetPriority { get; }

        /// <summary>攻撃的か防御的か</summary>
        float AggressionLevel { get; }

        /// <summary>チームメイトと連携するか</summary>
        bool CoordinateWithTeam { get; }

        /// <summary>スコアリング重視か生存重視か</summary>
        float ScoringPriority { get; }

        /// <summary>リスポーンがあるモードか</summary>
        bool CanRespawn { get; }
    }

    /// <summary>
    /// ターゲット選択の優先度
    /// </summary>
    public enum TargetPriority
    {
        /// <summary>最も近い敵</summary>
        Nearest,

        /// <summary>最もスコアの高い敵</summary>
        HighestScore,

        /// <summary>最も弱い敵（低HP）</summary>
        Weakest,

        /// <summary>自分を攻撃した敵</summary>
        Attacker,

        /// <summary>ランダム</summary>
        Random
    }

    /// <summary>
    /// アリーナモードの行動設定
    /// </summary>
    /// <remarks>
    /// - 時間制限内でスコアを稼ぐ
    /// - 死なないのでリスクを取れる
    /// - 高スコアを狙う（ヘッドショット重視）
    /// </remarks>
    [System.Serializable]
    public class ArenaModeBehavior : IModeBehavior
    {
        [SerializeField] private TargetPriority _targetPriority = TargetPriority.Nearest;
        [SerializeField, Range(0f, 1f)] private float _aggressionLevel = 0.8f;
        [SerializeField] private bool _coordinateWithTeam = false;
        [SerializeField, Range(0f, 1f)] private float _scoringPriority = 0.9f;

        public TargetPriority TargetPriority => _targetPriority;
        public float AggressionLevel => _aggressionLevel;
        public bool CoordinateWithTeam => _coordinateWithTeam;
        public float ScoringPriority => _scoringPriority;
        public bool CanRespawn => true; // 死なないモード
    }

    /// <summary>
    /// スコアマッチの行動設定
    /// </summary>
    /// <remarks>
    /// - 限られた矢でスコアを稼ぐ
    /// - 無駄撃ちを避ける
    /// - 確実なショットを優先
    /// </remarks>
    [System.Serializable]
    public class ScoreMatchBehavior : IModeBehavior
    {
        [SerializeField] private TargetPriority _targetPriority = TargetPriority.Nearest;
        [SerializeField, Range(0f, 1f)] private float _aggressionLevel = 0.5f;
        [SerializeField] private bool _coordinateWithTeam = false;
        [SerializeField, Range(0f, 1f)] private float _scoringPriority = 1.0f;

        /// <summary>矢を温存するか</summary>
        [Tooltip("矢を温存するか（高いほど慎重に射撃）")]
        [SerializeField, Range(0f, 1f)] private float _arrowConservation = 0.7f;

        /// <summary>最小チャージ量（これ以下では撃たない）</summary>
        [Tooltip("最小チャージ量")]
        [SerializeField, Range(0f, 1f)] private float _minChargeBeforeFire = 0.6f;

        public TargetPriority TargetPriority => _targetPriority;
        public float AggressionLevel => _aggressionLevel;
        public bool CoordinateWithTeam => _coordinateWithTeam;
        public float ScoringPriority => _scoringPriority;
        public bool CanRespawn => true;

        public float ArrowConservation => _arrowConservation;
        public float MinChargeBeforeFire => _minChargeBeforeFire;
    }

    /// <summary>
    /// チームファイトの行動設定
    /// </summary>
    /// <remarks>
    /// - チームで連携
    /// - 孤立した敵を狙う
    /// - 味方を援護
    /// </remarks>
    [System.Serializable]
    public class TeamFightBehavior : IModeBehavior
    {
        [SerializeField] private TargetPriority _targetPriority = TargetPriority.Weakest;
        [SerializeField, Range(0f, 1f)] private float _aggressionLevel = 0.6f;
        [SerializeField] private bool _coordinateWithTeam = true;
        [SerializeField, Range(0f, 1f)] private float _scoringPriority = 0.7f;

        /// <summary>味方との距離を保つか</summary>
        [Tooltip("味方との理想的な距離")]
        [SerializeField] private float _idealTeamDistance = 15f;

        /// <summary>集団行動を優先するか</summary>
        [Tooltip("集団行動を優先する度合い")]
        [SerializeField, Range(0f, 1f)] private float _groupBehavior = 0.7f;

        /// <summary>弱った敵を優先するか</summary>
        [Tooltip("弱った敵を優先する度合い")]
        [SerializeField, Range(0f, 1f)] private float _focusWeakEnemy = 0.8f;

        public TargetPriority TargetPriority => _targetPriority;
        public float AggressionLevel => _aggressionLevel;
        public bool CoordinateWithTeam => _coordinateWithTeam;
        public float ScoringPriority => _scoringPriority;
        public bool CanRespawn => true;

        public float IdealTeamDistance => _idealTeamDistance;
        public float GroupBehavior => _groupBehavior;
        public float FocusWeakEnemy => _focusWeakEnemy;
    }

    /// <summary>
    /// デスマッチの行動設定
    /// </summary>
    /// <remarks>
    /// - 生存が最優先
    /// - 1対1を避ける
    /// - 慎重な立ち回り
    /// </remarks>
    [System.Serializable]
    public class DeathmatchBehavior : IModeBehavior
    {
        [SerializeField] private TargetPriority _targetPriority = TargetPriority.Weakest;
        [SerializeField, Range(0f, 1f)] private float _aggressionLevel = 0.4f;
        [SerializeField] private bool _coordinateWithTeam = false;
        [SerializeField, Range(0f, 1f)] private float _scoringPriority = 0.3f;

        /// <summary>生存優先度</summary>
        [Tooltip("生存優先度（高いほど慎重）")]
        [SerializeField, Range(0f, 1f)] private float _survivalPriority = 0.9f;

        /// <summary>撤退HP閾値</summary>
        [Tooltip("このHP以下で撤退")]
        [SerializeField, Range(0f, 1f)] private float _retreatHealthThreshold = 0.3f;

        /// <summary>複数の敵がいる時の逃走確率</summary>
        [Tooltip("複数の敵がいる時の逃走確率")]
        [SerializeField, Range(0f, 1f)] private float _fleeFromMultipleEnemies = 0.7f;

        public TargetPriority TargetPriority => _targetPriority;
        public float AggressionLevel => _aggressionLevel;
        public bool CoordinateWithTeam => _coordinateWithTeam;
        public float ScoringPriority => _scoringPriority;
        public bool CanRespawn => false;

        public float SurvivalPriority => _survivalPriority;
        public float RetreatHealthThreshold => _retreatHealthThreshold;
        public float FleeFromMultipleEnemies => _fleeFromMultipleEnemies;
    }
}
