#nullable enable

using UnityEngine;

namespace CavalryFight.Gameplay
{
    /// <summary>
    /// スポーンポイント
    /// </summary>
    /// <remarks>
    /// プレイヤーやAIがスポーンする位置を示します。
    /// シーン内に複数配置可能で、SpawnManagerから管理されます。
    /// </remarks>
    public class SpawnPoint : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Settings")]
        [Tooltip("このスポーンポイントのチームID（0=チームなし、1=チーム1、2=チーム2）")]
        [SerializeField] private int _teamId = 0;

        [Tooltip("スポーン優先度（高いほど優先的に使用）")]
        [SerializeField] private int _priority = 0;

        [Tooltip("トレーニングモード専用かどうか")]
        [SerializeField] private bool _trainingOnly = false;

        [Header("Gizmo")]
        [Tooltip("ギズモの色")]
        [SerializeField] private Color _gizmoColor = Color.green;

        [Tooltip("ギズモのサイズ")]
        [SerializeField] private float _gizmoSize = 1f;

        #endregion

        #region Properties

        /// <summary>
        /// チームID
        /// </summary>
        public int TeamId => _teamId;

        /// <summary>
        /// スポーン優先度
        /// </summary>
        public int Priority => _priority;

        /// <summary>
        /// トレーニングモード専用かどうか
        /// </summary>
        public bool TrainingOnly => _trainingOnly;

        /// <summary>
        /// スポーン位置
        /// </summary>
        public Vector3 Position => transform.position;

        /// <summary>
        /// スポーン回転
        /// </summary>
        public Quaternion Rotation => transform.rotation;

        /// <summary>
        /// スポーン前方方向
        /// </summary>
        public Vector3 Forward => transform.forward;

        #endregion

        #region Public Methods

        /// <summary>
        /// このスポーンポイントにオブジェクトを配置します
        /// </summary>
        /// <param name="target">配置するGameObject</param>
        public void PlaceObject(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            target.transform.position = Position;
            target.transform.rotation = Rotation;

            Debug.Log($"[SpawnPoint] Placed {target.name} at {Position}");
        }

        /// <summary>
        /// このスポーンポイントにオブジェクトを配置します（オフセット付き）
        /// </summary>
        /// <param name="target">配置するGameObject</param>
        /// <param name="offset">ローカル座標でのオフセット</param>
        public void PlaceObject(GameObject target, Vector3 offset)
        {
            if (target == null)
            {
                return;
            }

            target.transform.position = Position + transform.TransformDirection(offset);
            target.transform.rotation = Rotation;

            Debug.Log($"[SpawnPoint] Placed {target.name} at {target.transform.position} (offset: {offset})");
        }

        #endregion

        #region Editor

#if UNITY_EDITOR
        /// <summary>
        /// ギズモ描画（常時）
        /// </summary>
        private void OnDrawGizmos()
        {
            DrawSpawnGizmo(0.5f);
        }

        /// <summary>
        /// ギズモ描画（選択時）
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            DrawSpawnGizmo(1f);
        }

        /// <summary>
        /// スポーンポイントのギズモを描画します
        /// </summary>
        /// <param name="alpha">透明度</param>
        private void DrawSpawnGizmo(float alpha)
        {
            Color color = _gizmoColor;
            color.a = alpha;
            Gizmos.color = color;

            // 位置を示す球
            Gizmos.DrawWireSphere(transform.position, _gizmoSize * 0.5f);

            // 方向を示す矢印
            Vector3 forward = transform.forward * _gizmoSize;
            Vector3 right = transform.right * _gizmoSize * 0.3f;
            Vector3 up = transform.up * _gizmoSize * 0.5f;

            // 矢印の軸
            Gizmos.DrawLine(transform.position, transform.position + forward);

            // 矢印の先端
            Vector3 arrowTip = transform.position + forward;
            Gizmos.DrawLine(arrowTip, arrowTip - forward * 0.3f + right);
            Gizmos.DrawLine(arrowTip, arrowTip - forward * 0.3f - right);

            // 上方向のライン（高さを示す）
            Gizmos.DrawLine(transform.position, transform.position + up);

            // チームIDのラベル用に色を変更
            if (_teamId == 1)
            {
                Gizmos.color = new Color(0, 0, 1, alpha); // 青
            }
            else if (_teamId == 2)
            {
                Gizmos.color = new Color(1, 0, 0, alpha); // 赤
            }

            // 地面に円を描画
            DrawCircle(transform.position, _gizmoSize, 16);
        }

        /// <summary>
        /// 円を描画します
        /// </summary>
        private void DrawCircle(Vector3 center, float radius, int segments)
        {
            float angleStep = 360f / segments;
            Vector3 prevPoint = center + new Vector3(radius, 0, 0);

            for (int i = 1; i <= segments; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                Vector3 newPoint = center + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
                Gizmos.DrawLine(prevPoint, newPoint);
                prevPoint = newPoint;
            }
        }
#endif

        #endregion
    }
}
