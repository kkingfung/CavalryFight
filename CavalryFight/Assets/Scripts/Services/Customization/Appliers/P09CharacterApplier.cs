#nullable enable

using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace CavalryFight.Services.Customization
{
    /// <summary>
    /// P09 Modular Humanoidキャラクターカスタマイズ適用
    /// </summary>
    /// <remarks>
    /// P09 Modular Humanoidアセットのキャラクターに
    /// カスタマイズを適用します。
    /// 子オブジェクトの表示/非表示、マテリアルの変更を行います。
    /// </remarks>
    public class P09CharacterApplier : ICharacterApplier
    {
        #region Constants

        /// <summary>
        /// 男性性別ID
        /// </summary>
        private const int MALE_SEX_ID = 1;

        /// <summary>
        /// 女性性別ID
        /// </summary>
        private const int FEMALE_SEX_ID = 2;

        /// <summary>
        /// 肌マテリアルのパターン
        /// </summary>
        private const string SKIN_MATERIAL_PATTERN = @"^P09_.*_Skin.*$";

        /// <summary>
        /// 目マテリアルのパターン
        /// </summary>
        private const string EYE_MATERIAL_PATTERN = @"^P09_Eye.*$";

        #endregion

        #region Fields

        /// <summary>
        /// 髪の色マテリアル配列（1-9）
        /// </summary>
        /// <remarks>
        /// インデックス0は未使用。1-9にマテリアルを設定してください。
        /// </remarks>
        public Material[] HairColorMaterials = new Material[10];

        /// <summary>
        /// 肌の色マテリアル配列（1-6）
        /// </summary>
        /// <remarks>
        /// インデックス0は未使用。1-6にマテリアルを設定してください。
        /// </remarks>
        public Material[] SkinToneMaterials = new Material[7];

        /// <summary>
        /// 目の色マテリアル配列（1-5）
        /// </summary>
        /// <remarks>
        /// インデックス0は未使用。1-5にマテリアルを設定してください。
        /// </remarks>
        public Material[] EyeColorMaterials = new Material[6];

        /// <summary>
        /// バストサイズのスケール配列（0-2）
        /// </summary>
        /// <remarks>
        /// 0 = 小、1 = 中、2 = 大
        /// </remarks>
        public Vector3[] BustSizes = new Vector3[]
        {
            new Vector3(0.8f, 0.8f, 0.8f),  // 小
            new Vector3(1.0f, 1.0f, 1.0f),  // 中
            new Vector3(1.2f, 1.2f, 1.2f)   // 大
        };

        #endregion

        #region ICharacterApplier Implementation

        /// <summary>
        /// キャラクターにカスタマイズを適用します
        /// </summary>
        /// <param name="characterObject">適用先のキャラクターGameObject</param>
        /// <param name="customization">適用するカスタマイズデータ</param>
        /// <returns>適用に成功したかどうか</returns>
        public bool Apply(GameObject characterObject, CharacterCustomization customization)
        {
            if (characterObject == null || customization == null)
            {
                Debug.LogError("[P09CharacterApplier] Character object or customization is null.");
                return false;
            }

            try
            {
                int sexId = customization.Gender == Gender.Male ? MALE_SEX_ID : FEMALE_SEX_ID;

                // すべての子オブジェクトに対してカスタマイズを適用
                foreach (Transform child in characterObject.GetComponentsInChildren<Transform>(true))
                {
                    // 性別
                    ApplyGender(child, sexId);

                    // 顔のタイプ
                    ApplyFaceType(child, customization.FaceType, sexId);

                    // 髪型
                    ApplyHairstyle(child, customization.HairstyleId, sexId);

                    // 髪の色
                    ApplyHairColor(child, customization.HairColorId, customization.HairstyleId);

                    // 肌の色
                    ApplySkinTone(child, customization.SkinToneId);

                    // 目の色
                    ApplyEyeColor(child, customization.EyeColorId);

                    // 顔のひげ（男性のみ）
                    if (customization.Gender == Gender.Male)
                    {
                        ApplyFacialHair(child, customization.FacialHairId);
                    }

                    // バストサイズ（女性のみ）
                    if (customization.Gender == Gender.Female)
                    {
                        ApplyBustSize(child, customization.BustSize);
                    }

                    // 防具
                    ApplyArmor(child, customization, sexId);

                    // 弓
                    ApplyBow(child, customization.BowId);
                }

                Debug.Log($"[P09CharacterApplier] Successfully applied customization to: {characterObject.name}");
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[P09CharacterApplier] Failed to apply customization: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 指定されたGameObjectがこのApplierで処理可能かどうかを確認します
        /// </summary>
        /// <param name="characterObject">確認するGameObject</param>
        /// <returns>処理可能な場合はtrue</returns>
        public bool CanApply(GameObject characterObject)
        {
            if (characterObject == null)
            {
                return false;
            }

            // P09キャラクターかどうかを確認
            // Animatorコンポーネント（自身または子に存在）とP09の構造（子オブジェクトの命名規則）で判定
            var animator = characterObject.GetComponentInChildren<Animator>(true);
            if (animator == null)
            {
                Debug.LogWarning($"[P09CharacterApplier] No Animator found in {characterObject.name} or its children.");
                return false;
            }

            // P09特有の子オブジェクトが存在するか確認
            var hasP09Structure = characterObject.GetComponentsInChildren<Transform>(true)
                .Any(t => t.name.StartsWith("P09_") || t.name.Contains("Hair") || t.name.Contains("Armor"));

            if (!hasP09Structure)
            {
                Debug.LogWarning($"[P09CharacterApplier] No P09 structure found in {characterObject.name}. Looking for children with 'P09_', 'Hair', or 'Armor' in their names.");
            }

            return hasP09Structure;
        }

        #endregion

        #region Apply Methods

        /// <summary>
        /// 性別を適用します
        /// </summary>
        private void ApplyGender(Transform child, int sexId)
        {
            // P09_Human_Male または P09_Human_Female などの命名規則
            if (child.name.Contains("_Male"))
            {
                child.gameObject.SetActive(sexId == MALE_SEX_ID);
            }
            else if (child.name.Contains("_Female") || child.name.Contains("_Fem"))
            {
                child.gameObject.SetActive(sexId == FEMALE_SEX_ID);
            }
        }

        /// <summary>
        /// 顔のタイプを適用します
        /// </summary>
        private void ApplyFaceType(Transform child, int faceType, int sexId)
        {
            // 顔タイプの命名規則: P09_Face_01, P09_Face_02 など
            for (int i = 0; i <= 1; i++)
            {
                string maleFaceName = $"P09_Face_{i:D2}_Male";
                string femaleFaceName = $"P09_Face_{i:D2}_Female";

                if (child.name == maleFaceName)
                {
                    child.gameObject.SetActive(sexId == MALE_SEX_ID && i == faceType);
                }
                else if (child.name == femaleFaceName)
                {
                    child.gameObject.SetActive(sexId == FEMALE_SEX_ID && i == faceType);
                }
            }
        }

        /// <summary>
        /// 髪型を適用します
        /// </summary>
        private void ApplyHairstyle(Transform child, int hairstyleId, int sexId)
        {
            // 髪型の命名規則: P09_Hair_01, P09_Hair_02 など
            for (int i = 0; i <= 14; i++)
            {
                string hairName = $"P09_Hair_{i:D2}";
                string maleHairName = $"P09_Hair_{i:D2}_Male";
                string femaleHairName = $"P09_Hair_{i:D2}_Female";

                if (child.name == hairName || child.name == maleHairName)
                {
                    child.gameObject.SetActive(sexId == MALE_SEX_ID && i == hairstyleId);
                }
                else if (child.name == femaleHairName)
                {
                    child.gameObject.SetActive(sexId == FEMALE_SEX_ID && i == hairstyleId);
                }
            }
        }

        /// <summary>
        /// 髪の色を適用します
        /// </summary>
        private void ApplyHairColor(Transform child, int hairColorId, int hairstyleId)
        {
            // 髪型のメッシュにマテリアルを適用
            if (child.name.StartsWith("P09_Hair_") && child.name.Contains($"{hairstyleId:D2}"))
            {
                var renderer = child.GetComponent<Renderer>();
                if (renderer != null && hairColorId > 0 && hairColorId < HairColorMaterials.Length)
                {
                    var material = HairColorMaterials[hairColorId];
                    if (material != null)
                    {
                        renderer.material = material;
                    }
                }
            }
        }

        /// <summary>
        /// 肌の色を適用します
        /// </summary>
        private void ApplySkinTone(Transform child, int skinToneId)
        {
            var renderer = child.GetComponent<Renderer>();
            if (renderer == null)
            {
                return;
            }

            if (skinToneId <= 0 || skinToneId >= SkinToneMaterials.Length)
            {
                return;
            }

            var skinMaterial = SkinToneMaterials[skinToneId];
            if (skinMaterial == null)
            {
                return;
            }

            Material[] materials = renderer.materials;
            bool changed = false;

            for (int i = 0; i < materials.Length; i++)
            {
                if (Regex.IsMatch(materials[i].name, SKIN_MATERIAL_PATTERN))
                {
                    materials[i] = skinMaterial;
                    changed = true;
                }
            }

            if (changed)
            {
                renderer.materials = materials;
            }
        }

        /// <summary>
        /// 目の色を適用します
        /// </summary>
        private void ApplyEyeColor(Transform child, int eyeColorId)
        {
            if (eyeColorId <= 0 || eyeColorId >= EyeColorMaterials.Length)
            {
                return;
            }

            var eyeMaterial = EyeColorMaterials[eyeColorId];
            if (eyeMaterial == null)
            {
                return;
            }

            var renderers = child.GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in renderers)
            {
                Material[] materials = renderer.materials;
                bool changed = false;

                for (int i = 0; i < materials.Length; i++)
                {
                    if (Regex.IsMatch(materials[i].name, EYE_MATERIAL_PATTERN))
                    {
                        materials[i] = eyeMaterial;
                        changed = true;
                    }
                }

                if (changed)
                {
                    renderer.materials = materials;
                }
            }
        }

        /// <summary>
        /// 顔のひげを適用します
        /// </summary>
        private void ApplyFacialHair(Transform child, int facialHairId)
        {
            // 顔のひげの命名規則: P09_FacialHair_01 など
            for (int i = 0; i <= 9; i++)
            {
                string facialHairName = $"P09_FacialHair_{i:D2}";
                if (child.name == facialHairName)
                {
                    child.gameObject.SetActive(i == facialHairId);
                }
            }
        }

        /// <summary>
        /// バストサイズを適用します
        /// </summary>
        private void ApplyBustSize(Transform child, int bustSize)
        {
            if (bustSize < 0 || bustSize >= BustSizes.Length)
            {
                return;
            }

            // バストの命名規則: P09_Bust_R, P09_Bust_L
            if (child.name == "P09_Bust_R" || child.name == "P09_Bust_L")
            {
                child.localScale = BustSizes[bustSize];
            }
        }

        /// <summary>
        /// 防具を適用します
        /// </summary>
        private void ApplyArmor(Transform child, CharacterCustomization customization, int sexId)
        {
            // 頭部防具
            ApplyArmorPiece(child, "P09_Armor_Head", customization.HeadArmorId, sexId);

            // 胸部防具
            ApplyArmorPiece(child, "P09_Armor_Chest", customization.ChestArmorId, sexId);

            // 腕部防具
            ApplyArmorPiece(child, "P09_Armor_Arm", customization.ArmsArmorId, sexId);

            // 腰部防具
            ApplyArmorPiece(child, "P09_Armor_Waist", customization.WaistArmorId, sexId);

            // 脚部防具
            ApplyArmorPiece(child, "P09_Armor_Leg", customization.LegsArmorId, sexId);
        }

        /// <summary>
        /// 防具パーツを適用します
        /// </summary>
        private void ApplyArmorPiece(Transform child, string armorPrefix, int armorId, int sexId)
        {
            // 防具の命名規則: P09_Armor_Head_01, P09_Armor_Head_01_Male など
            for (int i = 0; i <= 13; i++)
            {
                string armorName = $"{armorPrefix}_{i:D2}";
                string maleArmorName = $"{armorPrefix}_{i:D2}_Male";
                string femaleArmorName = $"{armorPrefix}_{i:D2}_Female";

                if (child.name == armorName || child.name == maleArmorName)
                {
                    child.gameObject.SetActive(sexId == MALE_SEX_ID && i == armorId);
                }
                else if (child.name == femaleArmorName)
                {
                    child.gameObject.SetActive(sexId == FEMALE_SEX_ID && i == armorId);
                }
            }
        }

        /// <summary>
        /// 弓を適用します
        /// </summary>
        private void ApplyBow(Transform child, int bowId)
        {
            // 弓の命名規則: P09_Bow_01, P09_Bow_02 など
            for (int i = 1; i <= 4; i++)
            {
                string bowName = $"P09_Bow_{i:D2}";
                if (child.name == bowName || child.name.StartsWith(bowName))
                {
                    child.gameObject.SetActive(i == bowId);
                }
            }
        }

        #endregion
    }
}
