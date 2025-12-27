#nullable enable

using UnityEngine;

namespace CavalryFight.Services.Customization
{
    /// <summary>
    /// 騎乗動物カスタマイズ設定
    /// </summary>
    /// <remarks>
    /// MalbersHorseApplierで使用するマテリアルを設定するScriptableObject。
    /// Assets/Settings/Customization/MountCustomizationConfig.asset として作成してください。
    /// </remarks>
    [CreateAssetMenu(fileName = "MountCustomizationConfig", menuName = "CavalryFight/Customization/Mount Config")]
    public class MountCustomizationConfig : ScriptableObject
    {
        [Header("Coat Color Materials")]
        [Tooltip("毛色マテリアル配列（HorseColor enum順）\n0: Black, 1: Brown, 2: Gray, 3: LightGray, 4: Palomino, 5: White")]
        [SerializeField] private Material[] _coatColorMaterials = new Material[6];

        [Header("Mane Color Materials")]
        [Tooltip("たてがみ色マテリアル配列（ManeColor enum順）\n0: Black, 1: Blond, 2: Brown, 3: Gray, 4: White")]
        [SerializeField] private Material[] _maneColorMaterials = new Material[5];

        [Header("Horn Materials")]
        [Tooltip("角マテリアル配列（HornMaterial enum順）\n0: BlackPA, 1: BlackRealistic, 2: RedPA, 3: WhitePA, 4: WhitePalomino, 5: WhiteRealistic")]
        [SerializeField] private Material[] _hornMaterials = new Material[6];

        [Header("Armor Materials")]
        [Tooltip("馬鎧マテリアル配列（0 = なし、1-3）\nIndex 0: None (null), Index 1-3: Armor 1-3")]
        [SerializeField] private Material[] _armorMaterials = new Material[4];

        [Header("Saddle Material")]
        [Tooltip("鞍マテリアル")]
        [SerializeField] private Material? _saddleMaterial;

        /// <summary>毛色マテリアル</summary>
        public Material[] CoatColorMaterials => _coatColorMaterials;

        /// <summary>たてがみ色マテリアル</summary>
        public Material[] ManeColorMaterials => _maneColorMaterials;

        /// <summary>角マテリアル</summary>
        public Material[] HornMaterials => _hornMaterials;

        /// <summary>馬鎧マテリアル</summary>
        public Material[] ArmorMaterials => _armorMaterials;

        /// <summary>鞍マテリアル</summary>
        public Material? SaddleMaterial => _saddleMaterial;

        /// <summary>
        /// 設定をMalbersHorseApplierに適用します
        /// </summary>
        /// <param name="applier">適用先のApplier</param>
        public void ApplyToApplier(MalbersHorseApplier applier)
        {
            if (applier == null)
            {
                Debug.LogError("[MountCustomizationConfig] Applier is null!");
                return;
            }

            // マテリアル配列をコピー
            if (_coatColorMaterials != null && _coatColorMaterials.Length > 0)
            {
                applier.CoatColorMaterials = (Material[])_coatColorMaterials.Clone();
                Debug.Log($"[MountCustomizationConfig] Applied {_coatColorMaterials.Length} coat color materials");
            }

            if (_maneColorMaterials != null && _maneColorMaterials.Length > 0)
            {
                applier.ManeColorMaterials = (Material[])_maneColorMaterials.Clone();
                Debug.Log($"[MountCustomizationConfig] Applied {_maneColorMaterials.Length} mane color materials");
            }

            if (_hornMaterials != null && _hornMaterials.Length > 0)
            {
                applier.HornMaterials = (Material[])_hornMaterials.Clone();
                Debug.Log($"[MountCustomizationConfig] Applied {_hornMaterials.Length} horn materials");
            }

            if (_armorMaterials != null && _armorMaterials.Length > 0)
            {
                applier.ArmorMaterials = (Material[])_armorMaterials.Clone();
                Debug.Log($"[MountCustomizationConfig] Applied {_armorMaterials.Length} armor materials");
            }

            if (_saddleMaterial != null)
            {
                applier.SaddleMaterial = _saddleMaterial;
                Debug.Log($"[MountCustomizationConfig] Applied saddle material");
            }
        }

        #region Validation

        private void OnValidate()
        {
            // 配列サイズを検証
            if (_coatColorMaterials.Length != 6)
            {
                Debug.LogWarning($"[MountCustomizationConfig] CoatColorMaterials should have 6 elements, but has {_coatColorMaterials.Length}");
            }

            if (_maneColorMaterials.Length != 5)
            {
                Debug.LogWarning($"[MountCustomizationConfig] ManeColorMaterials should have 5 elements, but has {_maneColorMaterials.Length}");
            }

            if (_hornMaterials.Length != 6)
            {
                Debug.LogWarning($"[MountCustomizationConfig] HornMaterials should have 6 elements, but has {_hornMaterials.Length}");
            }

            if (_armorMaterials.Length != 4)
            {
                Debug.LogWarning($"[MountCustomizationConfig] ArmorMaterials should have 4 elements, but has {_armorMaterials.Length}");
            }
        }

        #endregion
    }
}
