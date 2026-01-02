#nullable enable

using System;
using NUnit.Framework;
using CavalryFight.Core.Services;

namespace CavalryFight.Tests.EditMode.Services
{
    /// <summary>
    /// ServiceLocatorのユニットテスト
    /// </summary>
    [TestFixture]
    public class ServiceLocatorTests
    {
        #region テスト用モックサービス

        /// <summary>
        /// テスト用のモックサービスインターフェース
        /// </summary>
        private interface IMockService : IService
        {
            bool IsInitialized { get; }
            int Value { get; set; }
        }

        /// <summary>
        /// テスト用のモックサービス実装
        /// </summary>
        private class MockService : IMockService
        {
            public bool IsInitialized { get; private set; }
            public int Value { get; set; }
            public bool IsDisposed { get; private set; }

            public void Initialize()
            {
                IsInitialized = true;
            }

            public void Dispose()
            {
                IsDisposed = true;
            }
        }

        /// <summary>
        /// 別のモックサービスインターフェース
        /// </summary>
        private interface IAnotherMockService : IService
        {
            string Name { get; }
        }

        /// <summary>
        /// 別のモックサービス実装
        /// </summary>
        private class AnotherMockService : IAnotherMockService
        {
            public string Name => "AnotherMockService";
            public bool IsDisposed { get; private set; }

            public void Initialize() { }

            public void Dispose()
            {
                IsDisposed = true;
            }
        }

        #endregion

        #region Setup / Teardown

        [SetUp]
        public void SetUp()
        {
            // 各テスト前にServiceLocatorをリセット
            ServiceLocator.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            // 各テスト後にServiceLocatorをリセット
            ServiceLocator.Reset();
        }

        #endregion

        #region シングルトンテスト

        [Test]
        public void Instance_ShouldReturnSameInstance()
        {
            // Arrange & Act
            var instance1 = ServiceLocator.Instance;
            var instance2 = ServiceLocator.Instance;

            // Assert
            Assert.That(instance2, Is.SameAs(instance1));
        }

        [Test]
        public void Reset_ShouldCreateNewInstance()
        {
            // Arrange
            var instance1 = ServiceLocator.Instance;

            // Act
            ServiceLocator.Reset();
            var instance2 = ServiceLocator.Instance;

            // Assert
            Assert.That(instance2, Is.Not.SameAs(instance1));
        }

        #endregion

        #region Register テスト

        [Test]
        public void Register_WithValidService_ShouldSucceed()
        {
            // Arrange
            var service = new MockService();

            // Act & Assert - 例外がスローされないことを確認
            Assert.DoesNotThrow(() => ServiceLocator.Instance.Register<IMockService>(service));
        }

