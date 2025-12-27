#nullable enable

using CavalryFight.Core.MVVM;
using CavalryFight.Core.Services;
using CavalryFight.Services.Customization;
using CavalryFight.ViewModels;
using UnityEngine;
using UnityEngine.UIElements;

namespace CavalryFight.Views
{
    /// <summary>
    /// カスタマイズ画面のView
    /// </summary>
    /// <remarks>
    /// UI Toolkitを使用してカスタマイズUIを表示します。
    /// CustomizationViewModelとバインドされ、キャラクターと馬のカスタマイズを管理します。
    ///
    /// このクラスは以下のpartialクラスに分割されています:
    /// - CustomizationView.cs: メインクラス（フィールド、ライフサイクル）
    /// - CustomizationView.UIElements.cs: UI要素の取得と検証
    /// - CustomizationView.UIUpdate.cs: UIの更新処理
    /// - CustomizationView.EventHandlers.cs: イベント登録とハンドラ
    /// - CustomizationView.Preview.cs: 3Dプレビュー管理
    /// </remarks>
    [RequireComponent(typeof(UIDocument))]
    public partial class CustomizationView : UIToolkitViewBase<CustomizationViewModel>
    {
        #region Serialized Fields

        [Header("3D Preview")]
        [SerializeField] private Camera? _previewCamera;
        [SerializeField] private Transform? _Container;
        [SerializeField] private GameObject? _characterPreviewPrefab;
        [SerializeField] private GameObject? _mountPreviewPrefab;

        [Header("P09 Animator Controllers")]
        [SerializeField] private RuntimeAnimatorController? _maleAnimatorController;
        [SerializeField] private RuntimeAnimatorController? _femaleAnimatorController;
        [SerializeField] private RuntimeAnimatorController? _maleCombatIdleAnimatorController;
        [SerializeField] private RuntimeAnimatorController? _femaleCombatIdleAnimatorController;

        #endregion

        #region Private Fields - UI Elements

        // カテゴリタブ
        private Button? _characterTabButton;
        private Button? _mountTabButton;

        // キャラクターパネル
        private VisualElement? _characterPanel;
        private RadioButtonGroup? _genderRadioGroup;

        // FaceType用矢印コントロール
        private Button? _faceTypePrevButton;
        private Label? _faceTypeValue;
        private Button? _faceTypeNextButton;

        // Hairstyle用矢印コントロール
        private Button? _hairstylePrevButton;
        private Label? _hairstyleValue;
        private Button? _hairstyleNextButton;

        // HairColor用矢印コントロール
        private Button? _hairColorPrevButton;
        private Label? _hairColorValue;
        private Button? _hairColorNextButton;

        // EyeColor用矢印コントロール
        private Button? _eyeColorPrevButton;
        private Label? _eyeColorValue;
        private Button? _eyeColorNextButton;

        // FacialHair用矢印コントロール
        private Button? _facialHairPrevButton;
        private Label? _facialHairValue;
        private Button? _facialHairNextButton;

        // SkinTone用矢印コントロール
        private Button? _skinTonePrevButton;
        private Label? _skinToneValue;
        private Button? _skinToneNextButton;

        // BustSize用矢印コントロール
        private Button? _bustSizePrevButton;
        private Label? _bustSizeValue;
        private Button? _bustSizeNextButton;

        // HeadArmor用矢印コントロール
        private Button? _headArmorPrevButton;
        private Label? _headArmorValue;
        private Button? _headArmorNextButton;

        // ChestArmor用矢印コントロール
        private Button? _chestArmorPrevButton;
        private Label? _chestArmorValue;
        private Button? _chestArmorNextButton;

        // ArmsArmor用矢印コントロール
        private Button? _armsArmorPrevButton;
        private Label? _armsArmorValue;
        private Button? _armsArmorNextButton;

        // WaistArmor用矢印コントロール
        private Button? _waistArmorPrevButton;
        private Label? _waistArmorValue;
        private Button? _waistArmorNextButton;

        // LegsArmor用矢印コントロール
        private Button? _legsArmorPrevButton;
        private Label? _legsArmorValue;
        private Button? _legsArmorNextButton;

        // Bow用矢印コントロール
        private Button? _bowPrevButton;
        private Label? _bowValue;
        private Button? _bowNextButton;

        // 馬パネル
        private VisualElement? _mountPanel;

