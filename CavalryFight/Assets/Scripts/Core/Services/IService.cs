#nullable enable

using System;

namespace CavalryFight.Core.Services
{
    /// <summary>
    /// サービスの基底インターフェース
    /// </summary>
    /// <remarks>
    /// すべてのサービスはこのインターフェースを実装してください。
    /// サービスは、ビジネスロジック、データアクセス、外部システムとの通信など、
    /// アプリケーションの機能を提供する責務を持ちます。
    /// </remarks>
    public interface IService : IDisposable
    {
        /// <summary>
        /// サービスを初期化します。
        /// </summary>
        /// <remarks>
        /// ServiceLocatorに登録された直後に呼び出されます。
        /// 依存関係の解決や初期設定を行ってください。
        /// </remarks>
        void Initialize();
    }

    /// <summary>
    /// 非同期初期化をサポートするサービスのインターフェース
    /// </summary>
    public interface IAsyncService : IService
    {
        /// <summary>
        /// サービスを非同期で初期化します。
        /// </summary>
        /// <returns>初期化処理を表すTask</returns>
        System.Threading.Tasks.Task InitializeAsync();
    }
}
