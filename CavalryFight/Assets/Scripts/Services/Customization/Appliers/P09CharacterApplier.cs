#nullable enable

using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using P09.Modular.Humanoid.Data;

namespace CavalryFight.Services.Customization
{
    /// <summary>
    /// P09 Modular Humanoidキャラクターカスタマイズ適用
    /// </summary>
    /// <remarks>
    /// P09 Modular Humanoidアセットの公式ScriptableObjectシステムを使用します。
    /// EditPartDataContainer を使用したデータドリブンなカスタマイズ適用を行います。
    /// AvatarView.cs の実装を参考にしています。
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
        /// P09 EditPartDataContainerのリスト
        /// </summary>
        /// <remarks>
        /// Unity InspectorでP09のScriptableObjectフォルダから設定してください。
        /// DemoPageControllerと同じデータを使用できます。
        /// </remarks>
        public List<EditPartDataContainer> EditPartDataContainers = new List<EditPartDataContainer>();

        /// <summary>
        /// 男性用戦闘待機アニメーター
        /// </summary>
        public RuntimeAnimatorController? MaleCombatIdleAnimator;

        /// <summary>
        /// 女性用戦闘待機アニメーター
        /// </summary>
        public RuntimeAnimatorController? FemaleCombatIdleAnimator;

        /// <summary>
        /// 元のアニメーターコントローラーを保持するキャッシュ（キャラクターごと）
        /// </summary>
        private readonly Dictionary<GameObject, RuntimeAnimatorController> _originalAnimators = new Dictionary<GameObject, RuntimeAnimatorController>();

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

            if (EditPartDataContainers == null || EditPartDataContainers.Count == 0)
            {
                Debug.LogError("[P09CharacterApplier] EditPartDataContainers is empty! Please assign P09 ScriptableObject data in CustomizationService.");
                return false;
            }

