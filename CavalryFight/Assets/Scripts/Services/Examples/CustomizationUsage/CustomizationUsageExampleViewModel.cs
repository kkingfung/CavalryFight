#nullable enable

using CavalryFight.Core.Services;
using UnityEngine;

namespace CavalryFight.Services.Customization
{
    /// <summary>
    /// カスタマイズサービス使用例ViewModel
    /// </summary>
    /// <remarks>
    /// カスタマイズサービスの使用方法を示すサンプルコードです。
    /// 実際のゲームでは、このパターンを参考にして実装してください。
    /// </remarks>
    public class CustomizationUsageExampleViewModel : MonoBehaviour
    {
        #region Fields

        /// <summary>
        /// カスタマイズサービス
        /// </summary>
        private ICustomizationService? _customizationService;

        /// <summary>
        /// サンプル用キャラクターオブジェクト
        /// </summary>
        [SerializeField]
        [Tooltip("カスタマイズを適用するキャラクターオブジェクト")]
        private GameObject? _characterObject;

        /// <summary>
        /// サンプル用騎乗動物オブジェクト
        /// </summary>
        [SerializeField]
        [Tooltip("カスタマイズを適用する騎乗動物オブジェクト")]
        private GameObject? _mountObject;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            // サービスを取得
            _customizationService = ServiceLocator.Instance.Get<ICustomizationService>();

            if (_customizationService == null)
            {
                Debug.LogError("[CustomizationUsageExample] CustomizationService not found!");
                return;
            }

            // イベントを購読
            _customizationService.CharacterCustomizationChanged += OnCharacterCustomizationChanged;
            _customizationService.MountCustomizationChanged += OnMountCustomizationChanged;
            _customizationService.PresetSaved += OnPresetSaved;
            _customizationService.PresetLoaded += OnPresetLoaded;
            _customizationService.PresetDeleted += OnPresetDeleted;

            Debug.Log("[CustomizationUsageExample] Customization service initialized.");
        }

        private void OnDestroy()
        {
            // イベント購読を解除
            if (_customizationService != null)
            {
                _customizationService.CharacterCustomizationChanged -= OnCharacterCustomizationChanged;
                _customizationService.MountCustomizationChanged -= OnMountCustomizationChanged;
                _customizationService.PresetSaved -= OnPresetSaved;
                _customizationService.PresetLoaded -= OnPresetLoaded;
                _customizationService.PresetDeleted -= OnPresetDeleted;
            }
        }

        #endregion

        #region Sample Methods

        /// <summary>
        /// キャラクターカスタマイズを設定する例
        /// </summary>
        public void ExampleSetCharacterCustomization()
        {
            if (_customizationService == null)
            {
                return;
            }

            // カスタマイズデータを作成
            var characterCustomization = new CharacterCustomization
            {
                Gender = Gender.Male,
                FaceType = 0,
                HairstyleId = 5,
                HairColorId = 3,
                EyeColorId = 2,
                FacialHairId = 4,
                SkinToneId = 2,
                HeadArmorId = 5,
                ChestArmorId = 8,
                ArmsArmorId = 8,
                WaistArmorId = 6,
                LegsArmorId = 8,
                BowId = 2
            };

            // カスタマイズを設定
            _customizationService.SetCharacterCustomization(characterCustomization);

            Debug.Log("[CustomizationUsageExample] Character customization set.");
        }

        /// <summary>
        /// 騎乗動物カスタマイズを設定する例
        /// </summary>
        public void ExampleSetMountCustomization()
        {
            if (_customizationService == null)
            {
                return;
            }

            // カスタマイズデータを作成
            var mountCustomization = new MountCustomization
            {
                MountType = MountType.HorseRealistic,
                CoatColor = HorseColor.Brown,
                ManeStyle = ManeStyle.LongLeft,
                ManeColor = ManeColor.Black,
                ArmorId = 2,
                HasSaddle = true
            };

            // カスタマイズを設定
            _customizationService.SetMountCustomization(mountCustomization);

            Debug.Log("[CustomizationUsageExample] Mount customization set.");
        }

        /// <summary>
        /// カスタマイズを適用する例
        /// </summary>
        public void ExampleApplyCustomization()
        {
            if (_customizationService == null)
            {
                Debug.LogError("[CustomizationUsageExample] Customization service is null.");
                return;
            }

            if (_characterObject == null || _mountObject == null)
            {
                Debug.LogError("[CustomizationUsageExample] Character or mount object is not assigned.");
                return;
            }

            // キャラクターと騎乗動物の両方にカスタマイズを適用
            bool success = _customizationService.ApplyCustomization(_characterObject, _mountObject);

            if (success)
            {
                Debug.Log("[CustomizationUsageExample] Customization applied successfully.");
            }
            else
            {
                Debug.LogError("[CustomizationUsageExample] Failed to apply customization.");
            }
        }

