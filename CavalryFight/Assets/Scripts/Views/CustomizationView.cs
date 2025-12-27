#nullable enable

using System.Linq;
using CavalryFight.Core.MVVM;
using CavalryFight.Core.Services;
using CavalryFight.Services.Customization;
using CavalryFight.Services.SceneManagement;
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
    /// </remarks>
    [RequireComponent(typeof(UIDocument))]
    public class CustomizationView : UIToolkitViewBase<CustomizationViewModel>
    {
        #region Fields

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

        #region Protected Methods

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
            ValidateUIElements();

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

        #region Private Methods - Setup

        /// <summary>
        /// UI要素を取得します
        /// </summary>
        private void GetUIElements()
        {
            // カテゴリタブ
            _characterTabButton = Q<Button>("CharacterTabButton");
            _mountTabButton = Q<Button>("MountTabButton");

            // パネル
            _characterPanel = Q<VisualElement>("CharacterPanel");
            _mountPanel = Q<VisualElement>("MountPanel");
            _previewContainer = Q<VisualElement>("PreviewContainer");

            // キャラクターコントロール
            _genderRadioGroup = Q<RadioButtonGroup>("GenderRadioGroup");

            // 矢印コントロール
            _faceTypePrevButton = Q<Button>("FaceTypePrevButton");
            _faceTypeValue = Q<Label>("FaceTypeValue");
            _faceTypeNextButton = Q<Button>("FaceTypeNextButton");

            _hairstylePrevButton = Q<Button>("HairstylePrevButton");
            _hairstyleValue = Q<Label>("HairstyleValue");
            _hairstyleNextButton = Q<Button>("HairstyleNextButton");

            _hairColorPrevButton = Q<Button>("HairColorPrevButton");
            _hairColorValue = Q<Label>("HairColorValue");
            _hairColorNextButton = Q<Button>("HairColorNextButton");

            _eyeColorPrevButton = Q<Button>("EyeColorPrevButton");
            _eyeColorValue = Q<Label>("EyeColorValue");
            _eyeColorNextButton = Q<Button>("EyeColorNextButton");

            _facialHairPrevButton = Q<Button>("FacialHairPrevButton");
            _facialHairValue = Q<Label>("FacialHairValue");
            _facialHairNextButton = Q<Button>("FacialHairNextButton");

            _skinTonePrevButton = Q<Button>("SkinTonePrevButton");
            _skinToneValue = Q<Label>("SkinToneValue");
            _skinToneNextButton = Q<Button>("SkinToneNextButton");

            _bustSizePrevButton = Q<Button>("BustSizePrevButton");
            _bustSizeValue = Q<Label>("BustSizeValue");
            _bustSizeNextButton = Q<Button>("BustSizeNextButton");

            _headArmorPrevButton = Q<Button>("HeadArmorPrevButton");
            _headArmorValue = Q<Label>("HeadArmorValue");
            _headArmorNextButton = Q<Button>("HeadArmorNextButton");

            _chestArmorPrevButton = Q<Button>("ChestArmorPrevButton");
            _chestArmorValue = Q<Label>("ChestArmorValue");
            _chestArmorNextButton = Q<Button>("ChestArmorNextButton");

            _armsArmorPrevButton = Q<Button>("ArmsArmorPrevButton");
            _armsArmorValue = Q<Label>("ArmsArmorValue");
            _armsArmorNextButton = Q<Button>("ArmsArmorNextButton");

            _waistArmorPrevButton = Q<Button>("WaistArmorPrevButton");
            _waistArmorValue = Q<Label>("WaistArmorValue");
            _waistArmorNextButton = Q<Button>("WaistArmorNextButton");

            _legsArmorPrevButton = Q<Button>("LegsArmorPrevButton");
            _legsArmorValue = Q<Label>("LegsArmorValue");
            _legsArmorNextButton = Q<Button>("LegsArmorNextButton");

            _bowPrevButton = Q<Button>("BowPrevButton");
            _bowValue = Q<Label>("BowValue");
            _bowNextButton = Q<Button>("BowNextButton");

            // 馬コントロール
            _coatColorPrevButton = Q<Button>("CoatColorPrevButton");
            _coatColorValue = Q<Label>("CoatColorValue");
            _coatColorNextButton = Q<Button>("CoatColorNextButton");

            _maneStylePrevButton = Q<Button>("ManeStylePrevButton");
            _maneStyleValue = Q<Label>("ManeStyleValue");
            _maneStyleNextButton = Q<Button>("ManeStyleNextButton");

            _maneColorPrevButton = Q<Button>("ManeColorPrevButton");
            _maneColorValue = Q<Label>("ManeColorValue");
            _maneColorNextButton = Q<Button>("ManeColorNextButton");

            _hornTypePrevButton = Q<Button>("HornTypePrevButton");
            _hornTypeValue = Q<Label>("HornTypeValue");
            _hornTypeNextButton = Q<Button>("HornTypeNextButton");

            _hornMaterialPrevButton = Q<Button>("HornMaterialPrevButton");
            _hornMaterialValue = Q<Label>("HornMaterialValue");
            _hornMaterialNextButton = Q<Button>("HornMaterialNextButton");

            _mountArmorPrevButton = Q<Button>("MountArmorPrevButton");
            _mountArmorValue = Q<Label>("MountArmorValue");
            _mountArmorNextButton = Q<Button>("MountArmorNextButton");

            _saddleToggle = Q<Toggle>("SaddleToggle");
            _reinsToggle = Q<Toggle>("ReinsToggle");

            // 下部ボタン
            _resetButton = Q<Button>("ResetButton");
            _backButton = Q<Button>("BackButton");
            _combatIdleToggleButton = Q<Button>("CombatIdleToggleButton");
        }

        /// <summary>
        /// UI要素が正しく取得できているか検証します。
        /// </summary>
        private void ValidateUIElements()
        {
            // カテゴリタブ
            if (_characterTabButton == null)
            {
                Debug.LogWarning("[CustomizationView] CharacterTabButton not found in UXML.", this);
            }
            if (_mountTabButton == null)
            {
                Debug.LogWarning("[CustomizationView] MountTabButton not found in UXML.", this);
            }

            // パネル
            if (_characterPanel == null)
            {
                Debug.LogWarning("[CustomizationView] CharacterPanel not found in UXML.", this);
            }
            if (_mountPanel == null)
            {
                Debug.LogWarning("[CustomizationView] MountPanel not found in UXML.", this);
            }
            if (_previewContainer == null)
            {
                Debug.LogWarning("[CustomizationView] PreviewContainer not found in UXML.", this);
            }

            // ボタン
            if (_resetButton == null)
            {
                Debug.LogWarning("[CustomizationView] ResetButton not found in UXML.", this);
            }
            if (_backButton == null)
            {
                Debug.LogWarning("[CustomizationView] BackButton not found in UXML.", this);
            }
        }

        /// <summary>
        /// Dropdownの選択肢を設定します
        /// </summary>
        private void SetupDropdowns()
        {
            // 性別
            if (_genderRadioGroup != null)
            {
                _genderRadioGroup.choices = new System.Collections.Generic.List<string> { "Male", "Female" };
            }
        }

        /// <summary>
        /// ViewModelの値でUIを更新します
        /// </summary>
        private void UpdateUIFromViewModel()
        {
            if (ViewModel == null)
            {
                return;
            }

            UpdateCharacterUI();
            UpdateMountUI();
        }

        /// <summary>
        /// キャラクターUIを更新します
        /// </summary>
        private void UpdateCharacterUI()
        {
            if (ViewModel == null)
            {
                return;
            }

            var character = ViewModel.WorkingCharacter;

            // 性別
            if (_genderRadioGroup != null)
            {
                _genderRadioGroup.value = (int)character.Gender;
            }

            // 外見（矢印コントロール）
            if (_faceTypeValue != null)
            {
                _faceTypeValue.text = character.FaceType.ToString();
            }
            if (_hairstyleValue != null)
            {
                _hairstyleValue.text = character.HairstyleId.ToString();
            }
            if (_hairColorValue != null)
            {
                _hairColorValue.text = character.HairColorId.ToString();
            }
            if (_eyeColorValue != null)
            {
                _eyeColorValue.text = character.EyeColorId.ToString();
            }
            if (_facialHairValue != null)
            {
                _facialHairValue.text = character.FacialHairId.ToString();
            }
            if (_skinToneValue != null)
            {
                _skinToneValue.text = character.SkinToneId.ToString();
            }
            if (_bustSizeValue != null)
            {
                _bustSizeValue.text = character.BustSize.ToString();
            }

            // 防具
            if (_headArmorValue != null)
            {
                _headArmorValue.text = character.HeadArmorId.ToString();
            }
            if (_chestArmorValue != null)
            {
                _chestArmorValue.text = character.ChestArmorId.ToString();
            }
            if (_armsArmorValue != null)
            {
                _armsArmorValue.text = character.ArmsArmorId.ToString();
            }
            if (_waistArmorValue != null)
            {
                _waistArmorValue.text = character.WaistArmorId.ToString();
            }
            if (_legsArmorValue != null)
            {
                _legsArmorValue.text = character.LegsArmorId.ToString();
            }

            // 武器
            if (_bowValue != null)
            {
                _bowValue.text = character.BowId.ToString();
            }
        }

        /// <summary>
        /// 馬UIを更新します
        /// </summary>
        private void UpdateMountUI()
        {
            if (ViewModel == null)
            {
                return;
            }

            var mount = ViewModel.WorkingMount;

            // 外見
            if (_coatColorValue != null)
            {
                _coatColorValue.text = mount.CoatColor.ToString();
            }
            if (_maneStyleValue != null)
            {
                _maneStyleValue.text = mount.ManeStyle.ToString();
            }
            if (_maneColorValue != null)
            {
                _maneColorValue.text = mount.ManeColor.ToString();
            }
            if (_hornTypeValue != null)
            {
                _hornTypeValue.text = mount.HornType.ToString();
            }
            if (_hornMaterialValue != null)
            {
                _hornMaterialValue.text = mount.HornMaterial.ToString();
            }

            // 装備
            if (_mountArmorValue != null)
            {
                _mountArmorValue.text = mount.ArmorId.ToString();
            }
            if (_saddleToggle != null)
            {
                _saddleToggle.value = mount.HasSaddle;
            }
            if (_reinsToggle != null)
            {
                _reinsToggle.value = mount.HasReins;
            }
        }

        /// <summary>
        /// カテゴリの表示/非表示を更新します
        /// </summary>
        private void UpdateCategoryVisibility()
        {
            if (ViewModel == null)
            {
                return;
            }

            // パネルの表示/非表示を切り替え
            if (_characterPanel != null)
            {
                _characterPanel.style.display = ViewModel.IsCharacterCategory ? DisplayStyle.Flex : DisplayStyle.None;
            }
            if (_mountPanel != null)
            {
                _mountPanel.style.display = ViewModel.IsMountCategory ? DisplayStyle.Flex : DisplayStyle.None;
            }

            // タブボタンのスタイルを更新（アクティブ状態の表示）
            UpdateTabButtonStyles();

            // プレビューを更新
            UpdatePreview();
        }

        /// <summary>
        /// タブボタンのスタイルを更新します
        /// </summary>
        private void UpdateTabButtonStyles()
        {
            if (ViewModel == null)
            {
                return;
            }

            // 既存のアクティブクラスを削除
            _characterTabButton?.RemoveFromClassList("tab-active");
            _mountTabButton?.RemoveFromClassList("tab-active");

            // アクティブなタブにクラスを追加
            if (ViewModel.IsCharacterCategory)
            {
                _characterTabButton?.AddToClassList("tab-active");
            }
            else if (ViewModel.IsMountCategory)
            {
                _mountTabButton?.AddToClassList("tab-active");
            }
        }

        #endregion

        #region Private Methods - Event Registration

        /// <summary>
        /// イベントハンドラを登録します。
        /// </summary>
        private void RegisterEventHandlers()
        {
            // カテゴリタブ
            if (_characterTabButton != null)
            {
                _characterTabButton.clicked += () => OnTabButtonClicked(CustomizationCategory.Character);
            }
            if (_mountTabButton != null)
            {
                _mountTabButton.clicked += () => OnTabButtonClicked(CustomizationCategory.Mount);
            }

            // キャラクターコントロール
            if (_genderRadioGroup != null)
            {
                _genderRadioGroup.RegisterValueChangedCallback(OnGenderChanged);
            }

            // 矢印ボタンハンドラ
            if (_faceTypePrevButton != null)
            {
                _faceTypePrevButton.clicked += () => OnArrowButtonClicked("FaceType", -1, 1, 3);
            }
            if (_faceTypeNextButton != null)
            {
                _faceTypeNextButton.clicked += () => OnArrowButtonClicked("FaceType", 1, 1, 3);
            }

            if (_hairstylePrevButton != null)
            {
                _hairstylePrevButton.clicked += () => OnArrowButtonClicked("Hairstyle", -1, 1, 14);
            }
            if (_hairstyleNextButton != null)
            {
                _hairstyleNextButton.clicked += () => OnArrowButtonClicked("Hairstyle", 1, 1, 14);
            }

            if (_hairColorPrevButton != null)
            {
                _hairColorPrevButton.clicked += () => OnArrowButtonClicked("HairColor", -1, 1, 9);
            }
            if (_hairColorNextButton != null)
            {
                _hairColorNextButton.clicked += () => OnArrowButtonClicked("HairColor", 1, 1, 9);
            }

            if (_eyeColorPrevButton != null)
            {
                _eyeColorPrevButton.clicked += () => OnArrowButtonClicked("EyeColor", -1, 1, 5);
            }
            if (_eyeColorNextButton != null)
            {
                _eyeColorNextButton.clicked += () => OnArrowButtonClicked("EyeColor", 1, 1, 5);
            }

            if (_facialHairPrevButton != null)
            {
                _facialHairPrevButton.clicked += () => OnArrowButtonClicked("FacialHair", -1, 0, 8);
            }
            if (_facialHairNextButton != null)
            {
                _facialHairNextButton.clicked += () => OnArrowButtonClicked("FacialHair", 1, 0, 8);
            }

            if (_skinTonePrevButton != null)
            {
                _skinTonePrevButton.clicked += () => OnArrowButtonClicked("SkinTone", -1, 1, 3);
            }
            if (_skinToneNextButton != null)
            {
                _skinToneNextButton.clicked += () => OnArrowButtonClicked("SkinTone", 1, 1, 3);
            }

            if (_bustSizePrevButton != null)
            {
                _bustSizePrevButton.clicked += () => OnArrowButtonClicked("BustSize", -1, 1, 3);
            }
            if (_bustSizeNextButton != null)
            {
                _bustSizeNextButton.clicked += () => OnArrowButtonClicked("BustSize", 1, 1, 3);
            }

            if (_headArmorPrevButton != null)
            {
                _headArmorPrevButton.clicked += () => OnArrowButtonClicked("HeadArmor", -1, 0, 12);
            }
            if (_headArmorNextButton != null)
            {
                _headArmorNextButton.clicked += () => OnArrowButtonClicked("HeadArmor", 1, 0, 12);
            }

            if (_chestArmorPrevButton != null)
            {
                _chestArmorPrevButton.clicked += () => OnArrowButtonClicked("ChestArmor", -1, 0, 12);
            }
            if (_chestArmorNextButton != null)
            {
                _chestArmorNextButton.clicked += () => OnArrowButtonClicked("ChestArmor", 1, 0, 12);
            }

            if (_armsArmorPrevButton != null)
            {
                _armsArmorPrevButton.clicked += () => OnArrowButtonClicked("ArmsArmor", -1, 0, 12);
            }
            if (_armsArmorNextButton != null)
            {
                _armsArmorNextButton.clicked += () => OnArrowButtonClicked("ArmsArmor", 1, 0, 12);
            }

            if (_waistArmorPrevButton != null)
            {
                _waistArmorPrevButton.clicked += () => OnArrowButtonClicked("WaistArmor", -1, 1, 12);
            }
            if (_waistArmorNextButton != null)
            {
                _waistArmorNextButton.clicked += () => OnArrowButtonClicked("WaistArmor", 1, 1, 12);
            }

            if (_legsArmorPrevButton != null)
            {
                _legsArmorPrevButton.clicked += () => OnArrowButtonClicked("LegsArmor", -1, 0, 12);
            }
            if (_legsArmorNextButton != null)
            {
                _legsArmorNextButton.clicked += () => OnArrowButtonClicked("LegsArmor", 1, 0, 12);
            }

            if (_bowPrevButton != null)
            {
                _bowPrevButton.clicked += () => OnArrowButtonClicked("Bow", -1, 10, 13);
            }
            if (_bowNextButton != null)
            {
                _bowNextButton.clicked += () => OnArrowButtonClicked("Bow", 1, 10, 13);
            }

            // 馬コントロール - 矢印ボタン
            if (_coatColorPrevButton != null)
            {
                _coatColorPrevButton.clicked += () => OnMountEnumArrowClicked("CoatColor", -1);
            }
            if (_coatColorNextButton != null)
            {
                _coatColorNextButton.clicked += () => OnMountEnumArrowClicked("CoatColor", 1);
            }

            if (_maneStylePrevButton != null)
            {
                _maneStylePrevButton.clicked += () => OnMountEnumArrowClicked("ManeStyle", -1);
            }
            if (_maneStyleNextButton != null)
            {
                _maneStyleNextButton.clicked += () => OnMountEnumArrowClicked("ManeStyle", 1);
            }

            if (_maneColorPrevButton != null)
            {
                _maneColorPrevButton.clicked += () => OnMountEnumArrowClicked("ManeColor", -1);
            }
            if (_maneColorNextButton != null)
            {
                _maneColorNextButton.clicked += () => OnMountEnumArrowClicked("ManeColor", 1);
            }

            if (_hornTypePrevButton != null)
            {
                _hornTypePrevButton.clicked += () => OnMountEnumArrowClicked("HornType", -1);
            }
            if (_hornTypeNextButton != null)
            {
                _hornTypeNextButton.clicked += () => OnMountEnumArrowClicked("HornType", 1);
            }

            if (_hornMaterialPrevButton != null)
            {
                _hornMaterialPrevButton.clicked += () => OnMountEnumArrowClicked("HornMaterial", -1);
            }
            if (_hornMaterialNextButton != null)
            {
                _hornMaterialNextButton.clicked += () => OnMountEnumArrowClicked("HornMaterial", 1);
            }

            if (_mountArmorPrevButton != null)
            {
                _mountArmorPrevButton.clicked += () => OnArrowButtonClicked("MountArmor", -1, 0, 3);
            }
            if (_mountArmorNextButton != null)
            {
                _mountArmorNextButton.clicked += () => OnArrowButtonClicked("MountArmor", 1, 0, 3);
            }

            if (_saddleToggle != null)
            {
                _saddleToggle.RegisterValueChangedCallback(OnSaddleChanged);
            }
            if (_reinsToggle != null)
            {
                _reinsToggle.RegisterValueChangedCallback(OnReinsChanged);
            }

            // 下部ボタン
            if (_resetButton != null)
            {
                _resetButton.clicked += OnResetButtonClicked;
            }
            if (_backButton != null)
            {
                _backButton.clicked += OnBackButtonClicked;
            }
            if (_combatIdleToggleButton != null)
            {
                _combatIdleToggleButton.clicked += OnCombatIdleToggleClicked;
            }
        }

        /// <summary>
        /// イベントハンドラを解除します。
        /// </summary>
        private void UnregisterEventHandlers()
        {
            // カテゴリタブ
            if (_characterTabButton != null)
            {
                _characterTabButton.clicked -= () => OnTabButtonClicked(CustomizationCategory.Character);
            }
            if (_mountTabButton != null)
            {
                _mountTabButton.clicked -= () => OnTabButtonClicked(CustomizationCategory.Mount);
            }

            // キャラクターコントロール
            if (_genderRadioGroup != null)
            {
                _genderRadioGroup.UnregisterValueChangedCallback(OnGenderChanged);
            }

            // 矢印ボタンはGameObject破棄時に自動的にクリーンアップされる
            // 手動でイベント解除する必要はない

            // 馬コントロール
            if (_saddleToggle != null)
            {
                _saddleToggle.UnregisterValueChangedCallback(OnSaddleChanged);
            }
            if (_reinsToggle != null)
            {
                _reinsToggle.UnregisterValueChangedCallback(OnReinsChanged);
            }

            // 下部ボタン
            if (_resetButton != null)
            {
                _resetButton.clicked -= OnResetButtonClicked;
            }
            if (_backButton != null)
            {
                _backButton.clicked -= OnBackButtonClicked;
            }
            if (_combatIdleToggleButton != null)
            {
                _combatIdleToggleButton.clicked -= OnCombatIdleToggleClicked;
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// ViewModelのプロパティ変更イベントを処理します。
        /// </summary>
        private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (ViewModel == null)
            {
                return;
            }

            switch (e.PropertyName)
            {
                case nameof(CustomizationViewModel.CurrentCategory):
                case nameof(CustomizationViewModel.IsCharacterCategory):
                case nameof(CustomizationViewModel.IsMountCategory):
                    UpdateCategoryVisibility();
                    break;

                case nameof(CustomizationViewModel.WorkingCharacter):
                    UpdateCharacterUI();
                    break;

                case nameof(CustomizationViewModel.WorkingMount):
                    UpdateMountUI();
                    break;
            }
        }

        /// <summary>
        /// カテゴリが変更された時の処理
        /// </summary>
        private void OnCategoryChanged(object? sender, CustomizationCategory category)
        {
            UpdateCategoryVisibility();
        }

        /// <summary>
        /// プレビュー更新イベントを処理します
        /// </summary>
        private void OnPreviewUpdated(object? sender, System.EventArgs e)
        {
            UpdatePreview();
        }

        /// <summary>
        /// メニューに戻る要求イベントを処理します
        /// </summary>
        private void OnBackToMenuRequested(object? sender, System.EventArgs e)
        {
            // Advanced Scene Managerを使用してMainMenuに遷移
            var sceneService = ServiceLocator.Instance.Get<ISceneManagementService>();
            if (sceneService != null)
            {
                sceneService.LoadMainMenu();
            }
            else
            {
                Debug.LogWarning("[CustomizationView] Cannot navigate to MainMenu: SceneManagementService not found.", this);
            }
        }

        // タブボタンハンドラ
        private void OnTabButtonClicked(CustomizationCategory category)
        {
            ViewModel?.SelectCategoryCommand.Execute(category);
        }

        // キャラクター変更ハンドラ
        private void OnGenderChanged(ChangeEvent<int> evt)
        {
            if (ViewModel != null)
            {
                ViewModel.WorkingCharacter.Gender = (Gender)evt.newValue;
                ViewModel.NotifyCharacterChanged();
            }
        }

        /// <summary>
        /// キャラクターカスタマイズ用の矢印ボタンクリックハンドラ
        /// </summary>
        private void OnArrowButtonClicked(string propertyName, int direction, int min, int max)
        {
            if (ViewModel == null)
            {
                return;
            }

            int currentValue = 0;
            Label? valueLabel = null;
            bool isCharacterProperty = true;

            // プロパティ名に基づいて現在値とラベルを取得
            switch (propertyName)
            {
                // キャラクタープロパティ
                case "FaceType":
                    currentValue = ViewModel.WorkingCharacter.FaceType;
                    valueLabel = _faceTypeValue;
                    break;
                case "Hairstyle":
                    currentValue = ViewModel.WorkingCharacter.HairstyleId;
                    valueLabel = _hairstyleValue;
                    break;
                case "HairColor":
                    currentValue = ViewModel.WorkingCharacter.HairColorId;
                    valueLabel = _hairColorValue;
                    break;
                case "EyeColor":
                    currentValue = ViewModel.WorkingCharacter.EyeColorId;
                    valueLabel = _eyeColorValue;
                    break;
                case "FacialHair":
                    currentValue = ViewModel.WorkingCharacter.FacialHairId;
                    valueLabel = _facialHairValue;
                    break;
                case "SkinTone":
                    currentValue = ViewModel.WorkingCharacter.SkinToneId;
                    valueLabel = _skinToneValue;
                    break;
                case "BustSize":
                    currentValue = ViewModel.WorkingCharacter.BustSize;
                    valueLabel = _bustSizeValue;
                    break;
                case "HeadArmor":
                    currentValue = ViewModel.WorkingCharacter.HeadArmorId;
                    valueLabel = _headArmorValue;
                    break;
                case "ChestArmor":
                    currentValue = ViewModel.WorkingCharacter.ChestArmorId;
                    valueLabel = _chestArmorValue;
                    break;
                case "ArmsArmor":
                    currentValue = ViewModel.WorkingCharacter.ArmsArmorId;
                    valueLabel = _armsArmorValue;
                    break;
                case "WaistArmor":
                    currentValue = ViewModel.WorkingCharacter.WaistArmorId;
                    valueLabel = _waistArmorValue;
                    break;
                case "LegsArmor":
                    currentValue = ViewModel.WorkingCharacter.LegsArmorId;
                    valueLabel = _legsArmorValue;
                    break;
                case "Bow":
                    currentValue = ViewModel.WorkingCharacter.BowId;
                    valueLabel = _bowValue;
                    break;
                // 馬プロパティ
                case "MountArmor":
                    currentValue = ViewModel.WorkingMount.ArmorId;
                    valueLabel = _mountArmorValue;
                    isCharacterProperty = false;
                    break;
            }

            // ラッピング付きで新しい値を計算
            int newValue = currentValue + direction;
            if (newValue < min)
            {
                newValue = max;
            }
            else if (newValue > max)
            {
                newValue = min;
            }

            // 新しい値を設定
            switch (propertyName)
            {
                // キャラクタープロパティ
                case "FaceType":
                    ViewModel.WorkingCharacter.FaceType = newValue;
                    break;
                case "Hairstyle":
                    ViewModel.WorkingCharacter.HairstyleId = newValue;
                    break;
                case "HairColor":
                    ViewModel.WorkingCharacter.HairColorId = newValue;
                    break;
                case "EyeColor":
                    ViewModel.WorkingCharacter.EyeColorId = newValue;
                    break;
                case "FacialHair":
                    ViewModel.WorkingCharacter.FacialHairId = newValue;
                    break;
                case "SkinTone":
                    ViewModel.WorkingCharacter.SkinToneId = newValue;
                    break;
                case "BustSize":
                    ViewModel.WorkingCharacter.BustSize = newValue;
                    break;
                case "HeadArmor":
                    ViewModel.WorkingCharacter.HeadArmorId = newValue;
                    break;
                case "ChestArmor":
                    ViewModel.WorkingCharacter.ChestArmorId = newValue;
                    break;
                case "ArmsArmor":
                    ViewModel.WorkingCharacter.ArmsArmorId = newValue;
                    break;
                case "WaistArmor":
                    ViewModel.WorkingCharacter.WaistArmorId = newValue;
                    break;
                case "LegsArmor":
                    ViewModel.WorkingCharacter.LegsArmorId = newValue;
                    break;
                case "Bow":
                    ViewModel.WorkingCharacter.BowId = newValue;
                    break;
                // 馬プロパティ
                case "MountArmor":
                    ViewModel.WorkingMount.ArmorId = newValue;
                    break;
            }

            // ラベルを更新
            if (valueLabel != null)
            {
                valueLabel.text = newValue.ToString();
            }

            // 変更を通知
            if (isCharacterProperty)
            {
                ViewModel.NotifyCharacterChanged();
            }
            else
            {
                ViewModel.NotifyMountChanged();
            }
        }

        /// <summary>
        /// 馬の列挙型プロパティ用の矢印ボタンクリックハンドラ
        /// </summary>
        /// <param name="propertyName">プロパティ名</param>
        /// <param name="direction">方向（-1 = 前, 1 = 次）</param>
        private void OnMountEnumArrowClicked(string propertyName, int direction)
        {
            if (ViewModel == null)
            {
                return;
            }

            switch (propertyName)
            {
                case "CoatColor":
                    {
                        int currentValue = (int)ViewModel.WorkingMount.CoatColor;
                        int enumLength = System.Enum.GetValues(typeof(HorseColor)).Length;
                        int newValue = (currentValue + direction + enumLength) % enumLength;
                        ViewModel.WorkingMount.CoatColor = (HorseColor)newValue;
                        if (_coatColorValue != null)
                        {
                            _coatColorValue.text = ((HorseColor)newValue).ToString();
                        }
                        break;
                    }
                case "ManeStyle":
                    {
                        int currentValue = (int)ViewModel.WorkingMount.ManeStyle;
                        int enumLength = System.Enum.GetValues(typeof(ManeStyle)).Length;
                        int newValue = (currentValue + direction + enumLength) % enumLength;
                        ViewModel.WorkingMount.ManeStyle = (ManeStyle)newValue;
                        if (_maneStyleValue != null)
                        {
                            _maneStyleValue.text = ((ManeStyle)newValue).ToString();
                        }
                        break;
                    }
                case "ManeColor":
                    {
                        int currentValue = (int)ViewModel.WorkingMount.ManeColor;
                        int enumLength = System.Enum.GetValues(typeof(ManeColor)).Length;
                        int newValue = (currentValue + direction + enumLength) % enumLength;
                        ViewModel.WorkingMount.ManeColor = (ManeColor)newValue;
                        if (_maneColorValue != null)
                        {
                            _maneColorValue.text = ((ManeColor)newValue).ToString();
                        }
                        break;
                    }
                case "HornType":
                    {
                        int currentValue = (int)ViewModel.WorkingMount.HornType;
                        int enumLength = System.Enum.GetValues(typeof(HornType)).Length;
                        int newValue = (currentValue + direction + enumLength) % enumLength;
                        ViewModel.WorkingMount.HornType = (HornType)newValue;
                        if (_hornTypeValue != null)
                        {
                            _hornTypeValue.text = ((HornType)newValue).ToString();
                        }
                        break;
                    }
                case "HornMaterial":
                    {
                        int currentValue = (int)ViewModel.WorkingMount.HornMaterial;
                        int enumLength = System.Enum.GetValues(typeof(HornMaterial)).Length;
                        int newValue = (currentValue + direction + enumLength) % enumLength;
                        ViewModel.WorkingMount.HornMaterial = (HornMaterial)newValue;
                        if (_hornMaterialValue != null)
                        {
                            _hornMaterialValue.text = ((HornMaterial)newValue).ToString();
                        }
                        break;
                    }
            }

            // 変更を通知
            ViewModel.NotifyMountChanged();
        }

        private void OnSaddleChanged(ChangeEvent<bool> evt)
        {
            if (ViewModel != null)
            {
                ViewModel.WorkingMount.HasSaddle = evt.newValue;
                ViewModel.NotifyMountChanged();
            }
        }

        private void OnReinsChanged(ChangeEvent<bool> evt)
        {
            if (ViewModel != null)
            {
                ViewModel.WorkingMount.HasReins = evt.newValue;
                ViewModel.NotifyMountChanged();
            }
        }

        // ボタンハンドラ
        private void OnResetButtonClicked()
        {
            ViewModel?.ResetCommand.Execute(null);
        }

        private void OnBackButtonClicked()
        {
            ViewModel?.BackToMenuCommand.Execute(null);
        }

        private void OnCombatIdleToggleClicked()
        {
            _isCombatIdleMode = !_isCombatIdleMode;

            // ボタンテキストを更新
            if (_combatIdleToggleButton != null)
            {
                _combatIdleToggleButton.text = _isCombatIdleMode ? "Idle" : "Combat";
            }

            // アニメーターを切り替え
            if (_currentPreviewCharacter != null)
            {
                var customizationService = ServiceLocator.Instance.Get<ICustomizationService>();
                if (customizationService != null)
                {
                    // P09CharacterApplierにアニメーターを設定
                    var p09Applier = customizationService.GetP09CharacterApplier();
                    if (p09Applier != null)
                    {
                        p09Applier.MaleCombatIdleAnimator = _maleCombatIdleAnimatorController;
                        p09Applier.FemaleCombatIdleAnimator = _femaleCombatIdleAnimatorController;
                    }

                    // アニメーターモードを切り替え
                    customizationService.SetCharacterCombatIdleMode(_currentPreviewCharacter, _isCombatIdleMode);
                }
            }
        }

        #endregion

        #region 3D Preview

        /// <summary>
        /// プレビューオブジェクトを初期化します（シーン開始時に両方インスタンス化）
        /// </summary>
        private void InitializePreviewObjects()
        {
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

                    // アニメーションを再生（アイドルポーズ）
                    EnableIdleAnimation(_currentPreviewCharacter, ViewModel.WorkingCharacter.Gender);
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
