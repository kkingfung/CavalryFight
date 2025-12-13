#nullable enable

using System;
using System.IO;
using UnityEngine;

namespace CavalryFight.Services.Customization
{
    /// <summary>
    /// カスタマイズプリセット
    /// </summary>
    /// <remarks>
    /// キャラクターと騎乗動物のカスタマイズをセットで保存します。
    /// プリセットとして保存・読込が可能です。
    /// </remarks>
    [Serializable]
    public class CustomizationPreset
    {
        #region Fields

        /// <summary>
        /// プリセット名
        /// </summary>
        public string PresetName = "Default";

        /// <summary>
        /// キャラクターのカスタマイズ
        /// </summary>
        public CharacterCustomization Character = new CharacterCustomization();

        /// <summary>
        /// 騎乗動物のカスタマイズ
        /// </summary>
        public MountCustomization Mount = new MountCustomization();

        /// <summary>
        /// プリセット作成日時
        /// </summary>
        public string CreatedAt = string.Empty;

        #endregion

        #region Constructors

        /// <summary>
        /// CustomizationPresetの新しいインスタンスを初期化します
        /// </summary>
        public CustomizationPreset()
        {
            CreatedAt = DateTime.UtcNow.ToString("o");
        }

        /// <summary>
        /// CustomizationPresetの新しいインスタンスを初期化します
        /// </summary>
        /// <param name="presetName">プリセット名</param>
        public CustomizationPreset(string presetName)
        {
            PresetName = presetName;
            CreatedAt = DateTime.UtcNow.ToString("o");
        }

        /// <summary>
        /// CustomizationPresetの新しいインスタンスを初期化します
        /// </summary>
        /// <param name="presetName">プリセット名</param>
        /// <param name="character">キャラクターカスタマイズ</param>
        /// <param name="mount">騎乗動物カスタマイズ</param>
        public CustomizationPreset(string presetName, CharacterCustomization character, MountCustomization mount)
        {
            PresetName = presetName;
            Character = character;
            Mount = mount;
            CreatedAt = DateTime.UtcNow.ToString("o");
        }

        #endregion

        #region Methods

        /// <summary>
        /// このプリセットのコピーを作成します
        /// </summary>
        /// <returns>コピーされたプリセット</returns>
        public CustomizationPreset Clone()
        {
            return new CustomizationPreset
            {
                PresetName = this.PresetName,
                Character = this.Character.Clone(),
                Mount = this.Mount.Clone(),
                CreatedAt = this.CreatedAt
            };
        }

        /// <summary>
        /// JSONに変換します
        /// </summary>
        /// <returns>JSON文字列</returns>
        public string ToJson()
        {
            return JsonUtility.ToJson(this, true);
        }

        /// <summary>
        /// JSONから読み込みます
        /// </summary>
        /// <param name="json">JSON文字列</param>
        /// <returns>プリセットデータ</returns>
        public static CustomizationPreset? FromJson(string json)
        {
            try
            {
                return JsonUtility.FromJson<CustomizationPreset>(json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CustomizationPreset] Failed to parse JSON: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// ファイルに保存します
        /// </summary>
        /// <param name="filePath">ファイルパス</param>
        /// <returns>保存に成功したかどうか</returns>
        public bool SaveToFile(string filePath)
        {
            try
            {
                string? directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string json = ToJson();
                File.WriteAllText(filePath, json);

                Debug.Log($"[CustomizationPreset] Preset saved to: {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CustomizationPreset] Failed to save preset: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// ファイルから読み込みます
        /// </summary>
        /// <param name="filePath">ファイルパス</param>
        /// <returns>プリセットデータ（失敗時はnull）</returns>
        public static CustomizationPreset? LoadFromFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    Debug.LogWarning($"[CustomizationPreset] File not found: {filePath}");
                    return null;
                }

                string json = File.ReadAllText(filePath);
                return FromJson(json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CustomizationPreset] Failed to load preset: {ex.Message}");
                return null;
            }
        }

        #endregion
    }
}
