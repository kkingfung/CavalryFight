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
        private DropdownField? _mountTypeDropdown;
        private DropdownField? _coatColorDropdown;
        private DropdownField? _maneStyleDropdown;
        private DropdownField? _maneColorDropdown;

        // MountArmor用矢印コントロール
        private Button? _mountArmorPrevButton;
        private Label? _mountArmorValue;
        private Button? _mountArmorNextButton;

        private Toggle? _saddleToggle;

        // プレビュー
        private VisualElement? _previewContainer;

        // 下部ボタン
        private Button? _resetButton;
        private Button? _backButton;

        // 3Dプレビューオブジェクト
        private GameObject? _currentPreviewCharacter;
        private GameObject? _currentPreviewMount;

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
            _mountTypeDropdown = Q<DropdownField>("MountTypeDropdown");
            _coatColorDropdown = Q<DropdownField>("CoatColorDropdown");
            _maneStyleDropdown = Q<DropdownField>("ManeStyleDropdown");
            _maneColorDropdown = Q<DropdownField>("ManeColorDropdown");

            _mountArmorPrevButton = Q<Button>("MountArmorPrevButton");
            _mountArmorValue = Q<Label>("MountArmorValue");
            _mountArmorNextButton = Q<Button>("MountArmorNextButton");

            _saddleToggle = Q<Toggle>("SaddleToggle");

            // 下部ボタン
            _resetButton = Q<Button>("ResetButton");
            _backButton = Q<Button>("BackButton");
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

            // 馬のタイプ
            if (_mountTypeDropdown != null)
            {
                _mountTypeDropdown.choices = System.Enum.GetNames(typeof(MountType))
                    .Select(name => name).ToList();
            }

            // 毛色
            if (_coatColorDropdown != null)
            {
                _coatColorDropdown.choices = System.Enum.GetNames(typeof(HorseColor))
                    .Select(name => name).ToList();
            }

            // たてがみスタイル
            if (_maneStyleDropdown != null)
            {
                _maneStyleDropdown.choices = System.Enum.GetNames(typeof(ManeStyle))
                    .Select(name => name).ToList();
            }

            // たてがみの色
            if (_maneColorDropdown != null)
            {
                _maneColorDropdown.choices = System.Enum.GetNames(typeof(ManeColor))
                    .Select(name => name).ToList();
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

            // 馬のタイプ
            if (_mountTypeDropdown != null)
            {
                _mountTypeDropdown.index = (int)mount.MountType;
            }

            // 外見
            if (_coatColorDropdown != null)
            {
                _coatColorDropdown.index = (int)mount.CoatColor;
            }
            if (_maneStyleDropdown != null)
            {
                _maneStyleDropdown.index = (int)mount.ManeStyle;
            }
            if (_maneColorDropdown != null)
            {
                _maneColorDropdown.index = (int)mount.ManeColor;
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
                _faceTypePrevButton.clicked += () => OnArrowButtonClicked("FaceType", -1, 0, 1);
            }
            if (_faceTypeNextButton != null)
            {
                _faceTypeNextButton.clicked += () => OnArrowButtonClicked("FaceType", 1, 0, 1);
            }

            if (_hairstylePrevButton != null)
            {
                _hairstylePrevButton.clicked += () => OnArrowButtonClicked("Hairstyle", -1, 0, 14);
            }
            if (_hairstyleNextButton != null)
            {
                _hairstyleNextButton.clicked += () => OnArrowButtonClicked("Hairstyle", 1, 0, 14);
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
                _eyeColorPrevButton.clicked += () => OnArrowButtonClicked("EyeColor", -1, 1, 9);
            }
            if (_eyeColorNextButton != null)
            {
                _eyeColorNextButton.clicked += () => OnArrowButtonClicked("EyeColor", 1, 1, 9);
            }

            if (_facialHairPrevButton != null)
            {
                _facialHairPrevButton.clicked += () => OnArrowButtonClicked("FacialHair", -1, 0, 10);
            }
            if (_facialHairNextButton != null)
            {
                _facialHairNextButton.clicked += () => OnArrowButtonClicked("FacialHair", 1, 0, 10);
            }

            if (_skinTonePrevButton != null)
            {
                _skinTonePrevButton.clicked += () => OnArrowButtonClicked("SkinTone", -1, 1, 9);
            }
            if (_skinToneNextButton != null)
            {
                _skinToneNextButton.clicked += () => OnArrowButtonClicked("SkinTone", 1, 1, 9);
            }

            if (_bustSizePrevButton != null)
            {
                _bustSizePrevButton.clicked += () => OnArrowButtonClicked("BustSize", -1, 0, 5);
            }
            if (_bustSizeNextButton != null)
            {
                _bustSizeNextButton.clicked += () => OnArrowButtonClicked("BustSize", 1, 0, 5);
            }

            if (_headArmorPrevButton != null)
            {
                _headArmorPrevButton.clicked += () => OnArrowButtonClicked("HeadArmor", -1, 0, 10);
            }
            if (_headArmorNextButton != null)
            {
                _headArmorNextButton.clicked += () => OnArrowButtonClicked("HeadArmor", 1, 0, 10);
            }

            if (_chestArmorPrevButton != null)
            {
                _chestArmorPrevButton.clicked += () => OnArrowButtonClicked("ChestArmor", -1, 0, 10);
            }
            if (_chestArmorNextButton != null)
            {
                _chestArmorNextButton.clicked += () => OnArrowButtonClicked("ChestArmor", 1, 0, 10);
            }

            if (_armsArmorPrevButton != null)
            {
                _armsArmorPrevButton.clicked += () => OnArrowButtonClicked("ArmsArmor", -1, 0, 10);
            }
            if (_armsArmorNextButton != null)
            {
                _armsArmorNextButton.clicked += () => OnArrowButtonClicked("ArmsArmor", 1, 0, 10);
            }

            if (_waistArmorPrevButton != null)
            {
                _waistArmorPrevButton.clicked += () => OnArrowButtonClicked("WaistArmor", -1, 0, 10);
            }
            if (_waistArmorNextButton != null)
            {
                _waistArmorNextButton.clicked += () => OnArrowButtonClicked("WaistArmor", 1, 0, 10);
            }

            if (_legsArmorPrevButton != null)
            {
                _legsArmorPrevButton.clicked += () => OnArrowButtonClicked("LegsArmor", -1, 0, 10);
            }
            if (_legsArmorNextButton != null)
            {
                _legsArmorNextButton.clicked += () => OnArrowButtonClicked("LegsArmor", 1, 0, 10);
            }

            if (_bowPrevButton != null)
            {
                _bowPrevButton.clicked += () => OnArrowButtonClicked("Bow", -1, 1, 5);
            }
            if (_bowNextButton != null)
            {
                _bowNextButton.clicked += () => OnArrowButtonClicked("Bow", 1, 1, 5);
            }

            // 馬コントロール
            if (_mountTypeDropdown != null)
            {
                _mountTypeDropdown.RegisterValueChangedCallback(OnMountTypeChanged);
            }
            if (_coatColorDropdown != null)
            {
                _coatColorDropdown.RegisterValueChangedCallback(OnCoatColorChanged);
            }
            if (_maneStyleDropdown != null)
            {
                _maneStyleDropdown.RegisterValueChangedCallback(OnManeStyleChanged);
            }
            if (_maneColorDropdown != null)
            {
                _maneColorDropdown.RegisterValueChangedCallback(OnManeColorChanged);
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

            // 下部ボタン
            if (_resetButton != null)
            {
                _resetButton.clicked += OnResetButtonClicked;
            }
            if (_backButton != null)
            {
                _backButton.clicked += OnBackButtonClicked;
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
            if (_mountTypeDropdown != null)
            {
                _mountTypeDropdown.UnregisterValueChangedCallback(OnMountTypeChanged);
            }
            if (_coatColorDropdown != null)
            {
                _coatColorDropdown.UnregisterValueChangedCallback(OnCoatColorChanged);
            }
            if (_maneStyleDropdown != null)
            {
                _maneStyleDropdown.UnregisterValueChangedCallback(OnManeStyleChanged);
            }
            if (_maneColorDropdown != null)
            {
                _maneColorDropdown.UnregisterValueChangedCallback(OnManeColorChanged);
            }
            if (_saddleToggle != null)
            {
                _saddleToggle.UnregisterValueChangedCallback(OnSaddleChanged);
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

        // 馬変更ハンドラ
        private void OnMountTypeChanged(ChangeEvent<string> evt)
        {
            if (ViewModel != null && _mountTypeDropdown != null)
            {
                ViewModel.WorkingMount.MountType = (MountType)_mountTypeDropdown.index;
                ViewModel.NotifyMountChanged();
            }
        }

        private void OnCoatColorChanged(ChangeEvent<string> evt)
        {
            if (ViewModel != null && _coatColorDropdown != null)
            {
                ViewModel.WorkingMount.CoatColor = (HorseColor)_coatColorDropdown.index;
                ViewModel.NotifyMountChanged();
            }
        }

        private void OnManeStyleChanged(ChangeEvent<string> evt)
        {
            if (ViewModel != null && _maneStyleDropdown != null)
            {
                ViewModel.WorkingMount.ManeStyle = (ManeStyle)_maneStyleDropdown.index;
                ViewModel.NotifyMountChanged();
            }
        }

        private void OnManeColorChanged(ChangeEvent<string> evt)
        {
            if (ViewModel != null && _maneColorDropdown != null)
            {
                ViewModel.WorkingMount.ManeColor = (ManeColor)_maneColorDropdown.index;
                ViewModel.NotifyMountChanged();
            }
        }

        private void OnSaddleChanged(ChangeEvent<bool> evt)
        {
            if (ViewModel != null)
            {
                ViewModel.WorkingMount.HasSaddle = evt.newValue;
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

        #endregion

        #region 3D Preview

        /// <summary>
        /// プレビューを更新します
        /// </summary>
        private void UpdatePreview()
        {
            if (ViewModel == null || _previewCamera == null)
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
                // 必要に応じてキャラクタープレビューをインスタンス化
                if (_currentPreviewCharacter == null && _characterPreviewPrefab != null)
                {
                    _currentPreviewCharacter = Instantiate(_characterPreviewPrefab, _Container);
                    _currentPreviewCharacter.transform.localPosition = Vector3.zero;
                    _currentPreviewCharacter.transform.localRotation = Quaternion.identity;
                    SetLayerRecursively(_currentPreviewCharacter, LayerMask.NameToLayer("Preview"));
                    Debug.Log($"[CustomizationView] Instantiated character preview");
                }

                // キャラクターを表示
                if (_currentPreviewCharacter != null)
                {
                    _currentPreviewCharacter.SetActive(true);
                    customizationService.ApplyCharacterCustomization(_currentPreviewCharacter);
                }

                // 馬を非表示
                if (_currentPreviewMount != null)
                {
                    _currentPreviewMount.SetActive(false);
                }
            }
            else if (ViewModel.IsMountCategory)
            {
                // 馬も同様
                if (_currentPreviewMount == null && _mountPreviewPrefab != null)
                {
                    _currentPreviewMount = Instantiate(_mountPreviewPrefab, _Container);
                    _currentPreviewMount.transform.localPosition = Vector3.zero;
                    _currentPreviewMount.transform.localRotation = Quaternion.identity;
                    SetLayerRecursively(_currentPreviewMount, LayerMask.NameToLayer("Preview"));
                    Debug.Log($"[CustomizationView] Instantiated mount preview");
                }

                // 馬を表示
                if (_currentPreviewMount != null)
                {
                    _currentPreviewMount.SetActive(true);
                    customizationService.ApplyMountCustomization(_currentPreviewMount);
                }

                // キャラクターを非表示
                if (_currentPreviewCharacter != null)
                {
                    _currentPreviewCharacter.SetActive(false);
                }
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

        #endregion
    }
}
