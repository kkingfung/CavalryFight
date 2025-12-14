#nullable enable

using UnityEngine;

namespace CavalryFight.Services.Customization
{
    /// <summary>
    /// 騎乗動物カスタマイズ適用のインターフェース
    /// </summary>
    /// <remarks>
    /// Malbers Horse AnimSet Proアセットの騎乗動物GameObjectに
    /// カスタマイズを適用するための機能を定義します。
    /// </remarks>
    public interface IMountApplier
    {
        /// <summary>
        /// 騎乗動物にカスタマイズを適用します
        /// </summary>
        /// <param name="mountObject">適用先の騎乗動物GameObject</param>
        /// <param name="customization">適用するカスタマイズデータ</param>
        /// <returns>適用に成功したかどうか</returns>
        bool Apply(GameObject mountObject, MountCustomization customization);

        /// <summary>
        /// 指定されたGameObjectがこのApplierで処理可能かどうかを確認します
        /// </summary>
        /// <param name="mountObject">確認するGameObject</param>
        /// <returns>処理可能な場合はtrue</returns>
        bool CanApply(GameObject mountObject);
    }
}
