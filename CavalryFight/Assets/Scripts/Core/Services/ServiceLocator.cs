#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine;

namespace CavalryFight.Core.Services
{
    /// <summary>
    /// サービスロケーターパターンの実装
    /// アプリケーション全体でサービスを管理し、提供します。
    /// </summary>
    /// <remarks>
    /// シングルトンパターンで実装されており、
    /// どこからでもサービスにアクセスできます。
    /// サービスの登録と取得を一元管理します。
    /// </remarks>
    public class ServiceLocator
    {
        #region Singleton

        private static ServiceLocator? _instance;
        private static readonly object _lock = new object();

        /// <summary>
        /// ServiceLocatorのインスタンスを取得します。
        /// </summary>
        public static ServiceLocator Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new ServiceLocator();
                    }
                }
                return _instance;
            }
        }

        #endregion

        #region Fields

        private readonly Dictionary<Type, IService> _services = new Dictionary<Type, IService>();
        private readonly Dictionary<Type, Func<IService>> _factories = new Dictionary<Type, Func<IService>>();
        private bool _isInitialized;

        #endregion

        #region Constructor

        /// <summary>
        /// プライベートコンストラクタ（シングルトンパターン）
        /// </summary>
        private ServiceLocator()
        {
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// サービスを登録します。
        /// </summary>
        /// <typeparam name="TService">サービスのインターフェース型</typeparam>
        /// <param name="service">登録するサービスのインスタンス</param>
        /// <exception cref="ArgumentNullException">serviceがnullの場合</exception>
        /// <exception cref="InvalidOperationException">同じ型のサービスが既に登録されている場合</exception>
        public void Register<TService>(TService service) where TService : class, IService
        {
            if (service == null)
            {
                throw new ArgumentNullException(nameof(service));
            }

            var serviceType = typeof(TService);

            if (_services.ContainsKey(serviceType))
            {
                throw new InvalidOperationException($"Service of type {serviceType.Name} is already registered.");
            }

            _services[serviceType] = service;

            if (_isInitialized)
            {
                service.Initialize();
            }

            Debug.Log($"[ServiceLocator] Registered service: {serviceType.Name}");
        }

        /// <summary>
        /// サービスファクトリーを登録します（遅延初期化）
        /// </summary>
        /// <typeparam name="TService">サービスのインターフェース型</typeparam>
        /// <param name="factory">サービスを生成するファクトリー関数</param>
        /// <exception cref="ArgumentNullException">factoryがnullの場合</exception>
        public void RegisterFactory<TService>(Func<TService> factory) where TService : class, IService
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            var serviceType = typeof(TService);
            _factories[serviceType] = () => factory();

            Debug.Log($"[ServiceLocator] Registered factory for service: {serviceType.Name}");
        }

        /// <summary>
        /// サービスを取得します。
        /// </summary>
        /// <typeparam name="TService">取得するサービスのインターフェース型</typeparam>
        /// <returns>登録されているサービスのインスタンス</returns>
        /// <exception cref="InvalidOperationException">サービスが登録されていない場合</exception>
        public TService Get<TService>() where TService : class, IService
        {
            var serviceType = typeof(TService);

            // 既に生成されているサービスを返す
            if (_services.TryGetValue(serviceType, out var service))
            {
                return (TService)service;
            }

            // ファクトリーから生成
            if (_factories.TryGetValue(serviceType, out var factory))
            {
                var newService = (TService)factory();
                _services[serviceType] = newService;

                if (_isInitialized)
                {
                    newService.Initialize();
                }

                Debug.Log($"[ServiceLocator] Created service from factory: {serviceType.Name}");
                return newService;
            }

            throw new InvalidOperationException($"Service of type {serviceType.Name} is not registered.");
        }

        /// <summary>
        /// サービスを取得します（取得できない場合はnullを返します）
        /// </summary>
        /// <typeparam name="TService">取得するサービスのインターフェース型</typeparam>
        /// <returns>登録されているサービスのインスタンス、または null</returns>
        public TService? TryGet<TService>() where TService : class, IService
        {
            try
            {
                return Get<TService>();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// サービスが登録されているかどうかを確認します。
        /// </summary>
        /// <typeparam name="TService">確認するサービスのインターフェース型</typeparam>
        /// <returns>登録されている場合はtrue、それ以外はfalse</returns>
        public bool IsRegistered<TService>() where TService : class, IService
        {
            var serviceType = typeof(TService);
            return _services.ContainsKey(serviceType) || _factories.ContainsKey(serviceType);
        }

        /// <summary>
        /// サービスの登録を解除します。
        /// </summary>
        /// <typeparam name="TService">登録解除するサービスのインターフェース型</typeparam>
        public void Unregister<TService>() where TService : class, IService
        {
            var serviceType = typeof(TService);

            if (_services.TryGetValue(serviceType, out var service))
            {
                service?.Dispose();
                _services.Remove(serviceType);
                Debug.Log($"[ServiceLocator] Unregistered service: {serviceType.Name}");
            }

            _factories.Remove(serviceType);
        }

        /// <summary>
        /// すべてのサービスを初期化します。
        /// </summary>
        /// <remarks>
        /// アプリケーション起動時に一度だけ呼び出してください。
        /// </remarks>
        public void Initialize()
        {
            if (_isInitialized)
            {
                Debug.LogWarning("[ServiceLocator] Already initialized.");
                return;
            }

            Debug.Log("[ServiceLocator] Initializing all services...");

            foreach (var service in _services.Values)
            {
                try
                {
                    service.Initialize();
                    Debug.Log($"[ServiceLocator] Initialized: {service.GetType().Name}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[ServiceLocator] Failed to initialize {service.GetType().Name}: {ex.Message}");
                }
            }

            _isInitialized = true;
            Debug.Log("[ServiceLocator] All services initialized.");
        }

        /// <summary>
        /// すべてのサービスを非同期で初期化します。
        /// </summary>
        public async System.Threading.Tasks.Task InitializeAsync()
        {
            if (_isInitialized)
            {
                Debug.LogWarning("[ServiceLocator] Already initialized.");
                return;
            }

            Debug.Log("[ServiceLocator] Initializing all services asynchronously...");

            foreach (var service in _services.Values)
            {
                try
                {
                    if (service is IAsyncService asyncService)
                    {
                        await asyncService.InitializeAsync();
                        Debug.Log($"[ServiceLocator] Initialized (async): {service.GetType().Name}");
                    }
                    else
                    {
                        service.Initialize();
                        Debug.Log($"[ServiceLocator] Initialized: {service.GetType().Name}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[ServiceLocator] Failed to initialize {service.GetType().Name}: {ex.Message}");
                }
            }

            _isInitialized = true;
            Debug.Log("[ServiceLocator] All services initialized.");
        }

        /// <summary>
        /// すべてのサービスを破棄します。
        /// </summary>
        /// <remarks>
        /// アプリケーション終了時に呼び出してください。
        /// </remarks>
        public void Shutdown()
        {
            Debug.Log("[ServiceLocator] Shutting down all services...");

            foreach (var service in _services.Values)
            {
                try
                {
                    service?.Dispose();
                    Debug.Log($"[ServiceLocator] Disposed: {service?.GetType().Name}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[ServiceLocator] Failed to dispose {service?.GetType().Name}: {ex.Message}");
                }
            }

            _services.Clear();
            _factories.Clear();
            _isInitialized = false;

            Debug.Log("[ServiceLocator] Shutdown complete.");
        }

        /// <summary>
        /// ServiceLocatorをリセットします（テスト用）
        /// </summary>
        public static void Reset()
        {
            lock (_lock)
            {
                _instance?.Shutdown();
                _instance = null;
            }
        }

        #endregion
    }
}
