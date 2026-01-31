#nullable enable

using System.Collections.Generic;
using CavalryFight.Core.Services;
using UnityEngine;

namespace CavalryFight.Services.Combat
{
    /// <summary>
    /// シーン内の矢を追跡するサービスのインターフェース
    /// </summary>
    /// <remarks>
    /// FindObjectsOfTypeを避けるため、矢が自身を登録/解除します。
    /// AIの回避判定などに使用します。
    /// </remarks>
    public interface IArrowTrackerService : IService
    {
        /// <summary>
        /// アクティブな矢のリスト（読み取り専用）
        /// </summary>
        IReadOnlyList<Transform> ActiveArrows { get; }

        /// <summary>
        /// 矢を登録します
        /// </summary>
        void RegisterArrow(Transform arrow);

        /// <summary>
        /// 矢を解除します
        /// </summary>
        void UnregisterArrow(Transform arrow);

        /// <summary>
        /// 指定位置に最も近い矢を取得します
        /// </summary>
        /// <param name="position">基準位置</param>
        /// <param name="maxDistance">最大距離</param>
        /// <param name="nearestArrow">最も近い矢（見つからない場合はnull）</param>
        /// <param name="distance">距離</param>
        /// <returns>矢が見つかった場合true</returns>
        bool TryGetNearestArrow(Vector3 position, float maxDistance, out Transform? nearestArrow, out float distance);
    }
}
