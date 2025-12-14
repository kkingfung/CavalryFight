#nullable enable

using System.Linq;
using UnityEngine;

namespace CavalryFight.Services.Customization
{
    /// <summary>
    /// Malbers Horse AnimSet Pro騎乗動物カスタマイズ適用
    /// </summary>
    /// <remarks>
    /// Malbers Horse AnimSet Proアセットの騎乗動物に
    /// カスタマイズを適用します。
    /// 毛色、たてがみ、装備の表示/非表示とマテリアル変更を行います。
    /// </remarks>
    public class MalbersHorseApplier : IMountApplier
    {
        #region Fields

        /// <summary>
        /// 馬の毛色マテリアル配列（HorseColor enum順）
        /// </summary>
        /// <remarks>
        /// 0: Black, 1: Brown, 2: Gray, 3: LightGray, 4: Palomino, 5: White
        /// </remarks>
        public Material[] CoatColorMaterials = new Material[6];

        /// <summary>
        /// たてがみの色マテリアル配列（ManeColor enum順）
        /// </summary>
        /// <remarks>
        /// 0: Black, 1: Blond, 2: Brown, 3: Gray, 4: White
        /// </remarks>
        public Material[] ManeColorMaterials = new Material[5];

        /// <summary>
        /// 馬鎧マテリアル配列（0 = なし、1-3）
        /// </summary>
        /// <remarks>
        /// インデックス0は未使用。1-3にマテリアルを設定してください。
        /// </remarks>
        public Material[] ArmorMaterials = new Material[4];

        /// <summary>
        /// 鞍マテリアル
        /// </summary>
        public Material? SaddleMaterial;

        #endregion

        #region IMountApplier Implementation

