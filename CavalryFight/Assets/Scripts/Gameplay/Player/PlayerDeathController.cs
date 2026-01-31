#nullable enable

using UnityEngine;
using MalbersAnimations;
using CavalryFight.Gameplay.Match;

namespace CavalryFight.Gameplay.Player
{
    /// <summary>
    /// プレイヤーの死亡を制御し、ゲームモードのルールに従って死亡を許可または防止します
    /// </summary>
    /// <remarks>
    /// Malbers MDamageableと連携して、死亡が許可されていないゲームモード（Arena, ScoreMatch, TeamFight, Hunting）
    /// では死亡を防ぎ、代わりに体力を回復します。
    /// </remarks>
    [RequireComponent(typeof(MDamageable))]
    public class PlayerDeathController : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Settings")]
        [Tooltip("致命的なダメージを受けた後の一時的な無敵時間（秒）")]
        [SerializeField] private float _invulnerabilityDuration = 2f;

        [Tooltip("体力チェックの頻度（秒）")]
        [SerializeField] private float _healthCheckInterval = 0.1f;

        #endregion

        #region Private Fields

        private MDamageable? _damageable;
        private Stats? _stats;
        private StatID? _healthStatID;
        private bool _isInvulnerable;
        private float _invulnerabilityEndTime;
        private float _nextHealthCheckTime;
        private bool _wasAlive = true;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _damageable = GetComponent<MDamageable>();
            _stats = GetComponent<Stats>();

            // Health StatIDを取得
            _healthStatID = MTools.GetInstance<StatID>("Health");
        }

        private void Update()
        {
            // 無敵時間の管理
            if (_isInvulnerable && Time.time >= _invulnerabilityEndTime)
            {
                _isInvulnerable = false;
            }

            // 定期的に体力をチェック
            if (Time.time >= _nextHealthCheckTime)
            {
                _nextHealthCheckTime = Time.time + _healthCheckInterval;
                CheckHealthStatus();
            }
        }

        #endregion

        #region Health Monitoring

        /// <summary>
        /// 体力状態をチェックします
        /// </summary>
        private void CheckHealthStatus()
        {
            if (_stats == null || _healthStatID == null)
            {
                return;
            }

            var healthStat = _stats.Stat_Get(_healthStatID);
            if (healthStat == null)
            {
                return;
            }

            // 体力が0以下になったかチェック
            if (healthStat.Value <= 0 && _wasAlive)
            {
                _wasAlive = false;
                HandleLethalDamage();
            }
            else if (healthStat.Value > 0)
            {
                _wasAlive = true;
            }
        }

        /// <summary>
        /// 致命的なダメージを受けたときの処理
        /// </summary>
        private void HandleLethalDamage()
        {
            if (_stats == null || _healthStatID == null)
            {
                return;
            }

            // ゲームモードで死亡が許可されているかチェック
            var matchManager = MatchManager.Instance;
            bool canDie = matchManager?.ActiveHandler?.CanPlayersDie ?? true;

            if (!canDie)
            {
                // 死亡が許可されていないゲームモード（Arena, ScoreMatch, TeamFight, Hunting）
                // 体力を最大値に回復
                var healthStat = _stats.Stat_Get(_healthStatID);
                if (healthStat != null)
                {
                    healthStat.Value = healthStat.MaxValue;
                }

                // 一時的な無敵時間を付与
                _isInvulnerable = true;
                _invulnerabilityEndTime = Time.time + _invulnerabilityDuration;

                Debug.Log($"[PlayerDeathController] Player took lethal damage but cannot die in {matchManager?.CurrentGameMode} mode. Respawning with full health.");
            }
            else
            {
                // 死亡が許可されているゲームモード（Deathmatch）
                // Malbersの通常の死亡処理を継続
                Debug.Log($"[PlayerDeathController] Player died in {matchManager?.CurrentGameMode} mode.");
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 現在無敵状態かどうかを取得します
        /// </summary>
        public bool IsInvulnerable => _isInvulnerable;

        #endregion
    }
}