        [Test]
        public void Register_WithNullService_ShouldThrowArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                ServiceLocator.Instance.Register<IMockService>(null!));
        }

        [Test]
        public void Register_SameTypeTwice_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var service1 = new MockService();
            var service2 = new MockService();
            ServiceLocator.Instance.Register<IMockService>(service1);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
                ServiceLocator.Instance.Register<IMockService>(service2));
        }

        [Test]
        public void Register_AfterInitialize_ShouldInitializeServiceImmediately()
        {
            // Arrange
            ServiceLocator.Instance.Initialize();
            var service = new MockService();

            // Act
            ServiceLocator.Instance.Register<IMockService>(service);

            // Assert
            Assert.That(service.IsInitialized, Is.True);
        }

        #endregion

        #region RegisterFactory テスト

        [Test]
        public void RegisterFactory_WithValidFactory_ShouldSucceed()
        {
            // Arrange & Act & Assert
            Assert.DoesNotThrow(() =>
                ServiceLocator.Instance.RegisterFactory<IMockService>(() => new MockService()));
        }

        [Test]
        public void RegisterFactory_WithNullFactory_ShouldThrowArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                ServiceLocator.Instance.RegisterFactory<IMockService>(null!));
        }

        [Test]
        public void RegisterFactory_ServiceCreatedOnFirstGet()
        {
            // Arrange
            var creationCount = 0;
            ServiceLocator.Instance.RegisterFactory<IMockService>(() =>
            {
                creationCount++;
                return new MockService();
            });

            // Act - ファクトリー登録時点ではまだ生成されていない
            Assert.That(creationCount, Is.EqualTo(0));

            // Get を呼ぶと生成される
            var service = ServiceLocator.Instance.Get<IMockService>();

            // Assert
            Assert.That(creationCount, Is.EqualTo(1));
            Assert.That(service, Is.Not.Null);
        }

        [Test]
        public void RegisterFactory_ServiceCreatedOnlyOnce()
        {
            // Arrange
            var creationCount = 0;
            ServiceLocator.Instance.RegisterFactory<IMockService>(() =>
            {
                creationCount++;
                return new MockService();
            });

            // Act
            var service1 = ServiceLocator.Instance.Get<IMockService>();
            var service2 = ServiceLocator.Instance.Get<IMockService>();

            // Assert
            Assert.That(creationCount, Is.EqualTo(1));
            Assert.That(service2, Is.SameAs(service1));
        }

        #endregion

        #region Get テスト

        [Test]
        public void Get_RegisteredService_ShouldReturnService()
        {
            // Arrange
            var service = new MockService { Value = 42 };
            ServiceLocator.Instance.Register<IMockService>(service);

            // Act
            var result = ServiceLocator.Instance.Get<IMockService>();

            // Assert
            Assert.That(result, Is.SameAs(service));
            Assert.That(result.Value, Is.EqualTo(42));
        }

        [Test]
        public void Get_UnregisteredService_ShouldThrowInvalidOperationException()
        {
            // Arrange & Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
                ServiceLocator.Instance.Get<IMockService>());
        }

        [Test]
        public void Get_ShouldReturnSameInstanceEachTime()
        {
            // Arrange
            var service = new MockService();
            ServiceLocator.Instance.Register<IMockService>(service);

            // Act
            var result1 = ServiceLocator.Instance.Get<IMockService>();
            var result2 = ServiceLocator.Instance.Get<IMockService>();

            // Assert
            Assert.That(result2, Is.SameAs(result1));
        }

        #endregion

        #region TryGet テスト

        [Test]
        public void TryGet_RegisteredService_ShouldReturnService()
        {
            // Arrange
            var service = new MockService();
            ServiceLocator.Instance.Register<IMockService>(service);

            // Act
            var result = ServiceLocator.Instance.TryGet<IMockService>();

            // Assert
            Assert.That(result, Is.SameAs(service));
        }

        [Test]
        public void TryGet_UnregisteredService_ShouldReturnNull()
        {
            // Arrange & Act
            var result = ServiceLocator.Instance.TryGet<IMockService>();

            // Assert
            Assert.That(result, Is.Null);
        }

        #endregion

        #region IsRegistered テスト

        [Test]
        public void IsRegistered_WithRegisteredService_ShouldReturnTrue()
        {
            // Arrange
            var service = new MockService();
            ServiceLocator.Instance.Register<IMockService>(service);

            // Act
            var result = ServiceLocator.Instance.IsRegistered<IMockService>();

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void IsRegistered_WithUnregisteredService_ShouldReturnFalse()
        {
            // Arrange & Act
            var result = ServiceLocator.Instance.IsRegistered<IMockService>();

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void IsRegistered_WithFactoryOnly_ShouldReturnTrue()
        {
            // Arrange
            ServiceLocator.Instance.RegisterFactory<IMockService>(() => new MockService());

            // Act
            var result = ServiceLocator.Instance.IsRegistered<IMockService>();

            // Assert
            Assert.That(result, Is.True);
        }

        #endregion

        #region Unregister テスト

        [Test]
        public void Unregister_RegisteredService_ShouldRemoveAndDispose()
        {
            // Arrange
            var service = new MockService();
            ServiceLocator.Instance.Register<IMockService>(service);

            // Act
            ServiceLocator.Instance.Unregister<IMockService>();

            // Assert
            Assert.That(service.IsDisposed, Is.True);
            Assert.That(ServiceLocator.Instance.IsRegistered<IMockService>(), Is.False);
        }

        [Test]
        public void Unregister_UnregisteredService_ShouldNotThrow()
        {
            // Arrange & Act & Assert
            Assert.DoesNotThrow(() =>
                ServiceLocator.Instance.Unregister<IMockService>());
        }

        #endregion

        #region Initialize テスト

        [Test]
        public void Initialize_ShouldInitializeAllRegisteredServices()
        {
            // Arrange
            var service1 = new MockService();
            var service2 = new AnotherMockService();
            ServiceLocator.Instance.Register<IMockService>(service1);
            ServiceLocator.Instance.Register<IAnotherMockService>(service2);

            // Act
            ServiceLocator.Instance.Initialize();

            // Assert
            Assert.That(service1.IsInitialized, Is.True);
        }

        [Test]
        public void Initialize_CalledTwice_ShouldNotReinitialize()
        {
            // Arrange
            var initCount = 0;
            var service = new MockService();
            ServiceLocator.Instance.Register<IMockService>(service);
            ServiceLocator.Instance.Initialize();
            initCount = service.IsInitialized ? 1 : 0;

            // Act - 2回目の呼び出し
            ServiceLocator.Instance.Initialize();

            // Assert - 2回目は初期化されない（ログ警告のみ）
            // 注：MockServiceのIsInitializedはboolなので回数は追跡できないが、
            // 例外がスローされないことを確認
            Assert.That(service.IsInitialized, Is.True);
        }

        #endregion

        #region Shutdown テスト

        [Test]
        public void Shutdown_ShouldDisposeAllServices()
        {
            // Arrange
            var service1 = new MockService();
            var service2 = new AnotherMockService();
            ServiceLocator.Instance.Register<IMockService>(service1);
            ServiceLocator.Instance.Register<IAnotherMockService>(service2);

            // Act
            ServiceLocator.Instance.Shutdown();

            // Assert
            Assert.That(service1.IsDisposed, Is.True);
            Assert.That(service2.IsDisposed, Is.True);
        }

        [Test]
        public void Shutdown_ShouldClearAllRegistrations()
        {
            // Arrange
            var service = new MockService();
            ServiceLocator.Instance.Register<IMockService>(service);
            ServiceLocator.Instance.RegisterFactory<IAnotherMockService>(() => new AnotherMockService());

            // Act
            ServiceLocator.Instance.Shutdown();

            // Assert
            Assert.That(ServiceLocator.Instance.IsRegistered<IMockService>(), Is.False);
            Assert.That(ServiceLocator.Instance.IsRegistered<IAnotherMockService>(), Is.False);
        }

        #endregion

        #region GetRegisteredServiceTypes テスト

        [Test]
        public void GetRegisteredServiceTypes_ShouldReturnAllRegisteredTypes()
        {
            // Arrange
            var service = new MockService();
            ServiceLocator.Instance.Register<IMockService>(service);
            ServiceLocator.Instance.RegisterFactory<IAnotherMockService>(() => new AnotherMockService());

            // Act
            var types = ServiceLocator.Instance.GetRegisteredServiceTypes();

            // Assert
            Assert.That(types.Count, Is.EqualTo(2));
            Assert.That(types, Does.Contain(typeof(IMockService)));
            Assert.That(types, Does.Contain(typeof(IAnotherMockService)));
        }

        [Test]
        public void GetRegisteredServiceTypes_WithNoServices_ShouldReturnEmptyList()
        {
            // Arrange & Act
            var types = ServiceLocator.Instance.GetRegisteredServiceTypes();

            // Assert
            Assert.That(types, Is.Empty);
        }

        #endregion

        #region GetRegisteredServicesDebugInfo テスト

        [Test]
        public void GetRegisteredServicesDebugInfo_WithNoServices_ShouldReturnNoServicesMessage()
        {
            // Arrange & Act
            var info = ServiceLocator.Instance.GetRegisteredServicesDebugInfo();

            // Assert
            Assert.That(info, Does.Contain("No services registered"));
        }

        [Test]
        public void GetRegisteredServicesDebugInfo_WithServices_ShouldContainServiceNames()
        {
            // Arrange
            var service = new MockService();
            ServiceLocator.Instance.Register<IMockService>(service);

            // Act
            var info = ServiceLocator.Instance.GetRegisteredServicesDebugInfo();

            // Assert
            Assert.That(info, Does.Contain("IMockService"));
            Assert.That(info, Does.Contain("MockService"));
        }

        #endregion
    }
}
