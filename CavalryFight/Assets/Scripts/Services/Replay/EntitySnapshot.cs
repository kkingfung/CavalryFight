#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine;

namespace CavalryFight.Services.Replay
{
    /// <summary>
    /// アニメーションレイヤーの状態
    /// </summary>
    [Serializable]
    public struct AnimationLayerState
    {
        /// <summary>レイヤーインデックス</summary>
        public int LayerIndex;

        /// <summary>ステートハッシュ</summary>
        public int StateHash;

        /// <summary>正規化時間</summary>
        public float NormalizedTime;

        /// <summary>レイヤーウェイト</summary>
        public float Weight;
    }

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

        #region Riding State

        /// <summary>
        /// 騎乗中かどうか
        /// </summary>
        public bool IsMounted = false;

        /// <summary>
        /// 騎乗している馬のエンティティID
        /// </summary>
        public string MountedOnEntityId = string.Empty;

        /// <summary>
        /// 騎乗ポイントからのローカル位置
        /// </summary>
        public Vector3 LocalPositionOnMount = Vector3.zero;

        /// <summary>
        /// 騎乗ポイントからのローカル回転
        /// </summary>
        public Quaternion LocalRotationOnMount = Quaternion.identity;

        #endregion

        #region Malbers Animal Controller State

        /// <summary>
        /// Malbers Animal Controller の State ID
        /// </summary>
        /// <remarks>
        /// MAnimal.ActiveStateID から取得
        /// 例: Idle=1, Locomotion=2, Jump=3 など
        /// </remarks>
        public int MalbersStateId = 0;

        /// <summary>
        /// Malbers Animal Controller の Mode ID
        /// </summary>
        /// <remarks>
        /// MAnimal.ActiveMode?.ID から取得
        /// 例: Attack=1, Action=2 など
        /// </remarks>
        public int MalbersModeId = 0;

        /// <summary>
        /// Malbers Animal Controller の Stance ID
        /// </summary>
        /// <remarks>
        /// MAnimal.Stance から取得
        /// </remarks>
        public int MalbersStanceId = 0;

        /// <summary>
        /// 移動速度（Forward Speed）
        /// </summary>
        public float ForwardSpeed = 0f;

        /// <summary>
        /// 垂直入力
        /// </summary>
        public float VerticalInput = 0f;

        /// <summary>
        /// 水平入力
        /// </summary>
        public float HorizontalInput = 0f;

        #endregion

        #region Animation Parameters

        /// <summary>
        /// アニメーションパラメータ: Speed
        /// </summary>
        public float AnimSpeedParam = 0f;

        /// <summary>
        /// アニメーションパラメータ: StateTime
        /// </summary>
        public float AnimStateTimeParam = 0f;

        /// <summary>
        /// 追加のアニメーションレイヤー状態（レイヤー1以降）
        /// Key: レイヤーインデックス, Value: (ステートハッシュ, 正規化時間, ウェイト)
        /// </summary>
        /// <remarks>
        /// JsonUtilityはDictionaryをサポートしないため、シリアライズ用のリストを使用
        /// </remarks>
        public AnimationLayerState[] AdditionalLayerStates = Array.Empty<AnimationLayerState>();

        #endregion

        /// <summary>
        /// EntitySnapshotの新しいインスタンスを初期化します
        /// </summary>
        public EntitySnapshot()
        {
        }

