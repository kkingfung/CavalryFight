#nullable enable

using System.Linq;
using CavalryFight.Core.Services;
using CavalryFight.Services.Customization;
using UnityEngine;

namespace CavalryFight.Views
{
    /// <summary>
    /// CustomizationView partial class - 3Dプレビュー管理
    /// </summary>
    public partial class CustomizationView
    {
        #region Preview Initialization

        /// <summary>
        /// プレビューオブジェクトを初期化します（シーン開始時に両方インスタンス化）
        /// </summary>
        private void InitializePreviewObjects()
        {
            if (ViewModel == null)
            {
                Debug.LogError("[CustomizationView] ViewModel is null! Cannot initialize preview objects.");
                return;
            }

            var customizationService = ServiceLocator.Instance.Get<ICustomizationService>();
            if (customizationService == null)
            {
                Debug.LogError("[CustomizationView] CustomizationService not found!");
                return;
            }

            // キャラクタープレビューをインスタンス化（非アクティブ状態で作成）
            if (_characterPreviewPrefab != null && _currentPreviewCharacter == null)
            {
                _currentPreviewCharacter = Instantiate(_characterPreviewPrefab, _Container, false);

                // 即座に非アクティブ化（念のため）
                _currentPreviewCharacter.SetActive(false);

                // Transform設定
                _currentPreviewCharacter.transform.localPosition = Vector3.zero;
                _currentPreviewCharacter.transform.localRotation = Quaternion.identity;
                SetLayerRecursively(_currentPreviewCharacter, LayerMask.NameToLayer("Preview"));

                // 非表示のままカスタマイズを適用
                var p09Applier = customizationService.GetP09CharacterApplier();
                if (p09Applier != null)
                {
                    p09Applier.Apply(_currentPreviewCharacter, ViewModel.WorkingCharacter);
                }

                // Animatorを設定
                AssignAnimatorController(_currentPreviewCharacter, ViewModel.WorkingCharacter.Gender);
                EnableIdleAnimation(_currentPreviewCharacter, ViewModel.WorkingCharacter.Gender);

                // 非表示のまま維持（タブ切り替え時に表示）
            }
            else if (_characterPreviewPrefab == null)
            {
                Debug.LogWarning("[CustomizationView] _characterPreviewPrefab is null! Character preview will not be available.");
            }

            // 馬プレビューをインスタンス化（非アクティブ状態で作成）
            if (_mountPreviewPrefab != null && _currentPreviewMount == null)
            {
                _currentPreviewMount = Instantiate(_mountPreviewPrefab, _Container, false);

                // 即座に非アクティブ化（念のため）
                _currentPreviewMount.SetActive(false);

                // Transform設定
                _currentPreviewMount.transform.localPosition = Vector3.zero;
                _currentPreviewMount.transform.localRotation = Quaternion.identity;
                SetLayerRecursively(_currentPreviewMount, LayerMask.NameToLayer("Preview"));

                // 非表示のままカスタマイズを適用
                var malbersApplier = customizationService.GetMalbersHorseApplier();
                if (malbersApplier != null)
                {
                    malbersApplier.Apply(_currentPreviewMount, ViewModel.WorkingMount);
                }

                // Animationを設定
                EnableIdleAnimation(_currentPreviewMount, null);

                // 非表示のまま維持（タブ切り替え時に表示）
            }
            else if (_mountPreviewPrefab == null)
            {
                Debug.LogWarning("[CustomizationView] _mountPreviewPrefab is null! Mount preview will not be available.");
            }
        }

        /// <summary>
        /// プレビューオブジェクトを破棄します
        /// </summary>
        private void DestroyPreviewObjects()
        {
            if (_currentPreviewCharacter != null)
            {
                Destroy(_currentPreviewCharacter);
                _currentPreviewCharacter = null;
            }

            if (_currentPreviewMount != null)
            {
                Destroy(_currentPreviewMount);
                _currentPreviewMount = null;
            }

            // 注意: _previewContainer自体は破棄しない（シーンに配置された永続的なオブジェクト）
        }

        #endregion

        #region Preview Update

