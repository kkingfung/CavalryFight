#nullable enable

using UnityEngine;

namespace CavalryFight.Services.Match
{
    /// <summary>
    /// ヒットボックスコンポーネント
    /// </summary>
    /// <remarks>
    /// 各コライダーに取り付けて、命中部位を識別するために使用します。
    /// 矢の命中判定時に、どの部位に当たったかを特定します。
    /// </remarks>
    [RequireComponent(typeof(Collider))]
    public class HitboxComponent : MonoBehaviour
    {
        #region Inspector Fields

        /// <summary>
        /// この部位の命中タイプ
        /// </summary>
        [SerializeField] private HitLocation _hitLocation = HitLocation.Torso;

        #endregion

        #region Properties

        /// <summary>
        /// 命中部位を取得します
        /// </summary>
        public HitLocation HitLocation => _hitLocation;

        #endregion

        #region Unity Lifecycle

        /// <summary>
        /// インスペクターで値が変更された時に呼ばれます
        /// </summary>
        private void OnValidate()
        {
            // コライダーが存在することを確認
            if (GetComponent<Collider>() == null)
            {
                Debug.LogWarning($"[HitboxComponent] {gameObject.name} has HitboxComponent but no Collider!", this);
            }
        }

        #endregion

        #region Debug

#if UNITY_EDITOR
        /// <summary>
        /// Gizmosを描画します（エディタのみ）
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            // 部位に応じて色を変える
            Gizmos.color = _hitLocation switch
            {
                HitLocation.Heart => Color.red,
                HitLocation.Head => Color.yellow,
                HitLocation.Torso => Color.green,
                HitLocation.Arm => Color.cyan,
                HitLocation.Leg => Color.blue,
                HitLocation.Mount => Color.gray,
                _ => Color.white
            };

            var collider = GetComponent<Collider>();
            if (collider != null)
            {
                Gizmos.matrix = transform.localToWorldMatrix;

                if (collider is BoxCollider boxCollider)
                {
                    Gizmos.DrawWireCube(boxCollider.center, boxCollider.size);
                }
                else if (collider is SphereCollider sphereCollider)
                {
                    Gizmos.DrawWireSphere(sphereCollider.center, sphereCollider.radius);
                }
                else if (collider is CapsuleCollider capsuleCollider)
                {
                    // カプセルは簡易的に球で表示
                    Gizmos.DrawWireSphere(capsuleCollider.center, capsuleCollider.radius);
                }
            }
        }
#endif

        #endregion
    }
}
