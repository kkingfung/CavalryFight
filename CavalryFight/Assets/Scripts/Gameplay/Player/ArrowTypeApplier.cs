#nullable enable

using UnityEngine;
using CavalryFight.Core.Services;
using CavalryFight.Services.Customization;
using MalbersAnimations;
using MalbersAnimations.Weapons;

namespace CavalryFight.Gameplay.Player
{
    /// <summary>
    /// カスタマイズサービスからArrowTypeを取得し、MShootableに適用するコンポーネント
    /// </summary>
    /// <remarks>
    /// PlayerRiderプレハブにアタッチして使用します。
    /// Start時にICustomizationServiceから矢タイプを取得し、
    /// MWeaponManagerの弓のプロジェクタイルを設定します。
    ///
    /// 使用方法:
    /// 1. ArrowTypeConfig ScriptableObjectを作成（Create → CavalryFight → Arrow Type Config）
    /// 2. ArrowTypeConfigに14種類の矢プレハブを設定
    /// 3. PlayerRiderプレハブにこのコンポーネントを追加
    /// 4. ArrowTypeConfigをアサイン
    /// </remarks>
    public class ArrowTypeApplier : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Arrow Type Configuration")]
        [Tooltip("矢タイプ設定（ScriptableObject）- Create → CavalryFight → Arrow Type Config")]
        [SerializeField] private ArrowTypeConfig? _arrowTypeConfig;

        [Header("Debug")]
        [SerializeField] private bool _debugLog = true;

        #endregion

        #region Private Fields

        private MWeaponManager? _weaponManager;
        private ArrowType _currentArrowType = ArrowType.Arrow;
        private bool _applied = false;

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
        }

        private void OnEnable()
        {
            // 武器装備イベントを購読
            if (_weaponManager != null)
            {
                _weaponManager.OnEquipWeapon.AddListener(OnWeaponEquipped);
            }
        }

        private void OnDisable()
        {
            // 武器装備イベントの購読を解除
            if (_weaponManager != null)
            {
                _weaponManager.OnEquipWeapon.RemoveListener(OnWeaponEquipped);
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// 武器装備時のコールバック
        /// </summary>
        private void OnWeaponEquipped(GameObject weapon)
        {
            // 武器が装備されたらプロジェクタイルを設定
            if (!_applied)
            {
                ApplyToCurrentWeapon();
            }
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
            _applied = false;

            if (_arrowTypeConfig == null)
            {
                Debug.LogWarning("[ArrowTypeApplier] ArrowTypeConfig が設定されていません。");
                return;
            }

            // 現在の武器に適用
            ApplyToCurrentWeapon();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 現在装備中の武器にプロジェクタイルを設定します
        /// </summary>
        private void ApplyToCurrentWeapon()
        {
            if (_arrowTypeConfig == null || _weaponManager == null)
            {
                return;
            }

            // プレハブを取得
            GameObject? arrowPrefab = _arrowTypeConfig.GetArrowPrefab(_currentArrowType);

            if (arrowPrefab == null)
            {
                if (_debugLog)
                {
                    Debug.LogWarning($"[ArrowTypeApplier] ArrowType {_currentArrowType} のプレハブが設定されていません。");
                }
                return;
            }

            // 現在装備中の武器を取得
            MWeapon? currentWeapon = _weaponManager.Weapon;

            // MShootable（弓など遠距離武器）の場合
            if (currentWeapon is MShootable shootable)
            {
                shootable.SetProjectile(arrowPrefab);
                _applied = true;

                if (_debugLog)
                {
                    Debug.Log($"[ArrowTypeApplier] 矢タイプを設定: {_currentArrowType} → {arrowPrefab.name}");
                }
            }
            else
            {
                // 武器がまだ装備されていない場合は後で適用
                if (_debugLog && currentWeapon == null)
                {
                    Debug.Log("[ArrowTypeApplier] 武器がまだ装備されていません。装備時に適用されます。");
                }
            }
        }

        #endregion
    }
}
