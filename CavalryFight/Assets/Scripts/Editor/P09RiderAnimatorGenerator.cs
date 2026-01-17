#nullable enable

using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;

namespace CavalryFight.Editor
{
    /// <summary>
    /// P09 Rider用のAnimatorControllerとAvatarMaskを自動生成するエディタスクリプト
    /// </summary>
    public static class P09RiderAnimatorGenerator
    {
        // 出力パス
        private const string OutputFolder = "Assets/Animations/Controllers";
        private const string ControllerName = "P09_Rider_Controller";
        private const string MaskName = "P09_UpperBody_Mask";

        // アニメーションクリップのパス
        private const string RiderIdleClipPath = "Assets/Malbers Animations/Horse AnimSet Pro/2 - Animations/Animations Clips/Rider/Rider_Idle_01.FBX";
        private const string RiderBowClipPath = "Assets/Malbers Animations/Horse AnimSet Pro/2 - Animations/Animations Clips/Rider/Weapons While Riding/Rider_Weapon_Bow.FBX";

        [MenuItem("CavalryFight/Animation/Generate P09 Rider Controller")]
        public static void GenerateP09RiderController()
        {
            // 出力フォルダを作成
            if (!AssetDatabase.IsValidFolder(OutputFolder))
            {
                string[] folders = OutputFolder.Split('/');
                string currentPath = folders[0];
                for (int i = 1; i < folders.Length; i++)
                {
                    string nextPath = currentPath + "/" + folders[i];
                    if (!AssetDatabase.IsValidFolder(nextPath))
                    {
                        AssetDatabase.CreateFolder(currentPath, folders[i]);
                    }
                    currentPath = nextPath;
                }
            }

            // Avatar Maskを作成
            AvatarMask upperBodyMask = CreateUpperBodyMask();

            // AnimatorControllerを作成
            AnimatorController controller = CreateAnimatorController(upperBodyMask);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[P09RiderAnimatorGenerator] AnimatorController と AvatarMask を生成しました:\n" +
                      $"  Controller: {OutputFolder}/{ControllerName}.controller\n" +
                      $"  Mask: {OutputFolder}/{MaskName}.mask");

            // 生成したコントローラーを選択
            Selection.activeObject = controller;
        }

