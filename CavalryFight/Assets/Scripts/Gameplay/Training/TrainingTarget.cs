#nullable enable

using UnityEngine;
using CavalryFight.Core.Services;
using CavalryFight.Services.Audio;
using CavalryFight.Services.Training;

namespace CavalryFight.Gameplay.Training
{
    /// <summary>
    /// トレーニング用ターゲット
    /// </summary>
    /// <remarks>
    /// 矢が命中した際にスコアを記録し、TrainingManagerに通知します。
    /// 動く的として使用する場合は、アニメーションや移動スクリプトを追加してください。
    /// </remarks>
    public class TrainingTarget : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Target Settings")]
        [Tooltip("このターゲットに命中した際の基本スコア")]
        [SerializeField] private int _baseScore = 10;

        [Tooltip("ターゲットの半径（中心に近いほど高スコア）")]
        [SerializeField] private float _targetRadius = 1f;

        [Tooltip("中心命中時のスコア倍率")]
        [SerializeField] private float _bullseyeMultiplier = 2f;

        [Header("Visual Feedback")]
        [Tooltip("命中時に再生するエフェクト")]
        [SerializeField] private GameObject? _hitEffectPrefab;

        [Tooltip("命中時に再生する音")]
        [SerializeField] private AudioClip? _hitSound;

        [Header("Behavior")]
        [Tooltip("命中後に非アクティブにするか")]
        [SerializeField] private bool _deactivateOnHit = false;

        [Tooltip("非アクティブ後の復活時間（0で復活しない）")]
        [SerializeField] private float _respawnTime = 3f;

        #endregion

        #region Private Fields

        private IAudioService? _audioService;
        private Collider? _collider;
        private bool _isActive = true;

        #endregion

        #region Properties

        /// <summary>
        /// ターゲットがアクティブかどうかを取得します
        /// </summary>
        public bool IsActive => _isActive;

        /// <summary>
        /// 基本スコアを取得します
        /// </summary>
        public int BaseScore => _baseScore;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _collider = GetComponent<Collider>();
        }

        private void Start()
        {
            // AudioServiceを取得
            _audioService = ServiceLocator.Instance.Get<IAudioService>();
            if (_audioService == null)
            {
                Debug.LogWarning("[TrainingTarget] AudioService not found!");
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// ターゲットに命中した時の処理
        /// </summary>
        /// <param name="hitPoint">命中位置</param>
        /// <param name="chargeAmount">チャージ量（0.0～1.0）</param>
        /// <returns>獲得スコア</returns>
        public int OnHit(Vector3 hitPoint, float chargeAmount = 1f)
        {
            if (!_isActive)
            {
                return 0;
            }

            // スコア計算
            int score = CalculateScore(hitPoint, chargeAmount);

            // エフェクト再生
            PlayHitEffect(hitPoint);

            // 音再生
            PlayHitSound();

            // TrainingManagerに通知
            TrainingManager.Instance?.RecordHit(score, hitPoint);

            Debug.Log($"[TrainingTarget] Hit! Score: {score}, Position: {hitPoint}, Charge: {chargeAmount:F2}");

            // 非アクティブ処理
            if (_deactivateOnHit)
            {
                Deactivate();
            }

            return score;
        }

        /// <summary>
        /// ターゲットを非アクティブにします
        /// </summary>
        public void Deactivate()
        {
            _isActive = false;

            if (_collider != null)
            {
                _collider.enabled = false;
            }

            // 視覚的に非表示にする（オプション）
            // gameObject.SetActive(false);

            // 復活タイマー
            if (_respawnTime > 0)
            {
                Invoke(nameof(Respawn), _respawnTime);
            }

            Debug.Log($"[TrainingTarget] Deactivated: {gameObject.name}");
        }

        /// <summary>
        /// ターゲットを復活させます
        /// </summary>
        public void Respawn()
        {
            _isActive = true;

            if (_collider != null)
            {
                _collider.enabled = true;
            }

            // gameObject.SetActive(true);

            Debug.Log($"[TrainingTarget] Respawned: {gameObject.name}");
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// スコアを計算します
        /// </summary>
        /// <param name="hitPoint">命中位置</param>
        /// <param name="chargeAmount">チャージ量</param>
        /// <returns>計算されたスコア</returns>
        private int CalculateScore(Vector3 hitPoint, float chargeAmount)
        {
            // 中心からの距離を計算
            float distanceFromCenter = Vector3.Distance(transform.position, hitPoint);

            // 距離に基づく倍率（中心に近いほど高い）
            float distanceMultiplier = 1f;
            if (_targetRadius > 0)
            {
                float normalizedDistance = Mathf.Clamp01(distanceFromCenter / _targetRadius);
                distanceMultiplier = Mathf.Lerp(_bullseyeMultiplier, 1f, normalizedDistance);
            }

            // チャージ量による倍率（最大2倍）
            float chargeMultiplier = 1f + chargeAmount;

            // 最終スコア計算
            int finalScore = Mathf.RoundToInt(_baseScore * distanceMultiplier * chargeMultiplier);

            return Mathf.Max(1, finalScore); // 最低1点
        }

        /// <summary>
        /// 命中エフェクトを再生します
        /// </summary>
        /// <param name="hitPoint">命中位置</param>
        private void PlayHitEffect(Vector3 hitPoint)
        {
            if (_hitEffectPrefab != null)
            {
                GameObject effect = Instantiate(_hitEffectPrefab, hitPoint, Quaternion.identity);
                Destroy(effect, 3f); // 3秒後に削除
            }
        }

        /// <summary>
        /// 命中音を再生します
        /// </summary>
        private void PlayHitSound()
        {
            if (_hitSound != null && _audioService != null)
            {
                _audioService.PlaySfxAtPosition(_hitSound, transform.position);
            }
        }

        #endregion

        #region Editor

#if UNITY_EDITOR
        /// <summary>
        /// エディタでのギズモ描画
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            // ターゲット半径を表示
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _targetRadius);

            // ブルズアイ（中心）を表示
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, _targetRadius * 0.2f);
        }
#endif

        #endregion
    }
}
