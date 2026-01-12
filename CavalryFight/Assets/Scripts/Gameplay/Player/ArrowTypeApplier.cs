#nullable enable

using UnityEngine;
using CavalryFight.Core.Services;
using CavalryFight.Services.Customization;
using MalbersAnimations;
using MalbersAnimations.Weapons;

namespace CavalryFight.Gameplay.Player
{
    /// <summary>
    /// カスタマイズサービスからArrowTypeを取得し、発射された矢のビジュアルを差し替えるコンポーネント
    /// </summary>
    /// <remarks>
    /// PlayerRiderプレハブにアタッチして使用します。
    /// Start時にICustomizationServiceから矢タイプを取得し、
    /// MShootableのOnFireProjectileイベントで矢のビジュアルメッシュを差し替えます。
    ///
    /// 注意: Malbersのプロジェクタイルシステム（MProjectile）は維持し、
    /// ビジュアル部分のみを差し替えることで物理・ダメージ機能を保持します。
    ///
    /// 使用方法:
    /// 1. ArrowTypeConfig ScriptableObjectを作成（Create → CavalryFight → Arrow Type Config）
    /// 2. ArrowTypeConfigにカスタム矢のビジュアルプレハブを設定
    /// 3. PlayerRiderプレハブにこのコンポーネントを追加
    /// 4. ArrowTypeConfigをアサイン
    /// </remarks>
    public class ArrowTypeApplier : MonoBehaviour
    {
        #region Constants

        /// <summary>
        /// Malbers Arrow プレハブ内のビジュアルメッシュの名前
        /// </summary>
        private const string ARROW_MESH_NAME = "Arrow Mesh";

        #endregion

        #region Serialized Fields

        [Header("Arrow Type Configuration")]
        [Tooltip("矢タイプ設定（ScriptableObject）- Create → CavalryFight → Arrow Type Config")]
        [SerializeField] private ArrowTypeConfig? _arrowTypeConfig;

        [Header("Visual Replacement Settings")]
        [Tooltip("カスタム矢ビジュアルのスケール調整")]
        [SerializeField] private Vector3 _visualScale = new Vector3(0.01f, 0.01f, 0.01f);

        [Tooltip("カスタム矢ビジュアルの位置オフセット")]
        [SerializeField] private Vector3 _visualPositionOffset = Vector3.zero;

        [Tooltip("カスタム矢ビジュアルの回転オフセット（オイラー角）")]
        [SerializeField] private Vector3 _visualRotationOffset = new Vector3(-90f, 0f, 0f);

        [Header("Debug")]
        [SerializeField] private bool _debugLog = true;

        #endregion

        #region Private Fields

        private MWeaponManager? _weaponManager;
        private MShootable? _currentShootable;
        private ArrowType _currentArrowType = ArrowType.Arrow;
        private GameObject? _currentVisualPrefab;

        #endregion

        #region Properties

        /// <summary>
        /// 現在の矢タイプを取得します
        /// </summary>
        public ArrowType CurrentArrowType => _currentArrowType;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // MWeaponManagerを取得
            _weaponManager = GetComponent<MWeaponManager>();
            if (_weaponManager == null)
            {
                _weaponManager = GetComponentInChildren<MWeaponManager>();
            }

            if (_weaponManager == null && _debugLog)
            {
                Debug.LogWarning("[ArrowTypeApplier] MWeaponManager が見つかりませんでした。");
            }
        }

        private void Start()
        {
            // カスタマイズサービスから矢タイプを適用
            ApplyArrowTypeFromCustomization();

            // 既に武器が装備されている場合は、遅延後にチェック
            // MWeaponManagerはDelay_Action()で武器を装備するため、少し待つ
            StartCoroutine(CheckExistingWeaponDelayed());
        }

        /// <summary>
        /// 既に装備されている武器をチェックするコルーチン
        /// </summary>
        private System.Collections.IEnumerator CheckExistingWeaponDelayed()
        {
            // MWeaponManagerのDelay_Actionが完了するまで待機
            yield return new WaitForSeconds(0.5f);

            if (_weaponManager != null && _weaponManager.Weapon != null)
            {
                if (_debugLog)
                {
                    Debug.Log($"[ArrowTypeApplier] 既存の武器を検出: {_weaponManager.Weapon.name}");
                }

                // 既に装備されている武器をチェック
                var shootable = _weaponManager.Weapon.GetComponent<MShootable>();
                if (shootable != null)
                {
                    SubscribeToShootable(shootable);
                }
                else if (_debugLog)
                {
                    Debug.Log($"[ArrowTypeApplier] 武器 {_weaponManager.Weapon.name} は MShootable ではありません");
                }
            }
            else if (_debugLog)
            {
                Debug.Log("[ArrowTypeApplier] 既存の武器はありません");
            }
        }

