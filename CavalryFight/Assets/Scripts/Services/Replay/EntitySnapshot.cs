#nullable enable

using System;
using UnityEngine;

namespace CavalryFight.Services.Replay
{
    /// <summary>
    /// エンティティ（プレイヤー、敵、発射物）のスナップショット
    /// </summary>
    /// <remarks>
    /// リプレイフレームに記録される単一エンティティの状態です。
    /// 位置、回転、速度、アニメーション状態を含みます。
    /// </remarks>
    [Serializable]
    public class EntitySnapshot
    {
        /// <summary>
        /// エンティティの一意識別子
        /// </summary>
        /// <remarks>
        /// リプレイ再生時にどのエンティティかを識別するために使用します。
        /// </remarks>
        public string EntityId = string.Empty;

        /// <summary>
        /// エンティティのタイプ（Player, Enemy, Projectile等）
        /// </summary>
        public EntityType EntityType = EntityType.Unknown;

        /// <summary>
        /// 位置（X, Y, Z）
        /// </summary>
        public Vector3 Position = Vector3.zero;

        /// <summary>
        /// 回転（クォータニオン）
        /// </summary>
        public Quaternion Rotation = Quaternion.identity;

        /// <summary>
        /// 速度（X, Y, Z）
        /// </summary>
        /// <remarks>
        /// フレーム間の補間に使用します。
        /// </remarks>
        public Vector3 Velocity = Vector3.zero;

        /// <summary>
        /// アニメーションステートハッシュ
        /// </summary>
        /// <remarks>
        /// Animatorの現在のステートを識別します。
        /// Animator.GetCurrentAnimatorStateInfo().fullPathHash から取得します。
        /// </remarks>
        public int AnimationStateHash = 0;

        /// <summary>
        /// アニメーションの正規化時間（0.0～1.0）
        /// </summary>
        /// <remarks>
        /// アニメーションの再生位置を記録します。
        /// Animator.GetCurrentAnimatorStateInfo().normalizedTime から取得します。
        /// </remarks>
        public float AnimationNormalizedTime = 0f;

        /// <summary>
        /// エンティティが生存しているか
        /// </summary>
        /// <remarks>
        /// false の場合、リプレイ再生時に非表示にします。
        /// </remarks>
        public bool IsAlive = true;

        /// <summary>
        /// EntitySnapshotの新しいインスタンスを初期化します
        /// </summary>
        public EntitySnapshot()
        {
        }

        /// <summary>
        /// GameObjectからEntitySnapshotを作成します
        /// </summary>
        /// <param name="entityId">エンティティID</param>
        /// <param name="entityType">エンティティタイプ</param>
        /// <param name="gameObject">スナップショットを取るGameObject</param>
        /// <param name="rigidbody">Rigidbody（速度取得用、nullでも可）</param>
        /// <param name="animator">Animator（アニメーション状態取得用、nullでも可）</param>
        /// <returns>作成されたEntitySnapshot</returns>
        public static EntitySnapshot FromGameObject(
            string entityId,
            EntityType entityType,
            GameObject gameObject,
            Rigidbody? rigidbody = null,
            Animator? animator = null)
        {
            var snapshot = new EntitySnapshot
            {
                EntityId = entityId,
                EntityType = entityType,
                Position = gameObject.transform.position,
                Rotation = gameObject.transform.rotation,
                IsAlive = gameObject.activeInHierarchy
            };

            // Rigidbodyから速度を取得
            if (rigidbody != null)
            {
                snapshot.Velocity = rigidbody.linearVelocity;
            }

            // Animatorからアニメーション状態を取得
            if (animator != null && animator.isActiveAndEnabled)
            {
                var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                snapshot.AnimationStateHash = stateInfo.fullPathHash;
                snapshot.AnimationNormalizedTime = stateInfo.normalizedTime;
            }

            return snapshot;
        }

        /// <summary>
        /// スナップショットをGameObjectに適用します
        /// </summary>
        /// <param name="gameObject">適用先のGameObject</param>
        /// <param name="rigidbody">Rigidbody（速度設定用、nullでも可）</param>
        /// <param name="animator">Animator（アニメーション状態設定用、nullでも可）</param>
        public void ApplyToGameObject(
            GameObject gameObject,
            Rigidbody? rigidbody = null,
            Animator? animator = null)
        {
            // 位置と回転を設定
            gameObject.transform.position = Position;
            gameObject.transform.rotation = Rotation;

            // 生存状態を設定
            gameObject.SetActive(IsAlive);

            // Rigidbodyに速度を設定
            if (rigidbody != null)
            {
                rigidbody.linearVelocity = Velocity;
            }

            // Animatorにアニメーション状態を設定
            if (animator != null && animator.isActiveAndEnabled)
            {
                animator.Play(AnimationStateHash, 0, AnimationNormalizedTime);
            }
        }

        /// <summary>
        /// 2つのスナップショット間を補間します
        /// </summary>
        /// <param name="from">開始スナップショット</param>
        /// <param name="to">終了スナップショット</param>
        /// <param name="t">補間係数（0.0～1.0）</param>
        /// <returns>補間されたスナップショット</returns>
        public static EntitySnapshot Lerp(EntitySnapshot from, EntitySnapshot to, float t)
        {
            return new EntitySnapshot
            {
                EntityId = from.EntityId,
                EntityType = from.EntityType,
                Position = Vector3.Lerp(from.Position, to.Position, t),
                Rotation = Quaternion.Slerp(from.Rotation, to.Rotation, t),
                Velocity = Vector3.Lerp(from.Velocity, to.Velocity, t),
                AnimationStateHash = t < 0.5f ? from.AnimationStateHash : to.AnimationStateHash,
                AnimationNormalizedTime = Mathf.Lerp(from.AnimationNormalizedTime, to.AnimationNormalizedTime, t),
                IsAlive = to.IsAlive // Use latest alive state
            };
        }

        /// <summary>
        /// スナップショットのコピーを作成します
        /// </summary>
        /// <returns>コピーされたスナップショット</returns>
        public EntitySnapshot Clone()
        {
            return new EntitySnapshot
            {
                EntityId = this.EntityId,
                EntityType = this.EntityType,
                Position = this.Position,
                Rotation = this.Rotation,
                Velocity = this.Velocity,
                AnimationStateHash = this.AnimationStateHash,
                AnimationNormalizedTime = this.AnimationNormalizedTime,
                IsAlive = this.IsAlive
            };
        }
    }
}