        // CoatColor用矢印コントロール
        private Button? _coatColorPrevButton;
        private Label? _coatColorValue;
        private Button? _coatColorNextButton;

        // ManeStyle用矢印コントロール
        private Button? _maneStylePrevButton;
        private Label? _maneStyleValue;
        private Button? _maneStyleNextButton;

        // ManeColor用矢印コントロール
        private Button? _maneColorPrevButton;
        private Label? _maneColorValue;
        private Button? _maneColorNextButton;

        // HornType用矢印コントロール
        private Button? _hornTypePrevButton;
        private Label? _hornTypeValue;
        private Button? _hornTypeNextButton;

        // HornMaterial用矢印コントロール
        private Button? _hornMaterialPrevButton;
        private Label? _hornMaterialValue;
        private Button? _hornMaterialNextButton;

        // MountArmor用矢印コントロール
        private Button? _mountArmorPrevButton;
        private Label? _mountArmorValue;
        private Button? _mountArmorNextButton;

        private Toggle? _saddleToggle;
        private Toggle? _reinsToggle;

        // プレビュー
        private VisualElement? _previewContainer;

        // 下部ボタン
        private Button? _resetButton;
        private Button? _backButton;
        private Button? _combatIdleToggleButton;

        #endregion

        #region Private Fields - State

        // 戦闘待機モード状態
        private bool _isCombatIdleMode = false;

        // 3Dプレビューオブジェクト
        private GameObject? _currentPreviewCharacter;
        private GameObject? _currentPreviewMount;

        // 再入防止フラグ
        private bool _isUpdatingPreview = false;

        #endregion

        #region Unity Lifecycle

        /// <summary>
        /// 初期化処理
        /// </summary>
        protected override void Awake()
        {
            base.Awake();

            // ServiceLocatorからICustomizationServiceを取得
            var customizationService = ServiceLocator.Instance.Get<ICustomizationService>();
            if (customizationService == null)
            {
                Debug.LogError("[CustomizationView] ICustomizationService not found in ServiceLocator!", this);
                return;
            }

            // ViewModelを作成してバインド
            ViewModel = new CustomizationViewModel(customizationService);

            Debug.Log("[CustomizationView] View initialized with ViewModel.", this);
        }

        #endregion

        #region Protected Methods - ViewModel Binding

        /// <summary>
        /// RootVisualElementが準備できた時に呼び出されます。
        /// </summary>
        /// <param name="root">ルートVisualElement</param>
        protected override void OnRootVisualElementReady(VisualElement root)
        {
            base.OnRootVisualElementReady(root);

            // UI要素を取得
            GetUIElements();

            // UI要素の検証
            if (!ValidateUIElements())
            {
                Debug.LogError("[CustomizationView] Critical UI elements are missing. Disabling view.", this);
                enabled = false;
                return;
            }

            // Dropdownの選択肢を設定
            SetupDropdowns();

            // ViewModelの値でUIを更新
            UpdateUIFromViewModel();

            // イベントハンドラを登録
            RegisterEventHandlers();

            // プレビューオブジェクトを初期化（両方インスタンス化）
            InitializePreviewObjects();

            // 初期カテゴリを表示
            UpdateCategoryVisibility();
        }

        /// <summary>
        /// ViewModelとのバインディングを設定します。
        /// </summary>
        /// <param name="viewModel">バインドするViewModel</param>
        protected override void BindViewModel(CustomizationViewModel viewModel)
        {
            base.BindViewModel(viewModel);

            // PropertyChangedイベントを購読
            viewModel.PropertyChanged += OnViewModelPropertyChanged;

            // カスタムイベントを購読
            viewModel.CategoryChanged += OnCategoryChanged;
            viewModel.PreviewUpdated += OnPreviewUpdated;
            viewModel.BackToMenuRequested += OnBackToMenuRequested;
        }

        /// <summary>
        /// ViewModelとのバインディングを解除します。
        /// </summary>
        protected override void UnbindViewModel()
        {
            // イベント購読解除
            if (ViewModel != null)
            {
                ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
                ViewModel.CategoryChanged -= OnCategoryChanged;
                ViewModel.PreviewUpdated -= OnPreviewUpdated;
                ViewModel.BackToMenuRequested -= OnBackToMenuRequested;
            }

            // イベントハンドラを解除
            UnregisterEventHandlers();

            // プレビューオブジェクトを破棄
            DestroyPreviewObjects();

            base.UnbindViewModel();
        }

        #endregion
    }
}
