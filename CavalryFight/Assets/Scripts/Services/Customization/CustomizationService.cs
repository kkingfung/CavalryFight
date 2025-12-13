#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CavalryFight.Core.Services;
using UnityEngine;

namespace CavalryFight.Services.Customization
{
    /// <summary>
    /// カスタマイズサービスの実装
    /// </summary>
    /// <remarks>
    /// キャラクターと騎乗動物のカスタマイズを管理します。
    /// プリセットの保存・読込、カスタマイズの適用を提供します。
    /// </remarks>
    public class CustomizationService : ICustomizationService
    {
        #region Constants

        /// <summary>
        /// プリセット保存フォルダ名
        /// </summary>
        private const string PRESETS_FOLDER = "CustomizationPresets";

        /// <summary>
        /// プリセットファイル拡張子
        /// </summary>
        private const string PRESET_EXTENSION = ".json";

        #endregion

        #region Fields

        /// <summary>
        /// 初期化済みフラグ
        /// </summary>
        private bool _initialized = false;

        /// <summary>
        /// 現在のキャラクターカスタマイズ
        /// </summary>
        private CharacterCustomization _currentCharacter = new CharacterCustomization();

        /// <summary>
        /// 現在の騎乗動物カスタマイズ
        /// </summary>
        private MountCustomization _currentMount = new MountCustomization();

        /// <summary>
        /// キャラクターApplier
        /// </summary>
        private ICharacterApplier? _characterApplier;

        /// <summary>
        /// 騎乗動物Applier
        /// </summary>
        private IMountApplier? _mountApplier;

        /// <summary>
        /// プリセット保存パス
        /// </summary>
        private string _presetsPath = string.Empty;

        #endregion

        #region Events

        /// <summary>
        /// キャラクターカスタマイズが変更された時に発生します
        /// </summary>
        public event Action<CharacterCustomization>? CharacterCustomizationChanged;

        /// <summary>
        /// 騎乗動物カスタマイズが変更された時に発生します
        /// </summary>
        public event Action<MountCustomization>? MountCustomizationChanged;

        /// <summary>
        /// プリセットが保存された時に発生します
        /// </summary>
        public event Action<string>? PresetSaved;

        /// <summary>
        /// プリセットが読み込まれた時に発生します
        /// </summary>
        public event Action<string>? PresetLoaded;

        /// <summary>
        /// プリセットが削除された時に発生します
        /// </summary>
        public event Action<string>? PresetDeleted;

        #endregion

        #region Properties

        /// <summary>
        /// 現在のキャラクターカスタマイズを取得します
        /// </summary>
        public CharacterCustomization CurrentCharacter => _currentCharacter;

        /// <summary>
        /// 現在の騎乗動物カスタマイズを取得します
        /// </summary>
        public MountCustomization CurrentMount => _currentMount;

        #endregion

        #region Constructors

        /// <summary>
        /// CustomizationServiceの新しいインスタンスを初期化します
        /// </summary>
        public CustomizationService()
        {
        }

        /// <summary>
        /// CustomizationServiceの新しいインスタンスを初期化します
        /// </summary>
        /// <param name="characterApplier">キャラクターApplier</param>
        /// <param name="mountApplier">騎乗動物Applier</param>
        public CustomizationService(ICharacterApplier characterApplier, IMountApplier mountApplier)
        {
            _characterApplier = characterApplier;
            _mountApplier = mountApplier;
        }

        #endregion

        #region IService Implementation

        /// <summary>
        /// サービスを初期化します
        /// </summary>
        public void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            Debug.Log("[CustomizationService] Initializing...");

            // プリセット保存パスを設定
            _presetsPath = Path.Combine(Application.persistentDataPath, PRESETS_FOLDER);

            // プリセットフォルダが存在しない場合は作成
            if (!Directory.Exists(_presetsPath))
            {
                Directory.CreateDirectory(_presetsPath);
                Debug.Log($"[CustomizationService] Created presets folder: {_presetsPath}");
            }

            _initialized = true;
            Debug.Log("[CustomizationService] Initialization complete.");
        }

        /// <summary>
        /// サービスを破棄します
        /// </summary>
        public void Dispose()
        {
            Debug.Log("[CustomizationService] Disposing...");

            // イベントハンドラをクリア
            CharacterCustomizationChanged = null;
            MountCustomizationChanged = null;
            PresetSaved = null;
            PresetLoaded = null;
            PresetDeleted = null;

            _initialized = false;
        }

        #endregion

        #region Applier Settings

        /// <summary>
        /// キャラクターApplierを設定します
        /// </summary>
        /// <param name="applier">Applier</param>
        public void SetCharacterApplier(ICharacterApplier applier)
        {
            _characterApplier = applier;
        }

        /// <summary>
        /// 騎乗動物Applierを設定します
        /// </summary>
        /// <param name="applier">Applier</param>
        public void SetMountApplier(IMountApplier applier)
        {
            _mountApplier = applier;
        }

