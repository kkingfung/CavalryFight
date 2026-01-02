#nullable enable

using UnityEngine;

namespace CavalryFight.Services.Customization
{
    /// <summary>
    /// 矢タイプの設定を保持するScriptableObject
    /// </summary>
    /// <remarks>
    /// ArrowType enumに対応するプレハブ（Bullet/Muzzle/Hit）を設定します。
    /// GameBootstrapでICustomizationServiceに登録して使用します。
    /// </remarks>
    [CreateAssetMenu(fileName = "ArrowTypeConfig", menuName = "CavalryFight/Arrow Type Config")]
    public class ArrowTypeConfig : ScriptableObject
    {
        #region Serialized Fields

        [Header("Arrow Prefabs (24 types - matches ArrowType enum order)")]
        [Tooltip("矢プレハブ配列（ArrowType enumの順序に対応）")]
        [SerializeField] private GameObject?[] _arrowBulletPrefabs = new GameObject?[24];

        [Header("Muzzle Effect Prefabs")]
        [Tooltip("マズルエフェクト配列（ArrowType enumの順序に対応）")]
        [SerializeField] private GameObject?[] _muzzleEffectPrefabs = new GameObject?[24];

        [Header("Hit Effect Prefabs")]
        [Tooltip("ヒットエフェクト配列（ArrowType enumの順序に対応）")]
        [SerializeField] private GameObject?[] _hitEffectPrefabs = new GameObject?[24];

        #endregion

        #region Public Methods

        /// <summary>
        /// 指定した矢タイプの矢プレハブを取得します
        /// </summary>
        /// <param name="arrowType">矢タイプ</param>
        /// <returns>矢プレハブ（未設定の場合はnull）</returns>
        public GameObject? GetArrowPrefab(ArrowType arrowType)
        {
            int index = (int)arrowType;
            if (_arrowBulletPrefabs != null && index >= 0 && index < _arrowBulletPrefabs.Length)
            {
                return _arrowBulletPrefabs[index];
            }
            return null;
        }

        /// <summary>
        /// 指定した矢タイプのマズルエフェクトプレハブを取得します
        /// </summary>
        /// <param name="arrowType">矢タイプ</param>
        /// <returns>マズルエフェクトプレハブ（未設定の場合はnull）</returns>
        public GameObject? GetMuzzleEffectPrefab(ArrowType arrowType)
        {
            int index = (int)arrowType;
            if (_muzzleEffectPrefabs != null && index >= 0 && index < _muzzleEffectPrefabs.Length)
            {
                return _muzzleEffectPrefabs[index];
            }
            return null;
        }

        /// <summary>
        /// 指定した矢タイプのヒットエフェクトプレハブを取得します
        /// </summary>
        /// <param name="arrowType">矢タイプ</param>
        /// <returns>ヒットエフェクトプレハブ（未設定の場合はnull）</returns>
        public GameObject? GetHitEffectPrefab(ArrowType arrowType)
        {
            int index = (int)arrowType;
            if (_hitEffectPrefabs != null && index >= 0 && index < _hitEffectPrefabs.Length)
            {
                return _hitEffectPrefabs[index];
            }
            return null;
        }

        /// <summary>
        /// 指定した矢タイプのすべてのプレハブを取得します
        /// </summary>
        /// <param name="arrowType">矢タイプ</param>
        /// <returns>矢プレハブ、マズルエフェクト、ヒットエフェクトのタプル</returns>
        public (GameObject? arrow, GameObject? muzzle, GameObject? hit) GetAllPrefabs(ArrowType arrowType)
        {
            return (
                GetArrowPrefab(arrowType),
                GetMuzzleEffectPrefab(arrowType),
                GetHitEffectPrefab(arrowType)
            );
        }

        #endregion

        #region Validation

#if UNITY_EDITOR
        private void OnValidate()
        {
            // 配列サイズを24に固定（ArrowType enumの数に対応）
            const int arrowTypeCount = 24;
            if (_arrowBulletPrefabs == null || _arrowBulletPrefabs.Length != arrowTypeCount)
            {
                System.Array.Resize(ref _arrowBulletPrefabs, arrowTypeCount);
            }
            if (_muzzleEffectPrefabs == null || _muzzleEffectPrefabs.Length != arrowTypeCount)
            {
                System.Array.Resize(ref _muzzleEffectPrefabs, arrowTypeCount);
            }
            if (_hitEffectPrefabs == null || _hitEffectPrefabs.Length != arrowTypeCount)
            {
                System.Array.Resize(ref _hitEffectPrefabs, arrowTypeCount);
            }
        }
#endif

        #endregion
    }
}
