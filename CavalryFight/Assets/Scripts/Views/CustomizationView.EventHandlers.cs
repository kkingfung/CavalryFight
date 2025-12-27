#nullable enable

using System;
using CavalryFight.Core.Services;
using CavalryFight.Services.Customization;
using CavalryFight.Services.SceneManagement;
using CavalryFight.ViewModels;
using UnityEngine;
using UnityEngine.UIElements;

namespace CavalryFight.Views
{
    /// <summary>
    /// CustomizationView partial class - イベント登録とハンドラ
    /// </summary>
    public partial class CustomizationView
    {
        #region Event Registration

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

            // キャラクター矢印ボタン
            RegisterCharacterArrowButtons();

            // 馬矢印ボタン
            RegisterMountArrowButtons();

            // トグル
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
        /// キャラクター矢印ボタンのイベントを登録します
        /// </summary>
        private void RegisterCharacterArrowButtons()
        {
            // FaceType
            if (_faceTypePrevButton != null)
            {
                _faceTypePrevButton.clicked += () => OnArrowButtonClicked("FaceType", -1, 1, 3);
            }
            if (_faceTypeNextButton != null)
            {
                _faceTypeNextButton.clicked += () => OnArrowButtonClicked("FaceType", 1, 1, 3);
            }

            // Hairstyle
            if (_hairstylePrevButton != null)
            {
                _hairstylePrevButton.clicked += () => OnArrowButtonClicked("Hairstyle", -1, 1, 14);
            }
            if (_hairstyleNextButton != null)
            {
                _hairstyleNextButton.clicked += () => OnArrowButtonClicked("Hairstyle", 1, 1, 14);
            }

            // HairColor
            if (_hairColorPrevButton != null)
            {
                _hairColorPrevButton.clicked += () => OnArrowButtonClicked("HairColor", -1, 1, 9);
            }
            if (_hairColorNextButton != null)
            {
                _hairColorNextButton.clicked += () => OnArrowButtonClicked("HairColor", 1, 1, 9);
            }

            // EyeColor
            if (_eyeColorPrevButton != null)
            {
                _eyeColorPrevButton.clicked += () => OnArrowButtonClicked("EyeColor", -1, 1, 5);
            }
            if (_eyeColorNextButton != null)
            {
                _eyeColorNextButton.clicked += () => OnArrowButtonClicked("EyeColor", 1, 1, 5);
            }

            // FacialHair
            if (_facialHairPrevButton != null)
            {
                _facialHairPrevButton.clicked += () => OnArrowButtonClicked("FacialHair", -1, 0, 8);
            }
            if (_facialHairNextButton != null)
            {
                _facialHairNextButton.clicked += () => OnArrowButtonClicked("FacialHair", 1, 0, 8);
            }

            // SkinTone
            if (_skinTonePrevButton != null)
            {
                _skinTonePrevButton.clicked += () => OnArrowButtonClicked("SkinTone", -1, 1, 3);
            }
            if (_skinToneNextButton != null)
            {
                _skinToneNextButton.clicked += () => OnArrowButtonClicked("SkinTone", 1, 1, 3);
            }

            // BustSize
            if (_bustSizePrevButton != null)
            {
                _bustSizePrevButton.clicked += () => OnArrowButtonClicked("BustSize", -1, 1, 3);
            }
            if (_bustSizeNextButton != null)
            {
                _bustSizeNextButton.clicked += () => OnArrowButtonClicked("BustSize", 1, 1, 3);
            }

            // HeadArmor
            if (_headArmorPrevButton != null)
            {
                _headArmorPrevButton.clicked += () => OnArrowButtonClicked("HeadArmor", -1, 0, 12);
            }
            if (_headArmorNextButton != null)
            {
                _headArmorNextButton.clicked += () => OnArrowButtonClicked("HeadArmor", 1, 0, 12);
            }

            // ChestArmor
            if (_chestArmorPrevButton != null)
            {
                _chestArmorPrevButton.clicked += () => OnArrowButtonClicked("ChestArmor", -1, 0, 12);
            }
            if (_chestArmorNextButton != null)
            {
                _chestArmorNextButton.clicked += () => OnArrowButtonClicked("ChestArmor", 1, 0, 12);
            }

            // ArmsArmor
            if (_armsArmorPrevButton != null)
            {
                _armsArmorPrevButton.clicked += () => OnArrowButtonClicked("ArmsArmor", -1, 0, 12);
            }
            if (_armsArmorNextButton != null)
            {
                _armsArmorNextButton.clicked += () => OnArrowButtonClicked("ArmsArmor", 1, 0, 12);
            }

            // WaistArmor
            if (_waistArmorPrevButton != null)
            {
                _waistArmorPrevButton.clicked += () => OnArrowButtonClicked("WaistArmor", -1, 1, 12);
            }
            if (_waistArmorNextButton != null)
            {
                _waistArmorNextButton.clicked += () => OnArrowButtonClicked("WaistArmor", 1, 1, 12);
            }

            // LegsArmor
            if (_legsArmorPrevButton != null)
            {
                _legsArmorPrevButton.clicked += () => OnArrowButtonClicked("LegsArmor", -1, 0, 12);
            }
            if (_legsArmorNextButton != null)
            {
                _legsArmorNextButton.clicked += () => OnArrowButtonClicked("LegsArmor", 1, 0, 12);
            }

            // Bow
            if (_bowPrevButton != null)
            {
                _bowPrevButton.clicked += () => OnArrowButtonClicked("Bow", -1, 10, 13);
            }
            if (_bowNextButton != null)
            {
                _bowNextButton.clicked += () => OnArrowButtonClicked("Bow", 1, 10, 13);
            }
        }

