#nullable enable

using System.Collections.Generic;
using UnityEngine;
using CavalryFight.Gameplay.Training;
using CavalryFight.Services.Training;
using MalbersAnimations;

namespace CavalryFight.Gameplay.Projectiles
{
    /// <summary>
    /// 矢の発射体クラス
    /// </summary>
    /// <remarks>
    /// 物理挙動と衝突検出を管理します。
    /// MasterStylizedProjectilesのVFXと連携します。
    /// トレーニングモードではスコアを、戦闘モードではダメージを与えます。
    /// Malbers MDamageableを持つ敵にもダメージを与えます。
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

        #endregion

        #region Private Fields

        private Rigidbody? _rigidbody;
        private bool _hasHit = false;
        private float _spawnTime;
        private float _chargeAmount = 1.0f; // デフォルトはフルチャージ

        // VFX設定（外部から設定）
        private GameObject? _hitEffectPrefab;

        // 発射者（自分自身への当たり判定を無視するため）
        private readonly List<GameObject> _ignoredObjects = new();

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _spawnTime = Time.time;
        }

        private void Start()
        {
            // 寿命後に自動破壊をスケジュール（バックアップ）
            Destroy(gameObject, _lifetime);
        }

        private void Update()
        {
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

            // 発射者自身との衝突を無視
            if (IsOwnerOrChild(collision.gameObject))
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

            // 発射者自身との衝突を無視
            if (IsOwnerOrChild(other.gameObject))
            {
                return;
            }

            HandleHit(other.gameObject, other.ClosestPoint(transform.position));
        }

        /// <summary>
        /// オブジェクトが無視対象または無視対象の子かどうかを判定します
        /// </summary>
        private bool IsOwnerOrChild(GameObject obj)
        {
            if (_ignoredObjects.Count == 0)
            {
                return false;
            }

            // 直接一致
            foreach (var ignored in _ignoredObjects)
            {
                if (obj == ignored)
                {
                    return true;
                }
            }

            // 親階層をチェック
            Transform? current = obj.transform.parent;
            while (current != null)
            {
                foreach (var ignored in _ignoredObjects)
                {
                    if (current.gameObject == ignored)
                    {
                        return true;
                    }
                }
                current = current.parent;
            }

            return false;
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

            // トレイルパーティクルを停止
            StopTrailParticles();

            // ヒットエフェクト再生（5秒後に自動破壊）
            if (_hitEffectPrefab != null)
            {
                var hitEffect = Instantiate(_hitEffectPrefab, hitPoint, Quaternion.identity);
                Destroy(hitEffect, 5f);
            }

            // TrainingTarget に当たった場合
            TrainingTarget? trainingTarget = hitObject.GetComponent<TrainingTarget>();
            if (trainingTarget == null)
            {
                // 親オブジェクトもチェック
                trainingTarget = hitObject.GetComponentInParent<TrainingTarget>();
            }

            if (trainingTarget != null && trainingTarget.IsActive)
            {
                // TrainingTargetが自身でスコア計算とTrainingManager通知を行う
                trainingTarget.OnHit(hitPoint, _chargeAmount);
                Debug.Log($"[ArrowProjectile] Hit TrainingTarget: {hitObject.name} | Charge: {_chargeAmount:F2}");
            }
            else
            {
                // Malbers MDamageableを持つオブジェクトにダメージを与える
                var damageable = hitObject.GetComponent<MDamageable>();
                if (damageable == null)
                {
                    damageable = hitObject.GetComponentInParent<MDamageable>();
                }

                if (damageable != null)
                {
                    // ダメージ方向を計算（矢の飛行方向）
                    Vector3 damageDirection = _rigidbody != null ? _rigidbody.linearVelocity.normalized : transform.forward;

                    // Health StatIDを取得
                    var healthStatID = MTools.GetInstance<StatID>("Health");

                    if (healthStatID != null)
                    {
                        // MDamageableにダメージを与える
                        damageable.ReceiveDamage(damageDirection, gameObject, healthStatID, Damage, false, null, false);
                    }
                }
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
                // 先に速度を0にしてからkinematicにする（順序が重要）
                _rigidbody.linearVelocity = Vector3.zero;
                _rigidbody.angularVelocity = Vector3.zero;
                _rigidbody.isKinematic = true;
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

        /// <summary>
        /// ヒットエフェクトプレハブを設定します
        /// </summary>
        /// <param name="hitEffectPrefab">ヒットエフェクトプレハブ</param>
        public void SetHitEffectPrefab(GameObject? hitEffectPrefab)
        {
            _hitEffectPrefab = hitEffectPrefab;
        }

        /// <summary>
        /// 無視対象を追加します（発射者や馬との衝突を無視するため）
        /// </summary>
        /// <param name="obj">無視対象のGameObject</param>
        public void AddIgnoredObject(GameObject? obj)
        {
            if (obj != null && !_ignoredObjects.Contains(obj))
            {
                _ignoredObjects.Add(obj);
            }
        }

        /// <summary>
        /// 発射者を設定します（発射者との衝突を無視するため）
        /// </summary>
        /// <param name="owner">発射者のGameObject（プレイヤーや馬のルートオブジェクト）</param>
        [System.Obsolete("Use AddIgnoredObject instead")]
        public void SetOwner(GameObject? owner)
        {
            AddIgnoredObject(owner);
        }

        #endregion

        #region VFX

        /// <summary>
        /// パーティクルを停止します
        /// </summary>
        private void StopTrailParticles()
        {
            // 子オブジェクトのすべてのパーティクルを停止
            var allParticles = GetComponentsInChildren<ParticleSystem>();
            foreach (var ps in allParticles)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        #endregion
    }
}
