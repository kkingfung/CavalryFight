#nullable enable

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace CavalryFight.Views
{
    /// <summary>
    /// CustomizationView partial class - UI要素の取得と検証
    /// </summary>
    public partial class CustomizationView
    {
        #region UI Element Retrieval

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

        #endregion

        #region UI Element Validation

        /// <summary>
        /// UI要素が正しく取得できているか検証します。
        /// </summary>
        /// <returns>クリティカルなUI要素が全て取得できた場合はtrue</returns>
        private bool ValidateUIElements()
        {
            // クリティカルな要素 - これらが欠けているとViewが機能しない
            var criticalElements = new (string name, VisualElement? element)[]
            {
                (nameof(_characterTabButton), _characterTabButton),
                (nameof(_mountTabButton), _mountTabButton),
                (nameof(_characterPanel), _characterPanel),
                (nameof(_mountPanel), _mountPanel),
                (nameof(_resetButton), _resetButton),
                (nameof(_backButton), _backButton)
            };

            bool criticalValid = ValidateElements(criticalElements, LogLevel.Error);

            // オプショナルな要素 - 警告のみ出力
            var optionalElements = new (string name, VisualElement? element)[]
            {
                (nameof(_previewContainer), _previewContainer),
                (nameof(_genderRadioGroup), _genderRadioGroup),
                (nameof(_combatIdleToggleButton), _combatIdleToggleButton)
            };

            ValidateElements(optionalElements, LogLevel.Warning);

            return criticalValid;
        }

        /// <summary>
        /// UI要素の検証ログレベル
        /// </summary>
        private enum LogLevel
        {
            Warning,
            Error
        }

        /// <summary>
        /// UI要素のリストを検証します
        /// </summary>
        /// <param name="elements">検証する要素のリスト</param>
        /// <param name="logLevel">ログレベル</param>
        /// <returns>全ての要素が有効な場合はtrue</returns>
        private bool ValidateElements((string name, VisualElement? element)[] elements, LogLevel logLevel)
        {
            bool allValid = true;

            foreach (var (name, element) in elements)
            {
                if (element == null)
                {
                    string message = $"[CustomizationView] {name} not found in UXML.";

                    if (logLevel == LogLevel.Error)
                    {
                        Debug.LogError(message, this);
                    }
                    else
                    {
                        Debug.LogWarning(message, this);
                    }

                    allValid = false;
                }
            }

            return allValid;
        }

        #endregion

        #region Dropdown Setup

        /// <summary>
        /// Dropdownの選択肢を設定します
        /// </summary>
        private void SetupDropdowns()
        {
            // 性別
            if (_genderRadioGroup != null)
            {
                _genderRadioGroup.choices = new List<string> { "Male", "Female" };
            }
        }

        #endregion
    }
}
