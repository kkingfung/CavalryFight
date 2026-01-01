#nullable enable

using System;
using CavalryFight.Gameplay.Match;
using Unity.Netcode;
using UnityEngine;

namespace CavalryFight.Gameplay.Hunting
{
    /// <summary>
    /// ウルフプレイヤーのコントローラー
    /// </summary>
    /// <remarks>
    /// ハンティングモードでウルフ役のプレイヤーを制御します。
    /// ウルフはハンターをスタンさせることができます。
    /// </remarks>
    public class WolfController : NetworkBehaviour
    {
        #region Serialized Fields

        [Header("Movement")]
        [SerializeField] private float _moveSpeed = 8f;
        [SerializeField] private float _rotationSpeed = 360f;
        [SerializeField] private float _acceleration = 10f;

        [Header("Attack")]
        [SerializeField] private float _attackRange = 2f;
        [SerializeField] private float _attackCooldown = 1f;
        [SerializeField] private float _stunDuration = 3f;

        [Header("References")]
        [SerializeField] private Animator? _animator;
        [SerializeField] private Collider? _attackCollider;
        [SerializeField] private Renderer? _meshRenderer;

        [Header("Audio")]
        [SerializeField] private AudioClip? _attackSound;
        [SerializeField] private AudioClip? _hitSound;
        [SerializeField] private AudioSource? _audioSource;

        #endregion

        #region Network Variables

        private NetworkVariable<int> _teamIndex = new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        private NetworkVariable<bool> _isAttacking = new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        #endregion

        #region Private Fields

        private Vector3 _currentVelocity;
        private float _attackCooldownTimer;
        private HuntingRulesHandler? _huntingHandler;

        // アニメーターパラメータハッシュ
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int AttackHash = Animator.StringToHash("Attack");

        #endregion

        #region Properties

        /// <summary>
        /// チームインデックス
        /// </summary>
        public int TeamIndex => _teamIndex.Value;

        /// <summary>
        /// 攻撃中かどうか
        /// </summary>
        public bool IsAttacking => _isAttacking.Value;

        /// <summary>
        /// 攻撃可能かどうか
        /// </summary>
        public bool CanAttack => _attackCooldownTimer <= 0 && !_isAttacking.Value;

        #endregion

        #region Events

        /// <summary>
        /// ハンターをスタンさせた時に発生します
        /// </summary>
        public event Action<ulong>? HunterStunned;

        #endregion

        #region Unity Lifecycle

        private void Update()
        {
            if (!IsOwner)
            {
                return;
            }

            // クールダウンタイマーを更新
            if (_attackCooldownTimer > 0)
            {
                _attackCooldownTimer -= Time.deltaTime;
            }

            // 入力処理
            HandleInput();
        }

        private void FixedUpdate()
        {
            if (!IsOwner)
            {
                return;
            }

            // 移動処理
            HandleMovement();
        }

        #endregion

        #region Network Spawn

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            // ハンティングルールハンドラーを取得
            if (MatchManager.Instance?.ActiveHandler is HuntingRulesHandler handler)
            {
                _huntingHandler = handler;
            }

            // チームマテリアルを適用
            if (_meshRenderer != null && _huntingHandler != null)
            {
                var material = _huntingHandler.GetWolfMaterialForTeam(_teamIndex.Value);
                if (material != null)
                {
                    _meshRenderer.material = material;
                }
            }

            Debug.Log($"[WolfController] Spawned. Team: {_teamIndex.Value}, IsOwner: {IsOwner}");
        }

        #endregion

        #region Server Methods

        /// <summary>
        /// チームを設定します（サーバーのみ）
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void SetTeamServerRpc(int teamIndex)
        {
            _teamIndex.Value = teamIndex;
        }

        /// <summary>
        /// 攻撃を実行します（サーバーのみ）
        /// </summary>
        [ServerRpc]
        private void PerformAttackServerRpc()
        {
            if (!CanAttack)
            {
                return;
            }

            _isAttacking.Value = true;
            _attackCooldownTimer = _attackCooldown;

            // 攻撃判定
            CheckAttackHit();

            // 攻撃終了を予約
            Invoke(nameof(EndAttack), 0.5f);

            // クライアントに通知
            PlayAttackAnimationClientRpc();
        }

        private void EndAttack()
        {
            _isAttacking.Value = false;
        }

        private void CheckAttackHit()
        {
            // 攻撃範囲内のプレイヤーを検出
            var colliders = Physics.OverlapSphere(transform.position, _attackRange);

            foreach (var col in colliders)
            {
                // ハンターかどうかをチェック
                var hunterStunHandler = col.GetComponent<HunterStunHandler>();
                if (hunterStunHandler != null && hunterStunHandler.TeamIndex != _teamIndex.Value)
                {
                    // 相手チームのハンターにヒット
                    hunterStunHandler.ApplyStunServerRpc(_stunDuration);
                    HunterStunned?.Invoke(hunterStunHandler.OwnerClientId);

                    // ヒット音を再生
                    PlayHitSoundClientRpc();

                    Debug.Log($"[WolfController] Stunned hunter {hunterStunHandler.OwnerClientId}");
                    break;
                }
            }
        }

        #endregion

        #region Client RPCs

        [ClientRpc]
        private void PlayAttackAnimationClientRpc()
        {
            if (_animator != null)
            {
                _animator.SetTrigger(AttackHash);
            }

            if (_audioSource != null && _attackSound != null)
            {
                _audioSource.PlayOneShot(_attackSound);
            }
        }

        [ClientRpc]
        private void PlayHitSoundClientRpc()
        {
            if (_audioSource != null && _hitSound != null)
            {
                _audioSource.PlayOneShot(_hitSound);
            }
        }

        #endregion

        #region Input Handling

        private void HandleInput()
        {
            // 攻撃入力
            if (Input.GetButtonDown("Fire1") && CanAttack)
            {
                PerformAttackServerRpc();
            }
        }

        #endregion

        #region Movement

        private void HandleMovement()
        {
            // 入力取得
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");

            Vector3 inputDirection = new Vector3(horizontal, 0f, vertical).normalized;

            if (inputDirection.magnitude >= 0.1f)
            {
                // 目標速度を計算
                Vector3 targetVelocity = inputDirection * _moveSpeed;

                // 滑らかに加速
                _currentVelocity = Vector3.Lerp(_currentVelocity, targetVelocity, _acceleration * Time.fixedDeltaTime);

                // 移動
                transform.position += _currentVelocity * Time.fixedDeltaTime;

                // 回転
                Quaternion targetRotation = Quaternion.LookRotation(inputDirection);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    targetRotation,
                    _rotationSpeed * Time.fixedDeltaTime
                );
            }
            else
            {
                // 減速
                _currentVelocity = Vector3.Lerp(_currentVelocity, Vector3.zero, _acceleration * Time.fixedDeltaTime);
            }

            // アニメーター更新
            if (_animator != null)
            {
                _animator.SetFloat(SpeedHash, _currentVelocity.magnitude / _moveSpeed);
            }
        }

        #endregion

        #region Gizmos

        private void OnDrawGizmosSelected()
        {
            // 攻撃範囲を表示
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, _attackRange);
        }

        #endregion
    }
}