        /// <summary>
        /// 騎乗動物にカスタマイズを適用します
        /// </summary>
        /// <param name="mountObject">適用先の騎乗動物GameObject</param>
        /// <param name="customization">適用するカスタマイズデータ</param>
        /// <returns>適用に成功したかどうか</returns>
        public bool Apply(GameObject mountObject, MountCustomization customization)
        {
            if (mountObject == null || customization == null)
            {
                Debug.LogError("[MalbersHorseApplier] Mount object or customization is null.");
                return false;
            }

            try
            {
                // すべての子オブジェクトに対してカスタマイズを適用
                foreach (Transform child in mountObject.GetComponentsInChildren<Transform>(true))
                {
                    // 毛色を適用
                    ApplyCoatColor(child, customization.CoatColor);

                    // たてがみスタイルを適用
                    ApplyManeStyle(child, customization.ManeStyle);

                    // たてがみの色を適用
                    ApplyManeColor(child, customization.ManeColor, customization.ManeStyle);

                    // 馬鎧を適用
                    ApplyArmor(child, customization.ArmorId);

                    // 鞍を適用
                    ApplySaddle(child, customization.HasSaddle);

                    // 騎乗動物のタイプに応じた特殊処理
                    ApplyMountTypeSpecific(child, customization.MountType);
                }

                Debug.Log($"[MalbersHorseApplier] Successfully applied customization to: {mountObject.name}");
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[MalbersHorseApplier] Failed to apply customization: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 指定されたGameObjectがこのApplierで処理可能かどうかを確認します
        /// </summary>
        /// <param name="mountObject">確認するGameObject</param>
        /// <returns>処理可能な場合はtrue</returns>
        public bool CanApply(GameObject mountObject)
        {
            if (mountObject == null)
            {
                return false;
            }

            // Malbersの馬特有の構造（子オブジェクトの命名規則）で判定
            var hasMalbersStructure = mountObject.GetComponentsInChildren<Transform>(true)
                .Any(t => t.name.Contains("Horse") || t.name.Contains("Mane") || t.name.StartsWith("Horse_"));

            return hasMalbersStructure;
        }

        #endregion

        #region Apply Methods

        /// <summary>
        /// 毛色を適用します
        /// </summary>
        private void ApplyCoatColor(Transform child, HorseColor coatColor)
        {
            // 馬の体のメッシュに毛色マテリアルを適用
            // 一般的な命名規則: "Horse_Body", "Horse 4", "Horse" など
            if (child.name.Contains("Horse") && !child.name.Contains("Mane") && !child.name.Contains("Tail"))
            {
                Renderer? renderer = child.GetComponent<SkinnedMeshRenderer>();
                if (renderer == null)
                {
                    renderer = child.GetComponent<MeshRenderer>();
                }

                if (renderer != null)
                {
                    int colorIndex = (int)coatColor;
                    if (colorIndex >= 0 && colorIndex < CoatColorMaterials.Length)
                    {
                        var material = CoatColorMaterials[colorIndex];
                        if (material != null)
                        {
                            // 馬の体のマテリアルのみを変更（目などは除外）
                            Material[] materials = renderer.materials;
                            if (materials.Length > 0 && materials[0].name.Contains("Horse"))
                            {
                                materials[0] = material;
                                renderer.materials = materials;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// たてがみスタイルを適用します
        /// </summary>
        private void ApplyManeStyle(Transform child, ManeStyle maneStyle)
        {
            // たてがみの命名規則:
            // Horse_Mane01 Short
            // Horse_Mane02 Long Left
            // Horse_Mane03 Long Right
            // Horse_Mane04 V Left
            // Horse_Mane05 V Right

            if (child.name.StartsWith("Horse_Mane"))
            {
                bool shouldActivate = false;

                switch (maneStyle)
                {
                    case ManeStyle.Short:
                        shouldActivate = child.name.Contains("Mane01") || child.name.Contains("Short");
                        break;
                    case ManeStyle.LongLeft:
                        shouldActivate = child.name.Contains("Mane02") || child.name.Contains("Long Left");
                        break;
                    case ManeStyle.LongRight:
                        shouldActivate = child.name.Contains("Mane03") || child.name.Contains("Long Right");
                        break;
                    case ManeStyle.VShapeLeft:
                        shouldActivate = child.name.Contains("Mane04") || child.name.Contains("V Left");
                        break;
                    case ManeStyle.VShapeRight:
                        shouldActivate = child.name.Contains("Mane05") || child.name.Contains("V Right");
                        break;
                }

                child.gameObject.SetActive(shouldActivate);
            }
        }

        /// <summary>
        /// たてがみの色を適用します
        /// </summary>
        private void ApplyManeColor(Transform child, ManeColor maneColor, ManeStyle maneStyle)
        {
            // アクティブなたてがみと尻尾にマテリアルを適用
            if ((child.name.StartsWith("Horse_Mane") || child.name.StartsWith("Horse_Tail")) && child.gameObject.activeSelf)
            {
                Renderer? renderer = child.GetComponent<SkinnedMeshRenderer>();
                if (renderer == null)
                {
                    renderer = child.GetComponent<MeshRenderer>();
                }

                if (renderer != null)
                {
                    int colorIndex = (int)maneColor;
                    if (colorIndex >= 0 && colorIndex < ManeColorMaterials.Length)
                    {
                        var material = ManeColorMaterials[colorIndex];
                        if (material != null)
                        {
                            renderer.material = material;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 馬鎧を適用します
        /// </summary>
        private void ApplyArmor(Transform child, int armorId)
        {
            // 馬鎧の命名規則: "Horse_Armor", "Armor" など
            if (child.name.Contains("Armor") || child.name.Contains("Armour"))
            {
                if (armorId == 0)
                {
                    // 鎧なし
                    child.gameObject.SetActive(false);
                }
                else
                {
                    // 鎧あり
                    child.gameObject.SetActive(true);

                    // 鎧のマテリアルを適用
                    if (armorId > 0 && armorId < ArmorMaterials.Length)
                    {
                        var material = ArmorMaterials[armorId];
                        if (material != null)
                        {
                            Renderer? renderer = child.GetComponent<SkinnedMeshRenderer>();
                            if (renderer == null)
                            {
                                renderer = child.GetComponent<MeshRenderer>();
                            }

                            if (renderer != null)
                            {
                                renderer.material = material;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 鞍を適用します
        /// </summary>
        private void ApplySaddle(Transform child, bool hasSaddle)
        {
            // 鞍の命名規則: "Saddle", "Horse_Saddle" など
            if (child.name.Contains("Saddle"))
            {
                child.gameObject.SetActive(hasSaddle);

                if (hasSaddle && SaddleMaterial != null)
                {
                    Renderer? renderer = child.GetComponent<SkinnedMeshRenderer>();
                    if (renderer == null)
                    {
                        renderer = child.GetComponent<MeshRenderer>();
                    }

                    if (renderer != null)
                    {
                        renderer.material = SaddleMaterial;
                    }
                }
            }
        }

        /// <summary>
        /// 騎乗動物のタイプに応じた特殊処理を適用します
        /// </summary>
        private void ApplyMountTypeSpecific(Transform child, MountType mountType)
        {
            // ペガサスの翼
            if (child.name.Contains("Wing"))
            {
                child.gameObject.SetActive(mountType == MountType.Pegasus);
            }

            // ユニコーンの角
            if (child.name.Contains("Horn"))
            {
                child.gameObject.SetActive(mountType == MountType.Unicorn);
            }
        }

        #endregion
    }
}