        /// <summary>
        /// GameObjectからEntitySnapshotを作成します（基本版）
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
                CaptureAnimatorState(snapshot, animator);
            }

            return snapshot;
        }

        /// <summary>
        /// 拡張版: 騎乗状態とMalbersコンポーネントを含むスナップショットを作成します
        /// </summary>
        /// <param name="entityId">エンティティID</param>
        /// <param name="entityType">エンティティタイプ</param>
        /// <param name="gameObject">スナップショットを取るGameObject</param>
        /// <param name="rigidbody">Rigidbody（速度取得用、nullでも可）</param>
        /// <param name="animator">Animator（アニメーション状態取得用、nullでも可）</param>
        /// <param name="mountEntityId">騎乗中の馬のエンティティID</param>
        /// <param name="mountPoint">騎乗ポイントのTransform</param>
        /// <returns>作成されたEntitySnapshot</returns>
        public static EntitySnapshot FromGameObjectExtended(
            string entityId,
            EntityType entityType,
            GameObject gameObject,
            Rigidbody? rigidbody = null,
            Animator? animator = null,
            string? mountEntityId = null,
            Transform? mountPoint = null)
        {
            var snapshot = FromGameObject(entityId, entityType, gameObject, rigidbody, animator);

            // 騎乗状態を記録
            if (!string.IsNullOrEmpty(mountEntityId) && mountPoint != null)
            {
                snapshot.IsMounted = true;
                snapshot.MountedOnEntityId = mountEntityId;
                snapshot.LocalPositionOnMount = mountPoint.InverseTransformPoint(gameObject.transform.position);
                snapshot.LocalRotationOnMount = Quaternion.Inverse(mountPoint.rotation) * gameObject.transform.rotation;
            }

            return snapshot;
        }

        /// <summary>
        /// Animatorの状態をスナップショットに記録します
        /// </summary>
        private static void CaptureAnimatorState(EntitySnapshot snapshot, Animator animator)
        {
            // レイヤー0の状態
            var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            snapshot.AnimationStateHash = stateInfo.fullPathHash;
            snapshot.AnimationNormalizedTime = stateInfo.normalizedTime;

            // アニメーションパラメータを取得（存在する場合）
            try
            {
                snapshot.AnimSpeedParam = animator.GetFloat("Speed");
            }
            catch { /* パラメータが存在しない場合は無視 */ }

            try
            {
                snapshot.AnimStateTimeParam = animator.GetFloat("StateTime");
            }
            catch { /* パラメータが存在しない場合は無視 */ }

            // 追加レイヤーの状態を取得
            int layerCount = animator.layerCount;
            if (layerCount > 1)
            {
                var layerStates = new List<AnimationLayerState>();
                for (int i = 1; i < layerCount; i++)
                {
                    var layerInfo = animator.GetCurrentAnimatorStateInfo(i);
                    float weight = animator.GetLayerWeight(i);

                    // ウェイトが0より大きいレイヤーのみ記録
                    if (weight > 0.01f)
                    {
                        layerStates.Add(new AnimationLayerState
                        {
                            LayerIndex = i,
                            StateHash = layerInfo.fullPathHash,
                            NormalizedTime = layerInfo.normalizedTime,
                            Weight = weight
                        });
                    }
                }
                snapshot.AdditionalLayerStates = layerStates.ToArray();
            }
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
                IsAlive = to.IsAlive,

                // 騎乗状態
                IsMounted = to.IsMounted,
                MountedOnEntityId = to.MountedOnEntityId,
                LocalPositionOnMount = Vector3.Lerp(from.LocalPositionOnMount, to.LocalPositionOnMount, t),
                LocalRotationOnMount = Quaternion.Slerp(from.LocalRotationOnMount, to.LocalRotationOnMount, t),

                // Malbers状態（補間しない、最新を使用）
                MalbersStateId = to.MalbersStateId,
                MalbersModeId = to.MalbersModeId,
                MalbersStanceId = to.MalbersStanceId,
                ForwardSpeed = Mathf.Lerp(from.ForwardSpeed, to.ForwardSpeed, t),
                VerticalInput = Mathf.Lerp(from.VerticalInput, to.VerticalInput, t),
                HorizontalInput = Mathf.Lerp(from.HorizontalInput, to.HorizontalInput, t),

                // アニメーションパラメータ
                AnimSpeedParam = Mathf.Lerp(from.AnimSpeedParam, to.AnimSpeedParam, t),
                AnimStateTimeParam = Mathf.Lerp(from.AnimStateTimeParam, to.AnimStateTimeParam, t),
                AdditionalLayerStates = to.AdditionalLayerStates // 最新を使用
            };
        }

        /// <summary>
        /// スナップショットのコピーを作成します
        /// </summary>
        /// <returns>コピーされたスナップショット</returns>
        public EntitySnapshot Clone()
        {
            var clone = new EntitySnapshot
            {
                EntityId = this.EntityId,
                EntityType = this.EntityType,
                Position = this.Position,
                Rotation = this.Rotation,
                Velocity = this.Velocity,
                AnimationStateHash = this.AnimationStateHash,
                AnimationNormalizedTime = this.AnimationNormalizedTime,
                IsAlive = this.IsAlive,

                // 騎乗状態
                IsMounted = this.IsMounted,
                MountedOnEntityId = this.MountedOnEntityId,
                LocalPositionOnMount = this.LocalPositionOnMount,
                LocalRotationOnMount = this.LocalRotationOnMount,

                // Malbers状態
                MalbersStateId = this.MalbersStateId,
                MalbersModeId = this.MalbersModeId,
                MalbersStanceId = this.MalbersStanceId,
                ForwardSpeed = this.ForwardSpeed,
                VerticalInput = this.VerticalInput,
                HorizontalInput = this.HorizontalInput,

                // アニメーションパラメータ
                AnimSpeedParam = this.AnimSpeedParam,
                AnimStateTimeParam = this.AnimStateTimeParam
            };

            // 追加レイヤー状態のディープコピー
            if (this.AdditionalLayerStates != null && this.AdditionalLayerStates.Length > 0)
            {
                clone.AdditionalLayerStates = new AnimationLayerState[this.AdditionalLayerStates.Length];
                Array.Copy(this.AdditionalLayerStates, clone.AdditionalLayerStates, this.AdditionalLayerStates.Length);
            }

            return clone;
        }
    }
}