        /// <summary>
        /// プレビューを更新します
        /// </summary>
        private void UpdatePreview()
        {
            // 再入防止: すでに更新中の場合は何もしない
            if (_isUpdatingPreview)
            {
                return;
            }

            _isUpdatingPreview = true;

            try
            {
                if (ViewModel == null)
                {
                    return;
                }

                if (_previewCamera == null)
                {
                    return;
                }

                var customizationService = ServiceLocator.Instance.Get<ICustomizationService>();
                if (customizationService == null)
                {
                    return;
                }

                if (ViewModel.IsCharacterCategory)
                {
                    Debug.Log("[CustomizationView] Updating character preview...");

                    // キャラクターを表示・カスタマイズ適用
                    if (_currentPreviewCharacter != null)
                    {
                        _currentPreviewCharacter.SetActive(true);

                        AssignAnimatorController(_currentPreviewCharacter, ViewModel.WorkingCharacter.Gender);

                        // P09CharacterApplierを直接使用してカスタマイズを適用
                        var p09Applier = customizationService.GetP09CharacterApplier();
                        if (p09Applier != null)
                        {
                            p09Applier.Apply(_currentPreviewCharacter, ViewModel.WorkingCharacter);
                        }
                        else
                        {
                            Debug.LogError("[CustomizationView] P09CharacterApplier not found!");
                        }

                        // アニメーションを再生（現在のモードを保持）
                        // 戦闘待機モードの場合はそのモードを維持し、通常モードの場合はアイドルアニメーションを再生
                        if (_isCombatIdleMode)
                        {
                            // 戦闘待機モードを再適用
                            customizationService.SetCharacterCombatIdleMode(_currentPreviewCharacter, true);
                        }
                        else
                        {
                            // 通常のアイドルアニメーション
                            EnableIdleAnimation(_currentPreviewCharacter, ViewModel.WorkingCharacter.Gender);
                        }
                    }
                    else
                    {
                        Debug.LogWarning("[CustomizationView] Character preview object not initialized!");
                    }

                    // 馬を非表示
                    if (_currentPreviewMount != null)
                    {
                        _currentPreviewMount.SetActive(false);
                    }
                }
                else if (ViewModel.IsMountCategory)
                {
                    Debug.Log("[CustomizationView] Updating mount preview...");

                    // 馬を表示・カスタマイズ適用
                    if (_currentPreviewMount != null)
                    {
                        _currentPreviewMount.SetActive(true);

                        // MalbersHorseApplierを直接使用してカスタマイズを適用
                        var malbersApplier = customizationService.GetMalbersHorseApplier();
                        if (malbersApplier != null)
                        {
                            malbersApplier.Apply(_currentPreviewMount, ViewModel.WorkingMount);
                        }
                        else
                        {
                            Debug.LogError("[CustomizationView] MalbersHorseApplier not found!");
                        }

                        // アニメーションを再生（アイドルポーズ）
                        EnableIdleAnimation(_currentPreviewMount, null);
                    }
                    else
                    {
                        Debug.LogWarning("[CustomizationView] Mount preview object not initialized!");
                    }

                    // キャラクターを非表示
                    if (_currentPreviewCharacter != null)
                    {
                        _currentPreviewCharacter.SetActive(false);
                    }
                }
            }
            finally
            {
                _isUpdatingPreview = false;
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// GameObjectとその全ての子オブジェクトのレイヤーを再帰的に設定します
        /// </summary>
        /// <param name="obj">対象のGameObject</param>
        /// <param name="layer">設定するレイヤー</param>
        private void SetLayerRecursively(GameObject obj, int layer)
        {
            if (obj == null)
            {
                return;
            }

            obj.layer = layer;

            foreach (Transform child in obj.transform)
            {
                if (child != null)
                {
                    SetLayerRecursively(child.gameObject, layer);
                }
            }
        }

        /// <summary>
        /// GameObjectとその子オブジェクトの物理演算を無効化します
        /// </summary>
        /// <param name="obj">対象のGameObject</param>
        /// <remarks>
        /// プレビューオブジェクトが重力で落下しないように、
        /// すべてのRigidbodyコンポーネントをkinematicに設定し、
        /// Malbersの Animalコンポーネントも無効化します。
        /// </remarks>
        private void DisablePhysics(GameObject obj)
        {
            if (obj == null)
            {
                return;
            }

            // 自身と全ての子オブジェクトのRigidbodyを取得
            var rigidbodies = obj.GetComponentsInChildren<Rigidbody>(true);
            foreach (var rb in rigidbodies)
            {
                if (rb != null)
                {
                    rb.isKinematic = true;
                    rb.useGravity = false;
                    rb.detectCollisions = false;
                    Debug.Log($"[CustomizationView] Disabled physics on Rigidbody: {rb.gameObject.name}");
                }
            }

            // Malbers Animal componentを無効化（馬の場合）
            var animalComponents = obj.GetComponentsInChildren<Component>(true)
                .Where(c => c.GetType().Name == "Animal" || c.GetType().Name.Contains("MAnimal"))
                .ToArray();

            foreach (var animal in animalComponents)
            {
                if (animal != null)
                {
                    var animalBehaviour = animal as MonoBehaviour;
                    if (animalBehaviour != null)
                    {
                        animalBehaviour.enabled = false;
                        Debug.Log($"[CustomizationView] Disabled Malbers Animal component on {animal.gameObject.name}");
                    }
                }
            }

            Debug.Log($"[CustomizationView] Physics disabled: {rigidbodies.Length} Rigidbodies, {animalComponents.Length} Animal components in {obj.name}");
        }

        #endregion

        #region Animation Management

        /// <summary>
        /// アイドルアニメーションを有効化し、物理演算を無効化します
        /// </summary>
        /// <param name="previewObject">プレビューオブジェクト</param>
        /// <param name="gender">性別（キャラクターの場合のみ）</param>
        private void EnableIdleAnimation(GameObject previewObject, Gender? gender)
        {
            if (previewObject == null)
            {
                return;
            }

            // 物理演算を無効化（プレビューが落下しないように）
            DisablePhysics(previewObject);

            var animator = previewObject.GetComponent<Animator>();
            if (animator == null)
            {
                animator = previewObject.GetComponentInChildren<Animator>();
            }

            if (animator == null)
            {
                Debug.LogWarning($"[CustomizationView] No Animator found on {previewObject.name}. Cannot play idle animation.");
                return;
            }

            // Animatorを有効化
            animator.enabled = true;

            Debug.Log($"[CustomizationView] Found Animator on {animator.gameObject.name}, controller: {animator.runtimeAnimatorController?.name ?? "NULL"}");

            // キャラクターの場合、性別に応じたアイドルアニメーションを再生
            if (gender.HasValue)
            {
                // P09のアニメーションステートを試す（複数の命名規則に対応）
                string[] possibleStates = gender.Value == Gender.Male
                    ? new[] { "Idle", "idle", "P09_Male_idle", "P09 Male idle", "Male_Idle" }
                    : new[] { "Idle", "idle", "P09_Fem_idle", "P09 Fem idle", "Female_Idle" };

                bool foundAnimation = false;
                foreach (var stateName in possibleStates)
                {
                    if (HasAnimationState(animator, stateName))
                    {
                        animator.Play(stateName);
                        Debug.Log($"[CustomizationView] Playing '{stateName}' animation for character.");
                        foundAnimation = true;
                        break;
                    }
                }

                if (!foundAnimation)
                {
                    Debug.LogWarning($"[CustomizationView] No idle animation found. Tried: {string.Join(", ", possibleStates)}. Using default state.");
                }
            }
            else
            {
                // 馬の場合、Malbersのアニメーションシステムを使用
                // Malbersは通常Animalコンポーネントで制御されるため、
                // Animatorを直接操作せずにデフォルトのステートを使用
                Debug.Log($"[CustomizationView] Mount animator enabled. Using Malbers default animation system.");
            }
        }

        /// <summary>
        /// Animatorに指定されたステートが存在するかチェックします
        /// </summary>
        /// <param name="animator">Animator</param>
        /// <param name="stateName">ステート名</param>
        /// <returns>ステートが存在する場合はtrue</returns>
        private bool HasAnimationState(Animator animator, string stateName)
        {
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                return false;
            }

            foreach (var clip in animator.runtimeAnimatorController.animationClips)
            {
                if (clip.name == stateName)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// P09キャラクターにAnimator Controllerを割り当てます
        /// </summary>
        /// <param name="characterObject">キャラクターオブジェクト</param>
        /// <param name="gender">性別</param>
        /// <remarks>
        /// T-pose問題を修正するため、性別に応じた正しいAnimator Controllerを割り当てます
        /// </remarks>
        private void AssignAnimatorController(GameObject characterObject, Gender gender)
        {
            if (characterObject == null)
            {
                return;
            }

            var animator = characterObject.GetComponent<Animator>();
            if (animator == null)
            {
                animator = characterObject.GetComponentInChildren<Animator>();
            }

            if (animator == null)
            {
                Debug.LogWarning($"[CustomizationView] No Animator found on {characterObject.name}. Cannot assign controller.");
                return;
            }

            // 性別に応じてAnimator Controllerを割り当て
            RuntimeAnimatorController? controller = gender == Gender.Male ? _maleAnimatorController : _femaleAnimatorController;

            if (controller != null)
            {
                animator.runtimeAnimatorController = controller;
                Debug.Log($"[CustomizationView] Assigned {controller.name} to {animator.gameObject.name}");
            }
            else
            {
                Debug.LogWarning($"[CustomizationView] {(gender == Gender.Male ? "Male" : "Female")} Animator Controller not assigned in Inspector!");
            }
        }

        #endregion
    }
}