        /// <summary>
        /// プリセットを保存する例
        /// </summary>
        public void ExampleSavePreset()
        {
            if (_customizationService == null)
            {
                return;
            }

            string presetName = "MyPreset_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss");

            bool success = _customizationService.SavePreset(presetName);

            if (success)
            {
                Debug.Log($"[CustomizationUsageExample] Preset saved: {presetName}");
            }
            else
            {
                Debug.LogError("[CustomizationUsageExample] Failed to save preset.");
            }
        }

        /// <summary>
        /// プリセットを読み込む例
        /// </summary>
        public void ExampleLoadPreset(string presetName)
        {
            if (_customizationService == null)
            {
                return;
            }

            bool success = _customizationService.LoadPreset(presetName);

            if (success)
            {
                Debug.Log($"[CustomizationUsageExample] Preset loaded: {presetName}");

                // 読み込んだカスタマイズを適用
                if (_characterObject != null && _mountObject != null)
                {
                    _customizationService.ApplyCustomization(_characterObject, _mountObject);
                }
            }
            else
            {
                Debug.LogError($"[CustomizationUsageExample] Failed to load preset: {presetName}");
            }
        }

        /// <summary>
        /// 保存されているプリセット一覧を取得する例
        /// </summary>
        public void ExampleGetPresetNames()
        {
            if (_customizationService == null)
            {
                return;
            }

            var presetNames = _customizationService.GetPresetNames();

            Debug.Log($"[CustomizationUsageExample] Found {presetNames.Count} presets:");
            foreach (var name in presetNames)
            {
                Debug.Log($"  - {name}");
            }
        }

        /// <summary>
        /// プリセットを削除する例
        /// </summary>
        public void ExampleDeletePreset(string presetName)
        {
            if (_customizationService == null)
            {
                return;
            }

            bool success = _customizationService.DeletePreset(presetName);

            if (success)
            {
                Debug.Log($"[CustomizationUsageExample] Preset deleted: {presetName}");
            }
            else
            {
                Debug.LogError($"[CustomizationUsageExample] Failed to delete preset: {presetName}");
            }
        }

        /// <summary>
        /// デフォルト設定にリセットする例
        /// </summary>
        public void ExampleResetToDefault()
        {
            if (_customizationService == null)
            {
                return;
            }

            _customizationService.ResetToDefault();

            Debug.Log("[CustomizationUsageExample] Customization reset to default.");

            // リセット後のカスタマイズを適用
            if (_characterObject != null && _mountObject != null)
            {
                _customizationService.ApplyCustomization(_characterObject, _mountObject);
            }
        }

        /// <summary>
        /// 防具スロットの値を変更する例
        /// </summary>
        public void ExampleChangeArmorSlot()
        {
            if (_customizationService == null)
            {
                return;
            }

            // 現在のカスタマイズを取得
            var characterCustomization = _customizationService.CurrentCharacter.Clone();

            // 頭部防具を変更
            characterCustomization.SetArmorId(ArmorSlot.Head, 7);

            // 変更を適用
            _customizationService.SetCharacterCustomization(characterCustomization);

            Debug.Log("[CustomizationUsageExample] Armor slot changed.");
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// キャラクターカスタマイズ変更イベントハンドラ
        /// </summary>
        private void OnCharacterCustomizationChanged(CharacterCustomization customization)
        {
            Debug.Log($"[CustomizationUsageExample] Character customization changed: Gender={customization.Gender}, HairstyleId={customization.HairstyleId}");

            // UI更新などの処理をここに記述
        }

        /// <summary>
        /// 騎乗動物カスタマイズ変更イベントハンドラ
        /// </summary>
        private void OnMountCustomizationChanged(MountCustomization customization)
        {
            Debug.Log($"[CustomizationUsageExample] Mount customization changed: MountType={customization.MountType}, CoatColor={customization.CoatColor}");

            // UI更新などの処理をここに記述
        }

        /// <summary>
        /// プリセット保存イベントハンドラ
        /// </summary>
        private void OnPresetSaved(string presetName)
        {
            Debug.Log($"[CustomizationUsageExample] Preset saved event: {presetName}");

            // プリセットリストUI更新などの処理をここに記述
        }

        /// <summary>
        /// プリセット読み込みイベントハンドラ
        /// </summary>
        private void OnPresetLoaded(string presetName)
        {
            Debug.Log($"[CustomizationUsageExample] Preset loaded event: {presetName}");

            // UI更新などの処理をここに記述
        }

        /// <summary>
        /// プリセット削除イベントハンドラ
        /// </summary>
        private void OnPresetDeleted(string presetName)
        {
            Debug.Log($"[CustomizationUsageExample] Preset deleted event: {presetName}");

            // プリセットリストUI更新などの処理をここに記述
        }

        #endregion
    }
}
