#nullable enable

using System.Collections.Generic;
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
    /// パフォーマンス最適化のため、子Transformをキャッシュします。
    /// </remarks>
    public class MalbersHorseApplier : IMountApplier
    {
        #region Fields

        /// <summary>
        /// GameObjectのインスタンスIDごとにTransform配列をキャッシュ
        /// </summary>
        /// <remarks>
        /// パフォーマンス最適化: 毎回GetComponentsInChildrenを呼ばないようにキャッシュ
        /// </remarks>
        private static Dictionary<int, Transform[]> _transformCache = new Dictionary<int, Transform[]>();

        /// <summary>
        /// 馬の毛色マテリアル配列（HorseColor enum順）
        /// </summary>
        /// <remarks>
        /// MUST match HorseColor enum order:
        /// 0: Black, 1: Brown, 2: Gray, 3: LightGray, 4: Palomino, 5: White
        /// </remarks>
        public Material[] CoatColorMaterials = new Material[6];

        /// <summary>
        /// たてがみの色マテリアル配列（ManeColor enum順）
        /// </summary>
        /// <remarks>
        /// MUST match ManeColor enum order:
        /// 0: Black, 1: Blond, 2: Brown, 3: Gray, 4: White
        /// Use opaque materials only (non-transparent)
        /// </remarks>
        public Material[] ManeColorMaterials = new Material[5];

        /// <summary>
        /// 角マテリアル配列（HornMaterial enum順）
        /// </summary>
        /// <remarks>
        /// 0: BlackPA, 1: BlackRealistic, 2: RedPA, 3: WhitePA, 4: WhitePalomino, 5: WhiteRealistic
        /// </remarks>
        public Material[] HornMaterials = new Material[6];

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
                // パフォーマンス最適化: キャッシュされたTransformを取得
                var allChildren = GetCachedTransforms(mountObject);

                // すべての子オブジェクトに対してカスタマイズを適用
                foreach (Transform child in allChildren)
                {
                    // 毛色を適用
                    ApplyCoatColor(child, customization.CoatColor);

                    // たてがみスタイルを適用
                    ApplyManeStyle(child, customization.ManeStyle);

                    // たてがみの色を適用
                    ApplyManeColor(child, customization.ManeColor);

                    // 角を適用
                    ApplyHorn(child, customization.HornType, customization.HornMaterial);

                    // 馬鎧を適用
                    ApplyArmor(child, customization.ArmorId);

                    // 鞍を適用
                    ApplySaddle(child, customization.HasSaddle);

                    // 手綱を適用
                    ApplyReins(child, customization.HasReins);
                }

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

            // Malbersの馬特有の構造（子オブジェクトの命名規則）で判定（キャッシュ使用）
            var allTransforms = GetCachedTransforms(mountObject);
            var hasMalbersStructure = allTransforms.Any(t => t.name.Contains("Horse") || t.name.Contains("Mane") || t.name.StartsWith("Horse_"));

            if (!hasMalbersStructure)
            {
                Debug.LogWarning($"[MalbersHorseApplier] No Malbers structure found in {mountObject.name}. Looking for children with 'Horse', 'Mane', or 'Horse_' in their names.");
            }

            return hasMalbersStructure;
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// GameObjectのすべての子Transformをキャッシュから取得します
        /// </summary>
        /// <param name="mountObject">対象のGameObject</param>
        /// <returns>キャッシュされたTransform配列</returns>
        /// <remarks>
        /// 初回呼び出し時にGetComponentsInChildrenを実行し、結果をキャッシュします。
        /// 2回目以降はキャッシュから取得するため、パフォーマンスが大幅に向上します。
        /// </remarks>
        private Transform[] GetCachedTransforms(GameObject mountObject)
        {
            int instanceId = mountObject.GetInstanceID();

            // キャッシュに存在する場合はそれを返す
            if (_transformCache.TryGetValue(instanceId, out var cachedTransforms))
            {
                // nullチェック: キャッシュされたTransformが破棄されていないか確認
                if (cachedTransforms != null && cachedTransforms.Length > 0 && cachedTransforms[0] != null)
                {
                    return cachedTransforms;
                }
                else
                {
                    // 破棄されている場合はキャッシュから削除
                    _transformCache.Remove(instanceId);
                }
            }

            // キャッシュにない場合は取得してキャッシュに追加
            var transforms = mountObject.GetComponentsInChildren<Transform>(true);
            _transformCache[instanceId] = transforms;
            return transforms;
        }

        /// <summary>
        /// キャッシュをクリアします
        /// </summary>
        /// <remarks>
        /// シーンの切り替え時やメモリ解放が必要な場合に呼び出してください。
        /// </remarks>
        public static void ClearCache()
        {
            _transformCache.Clear();
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
                            else
                            {
                                Debug.LogWarning($"[MalbersHorseApplier] First material doesn't contain 'Horse': {materials[0].name}");
                            }
                        }
                        else
                        {
                            Debug.LogError($"[MalbersHorseApplier] CoatColorMaterials[{colorIndex}] is null!");
                        }
                    }
                    else
                    {
                        Debug.LogError($"[MalbersHorseApplier] Invalid coat color index: {colorIndex}, array length: {CoatColorMaterials.Length}");
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
            // Mane/Mane 01 Short
            // Mane/Mane 02 Long/Mane 02 Long Left
            // Mane/Mane 02 Long/Mane 02 Long Right
            // Mane/Mane 03 V Shape/Horse_Mane04 V Left
            // Mane/Mane 03 V Shape/Horse_Mane04 V Right

            // たてがみ関連のオブジェクトのみ処理
            if (child.name.Contains("Mane") && !child.name.Contains("Tail"))
            {
                // 親フォルダはスキップ（常にアクティブのまま）
                // レンダラーコンポーネントがあるかチェック
                var renderer = child.GetComponent<SkinnedMeshRenderer>() ?? (Renderer)child.GetComponent<MeshRenderer>();
                if (renderer == null)
                {
                    // レンダラーがない = 親フォルダなのでスキップ
                    return;
                }

                bool shouldActivate = false;

                switch (maneStyle)
                {
                    case ManeStyle.Short:
                        shouldActivate = child.name.Contains("Mane 01 Short");
                        break;
                    case ManeStyle.LongLeft:
                        shouldActivate = child.name.Contains("Mane 02 Long Left");
                        break;
                    case ManeStyle.LongRight:
                        shouldActivate = child.name.Contains("Mane 02 Long Right");
                        break;
                    case ManeStyle.VShapeLeft:
                        shouldActivate = child.name.Contains("V Left");
                        break;
                    case ManeStyle.VShapeRight:
                        shouldActivate = child.name.Contains("V Right");
                        break;
                }

                // 実際のメッシュオブジェクトのみトグル
                if (child.gameObject.activeSelf != shouldActivate)
                {
                    child.gameObject.SetActive(shouldActivate);
                }
            }
        }

        /// <summary>
        /// たてがみの色を適用します
        /// </summary>
        private void ApplyManeColor(Transform child, ManeColor maneColor)
        {
            // たてがみと尻尾にマテリアルを適用
            // LOD0とLOD1の両方に適用する必要があるため、activeステータスはチェックしない
            bool isMane = child.name.Contains("Mane") && !child.name.Contains("Tail");
            bool isTail = child.name.Contains("Tail");

            if (isMane || isTail)
            {
                Renderer? renderer = child.GetComponent<SkinnedMeshRenderer>();
                if (renderer == null)
                {
                    renderer = child.GetComponent<MeshRenderer>();
                }

                if (renderer != null && renderer.sharedMaterial != null)
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
        /// 角を適用します
        /// </summary>
        private void ApplyHorn(Transform child, HornType hornType, HornMaterial hornMaterial)
        {
            // 角の命名規則: Horns/Horn1, Horns/Horn2
            if (child.name.Contains("Horn1") || child.name.Contains("Horn2"))
            {
                bool shouldActivate = false;

                // メッシュタイプに基づいて適切な角を表示
                switch (hornType)
                {
                    case HornType.None:
                        shouldActivate = false;
                        break;
                    case HornType.Horn1:
                        shouldActivate = child.name.Contains("Horn1");
                        break;
                    case HornType.Horn2:
                        shouldActivate = child.name.Contains("Horn2");
                        break;
                }

                if (child.gameObject.activeSelf != shouldActivate)
                {
                    child.gameObject.SetActive(shouldActivate);
                }

                // 角が有効な場合、マテリアルを適用
                if (shouldActivate)
                {
                    int materialIndex = (int)hornMaterial;
                    if (materialIndex >= 0 && materialIndex < HornMaterials.Length)
                    {
                        var material = HornMaterials[materialIndex];
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
        /// 馬鎧を適用します
        /// </summary>
        private void ApplyArmor(Transform child, int armorId)
        {
            // 馬鎧の命名規則: Armor/Armour01 (すべての鎧タイプで使用)
            // ArmorId 0 = 鎧なし、1-3 = Armour01メッシュに異なるマテリアルを適用
            if (child.name.Contains("Armour01"))
            {
                // ArmorId 0 = 鎧なし、1-3 = 鎧あり
                bool shouldActivate = armorId > 0;

                if (child.gameObject.activeSelf != shouldActivate)
                {
                    child.gameObject.SetActive(shouldActivate);
                }

                // 鎧が有効な場合、マテリアルを適用
                if (shouldActivate && armorId > 0 && armorId < ArmorMaterials.Length)
                {
                    var material = ArmorMaterials[armorId];
                    if (material != null)
                    {
                        // SkinnedMeshRenderer または MeshRenderer を取得
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
            // Armour02とArmour03は使用しないので常に非アクティブ
            else if (child.name.Contains("Armour02") || child.name.Contains("Armour03"))
            {
                if (child.gameObject.activeSelf)
                {
                    child.gameObject.SetActive(false);
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
        /// 手綱を適用します
        /// </summary>
        private void ApplyReins(Transform child, bool hasReins)
        {
            // 手綱の命名規則: "Reins/Reins01_Head", "Reins/Reins01", "Reins/Reins Carriage"
            // 親の"Reins"フォルダではなく、具体的な手綱オブジェクトのみ切り替え
            if (child.name.Contains("Reins") && child.name != "Reins")
            {
                child.gameObject.SetActive(hasReins);
            }
        }

        #endregion
    }
}
