#nullable enable

using System;
using NUnit.Framework;
using CavalryFight.Core.Commands;

namespace CavalryFight.Tests.EditMode.Commands
{
    /// <summary>
    /// RelayCommandのユニットテスト
    /// </summary>
    [TestFixture]
    public class RelayCommandTests
    {
        #region コンストラクタ テスト

        [Test]
        public void Constructor_WithNullExecute_ShouldThrowArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new RelayCommand((Action<object?>)null!));
        }

        [Test]
        public void Constructor_WithValidExecute_ShouldSucceed()
        {
            // Arrange & Act & Assert
            Assert.DoesNotThrow(() =>
                new RelayCommand(_ => { }));
        }

        [Test]
        public void Constructor_ParameterlessWithNullExecute_ShouldThrowArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new RelayCommand((Action)null!));
        }

        [Test]
        public void Constructor_ParameterlessWithValidExecute_ShouldSucceed()
        {
            // Arrange & Act & Assert
            Assert.DoesNotThrow(() =>
                new RelayCommand(() => { }));
        }

        #endregion

        #region CanExecute テスト

        [Test]
        public void CanExecute_WithoutCanExecuteFunc_ShouldReturnTrue()
        {
            // Arrange
            var command = new RelayCommand(_ => { });

            // Act
            var result = command.CanExecute(null);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void CanExecute_WithCanExecuteFuncReturningTrue_ShouldReturnTrue()
        {
            // Arrange
            var command = new RelayCommand(_ => { }, _ => true);

            // Act
            var result = command.CanExecute(null);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void CanExecute_WithCanExecuteFuncReturningFalse_ShouldReturnFalse()
        {
            // Arrange
            var command = new RelayCommand(_ => { }, _ => false);

            // Act
            var result = command.CanExecute(null);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void CanExecute_ShouldPassParameterToCanExecuteFunc()
        {
            // Arrange
            object? receivedParameter = null;
            var command = new RelayCommand(_ => { }, p =>
            {
                receivedParameter = p;
                return true;
            });
            var parameter = new object();

            // Act
            command.CanExecute(parameter);

            // Assert
            Assert.That(receivedParameter, Is.SameAs(parameter));
        }

        [Test]
        public void CanExecute_Parameterless_WithoutCanExecuteFunc_ShouldReturnTrue()
        {
            // Arrange
            var command = new RelayCommand(() => { });

            // Act
            var result = command.CanExecute(null);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void CanExecute_Parameterless_WithCanExecuteFuncReturningFalse_ShouldReturnFalse()
        {
            // Arrange
            var command = new RelayCommand(() => { }, () => false);

            // Act
            var result = command.CanExecute(null);

            // Assert
            Assert.That(result, Is.False);
        }

        #endregion

        #region Execute テスト

        [Test]
        public void Execute_WhenCanExecuteTrue_ShouldInvokeExecuteAction()
        {
            // Arrange
            var wasExecuted = false;
            var command = new RelayCommand(_ => wasExecuted = true);

            // Act
            command.Execute(null);

            // Assert
            Assert.That(wasExecuted, Is.True);
        }

        [Test]
        public void Execute_WhenCanExecuteFalse_ShouldNotInvokeExecuteAction()
        {
            // Arrange
            var wasExecuted = false;
            var command = new RelayCommand(_ => wasExecuted = true, _ => false);

            // Act
            command.Execute(null);

            // Assert
            Assert.That(wasExecuted, Is.False);
        }

        [Test]
        public void Execute_ShouldPassParameterToExecuteAction()
        {
            // Arrange
            object? receivedParameter = null;
            var command = new RelayCommand(p => receivedParameter = p);
            var parameter = new object();

            // Act
            command.Execute(parameter);

            // Assert
            Assert.That(receivedParameter, Is.SameAs(parameter));
        }

        [Test]
        public void Execute_Parameterless_ShouldInvokeExecuteAction()
        {
            // Arrange
            var wasExecuted = false;
            var command = new RelayCommand(() => wasExecuted = true);

            // Act
            command.Execute(null);

            // Assert
            Assert.That(wasExecuted, Is.True);
        }

        [Test]
        public void Execute_Parameterless_WhenCanExecuteFalse_ShouldNotInvokeExecuteAction()
        {
            // Arrange
            var wasExecuted = false;
            var command = new RelayCommand(() => wasExecuted = true, () => false);

            // Act
            command.Execute(null);

            // Assert
            Assert.That(wasExecuted, Is.False);
        }

        #endregion

        #region RaiseCanExecuteChanged テスト

        [Test]
        public void RaiseCanExecuteChanged_ShouldRaiseCanExecuteChangedEvent()
        {
            // Arrange
            var command = new RelayCommand(_ => { });
            var wasRaised = false;
            command.CanExecuteChanged += (s, e) => wasRaised = true;

            // Act
            command.RaiseCanExecuteChanged();

            // Assert
            Assert.That(wasRaised, Is.True);
        }

        [Test]
        public void RaiseCanExecuteChanged_WithNoSubscribers_ShouldNotThrow()
        {
            // Arrange
            var command = new RelayCommand(_ => { });

            // Act & Assert
            Assert.DoesNotThrow(() => command.RaiseCanExecuteChanged());
        }

        [Test]
        public void RaiseCanExecuteChanged_ShouldPassCorrectSender()
        {
            // Arrange
            var command = new RelayCommand(_ => { });
            object? receivedSender = null;
            command.CanExecuteChanged += (s, e) => receivedSender = s;

            // Act
            command.RaiseCanExecuteChanged();

            // Assert
            Assert.That(receivedSender, Is.SameAs(command));
        }

        #endregion
    }

    /// <summary>
    /// RelayCommand&lt;T&gt;のユニットテスト
    /// </summary>
    [TestFixture]
    public class RelayCommandGenericTests
    {
        #region コンストラクタ テスト

        [Test]
        public void Constructor_WithNullExecute_ShouldThrowArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new RelayCommand<string>(null!));
        }

        [Test]
        public void Constructor_WithValidExecute_ShouldSucceed()
        {
            // Arrange & Act & Assert
            Assert.DoesNotThrow(() =>
                new RelayCommand<string>(_ => { }));
        }

        #endregion

        #region CanExecute テスト

        [Test]
        public void CanExecute_WithoutCanExecuteFunc_ShouldReturnTrue()
        {
            // Arrange
            var command = new RelayCommand<string>(_ => { });

            // Act
            var result = command.CanExecute("test");

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void CanExecute_WithCanExecuteFuncReturningTrue_ShouldReturnTrue()
        {
            // Arrange
            var command = new RelayCommand<string>(_ => { }, _ => true);

            // Act
            var result = command.CanExecute("test");

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void CanExecute_WithCanExecuteFuncReturningFalse_ShouldReturnFalse()
        {
            // Arrange
            var command = new RelayCommand<string>(_ => { }, _ => false);

            // Act
            var result = command.CanExecute("test");

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void CanExecute_WithNullForValueType_ShouldReturnFalse()
        {
            // Arrange
            var command = new RelayCommand<int>(_ => { });

            // Act
            var result = command.CanExecute(null);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void CanExecute_WithNullForNullableValueType_ShouldReturnTrue()
        {
            // Arrange
            var command = new RelayCommand<int?>(_ => { });

            // Act
            var result = command.CanExecute(null);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void CanExecute_WithNullForReferenceType_ShouldReturnTrue()
        {
            // Arrange
            var command = new RelayCommand<string>(_ => { });

            // Act
            var result = command.CanExecute(null);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void CanExecute_ShouldPassTypedParameterToCanExecuteFunc()
        {
            // Arrange
            string? receivedParameter = null;
            var command = new RelayCommand<string>(_ => { }, p =>
            {
                receivedParameter = p;
                return true;
            });

            // Act
            command.CanExecute("TestValue");

            // Assert
            Assert.That(receivedParameter, Is.EqualTo("TestValue"));
        }

        #endregion

        #region Execute テスト

        [Test]
        public void Execute_WhenCanExecuteTrue_ShouldInvokeExecuteAction()
        {
            // Arrange
            var wasExecuted = false;
            var command = new RelayCommand<string>(_ => wasExecuted = true);

            // Act
            command.Execute("test");

            // Assert
            Assert.That(wasExecuted, Is.True);
        }

        [Test]
        public void Execute_WhenCanExecuteFalse_ShouldNotInvokeExecuteAction()
        {
            // Arrange
            var wasExecuted = false;
            var command = new RelayCommand<string>(_ => wasExecuted = true, _ => false);

            // Act
            command.Execute("test");

            // Assert
            Assert.That(wasExecuted, Is.False);
        }

        [Test]
        public void Execute_ShouldPassTypedParameterToExecuteAction()
        {
            // Arrange
            string? receivedParameter = null;
            var command = new RelayCommand<string>(p => receivedParameter = p);

            // Act
            command.Execute("TestValue");

            // Assert
            Assert.That(receivedParameter, Is.EqualTo("TestValue"));
        }

        [Test]
        public void Execute_WithIntParameter_ShouldPassCorrectValue()
        {
            // Arrange
            int receivedValue = 0;
            var command = new RelayCommand<int>(p => receivedValue = p);

            // Act
            command.Execute(42);

            // Assert
            Assert.That(receivedValue, Is.EqualTo(42));
        }

        [Test]
        public void Execute_WithComplexObject_ShouldPassCorrectObject()
        {
            // Arrange
            TestData? receivedData = null;
            var command = new RelayCommand<TestData>(p => receivedData = p);
            var testData = new TestData { Id = 1, Name = "Test" };

            // Act
            command.Execute(testData);

            // Assert
            Assert.That(receivedData, Is.SameAs(testData));
            Assert.That(receivedData?.Id, Is.EqualTo(1));
            Assert.That(receivedData?.Name, Is.EqualTo("Test"));
        }

        private class TestData
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }

        #endregion

        #region RaiseCanExecuteChanged テスト

        [Test]
        public void RaiseCanExecuteChanged_ShouldRaiseCanExecuteChangedEvent()
        {
            // Arrange
            var command = new RelayCommand<string>(_ => { });
            var wasRaised = false;
            command.CanExecuteChanged += (s, e) => wasRaised = true;

            // Act
            command.RaiseCanExecuteChanged();

            // Assert
            Assert.That(wasRaised, Is.True);
        }

        [Test]
        public void RaiseCanExecuteChanged_WithNoSubscribers_ShouldNotThrow()
        {
            // Arrange
            var command = new RelayCommand<string>(_ => { });

            // Act & Assert
            Assert.DoesNotThrow(() => command.RaiseCanExecuteChanged());
        }

        #endregion

        #region 状態変更シナリオ テスト

        [Test]
        public void CanExecute_ShouldReflectStateChanges()
        {
            // Arrange
            var isEnabled = false;
            var command = new RelayCommand<string>(_ => { }, _ => isEnabled);

            // Act & Assert - 初期状態
            Assert.That(command.CanExecute("test"), Is.False);

            // Act & Assert - 状態変更後
            isEnabled = true;
            Assert.That(command.CanExecute("test"), Is.True);
        }

        [Test]
        public void Execute_ShouldRespectDynamicCanExecute()
        {
            // Arrange
            var executionCount = 0;
            var isEnabled = true;
            var command = new RelayCommand<string>(_ => executionCount++, _ => isEnabled);

            // Act - 有効時に実行
            command.Execute("test");
            Assert.That(executionCount, Is.EqualTo(1));

            // Act - 無効化して実行
            isEnabled = false;
            command.Execute("test");

            // Assert - 無効時は実行されない
            Assert.That(executionCount, Is.EqualTo(1));
        }

        #endregion
    }
}