        /// <summary>
        /// 上半身用のAvatarMaskを作成します
        /// </summary>
        private static AvatarMask CreateUpperBodyMask()
        {
            string maskPath = $"{OutputFolder}/{MaskName}.mask";

            // 既存のマスクがあれば削除
            AvatarMask? existingMask = AssetDatabase.LoadAssetAtPath<AvatarMask>(maskPath);
            if (existingMask != null)
            {
                AssetDatabase.DeleteAsset(maskPath);
            }

            AvatarMask mask = new AvatarMask();
            mask.name = MaskName;

            // Humanoidボディパーツの設定
            // true = 有効（アニメーション適用）, false = 無効
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Root, false);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Body, true);      // 胴体
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Head, true);      // 頭
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftLeg, false);  // 左足（無効）
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightLeg, false); // 右足（無効）
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftArm, true);   // 左腕
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightArm, true);  // 右腕
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftFingers, true);  // 左指
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightFingers, true); // 右指
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftFootIK, false);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightFootIK, false);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftHandIK, false);
            mask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightHandIK, false);

            AssetDatabase.CreateAsset(mask, maskPath);

            return mask;
        }

        /// <summary>
        /// AnimatorControllerを作成します
        /// </summary>
        private static AnimatorController CreateAnimatorController(AvatarMask upperBodyMask)
        {
            string controllerPath = $"{OutputFolder}/{ControllerName}.controller";

            // 既存のコントローラーがあれば削除
            AnimatorController? existingController = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (existingController != null)
            {
                AssetDatabase.DeleteAsset(controllerPath);
            }

            // 新しいAnimatorControllerを作成
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

            // パラメータを追加
            controller.AddParameter("IsMounted", AnimatorControllerParameterType.Bool);
            controller.AddParameter("IsAiming", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Shoot", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("ChargeAmount", AnimatorControllerParameterType.Float);
            controller.AddParameter("AimHorizontal", AnimatorControllerParameterType.Float);
            controller.AddParameter("AimVertical", AnimatorControllerParameterType.Float);

            // アニメーションクリップを取得
            AnimationClip? riderIdleClip = LoadAnimationClip(RiderIdleClipPath);
            AnimationClip? riderBowClip = LoadAnimationClip(RiderBowClipPath);

            // Base Layer の設定
            AnimatorStateMachine baseStateMachine = controller.layers[0].stateMachine;
            SetupBaseLayer(baseStateMachine, riderIdleClip);

            // Aim Layer を追加
            AddAimLayer(controller, upperBodyMask, riderBowClip);

            return controller;
        }

        /// <summary>
        /// Base Layerを設定します
        /// </summary>
        private static void SetupBaseLayer(AnimatorStateMachine stateMachine, AnimationClip? riderIdleClip)
        {
            // Rider_Idle ステートを作成
            AnimatorState riderIdleState = stateMachine.AddState("Rider_Idle", new Vector3(300, 0, 0));

            if (riderIdleClip != null)
            {
                riderIdleState.motion = riderIdleClip;
            }
            else
            {
                Debug.LogWarning("[P09RiderAnimatorGenerator] Rider_Idle_01 アニメーションが見つかりません。手動で設定してください。");
            }

            // デフォルトステートに設定
            stateMachine.defaultState = riderIdleState;
        }

        /// <summary>
        /// Aim Layerを追加します
        /// </summary>
        private static void AddAimLayer(AnimatorController controller, AvatarMask mask, AnimationClip? bowClip)
        {
            // 新しいレイヤーを追加
            AnimatorControllerLayer aimLayer = new AnimatorControllerLayer
            {
                name = "Aim",
                defaultWeight = 0f, // コードで制御
                avatarMask = mask,
                blendingMode = AnimatorLayerBlendingMode.Override,
                stateMachine = new AnimatorStateMachine
                {
                    name = "Aim",
                    hideFlags = HideFlags.HideInHierarchy
                }
            };

            // StateMachineをアセットに追加
            AssetDatabase.AddObjectToAsset(aimLayer.stateMachine, controller);

            // Empty ステート（デフォルト）
            AnimatorState emptyState = aimLayer.stateMachine.AddState("Empty", new Vector3(300, 0, 0));
            aimLayer.stateMachine.defaultState = emptyState;

            // Bow_Aim ステート
            AnimatorState bowAimState = aimLayer.stateMachine.AddState("Bow_Aim", new Vector3(300, 100, 0));
            if (bowClip != null)
            {
                bowAimState.motion = bowClip;
            }
            else
            {
                Debug.LogWarning("[P09RiderAnimatorGenerator] Rider_Weapon_Bow アニメーションが見つかりません。手動で設定してください。");
            }

            // トランジション: Empty → Bow_Aim (IsAiming = true)
            AnimatorStateTransition toAim = emptyState.AddTransition(bowAimState);
            toAim.AddCondition(AnimatorConditionMode.If, 0, "IsAiming");
            toAim.hasExitTime = false;
            toAim.duration = 0.15f;

            // トランジション: Bow_Aim → Empty (IsAiming = false)
            AnimatorStateTransition fromAim = bowAimState.AddTransition(emptyState);
            fromAim.AddCondition(AnimatorConditionMode.IfNot, 0, "IsAiming");
            fromAim.hasExitTime = false;
            fromAim.duration = 0.25f;

            // レイヤーを追加
            controller.AddLayer(aimLayer);
        }

        /// <summary>
        /// FBXからアニメーションクリップを読み込みます
        /// </summary>
        private static AnimationClip? LoadAnimationClip(string fbxPath)
        {
            // FBXに含まれるすべてのアセットを取得
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);

            foreach (Object asset in assets)
            {
                if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                {
                    return clip;
                }
            }

            return null;
        }

        [MenuItem("CavalryFight/Animation/Assign Controller to PlayerRider")]
        public static void AssignControllerToPlayerRider()
        {
            string controllerPath = $"{OutputFolder}/{ControllerName}.controller";
            AnimatorController? controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);

            if (controller == null)
            {
                EditorUtility.DisplayDialog("Error",
                    "P09_Rider_Controller が見つかりません。\n先に 'Generate P09 Rider Controller' を実行してください。",
                    "OK");
                return;
            }

            // PlayerRiderプレハブを検索
            string[] guids = AssetDatabase.FindAssets("PlayerRider t:Prefab");
            if (guids.Length == 0)
            {
                EditorUtility.DisplayDialog("Error",
                    "PlayerRider プレハブが見つかりません。",
                    "OK");
                return;
            }

            foreach (string guid in guids)
            {
                string prefabPath = AssetDatabase.GUIDToAssetPath(guid);
                GameObject? prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

                if (prefab != null)
                {
                    // P09モデルを探す
                    Animator? animator = FindP09Animator(prefab);
                    if (animator != null)
                    {
                        // プレハブを編集モードで開く
                        string assetPath = AssetDatabase.GetAssetPath(prefab);
                        using (var editScope = new PrefabUtility.EditPrefabContentsScope(assetPath))
                        {
                            Animator? editAnimator = FindP09Animator(editScope.prefabContentsRoot);
                            if (editAnimator != null)
                            {
                                editAnimator.runtimeAnimatorController = controller;
                                Debug.Log($"[P09RiderAnimatorGenerator] コントローラーを設定しました: {prefabPath}");
                            }
                        }
                    }
                }
            }

            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("Complete",
                "PlayerRider プレハブにコントローラーを設定しました。",
                "OK");
        }

        /// <summary>
        /// P09モデルのAnimatorを探します
        /// </summary>
        private static Animator? FindP09Animator(GameObject root)
        {
            // 直接P09を含む名前のオブジェクトを探す
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name.Contains("P09"))
                {
                    Animator? animator = child.GetComponent<Animator>();
                    if (animator != null)
                    {
                        return animator;
                    }
                }
            }

            // 見つからない場合は最初のAnimatorを返す
            return root.GetComponentInChildren<Animator>();
        }
    }
}