        #endregion

        #region Customization Settings

        /// <summary>
        /// キャラクターカスタマイズを設定します
        /// </summary>
        /// <param name="customization">設定するカスタマイズ</param>
        public void SetCharacterCustomization(CharacterCustomization customization)
        {
            if (customization == null)
            {
                Debug.LogError("[CustomizationService] Character customization is null.");
                return;
            }

            _currentCharacter = customization.Clone();
            CharacterCustomizationChanged?.Invoke(_currentCharacter);

            Debug.Log("[CustomizationService] Character customization updated.");
        }

        /// <summary>
        /// 騎乗動物カスタマイズを設定します
        /// </summary>
        /// <param name="customization">設定するカスタマイズ</param>
        public void SetMountCustomization(MountCustomization customization)
        {
            if (customization == null)
            {
                Debug.LogError("[CustomizationService] Mount customization is null.");
                return;
            }

            _currentMount = customization.Clone();
            MountCustomizationChanged?.Invoke(_currentMount);

            Debug.Log("[CustomizationService] Mount customization updated.");
        }

        /// <summary>
        /// キャラクターと騎乗動物のカスタマイズを同時に設定します
        /// </summary>
        /// <param name="character">キャラクターカスタマイズ</param>
        /// <param name="mount">騎乗動物カスタマイズ</param>
        public void SetCustomization(CharacterCustomization character, MountCustomization mount)
        {
            if (character == null || mount == null)
            {
                Debug.LogError("[CustomizationService] Character or mount customization is null.");
                return;
            }

            _currentCharacter = character.Clone();
            _currentMount = mount.Clone();

            CharacterCustomizationChanged?.Invoke(_currentCharacter);
            MountCustomizationChanged?.Invoke(_currentMount);

            Debug.Log("[CustomizationService] Character and mount customization updated.");
        }

        #endregion

        #region Apply Customization

        /// <summary>
        /// キャラクターにカスタマイズを適用します
        /// </summary>
        /// <param name="characterObject">適用先のキャラクターGameObject</param>
        /// <returns>適用に成功したかどうか</returns>
        public bool ApplyCharacterCustomization(GameObject characterObject)
        {
            if (characterObject == null)
            {
                Debug.LogError("[CustomizationService] Character GameObject is null.");
                return false;
            }

            if (_characterApplier == null)
            {
                Debug.LogError("[CustomizationService] Character applier is not set.");
                return false;
            }

            if (!_characterApplier.CanApply(characterObject))
            {
                Debug.LogError($"[CustomizationService] Character applier cannot apply to: {characterObject.name}");
                return false;
            }

            bool success = _characterApplier.Apply(characterObject, _currentCharacter);

            if (success)
            {
                Debug.Log($"[CustomizationService] Character customization applied to: {characterObject.name}");
            }
            else
            {
                Debug.LogError($"[CustomizationService] Failed to apply character customization to: {characterObject.name}");
            }

            return success;
        }

        /// <summary>
        /// 騎乗動物にカスタマイズを適用します
        /// </summary>
        /// <param name="mountObject">適用先の騎乗動物GameObject</param>
        /// <returns>適用に成功したかどうか</returns>
        public bool ApplyMountCustomization(GameObject mountObject)
        {
            if (mountObject == null)
            {
                Debug.LogError("[CustomizationService] Mount GameObject is null.");
                return false;
            }

            if (_mountApplier == null)
            {
                Debug.LogError("[CustomizationService] Mount applier is not set.");
                return false;
            }

            if (!_mountApplier.CanApply(mountObject))
            {
                Debug.LogError($"[CustomizationService] Mount applier cannot apply to: {mountObject.name}");
                return false;
            }

            bool success = _mountApplier.Apply(mountObject, _currentMount);

            if (success)
            {
                Debug.Log($"[CustomizationService] Mount customization applied to: {mountObject.name}");
            }
            else
            {
                Debug.LogError($"[CustomizationService] Failed to apply mount customization to: {mountObject.name}");
            }

            return success;
        }

        /// <summary>
        /// キャラクターと騎乗動物の両方にカスタマイズを適用します
        /// </summary>
        /// <param name="characterObject">適用先のキャラクターGameObject</param>
        /// <param name="mountObject">適用先の騎乗動物GameObject</param>
        /// <returns>適用に成功したかどうか</returns>
        public bool ApplyCustomization(GameObject characterObject, GameObject mountObject)
        {
            bool characterSuccess = ApplyCharacterCustomization(characterObject);
            bool mountSuccess = ApplyMountCustomization(mountObject);

            return characterSuccess && mountSuccess;
        }

        #endregion

        #region Preset Management

