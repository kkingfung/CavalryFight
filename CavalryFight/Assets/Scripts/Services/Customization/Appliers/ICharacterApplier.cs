#nullable enable

using UnityEngine;

namespace CavalryFight.Services.Customization
{
    /// <summary>
    /// キャラクターカスタマイズ適用のインターフェース
    /// </summary>
    /// <remarks>
    /// P09 Modular HumanoidアセットのキャラクターGameObjectに
    /// カスタマイズを適用するための機能を定義します。
    /// </remarks>
    public interface ICharacterApplier
    {
        /// <summary>
        /// キャラクターにカスタマイズを適用します
        /// </summary>
        /// <param name="characterObject">適用先のキャラクターGameObject</param>
        /// <param name="customization">適用するカスタマイズデータ</param>
        /// <returns>適用に成功したかどうか</returns>
        bool Apply(GameObject characterObject, CharacterCustomization customization);

        /// <summary>
        /// 指定されたGameObjectがこのApplierで処理可能かどうかを確認します
        /// </summary>
        /// <param name="characterObject">確認するGameObject</param>
        /// <returns>処理可能な場合はtrue</returns>
        bool CanApply(GameObject characterObject);
    }
}
