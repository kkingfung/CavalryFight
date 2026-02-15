#nullable enable

using System.Collections.Generic;
using UnityEngine;
using CavalryFight.Core.Services;
using CavalryFight.Gameplay.Training;
using CavalryFight.Gameplay.Match;
using CavalryFight.Services.Combat;
using CavalryFight.Services.Training;
using CavalryFight.Services.AI;
using MalbersAnimations;
using Unity.Netcode;

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

        // 発射者のGameObject（スコア通知用）
        private GameObject? _ownerObject;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _spawnTime = Time.time;
        }

        private void Start()
        {
            // ArrowTrackerServiceに登録
            ServiceLocator.Instance.Get<IArrowTrackerService>()?.RegisterArrow(transform);

            // 寿命後に自動破壊をスケジュール（バックアップ）
            Destroy(gameObject, _lifetime);
        }

        private void OnDestroy()
        {
            // ArrowTrackerServiceから解除
            ServiceLocator.Instance.Get<IArrowTrackerService>()?.UnregisterArrow(transform);
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

            // Malbersのゾーン（Zone Jump, Zone Stance等）を無視
            // これらはAIの制御用トリガーで、実際のヒット対象ではない
            if (other.gameObject.name.StartsWith("Zone ") ||
                other.gameObject.layer == LayerMask.NameToLayer("Ignore Raycast"))
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
            }
            else
            {
                // AIPlayerControllerを持つオブジェクトにダメージを与える
                var aiController = hitObject.GetComponent<AIPlayerController>();
                if (aiController == null)
                {
                    aiController = hitObject.GetComponentInParent<AIPlayerController>();
                }

                // AIマウントに当たった場合、ルートオブジェクトの子階層からAIPlayerControllerを探す
                // （AIRiderはAIMountの子として配置されているため）
                if (aiController == null)
                {
                    Transform root = hitObject.transform.root;
                    aiController = root.GetComponentInChildren<AIPlayerController>();
                }

                if (aiController != null)
                {
                    // AIにダメージを与える（attackerとして発射者を渡す、なければ矢自身）
                    int damage = Mathf.RoundToInt(Damage);
                    aiController.TakeDamage(damage, _ownerObject ?? gameObject);

                    // ヒット部位を検出
                    HitLocation hitLocation = DetectHitLocation(hitObject);

                    // スコアを通知（MatchManagerへ）
                    NotifyScore(hitLocation, hitPoint);
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
                            float damageToApply = Damage;

                            // ゲームモードで死亡が許可されていない場合、致命的なダメージを防ぐ
                            var matchManager = Match.MatchManager.Instance;
                            bool canDie = matchManager?.ActiveHandler?.CanPlayersDie ?? true;

                            if (!canDie)
                            {
                                // 現在の体力を取得
                                var stats = damageable.GetComponent<Stats>();
                                if (stats != null)
                                {
                                    var healthStat = stats.Stat_Get(healthStatID);
                                    if (healthStat != null)
                                    {
                                        float currentHealth = healthStat.Value;
                                        // ダメージが体力を下回る場合、体力を1残すようにダメージを調整
                                        if (damageToApply >= currentHealth)
                                        {
                                            damageToApply = Mathf.Max(0, currentHealth - 1f);
                                            Debug.Log($"[ArrowProjectile] Capped damage to {damageToApply} to prevent death in {matchManager?.CurrentGameMode} mode (current health: {currentHealth})");
                                        }
                                    }
                                }
                            }

                            // MDamageableにダメージを与える
                            damageable.ReceiveDamage(damageDirection, gameObject, healthStatID, damageToApply, false, null, false);

                            // ヒット部位を検出
                            HitLocation hitLocation = DetectHitLocation(hitObject);

                            // スコアを通知（MatchManagerへ）
                            NotifyScore(hitLocation, hitPoint);
                        }
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

            // Colliderを無効化して物理的な衝突を防ぐ（プレイヤー/AIが矢に押されるのを防ぐ）
            var colliders = GetComponents<Collider>();
            foreach (var collider in colliders)
            {
                collider.enabled = false;
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
        /// <param name="isOwner">発射者本人の場合true（スコア通知用）</param>
        public void AddIgnoredObject(GameObject? obj, bool isOwner = false)
        {
            if (obj != null && !_ignoredObjects.Contains(obj))
            {
                _ignoredObjects.Add(obj);
                if (isOwner)
                {
                    _ownerObject = obj;
                }
            }
        }

        /// <summary>
        /// 発射者を設定します（発射者との衝突を無視するため）
        /// </summary>
        /// <param name="owner">発射者のGameObject（プレイヤーや馬のルートオブジェクト）</param>
        [System.Obsolete("Use AddIgnoredObject instead")]
        public void SetOwner(GameObject? owner)
        {
            AddIgnoredObject(owner, isOwner: true);
        }

        /// <summary>
        /// スコアをMatchManagerに通知します
        /// </summary>
        /// <param name="hitLocation">命中部位</param>
        /// <param name="hitPosition">ヒット位置（ワールド座標）</param>
        private void NotifyScore(HitLocation hitLocation, Vector3 hitPosition)
        {
            if (MatchManager.Instance == null)
            {
                Debug.LogWarning($"[ArrowProjectile] MatchManager.Instance is NULL!");
                return;
            }

            // オーナーのクライアントIDを取得
            ulong clientId = 0;
            if (_ownerObject != null)
            {
                // NetworkObjectから取得（プレイヤーとAIの両方に対応）
                // 注意: ライダーは馬の子なので、ライダー自身にはNetworkObjectがない場合がある
                // その場合は親階層をチェックして馬のNetworkObjectを取得する
                var networkObject = _ownerObject.GetComponent<NetworkObject>();
                if (networkObject == null)
                {
                    // ライダーにない場合、親（馬）から取得
                    networkObject = _ownerObject.GetComponentInParent<NetworkObject>();
                }

                if (networkObject != null && networkObject.IsSpawned)
                {
                    clientId = networkObject.OwnerClientId;
                }
                else if (networkObject != null)
                {
                    Debug.LogWarning($"[ArrowProjectile] NetworkObject NOT spawned! Owner={_ownerObject.name}");
                }
                else
                {
                    Debug.LogWarning($"[ArrowProjectile] NetworkObject NOT FOUND! Owner={_ownerObject.name}");
                }
            }
            else
            {
                Debug.LogWarning($"[ArrowProjectile] _ownerObject is NULL!");
            }

            // ヒット部位に基づいてスコアを計算
            int score = GetScoreForHitLocation(hitLocation);

            // ネットワークモードかローカルモードかで処理を分岐
            if (MatchManager.Instance.IsSpawned)
            {
                // ネットワークモード: RPCを使用
                MatchManager.Instance.AddPlayerScoreRpc(clientId, score, hitLocation, hitPosition);
            }
            else
            {
                // ローカルモード: 直接スコアを追加
                MatchManager.Instance.AddPlayerScoreLocal(clientId, score, hitLocation, hitPosition);
            }
        }

        /// <summary>
        /// ヒット部位に基づいてスコアを計算します
        /// </summary>
        /// <param name="hitLocation">命中部位</param>
        /// <returns>獲得スコア</returns>
        private int GetScoreForHitLocation(HitLocation hitLocation)
        {
            // 基本スコア = ベーススコア * チャージ量
            float baseScore = _baseScore * _chargeAmount;

            // 部位ごとのスコア倍率
            float multiplier = hitLocation switch
            {
                HitLocation.Heart => 3.0f,   // 心臓: 3倍
                HitLocation.Head => 2.0f,    // 頭部: 2倍
                HitLocation.Torso => 1.0f,   // 胴体: 1倍（標準）
                HitLocation.Arm => 0.5f,     // 腕: 0.5倍
                HitLocation.Leg => 0.5f,     // 脚: 0.5倍
                HitLocation.Mount => 0.3f,   // 馬: 0.3倍
                _ => 1.0f                     // その他: 1倍
            };

            return Mathf.RoundToInt(baseScore * multiplier);
        }

        /// <summary>
        /// 衝突したコライダーの名前からヒット部位を判定します
        /// </summary>
        /// <param name="hitObject">衝突したGameObject</param>
        /// <returns>命中部位</returns>
        private HitLocation DetectHitLocation(GameObject hitObject)
        {
            string name = hitObject.name.ToLower();

            // コライダー名に基づいて部位を判定
            if (name.Contains("heart") || name.Contains("chest"))
            {
                return HitLocation.Heart;
            }
            else if (name.Contains("head") || name.Contains("skull"))
            {
                return HitLocation.Head;
            }
            else if (name.Contains("torso") || name.Contains("body") || name.Contains("spine"))
            {
                return HitLocation.Torso;
            }
            else if (name.Contains("arm") || name.Contains("hand") || name.Contains("shoulder"))
            {
                return HitLocation.Arm;
            }
            else if (name.Contains("leg") || name.Contains("foot") || name.Contains("thigh") || name.Contains("calf"))
            {
                return HitLocation.Leg;
            }
            else if (name.Contains("horse") || name.Contains("mount"))
            {
                return HitLocation.Mount;
            }

            // デフォルトは胴体
            return HitLocation.Torso;
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