        /// <summary>
        /// 現在のカスタマイズをプリセットとして保存します
        /// </summary>
        /// <param name="presetName">プリセット名</param>
        /// <returns>保存に成功したかどうか</returns>
        public bool SavePreset(string presetName)
        {
            if (string.IsNullOrWhiteSpace(presetName))
            {
                Debug.LogError("[CustomizationService] Preset name is empty.");
                return false;
            }

            try
            {
                string filePath = GetPresetFilePath(presetName);
                CustomizationPreset preset = new CustomizationPreset(presetName, _currentCharacter, _currentMount);

                bool success = preset.SaveToFile(filePath);

                if (success)
                {
                    PresetSaved?.Invoke(presetName);
                    Debug.Log($"[CustomizationService] Preset saved: {presetName}");
                }

                return success;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CustomizationService] Failed to save preset '{presetName}': {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// プリセットを読み込んで現在のカスタマイズに設定します
        /// </summary>
        /// <param name="presetName">プリセット名</param>
        /// <returns>読み込みに成功したかどうか</returns>
        public bool LoadPreset(string presetName)
        {
            if (string.IsNullOrWhiteSpace(presetName))
            {
                Debug.LogError("[CustomizationService] Preset name is empty.");
                return false;
            }

            try
            {
                string filePath = GetPresetFilePath(presetName);
                CustomizationPreset? preset = CustomizationPreset.LoadFromFile(filePath);

                if (preset == null)
                {
                    Debug.LogError($"[CustomizationService] Failed to load preset: {presetName}");
                    return false;
                }

                SetCustomization(preset.Character, preset.Mount);
                PresetLoaded?.Invoke(presetName);

                Debug.Log($"[CustomizationService] Preset loaded: {presetName}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CustomizationService] Failed to load preset '{presetName}': {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// プリセットを削除します
        /// </summary>
        /// <param name="presetName">プリセット名</param>
        /// <returns>削除に成功したかどうか</returns>
        public bool DeletePreset(string presetName)
        {
            if (string.IsNullOrWhiteSpace(presetName))
            {
                Debug.LogError("[CustomizationService] Preset name is empty.");
                return false;
            }

            try
            {
                string filePath = GetPresetFilePath(presetName);

                if (!File.Exists(filePath))
                {
                    Debug.LogWarning($"[CustomizationService] Preset not found: {presetName}");
                    return false;
                }

                File.Delete(filePath);
                PresetDeleted?.Invoke(presetName);

                Debug.Log($"[CustomizationService] Preset deleted: {presetName}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CustomizationService] Failed to delete preset '{presetName}': {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 保存されている全てのプリセット名を取得します
        /// </summary>
        /// <returns>プリセット名のリスト</returns>
        public List<string> GetPresetNames()
        {
            try
            {
                if (!Directory.Exists(_presetsPath))
                {
                    return new List<string>();
                }

                var files = Directory.GetFiles(_presetsPath, $"*{PRESET_EXTENSION}");
                return files.Select(f => Path.GetFileNameWithoutExtension(f)).ToList();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CustomizationService] Failed to get preset names: {ex.Message}");
                return new List<string>();
            }
        }

        /// <summary>
        /// プリセットが存在するかどうかを確認します
        /// </summary>
        /// <param name="presetName">プリセット名</param>
        /// <returns>プリセットが存在する場合はtrue</returns>
        public bool PresetExists(string presetName)
        {
            if (string.IsNullOrWhiteSpace(presetName))
            {
                return false;
            }

            string filePath = GetPresetFilePath(presetName);
            return File.Exists(filePath);
        }

        /// <summary>
        /// プリセットのデータを取得します（現在のカスタマイズには設定しません）
        /// </summary>
        /// <param name="presetName">プリセット名</param>
        /// <returns>プリセットデータ（存在しない場合はnull）</returns>
        public CustomizationPreset? GetPreset(string presetName)
        {
            if (string.IsNullOrWhiteSpace(presetName))
            {
                return null;
            }

            try
            {
                string filePath = GetPresetFilePath(presetName);
                return CustomizationPreset.LoadFromFile(filePath);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CustomizationService] Failed to get preset '{presetName}': {ex.Message}");
                return null;
            }
        }

        #endregion

        #region Default Settings

        /// <summary>
        /// カスタマイズをデフォルト設定にリセットします
        /// </summary>
        public void ResetToDefault()
        {
            _currentCharacter = new CharacterCustomization();
            _currentMount = new MountCustomization();

            CharacterCustomizationChanged?.Invoke(_currentCharacter);
            MountCustomizationChanged?.Invoke(_currentMount);

            Debug.Log("[CustomizationService] Customization reset to default.");
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// プリセットのファイルパスを取得します
        /// </summary>
        /// <param name="presetName">プリセット名</param>
        /// <returns>ファイルパス</returns>
        private string GetPresetFilePath(string presetName)
        {
            return Path.Combine(_presetsPath, $"{presetName}{PRESET_EXTENSION}");
        }

        #endregion
    }
}
