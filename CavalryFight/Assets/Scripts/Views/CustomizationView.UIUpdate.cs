#nullable enable

using UnityEngine.UIElements;

namespace CavalryFight.Views
{
    /// <summary>
    /// CustomizationView partial class - UIの更新処理
    /// </summary>
    public partial class CustomizationView
    {
        #region UI Update from ViewModel

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

        #endregion

        #region Category Visibility

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
    }
}
