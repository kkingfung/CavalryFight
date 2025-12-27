#nullable enable

using System;
using System.Collections.Generic;
using CavalryFight.Core.Services;
using UnityEngine;

namespace CavalryFight.Services.Customization
{
    /// <summary>
    /// カスタマイズサービスのインターフェース
    /// </summary>
    /// <remarks>
    /// キャラクターと騎乗動物のカスタマイズを管理します。
    /// プリセットの保存・読込、カスタマイズの適用を提供します。
    /// </remarks>
    public interface ICustomizationService : IService
    {
        #region Events

        /// <summary>
        /// キャラクターカスタマイズが変更された時に発生します
        /// </summary>
        event Action<CharacterCustomization>? CharacterCustomizationChanged;

        /// <summary>
        /// 騎乗動物カスタマイズが変更された時に発生します
        /// </summary>
        event Action<MountCustomization>? MountCustomizationChanged;

        /// <summary>
        /// プリセットが保存された時に発生します
        /// </summary>
        event Action<string>? PresetSaved;

        /// <summary>
        /// プリセットが読み込まれた時に発生します
        /// </summary>
        event Action<string>? PresetLoaded;

        /// <summary>
        /// プリセットが削除された時に発生します
        /// </summary>
        event Action<string>? PresetDeleted;

        #endregion

        #region Properties

        /// <summary>
        /// 現在のキャラクターカスタマイズを取得します
        /// </summary>
        CharacterCustomization CurrentCharacter { get; }

        /// <summary>
        /// 現在の騎乗動物カスタマイズを取得します
        /// </summary>
        MountCustomization CurrentMount { get; }

        #endregion

        #region Customization Settings

        /// <summary>
        /// キャラクターカスタマイズを設定します
        /// </summary>
        /// <param name="customization">設定するカスタマイズ</param>
        void SetCharacterCustomization(CharacterCustomization customization);

        /// <summary>
        /// 騎乗動物カスタマイズを設定します
        /// </summary>
        /// <param name="customization">設定するカスタマイズ</param>
        void SetMountCustomization(MountCustomization customization);

        /// <summary>
        /// キャラクターと騎乗動物のカスタマイズを同時に設定します
        /// </summary>
        /// <param name="character">キャラクターカスタマイズ</param>
        /// <param name="mount">騎乗動物カスタマイズ</param>
        void SetCustomization(CharacterCustomization character, MountCustomization mount);

        #endregion

        #region Apply Customization

        /// <summary>
        /// キャラクターにカスタマイズを適用します
        /// </summary>
        /// <param name="characterObject">適用先のキャラクターGameObject</param>
        /// <returns>適用に成功したかどうか</returns>
        bool ApplyCharacterCustomization(GameObject characterObject);

        /// <summary>
        /// 騎乗動物にカスタマイズを適用します
        /// </summary>
        /// <param name="mountObject">適用先の騎乗動物GameObject</param>
        /// <returns>適用に成功したかどうか</returns>
        bool ApplyMountCustomization(GameObject mountObject);

        /// <summary>
        /// キャラクターと騎乗動物の両方にカスタマイズを適用します
        /// </summary>
        /// <param name="characterObject">適用先のキャラクターGameObject</param>
        /// <param name="mountObject">適用先の騎乗動物GameObject</param>
        /// <returns>適用に成功したかどうか</returns>
        bool ApplyCustomization(GameObject characterObject, GameObject mountObject);

        #endregion

        #region Preset Management

        /// <summary>
        /// 現在のカスタマイズをプリセットとして保存します
        /// </summary>
        /// <param name="presetName">プリセット名</param>
        /// <returns>保存に成功したかどうか</returns>
        bool SavePreset(string presetName);

        /// <summary>
        /// プリセットを読み込んで現在のカスタマイズに設定します
        /// </summary>
        /// <param name="presetName">プリセット名</param>
        /// <returns>読み込みに成功したかどうか</returns>
        bool LoadPreset(string presetName);

        /// <summary>
        /// プリセットを削除します
        /// </summary>
        /// <param name="presetName">プリセット名</param>
        /// <returns>削除に成功したかどうか</returns>
        bool DeletePreset(string presetName);

        /// <summary>
        /// 保存されている全てのプリセット名を取得します
        /// </summary>
        /// <returns>プリセット名のリスト</returns>
        List<string> GetPresetNames();

        /// <summary>
        /// プリセットが存在するかどうかを確認します
        /// </summary>
        /// <param name="presetName">プリセット名</param>
        /// <returns>プリセットが存在する場合はtrue</returns>
        bool PresetExists(string presetName);

        /// <summary>
        /// プリセットのデータを取得します（現在のカスタマイズには設定しません）
        /// </summary>
        /// <param name="presetName">プリセット名</param>
        /// <returns>プリセットデータ（存在しない場合はnull）</returns>
        CustomizationPreset? GetPreset(string presetName);

        #endregion

        #region Default Settings

        /// <summary>
        /// カスタマイズをデフォルト設定にリセットします
        /// </summary>
        void ResetToDefault();

        #endregion

        #region Combat Idle Mode

        /// <summary>
        /// キャラクターを戦闘待機モードに切り替えます
        /// </summary>
        /// <param name="characterObject">対象のキャラクターGameObject</param>
        /// <param name="useCombatIdle">trueの場合は戦闘待機アニメーター、falseの場合は通常アニメーター</param>
        /// <returns>切り替えに成功したかどうか</returns>
        bool SetCharacterCombatIdleMode(GameObject characterObject, bool useCombatIdle);

        #endregion

        #region Applier Access

        /// <summary>
        /// P09CharacterApplierを取得します（データ設定用）
        /// </summary>
        /// <returns>P09CharacterApplier（設定されていない場合はnull）</returns>
        P09CharacterApplier? GetP09CharacterApplier();

        /// <summary>
        /// MalbersHorseApplierを取得します（データ設定用）
        /// </summary>
        /// <returns>MalbersHorseApplier（設定されていない場合はnull）</returns>
        MalbersHorseApplier? GetMalbersHorseApplier();

        #endregion
    }
}