            try
            {
                int sexId = customization.Gender == Gender.Male ? MALE_SEX_ID : FEMALE_SEX_ID;

                // すべての子Transformを取得
                var allChildren = characterObject.GetComponentsInChildren<Transform>(true);

                // まず、コアボディパーツ（Male_Body_*, Female_Body_*）を確実に有効化
                foreach (Transform child in allChildren)
                {
                    if ((child.name.Contains("Male_Body_") || child.name.Contains("Female_Body_")) &&
                        !child.name.Contains("Nakid") && !child.name.Contains("Armor"))
                    {
                        if (!child.gameObject.activeSelf)
                        {
                            child.gameObject.SetActive(true);
                        }
                    }
                }

                // 剣をすべて無効化（このゲームは弓専用）
                foreach (Transform child in allChildren)
                {
                    if (child.name.Contains("Sword") && !child.name.Contains("Bow"))
                    {
                        if (child.gameObject.activeSelf)
                        {
                            child.gameObject.SetActive(false);
                        }
                    }
                }

                int changeCount = 0;

                // P09のデータドリブンシステムを使用してカスタマイズを適用
                foreach (Transform child in allChildren)
                {
                    // 性別
                    if (ApplyPartData(child, EditPartType.Sex, customization.Gender == Gender.Male ? 1 : 2, sexId))
                    {
                        changeCount++;
                    }

                    // 顔タイプ
                    if (ApplyPartData(child, EditPartType.FaceType, customization.FaceType, sexId))
                    {
                        changeCount++;
                    }

                    // 髪型
                    if (ApplyPartData(child, EditPartType.HairStyle, customization.HairstyleId, sexId))
                    {
                        changeCount++;
                    }

                    // 髪の色
                    ApplyHairColor(child, customization.HairColorId, customization.HairstyleId);

                    // 肌の色
                    ApplySkinColor(child, customization.SkinToneId, sexId);

                    // 目の色
                    ApplyEyeColor(child, customization.EyeColorId);

                    // 顔のひげ（男性のみ）
                    if (customization.Gender == Gender.Male)
                    {
                        if (ApplyPartData(child, EditPartType.FacialHair, customization.FacialHairId, sexId))
                        {
                            changeCount++;
                        }
                    }

                    // バストサイズ（女性のみ）
                    if (customization.Gender == Gender.Female)
                    {
                        ApplyBustSize(child, customization.BustSize);
                    }

                    // 防具
                    if (ApplyPartData(child, EditPartType.Head, customization.HeadArmorId, sexId))
                    {
                        changeCount++;
                    }

                    if (ApplyPartData(child, EditPartType.Chest, customization.ChestArmorId, sexId))
                    {
                        changeCount++;
                    }
                    if (ApplyPartData(child, EditPartType.Arm, customization.ArmsArmorId, sexId))
                    {
                        changeCount++;
                    }
                    if (ApplyPartData(child, EditPartType.Waist, customization.WaistArmorId, sexId))
                    {
                        changeCount++;
                    }
                    if (ApplyPartData(child, EditPartType.Leg, customization.LegsArmorId, sexId))
                    {
                        changeCount++;
                    }

                    // 弓/武器
                    if (ApplyPartData(child, EditPartType.Weapon, customization.BowId, sexId))
                    {
                        changeCount++;
                    }
                }

                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[P09CharacterApplier] Failed to apply customization: {ex.Message}\n{ex.StackTrace}");
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
            var animator = characterObject.GetComponentInChildren<Animator>(true);
            if (animator == null)
            {
                Debug.LogWarning($"[P09CharacterApplier] No Animator found in {characterObject.name}.");
                return false;
            }

            // P09構造があるか確認
            var allTransforms = characterObject.GetComponentsInChildren<Transform>(true);
            var hasP09Structure = allTransforms.Any(t => t.name.StartsWith("P09_") || t.name.Contains("Hair") || t.name.Contains("Armor"));

            return hasP09Structure;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// アニメーターを戦闘待機モードに切り替えます
        /// </summary>
        /// <param name="characterObject">対象のキャラクターGameObject</param>
        /// <param name="gender">キャラクターの性別</param>
        /// <param name="useCombatIdle">trueの場合は戦闘待機アニメーター、falseの場合は通常アニメーターに戻す</param>
        /// <returns>切り替えに成功したかどうか</returns>
        public bool SetCombatIdleMode(GameObject characterObject, Gender gender, bool useCombatIdle)
        {
            if (characterObject == null)
            {
                Debug.LogError("[P09CharacterApplier] Character object is null.");
                return false;
            }

            var animator = characterObject.GetComponentInChildren<Animator>(true);
            if (animator == null)
            {
                Debug.LogError($"[P09CharacterApplier] No Animator found in {characterObject.name}.");
                return false;
            }

            if (useCombatIdle)
            {
                // 元のアニメーターを保存（初回のみ）
                if (!_originalAnimators.ContainsKey(characterObject) && animator.runtimeAnimatorController != null)
                {
                    _originalAnimators[characterObject] = animator.runtimeAnimatorController;
                }

                // 戦闘待機アニメーターに切り替え
                RuntimeAnimatorController? combatAnimator = gender == Gender.Male ? MaleCombatIdleAnimator : FemaleCombatIdleAnimator;

                if (combatAnimator == null)
                {
                    Debug.LogWarning($"[P09CharacterApplier] Combat idle animator for {gender} is not assigned.");
                    return false;
                }

                animator.runtimeAnimatorController = combatAnimator;
            }
            else
            {
                // 通常アニメーターに戻す
                if (_originalAnimators.TryGetValue(characterObject, out var originalAnimator))
                {
                    animator.runtimeAnimatorController = originalAnimator;
                }
                else
                {
                    Debug.LogWarning($"[P09CharacterApplier] No original animator found for {characterObject.name}. Cannot revert.");
                    return false;
                }
            }

            return true;
        }

        #endregion

        #region Data-Driven Apply Methods

        /// <summary>
        /// EditPartDataを使用してパーツの表示/非表示を適用します
        /// </summary>
        /// <param name="child">対象のTransform</param>
        /// <param name="partType">パーツタイプ</param>
        /// <param name="currentId">現在選択されているID</param>
        /// <param name="sexId">性別ID</param>
        /// <returns>変更があった場合はtrue</returns>
        /// <remarks>
        /// AvatarView.UpdateRenderer() と同じロジックを使用
        /// </remarks>
        private bool ApplyPartData(Transform child, EditPartType partType, int currentId, int sexId)
        {
            // コアボディパーツは絶対にApplyPartDataで処理しない（常に表示されるべき）
            if ((child.name.Contains("Male_Body_") || child.name.Contains("Female_Body_")) &&
                !child.name.Contains("Nakid") && !child.name.Contains("Armor"))
            {
                return false;
            }

            var dataList = GetEditPartDataList(partType, sexId);
            if (dataList == null || dataList.Count == 0)
            {
                return false;
            }

            bool changed = false;

            foreach (var data in dataList)
            {
                // パターン1: 完全一致
                if (child.name == data.MeshName)
                {
                    bool shouldBeActive = data.ContentId == currentId;
                    if (child.gameObject.activeSelf != shouldBeActive)
                    {
                        child.gameObject.SetActive(shouldBeActive);
                        changed = true;
                    }
                }
                // パターン2: テンプレート（Male）
                else if (child.name == string.Format(data.MeshName, "Male"))
                {
                    bool shouldBeActive = sexId == MALE_SEX_ID && data.ContentId == currentId;
                    if (child.gameObject.activeSelf != shouldBeActive)
                    {
                        child.gameObject.SetActive(shouldBeActive);
                        changed = true;
                    }
                }
                // パターン3: テンプレート（Female/Fem）
                else if (child.name == string.Format(data.MeshName, "Female") || child.name == string.Format(data.MeshName, "Fem"))
                {
                    bool shouldBeActive = sexId == FEMALE_SEX_ID && data.ContentId == currentId;
                    if (child.gameObject.activeSelf != shouldBeActive)
                    {
                        child.gameObject.SetActive(shouldBeActive);
                        changed = true;
                    }
                }
            }

            return changed;
        }

        /// <summary>
        /// 髪の色を適用します
        /// </summary>
        private void ApplyHairColor(Transform child, int hairColorId, int hairstyleId)
        {
            var dataList = GetEditPartDataList(EditPartType.HairColor, 0);
            if (dataList == null || dataList.Count == 0)
            {
                return;
            }

            var currentData = dataList.FirstOrDefault(d => d.ContentId == hairColorId) as HairColorEditPartData;
            if (currentData == null)
            {
                return;
            }

            foreach (var data in dataList)
            {
                // MeshNameがテンプレート形式: "Hair_{0}" → "Hair_01", "Hair_02" etc.
                if (child.name == string.Format(data.MeshName, hairstyleId))
                {
                    var renderer = child.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        var material = currentData.GetMaterial(hairstyleId);
                        if (material != null)
                        {
                            renderer.material = material;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 肌の色を適用します
        /// </summary>
        private void ApplySkinColor(Transform child, int skinColorId, int sexId)
        {
            var dataList = GetEditPartDataList(EditPartType.Skin, sexId);
            if (dataList == null || dataList.Count == 0)
            {
                return;
            }

            var currentData = dataList.FirstOrDefault(d => d.ContentId == skinColorId) as ColorEditPartData;
            if (currentData?.Material == null)
            {
                return;
            }

            var renderer = child.GetComponent<Renderer>();
            if (renderer == null)
            {
                return;
            }

            Material[] materials = renderer.materials;
            bool changed = false;

            for (int i = 0; i < materials.Length; i++)
            {
                string matName = materials[i].name;
                // Unity adds "(Instance)" suffix, so check if it starts with the pattern
                bool matches = matName.Contains("Skin") &&
                              (matName.StartsWith("P09_Male_Skin") ||
                               matName.StartsWith("P09_Female_Skin") ||
                               matName.StartsWith("P09_Fem_Skin"));

                if (matches)
                {
                    materials[i] = currentData.Material;
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
            var dataList = GetEditPartDataList(EditPartType.EyeColor, 0);
            if (dataList == null || dataList.Count == 0)
            {
                return;
            }

            var currentData = dataList.FirstOrDefault(d => d.ContentId == eyeColorId) as ColorEditPartData;
            if (currentData == null || currentData.Material == null)
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
                        materials[i] = currentData.Material;
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
        /// バストサイズを適用します
        /// </summary>
        private void ApplyBustSize(Transform child, int bustSize)
        {
            var dataList = GetEditPartDataList(EditPartType.BustSize, FEMALE_SEX_ID);
            if (dataList == null || dataList.Count == 0)
            {
                return;
            }

            var currentData = dataList.FirstOrDefault(d => d.ContentId == bustSize) as BustSizeEditPartData;
            if (currentData == null)
            {
                return;
            }

            // MeshNameがテンプレート: "Bust_{0}" → "Bust_R", "Bust_L"
            if (child.name == string.Format(currentData.MeshName, "R") ||
                child.name == string.Format(currentData.MeshName, "L"))
            {
                if (child.localScale != currentData.Size)
                {
                    child.localScale = currentData.Size;
                }
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// EditPartTypeに対応するデータリストを取得します
        /// </summary>
        /// <param name="partType">パーツタイプ</param>
        /// <param name="sexId">性別ID（0=両方、1=男性、2=女性）</param>
        /// <returns>データリスト</returns>
        private List<IEditPartData>? GetEditPartDataList(EditPartType partType, int sexId)
        {
            // まずsexId=0（両方）のデータを探す
            var container = EditPartDataContainers.FirstOrDefault(c => c.Type == partType && c.SexId == 0);
            if (container != null && container.PartDataList != null && container.PartDataList.Count > 0)
            {
                return container.PartDataList;
            }

            // 次に性別固有のデータを探す
            container = EditPartDataContainers.FirstOrDefault(c => c.Type == partType && c.SexId == sexId);
            if (container != null && container.PartDataList != null)
            {
                return container.PartDataList;
            }

            return null;
        }

        #endregion
    }
}
