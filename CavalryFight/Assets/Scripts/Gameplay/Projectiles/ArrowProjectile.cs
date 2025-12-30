#nullable enable

using UnityEngine;
using CavalryFight.Services.Training;

namespace CavalryFight.Gameplay.Projectiles
{
    /// <summary>
    /// 矢の発射体クラス
    /// </summary>
    /// <remarks>
    /// 物理挙動と衝突検出を管理します。
    /// トレーニングモードではスコアを、戦闘モードではダメージを与えます。
    /// </remarks>
    [RequireComponent(typeof(Rigidbody))]
    public class ArrowProjectile : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Settings")]
        [SerializeField] private float _lifetime = 10f;
        [SerializeField] private float _baseDamage = 10f;
        [SerializeField] private int _baseScore = 10;
        [SerializeField] private bool _stickOnImpact = true;

        [Header("Effects")]
        [SerializeField] private GameObject? _hitEffectPrefab;

        #endregion

        #region Private Fields

        private Rigidbody? _rigidbody;
        private bool _hasHit = false;
        private float _spawnTime;
        private float _chargeAmount = 1.0f; // デフォルトはフルチャージ

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _spawnTime = Time.time;
        }

        private void Update()
        {
            // 寿命チェック
            if (Time.time - _spawnTime >= _lifetime)
            {
                Destroy(gameObject);
                return;
            }

            // 飛行中は進行方向を向く
            if (!_hasHit && _rigidbody != null && _rigidbody.linearVelocity.magnitude > 0.1f)
            {
                transform.rotation = Quaternion.LookRotation(_rigidbody.linearVelocity);
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (_hasHit)
            {
                return;
            }

            HandleHit(collision.gameObject, collision.GetContact(0).point);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_hasHit)
            {
                return;
            }

            HandleHit(other.gameObject, other.ClosestPoint(transform.position));
        }

        #endregion

        #region Hit Handling

        /// <summary>
        /// 衝突処理
        /// </summary>
        /// <param name="hitObject">衝突したGameObject</param>
        /// <param name="hitPoint">衝突地点</param>
        private void HandleHit(GameObject hitObject, Vector3 hitPoint)
        {
            _hasHit = true;

            // ヒットエフェクト再生
            if (_hitEffectPrefab != null)
            {
                Instantiate(_hitEffectPrefab, hitPoint, Quaternion.identity);
            }

            // Blaze AI敵に当たった場合
            BlazeAI? blazeAI = hitObject.GetComponent<BlazeAI>();
            if (blazeAI != null)
            {
                // トレーニングモード: スコアを記録
                // チャージ量に応じてスコアを計算（最大200%）
                int score = Mathf.RoundToInt(_baseScore * _chargeAmount * 2f);

                // Blaze AIにヒット状態をトリガー（敵GameObjectはnullでOK）
                blazeAI.Hit(null, false);

                Debug.Log($"[ArrowProjectile] Hit Blaze AI: {hitObject.name} | Score: {score} | Charge: {_chargeAmount:F2}");

                // TrainingManagerに通知
                TrainingManager.Instance?.RecordHit(score, hitPoint);
            }

            // 刺さるか破壊するか
            if (_stickOnImpact)
            {
                StickToTarget(hitObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// ターゲットに矢を刺します
        /// </summary>
        /// <param name="target">刺さる対象のGameObject</param>
        private void StickToTarget(GameObject target)
        {
            // Rigidbodyを無効化して物理を停止
            if (_rigidbody != null)
            {
                _rigidbody.isKinematic = true;
                _rigidbody.linearVelocity = Vector3.zero;
                _rigidbody.angularVelocity = Vector3.zero;
            }

            // ターゲットの子オブジェクトにする
            transform.SetParent(target.transform);

            // 一定時間後に破壊
            Destroy(gameObject, _lifetime);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 矢の速度を設定します
        /// </summary>
        /// <param name="velocity">速度ベクトル</param>
        public void SetVelocity(Vector3 velocity)
        {
            if (_rigidbody != null)
            {
                _rigidbody.linearVelocity = velocity;
            }
        }

        /// <summary>
        /// チャージ量を設定します
        /// </summary>
        /// <param name="chargeAmount">チャージ量（0.0～1.0）</param>
        public void SetChargeAmount(float chargeAmount)
        {
            _chargeAmount = Mathf.Clamp01(chargeAmount);
        }

        /// <summary>
        /// ダメージ量を取得します
        /// </summary>
        public float Damage => _baseDamage * _chargeAmount;

        /// <summary>
        /// スコアを取得します
        /// </summary>
        public int Score => Mathf.RoundToInt(_baseScore * _chargeAmount * 2f);

        #endregion
    }
}
