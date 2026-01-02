#nullable enable

using System;
using CavalryFight.Gameplay.Match;
using Unity.Netcode;
using UnityEngine;

namespace CavalryFight.Gameplay.Hunting
{
    /// <summary>
    /// ハンターのスタン処理を管理するコンポーネント
    /// </summary>
    /// <remarks>
    /// ハンティングモードでハンターがウルフや他のハンターからスタンを受けた時の
    /// 処理を行います。スタン中はプレイヤーの移動と攻撃が無効化されます。
    /// </remarks>
    public class HunterStunHandler : NetworkBehaviour
    {
        #region Serialized Fields

        [Header("Visual Effects")]
        [SerializeField] private GameObject? _stunEffectPrefab;
        [SerializeField] private Transform? _stunEffectSpawnPoint;

        [Header("Audio")]
        [SerializeField] private AudioClip? _stunStartSound;
        [SerializeField] private AudioClip? _stunEndSound;
        [SerializeField] private AudioSource? _audioSource;

        [Header("Animation")]
        [SerializeField] private Animator? _animator;

        #endregion

        #region Network Variables

        private NetworkVariable<bool> _isStunned = new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        private NetworkVariable<float> _stunRemainingTime = new NetworkVariable<float>(
            0f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        private NetworkVariable<int> _teamIndex = new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        #endregion

        #region Private Fields

        private GameObject? _activeStunEffect;
        private HuntingRulesHandler? _huntingHandler;

        // アニメーターパラメータハッシュ
        private static readonly int StunnedHash = Animator.StringToHash("Stunned");

        #endregion

        #region Properties

        /// <summary>
        /// スタン中かどうか
        /// </summary>
        public bool IsStunned => _isStunned.Value;

        /// <summary>
        /// スタン残り時間
        /// </summary>
        public float StunRemainingTime => _stunRemainingTime.Value;

        /// <summary>
        /// チームインデックス
        /// </summary>
        public int TeamIndex => _teamIndex.Value;

        #endregion

        #region Events

        /// <summary>
        /// スタン開始時に発生します
        /// </summary>
        public event Action? StunStarted;

        /// <summary>
        /// スタン終了時に発生します
        /// </summary>
        public event Action? StunEnded;

        #endregion

        #region Unity Lifecycle

        private void Update()
        {
            if (!IsServer)
            {
                return;
            }

            // スタンタイマーを更新
            if (_isStunned.Value)
            {
                _stunRemainingTime.Value -= Time.deltaTime;

                if (_stunRemainingTime.Value <= 0)
                {
                    EndStun();
                }
            }
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

            // スタン状態の変更を監視
            _isStunned.OnValueChanged += OnStunStateChanged;

            Debug.Log($"[HunterStunHandler] Spawned. IsOwner: {IsOwner}");
        }

        public override void OnNetworkDespawn()
        {
            _isStunned.OnValueChanged -= OnStunStateChanged;

            // スタンエフェクトを削除
            if (_activeStunEffect != null)
            {
                Destroy(_activeStunEffect);
                _activeStunEffect = null;
            }

            base.OnNetworkDespawn();
        }

        #endregion

        #region Server Methods

        /// <summary>
        /// チームを設定します（サーバーのみ）
        /// </summary>
        [Rpc(SendTo.Server)]
        public void SetTeamRpc(int teamIndex)
        {
            _teamIndex.Value = teamIndex;
        }

        /// <summary>
        /// スタンを適用します（サーバーのみ）
        /// </summary>
        [Rpc(SendTo.Server)]
        public void ApplyStunRpc(float duration)
        {
            ApplyStun(duration);
        }

        /// <summary>
        /// スタンを適用します（内部用）
        /// </summary>
        public void ApplyStun(float duration)
        {
            if (!IsServer)
            {
                return;
            }

            // 既にスタン中の場合は時間を延長
            if (_isStunned.Value)
            {
                _stunRemainingTime.Value = Mathf.Max(_stunRemainingTime.Value, duration);
            }
            else
            {
                _isStunned.Value = true;
                _stunRemainingTime.Value = duration;

                // ハンティングルールハンドラーに通知
                _huntingHandler?.StunPlayer(OwnerClientId, duration);

                Debug.Log($"[HunterStunHandler] Player {OwnerClientId} stunned for {duration}s");
            }

            // クライアントにエフェクト再生を通知
            PlayStunEffectsClientRpc();
        }

        /// <summary>
        /// スタンを終了します
        /// </summary>
        private void EndStun()
        {
            if (!IsServer)
            {
                return;
            }

            _isStunned.Value = false;
            _stunRemainingTime.Value = 0f;

            Debug.Log($"[HunterStunHandler] Player {OwnerClientId} recovered from stun");

            // クライアントにエフェクト終了を通知
            StopStunEffectsClientRpc();
        }

        /// <summary>
        /// スタンを強制終了します
        /// </summary>
        [Rpc(SendTo.Server)]
        public void ForceEndStunRpc()
        {
            EndStun();
        }

        #endregion

        #region Client RPCs

        [ClientRpc]
        private void PlayStunEffectsClientRpc()
        {
            // アニメーション
            if (_animator != null)
            {
                _animator.SetBool(StunnedHash, true);
            }

            // 効果音
            if (_audioSource != null && _stunStartSound != null)
            {
                _audioSource.PlayOneShot(_stunStartSound);
            }

            // ビジュアルエフェクト
            if (_stunEffectPrefab != null)
            {
                var spawnPoint = _stunEffectSpawnPoint != null ? _stunEffectSpawnPoint : transform;
                _activeStunEffect = Instantiate(_stunEffectPrefab, spawnPoint.position, Quaternion.identity, spawnPoint);
            }

            StunStarted?.Invoke();
        }

        [ClientRpc]
        private void StopStunEffectsClientRpc()
        {
            // アニメーション
            if (_animator != null)
            {
                _animator.SetBool(StunnedHash, false);
            }

            // 効果音
            if (_audioSource != null && _stunEndSound != null)
            {
                _audioSource.PlayOneShot(_stunEndSound);
            }

            // ビジュアルエフェクト
            if (_activeStunEffect != null)
            {
                Destroy(_activeStunEffect);
                _activeStunEffect = null;
            }

            StunEnded?.Invoke();
        }

        #endregion

        #region Event Handlers

        private void OnStunStateChanged(bool previousValue, bool newValue)
        {
            Debug.Log($"[HunterStunHandler] Stun state changed: {previousValue} -> {newValue}");
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// ハンターが他のハンターをスタンさせます
        /// </summary>
        public void StunByHunter(ulong attackerClientId, float duration)
        {
            if (!IsServer)
            {
                return;
            }

            // 同じチームのハンターはスタンさせない
            // チェックはサーバー側で行う
            ApplyStun(duration);
        }

        /// <summary>
        /// ウルフがハンターをスタンさせます
        /// </summary>
        public void StunByWolf(ulong wolfClientId, float duration)
        {
            if (!IsServer)
            {
                return;
            }

            ApplyStun(duration);
        }

        #endregion
    }
}
