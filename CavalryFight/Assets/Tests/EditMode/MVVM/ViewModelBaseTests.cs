#nullable enable

using System;
using System.ComponentModel;
using System.Collections.Generic;
using NUnit.Framework;
using CavalryFight.Core.MVVM;

namespace CavalryFight.Tests.EditMode.MVVM
{
    /// <summary>
    /// ViewModelBaseのユニットテスト
    /// </summary>
    [TestFixture]
    public class ViewModelBaseTests
    {
        #region テスト用モックViewModel

        /// <summary>
        /// テスト用のモックViewModel
        /// </summary>
        private class MockViewModel : ViewModelBase
        {
            private string _name = string.Empty;
            private int _value;
            private bool _isActive;

            public string Name
            {
                get => _name;
                set => SetProperty(ref _name, value);
            }

            public int Value
            {
                get => _value;
                set => SetProperty(ref _value, value);
            }

            public bool IsActive
            {
                get => _isActive;
                set => SetProperty(ref _isActive, value);
            }

            // コールバック付きSetPropertyのテスト用
            private int _valueWithCallback;
            public bool CallbackWasCalled { get; set; }

            public int ValueWithCallback
            {
                get => _valueWithCallback;
                set => SetProperty(ref _valueWithCallback, value, () => CallbackWasCalled = true);
            }

            public void ResetCallbackFlag()
            {
                CallbackWasCalled = false;
            }

            // OnPropertiesChangedのテスト用
            public void NotifyMultipleProperties()
            {
                OnPropertiesChanged(nameof(Name), nameof(Value), nameof(IsActive));
            }

            // OnPropertyChangedの直接呼び出しテスト用
            public void NotifyPropertyChanged(string propertyName)
            {
                OnPropertyChanged(propertyName);
            }

            // Disposeの確認用
            public bool OnDisposeWasCalled { get; private set; }

            protected override void OnDispose()
            {
                OnDisposeWasCalled = true;
                base.OnDispose();
            }

            // IsDisposedのアクセス用
            public bool CheckIsDisposed() => IsDisposed;
        }

        #endregion

        #region フィールド

        private MockViewModel _viewModel = null!;
        private List<string> _changedProperties = null!;

        #endregion

        #region Setup / Teardown

        [SetUp]
        public void SetUp()
        {
            _viewModel = new MockViewModel();
            _changedProperties = new List<string>();
            _viewModel.PropertyChanged += OnPropertyChanged;
        }

        [TearDown]
        public void TearDown()
        {
            _viewModel.PropertyChanged -= OnPropertyChanged;
            _viewModel.Dispose();
        }