        private void OnEnable()
        {
            // 武器装備イベントを購読
            if (_weaponManager != null)
            {
                _weaponManager.OnEquipWeapon.AddListener(OnWeaponEquipped);
                _weaponManager.OnUnequipWeapon.AddListener(OnWeaponUnequipped);

                if (_debugLog)
                {
                    Debug.Log("[ArrowTypeApplier] MWeaponManager イベントを購読しました");
                }
            }
            else if (_debugLog)
            {
                Debug.LogWarning("[ArrowTypeApplier] OnEnable: MWeaponManager が null です");
            }
        }

        private void OnDisable()
        {
            // 武器装備イベントの購読を解除
            if (_weaponManager != null)
            {
                _weaponManager.OnEquipWeapon.RemoveListener(OnWeaponEquipped);
                _weaponManager.OnUnequipWeapon.RemoveListener(OnWeaponUnequipped);
            }

            // MShootableイベントの購読解除
            UnsubscribeFromShootable();
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// 武器装備時のコールバック
        /// </summary>
        private void OnWeaponEquipped(GameObject weapon)
        {
            if (_debugLog)
            {
                Debug.Log($"[ArrowTypeApplier] OnWeaponEquipped: {weapon.name}");
            }

            // MShootableの場合、発射イベントを購読
            var shootable = weapon.GetComponent<MShootable>();
            if (shootable != null)
            {
                SubscribeToShootable(shootable);
            }
            else if (_debugLog)
            {
                Debug.Log($"[ArrowTypeApplier] {weapon.name} は MShootable ではありません");
            }
        }

        /// <summary>
        /// 武器解除時のコールバック
        /// </summary>
        private void OnWeaponUnequipped(GameObject weapon)
        {
            UnsubscribeFromShootable();
        }

        /// <summary>
        /// プロジェクタイル発射時のコールバック
        /// </summary>
        private void OnProjectileFired(GameObject projectile)
        {
            if (_debugLog)
            {
                Debug.Log($"[ArrowTypeApplier] OnProjectileFired: {(projectile != null ? projectile.name : "null")} | VisualPrefab: {(_currentVisualPrefab != null ? _currentVisualPrefab.name : "null")}");
            }

            if (projectile == null)
            {
                if (_debugLog)
                {
                    Debug.LogWarning("[ArrowTypeApplier] projectile が null です");
                }
                return;
            }

            if (_currentVisualPrefab == null)
            {
                if (_debugLog)
                {
                    Debug.Log("[ArrowTypeApplier] _currentVisualPrefab が null のため、ビジュアル置換をスキップ");
                }
                return;
            }

            ReplaceProjectileVisual(projectile);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// カスタマイズサービスから矢タイプを適用します
        /// </summary>
        public void ApplyArrowTypeFromCustomization()
        {
            var customizationService = ServiceLocator.Instance.Get<ICustomizationService>();
            if (customizationService == null)
            {
                if (_debugLog)
                {
                    Debug.Log("[ArrowTypeApplier] ICustomizationService が取得できませんでした。デフォルトの矢を使用します。");
                }
                return;
            }

            ArrowType arrowType = customizationService.CurrentCharacter.ArrowType;
            SetArrowType(arrowType);
        }

        /// <summary>
        /// 矢タイプを設定します
        /// </summary>
        /// <param name="arrowType">設定する矢タイプ</param>
        public void SetArrowType(ArrowType arrowType)
        {
            _currentArrowType = arrowType;

            if (_arrowTypeConfig == null)
            {
                Debug.LogWarning("[ArrowTypeApplier] ArrowTypeConfig が設定されていません。");
                return;
            }

            // ビジュアルプレハブを取得
            _currentVisualPrefab = _arrowTypeConfig.GetArrowPrefab(_currentArrowType);

            if (_debugLog)
            {
                if (_currentVisualPrefab != null)
                {
                    Debug.Log($"[ArrowTypeApplier] 矢タイプを設定: {_currentArrowType} → {_currentVisualPrefab.name}");
                }
                else
                {
                    Debug.Log($"[ArrowTypeApplier] 矢タイプ {_currentArrowType} のビジュアルプレハブが未設定。デフォルトを使用します。");
                }
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// MShootableの発射イベントを購読します
        /// </summary>
        private void SubscribeToShootable(MShootable shootable)
        {
            UnsubscribeFromShootable();

            _currentShootable = shootable;
            _currentShootable.OnFireProjectile.AddListener(OnProjectileFired);

            if (_debugLog)
            {
                Debug.Log($"[ArrowTypeApplier] MShootable のイベントを購読: {shootable.name}");
            }
        }

        /// <summary>
        /// MShootableの発射イベントの購読を解除します
        /// </summary>
        private void UnsubscribeFromShootable()
        {
            if (_currentShootable != null)
            {
                _currentShootable.OnFireProjectile.RemoveListener(OnProjectileFired);
                _currentShootable = null;
            }
        }

        /// <summary>
        /// プロジェクタイルのビジュアルを差し替えます
        /// </summary>
        /// <param name="projectile">発射されたプロジェクタイル</param>
        private void ReplaceProjectileVisual(GameObject projectile)
        {
            if (_currentVisualPrefab == null)
            {
                return;
            }

            if (_debugLog)
            {
                Debug.Log($"[ArrowTypeApplier] ReplaceProjectileVisual 開始: {projectile.name}");
                // プロジェクタイルの子オブジェクトをリスト表示
                for (int i = 0; i < projectile.transform.childCount; i++)
                {
                    Debug.Log($"[ArrowTypeApplier]   子オブジェクト[{i}]: {projectile.transform.GetChild(i).name}");
                }
            }

            // 元のビジュアルメッシュを検索して非表示にする
            Transform? originalMesh = projectile.transform.Find(ARROW_MESH_NAME);
            if (originalMesh != null)
            {
                originalMesh.gameObject.SetActive(false);

                if (_debugLog)
                {
                    Debug.Log($"[ArrowTypeApplier] 元のビジュアルメッシュを非表示: {originalMesh.name}");
                }
            }
            else if (_debugLog)
            {
                Debug.LogWarning($"[ArrowTypeApplier] '{ARROW_MESH_NAME}' が見つかりませんでした。");
            }

            // カスタムビジュアルをインスタンス化してプロジェクタイルの子にする
            GameObject customVisual = Instantiate(_currentVisualPrefab, projectile.transform);
            customVisual.name = "Custom Arrow Visual";

            // 位置・回転・スケールを設定
            customVisual.transform.localPosition = _visualPositionOffset;
            customVisual.transform.localRotation = Quaternion.Euler(_visualRotationOffset);
            customVisual.transform.localScale = _visualScale;

            if (_debugLog)
            {
                Debug.Log($"[ArrowTypeApplier] カスタムビジュアル配置: pos={_visualPositionOffset}, rot={_visualRotationOffset}, scale={_visualScale}");
            }

            // レイヤーを親に合わせる
            SetLayerRecursively(customVisual, projectile.layer);

            // カスタムビジュアルのコライダーとRigidbodyを無効化（ビジュアル専用にする）
            DisablePhysicsRecursively(customVisual);

            if (_debugLog)
            {
                Debug.Log($"[ArrowTypeApplier] カスタムビジュアルを適用完了: {_currentVisualPrefab.name} → {projectile.name}");
            }
        }

        /// <summary>
        /// GameObjectとその子孫のレイヤーを再帰的に設定します
        /// </summary>
        private void SetLayerRecursively(GameObject obj, int layer)
        {
            obj.layer = layer;
            foreach (Transform child in obj.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }

        /// <summary>
        /// GameObjectとその子孫のコライダーとRigidbodyを無効化します
        /// </summary>
        private void DisablePhysicsRecursively(GameObject obj)
        {
            // コライダーを無効化
            var colliders = obj.GetComponentsInChildren<Collider>(true);
            foreach (var collider in colliders)
            {
                collider.enabled = false;
            }

            // Rigidbodyを無効化（kinematicにする）
            var rigidbodies = obj.GetComponentsInChildren<Rigidbody>(true);
            foreach (var rb in rigidbodies)
            {
                rb.isKinematic = true;
                rb.detectCollisions = false;
            }

            if (_debugLog && (colliders.Length > 0 || rigidbodies.Length > 0))
            {
                Debug.Log($"[ArrowTypeApplier] 物理コンポーネントを無効化: Colliders={colliders.Length}, Rigidbodies={rigidbodies.Length}");
            }
        }

        #endregion
    }
}