        /// <summary>
        /// 馬矢印ボタンのイベントを登録します
        /// </summary>
        private void RegisterMountArrowButtons()
        {
            // CoatColor
            if (_coatColorPrevButton != null)
            {
                _coatColorPrevButton.clicked += () => OnMountEnumArrowClicked("CoatColor", -1);
            }
            if (_coatColorNextButton != null)
            {
                _coatColorNextButton.clicked += () => OnMountEnumArrowClicked("CoatColor", 1);
            }

            // ManeStyle
            if (_maneStylePrevButton != null)
            {
                _maneStylePrevButton.clicked += () => OnMountEnumArrowClicked("ManeStyle", -1);
            }
            if (_maneStyleNextButton != null)
            {
                _maneStyleNextButton.clicked += () => OnMountEnumArrowClicked("ManeStyle", 1);
            }

            // ManeColor
            if (_maneColorPrevButton != null)
            {
                _maneColorPrevButton.clicked += () => OnMountEnumArrowClicked("ManeColor", -1);
            }
            if (_maneColorNextButton != null)
            {
                _maneColorNextButton.clicked += () => OnMountEnumArrowClicked("ManeColor", 1);
            }

            // HornType
            if (_hornTypePrevButton != null)
            {
                _hornTypePrevButton.clicked += () => OnMountEnumArrowClicked("HornType", -1);
            }
            if (_hornTypeNextButton != null)
            {
                _hornTypeNextButton.clicked += () => OnMountEnumArrowClicked("HornType", 1);
            }

            // HornMaterial
            if (_hornMaterialPrevButton != null)
            {
                _hornMaterialPrevButton.clicked += () => OnMountEnumArrowClicked("HornMaterial", -1);
            }
            if (_hornMaterialNextButton != null)
            {
                _hornMaterialNextButton.clicked += () => OnMountEnumArrowClicked("HornMaterial", 1);
            }

            // MountArmor
            if (_mountArmorPrevButton != null)
            {
                _mountArmorPrevButton.clicked += () => OnArrowButtonClicked("MountArmor", -1, 0, 3);
            }
            if (_mountArmorNextButton != null)
            {
                _mountArmorNextButton.clicked += () => OnArrowButtonClicked("MountArmor", 1, 0, 3);
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

        #region Event Handlers - ViewModel Events

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
        private void OnPreviewUpdated(object? sender, EventArgs e)
        {
            UpdatePreview();
        }

        /// <summary>
        /// メニューに戻る要求イベントを処理します
        /// </summary>
        private void OnBackToMenuRequested(object? sender, EventArgs e)
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

        #endregion

        #region Event Handlers - UI Events

        /// <summary>
        /// タブボタンクリックハンドラ
        /// </summary>
        private void OnTabButtonClicked(CustomizationCategory category)
        {
            ViewModel?.SelectCategoryCommand.Execute(category);
        }

        /// <summary>
        /// 性別変更ハンドラ
        /// </summary>
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
                        int enumLength = Enum.GetValues(typeof(HorseColor)).Length;
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
                        int enumLength = Enum.GetValues(typeof(ManeStyle)).Length;
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
                        int enumLength = Enum.GetValues(typeof(ManeColor)).Length;
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
                        int enumLength = Enum.GetValues(typeof(HornType)).Length;
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
                        int enumLength = Enum.GetValues(typeof(HornMaterial)).Length;
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

        /// <summary>
        /// 鞍トグル変更ハンドラ
        /// </summary>
        private void OnSaddleChanged(ChangeEvent<bool> evt)
        {
            if (ViewModel != null)
            {
                ViewModel.WorkingMount.HasSaddle = evt.newValue;
                ViewModel.NotifyMountChanged();
            }
        }

        /// <summary>
        /// 手綱トグル変更ハンドラ
        /// </summary>
        private void OnReinsChanged(ChangeEvent<bool> evt)
        {
            if (ViewModel != null)
            {
                ViewModel.WorkingMount.HasReins = evt.newValue;
                ViewModel.NotifyMountChanged();
            }
        }

        /// <summary>
        /// リセットボタンクリックハンドラ
        /// </summary>
        private void OnResetButtonClicked()
        {
            ViewModel?.ResetCommand.Execute(null);
        }

        /// <summary>
        /// 戻るボタンクリックハンドラ
        /// </summary>
        private void OnBackButtonClicked()
        {
            ViewModel?.BackToMenuCommand.Execute(null);
        }

        /// <summary>
        /// 戦闘待機トグルボタンクリックハンドラ
        /// </summary>
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
    }
}
