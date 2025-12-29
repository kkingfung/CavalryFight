#nullable enable

using System;
using CavalryFight.Core.Services;

namespace CavalryFight.Services.Performance
{
    /// <summary>
    /// パフォーマンス監視サービスのインターフェース
    /// </summary>
    /// <remarks>
    /// FPSなどのパフォーマンス指標を追跡します。
    /// </remarks>
    public interface IPerformanceMonitor : IService
    {
        #region Properties

        /// <summary>
        /// 現在のFPS（フレーム/秒）を取得します
        /// </summary>
        int CurrentFPS { get; }

        #endregion

        #region Events

        /// <summary>
        /// FPSが更新された時に発生します
        /// </summary>
        event Action<int>? FPSUpdated;

        #endregion
    }
}