        private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != null)
            {
                _changedProperties.Add(e.PropertyName);
            }
        }

        #endregion

        #region SetProperty テスト

        [Test]
        public void SetProperty_WithDifferentValue_ShouldRaisePropertyChanged()
        {
            // Arrange & Act
            _viewModel.Name = "TestName";

            // Assert
            Assert.That(_changedProperties, Does.Contain(nameof(MockViewModel.Name)));
        }

        [Test]
        public void SetProperty_WithSameValue_ShouldNotRaisePropertyChanged()
        {
            // Arrange
            _viewModel.Name = "TestName";
            _changedProperties.Clear();

            // Act
            _viewModel.Name = "TestName";

            // Assert
            Assert.That(_changedProperties, Is.Empty);
        }

        [Test]
        public void SetProperty_WithDifferentValue_ShouldReturnTrue()
        {
            // Arrange
            var initialValue = _viewModel.Value;

            // Act
            _viewModel.Value = initialValue + 1;

            // Assert - プロパティが変更されたので PropertyChanged が発火するはず
            Assert.That(_changedProperties, Does.Contain(nameof(MockViewModel.Value)));
        }

        [Test]
        public void SetProperty_WithSameValue_ShouldReturnFalse()
        {
            // Arrange
            _viewModel.Value = 42;
            _changedProperties.Clear();

            // Act
            _viewModel.Value = 42;

            // Assert
            Assert.That(_changedProperties, Does.Not.Contain(nameof(MockViewModel.Value)));
        }

        [Test]
        public void SetProperty_WithIntValue_ShouldUpdateValue()
        {
            // Arrange & Act
            _viewModel.Value = 100;

            // Assert
            Assert.That(_viewModel.Value, Is.EqualTo(100));
        }

        [Test]
        public void SetProperty_WithBoolValue_ShouldUpdateValue()
        {
            // Arrange & Act
            _viewModel.IsActive = true;

            // Assert
            Assert.That(_viewModel.IsActive, Is.True);
            Assert.That(_changedProperties, Does.Contain(nameof(MockViewModel.IsActive)));
        }

        [Test]
        public void SetProperty_WithStringValue_ShouldUpdateValue()
        {
            // Arrange & Act
            _viewModel.Name = "NewName";

            // Assert
            Assert.That(_viewModel.Name, Is.EqualTo("NewName"));
        }

        [Test]
        public void SetProperty_WithNullString_ShouldUpdateValue()
        {
            // Arrange
            _viewModel.Name = "Initial";
            _changedProperties.Clear();

            // Act
            _viewModel.Name = null!;

            // Assert
            Assert.That(_viewModel.Name, Is.Null);
            Assert.That(_changedProperties, Does.Contain(nameof(MockViewModel.Name)));
        }

        #endregion

        #region SetProperty with Callback テスト

        [Test]
        public void SetProperty_WithCallback_WhenValueChanges_ShouldInvokeCallback()
        {
            // Arrange & Act
            _viewModel.ValueWithCallback = 42;

            // Assert
            Assert.That(_viewModel.CallbackWasCalled, Is.True);
        }

        [Test]
        public void SetProperty_WithCallback_WhenValueSame_ShouldNotInvokeCallback()
        {
            // Arrange
            _viewModel.ValueWithCallback = 42;
            _viewModel.ResetCallbackFlag(); // リセット

            // Act
            _viewModel.ValueWithCallback = 42;

            // Assert
            Assert.That(_viewModel.CallbackWasCalled, Is.False);
        }

        #endregion

        #region OnPropertiesChanged テスト

        [Test]
        public void OnPropertiesChanged_ShouldRaiseMultiplePropertyChangedEvents()
        {
            // Arrange & Act
            _viewModel.NotifyMultipleProperties();

            // Assert
            Assert.That(_changedProperties.Count, Is.EqualTo(3));
            Assert.That(_changedProperties, Does.Contain(nameof(MockViewModel.Name)));
            Assert.That(_changedProperties, Does.Contain(nameof(MockViewModel.Value)));
            Assert.That(_changedProperties, Does.Contain(nameof(MockViewModel.IsActive)));
        }

        #endregion

        #region OnPropertyChanged テスト

        [Test]
        public void OnPropertyChanged_WithPropertyName_ShouldRaiseEvent()
        {
            // Arrange & Act
            _viewModel.NotifyPropertyChanged("CustomProperty");

            // Assert
            Assert.That(_changedProperties, Does.Contain("CustomProperty"));
        }

        [Test]
        public void OnPropertyChanged_WithNull_ShouldRaiseEventWithNull()
        {
            // Arrange
            string? receivedPropertyName = "initial";

            _viewModel.PropertyChanged += (sender, e) =>
            {
                receivedPropertyName = e.PropertyName;
            };

            // Act
            _viewModel.NotifyPropertyChanged(null!);

            // Assert
            Assert.That(receivedPropertyName, Is.Null);
        }

        #endregion

        #region IDisposable テスト

        [Test]
        public void Dispose_ShouldCallOnDispose()
        {
            // Arrange
            var viewModel = new MockViewModel();

            // Act
            viewModel.Dispose();

            // Assert
            Assert.That(viewModel.OnDisposeWasCalled, Is.True);
        }

        [Test]
        public void Dispose_ShouldSetIsDisposedToTrue()
        {
            // Arrange
            var viewModel = new MockViewModel();

            // Act
            viewModel.Dispose();

            // Assert
            Assert.That(viewModel.CheckIsDisposed(), Is.True);
        }

        [Test]
        public void Dispose_CalledMultipleTimes_ShouldOnlyDisposeOnce()
        {
            // Arrange
            var disposeCount = 0;
            var viewModel = new DisposableCountingViewModel(() => disposeCount++);

            // Act
            viewModel.Dispose();
            viewModel.Dispose();
            viewModel.Dispose();

            // Assert
            Assert.That(disposeCount, Is.EqualTo(1));
        }

        /// <summary>
        /// Disposeの呼び出し回数を追跡するためのテスト用ViewModel
        /// </summary>
        private class DisposableCountingViewModel : ViewModelBase
        {
            private readonly Action _onDispose;

            public DisposableCountingViewModel(Action onDispose)
            {
                _onDispose = onDispose;
            }

            protected override void OnDispose()
            {
                _onDispose?.Invoke();
                base.OnDispose();
            }
        }

        #endregion

        #region PropertyChanged イベント購読テスト

        [Test]
        public void PropertyChanged_WithNoSubscribers_ShouldNotThrow()
        {
            // Arrange
            var viewModel = new MockViewModel();
            // イベント購読なし

            // Act & Assert
            Assert.DoesNotThrow(() => viewModel.Name = "Test");
        }

        [Test]
        public void PropertyChanged_WithMultipleSubscribers_ShouldNotifyAll()
        {
            // Arrange
            var notifications1 = new List<string>();
            var notifications2 = new List<string>();

            _viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName != null) notifications1.Add(e.PropertyName);
            };
            _viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName != null) notifications2.Add(e.PropertyName);
            };

            // Act
            _viewModel.Name = "Test";

            // Assert
            Assert.That(notifications1, Does.Contain(nameof(MockViewModel.Name)));
            Assert.That(notifications2, Does.Contain(nameof(MockViewModel.Name)));
        }

        #endregion

        #region 連続変更テスト

        [Test]
        public void SetProperty_MultipleChanges_ShouldRaiseEventForEachChange()
        {
            // Arrange & Act
            _viewModel.Value = 1;
            _viewModel.Value = 2;
            _viewModel.Value = 3;

            // Assert
            var valueChanges = _changedProperties.FindAll(p => p == nameof(MockViewModel.Value));
            Assert.That(valueChanges.Count, Is.EqualTo(3));
        }

        [Test]
        public void SetProperty_DifferentProperties_ShouldRaiseCorrectEvents()
        {
            // Arrange & Act
            _viewModel.Name = "Test";
            _viewModel.Value = 42;
            _viewModel.IsActive = true;

            // Assert
            Assert.That(_changedProperties.Count, Is.EqualTo(3));
            Assert.That(_changedProperties[0], Is.EqualTo(nameof(MockViewModel.Name)));
            Assert.That(_changedProperties[1], Is.EqualTo(nameof(MockViewModel.Value)));
            Assert.That(_changedProperties[2], Is.EqualTo(nameof(MockViewModel.IsActive)));
        }

        #endregion
    }
}
