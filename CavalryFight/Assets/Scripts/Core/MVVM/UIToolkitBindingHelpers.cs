#nullable enable

using System;
using System.ComponentModel;
using CavalryFight.Core.Commands;
using UnityEngine.UIElements;

namespace CavalryFight.Core.MVVM
{
    /// <summary>
    /// UI Toolkit用のバインディングヘルパークラス
    /// </summary>
    public static class UIToolkitBindingHelpers
    {
        #region Button Command Binding

        /// <summary>
        /// ButtonをICommandにバインドします。
        /// </summary>
        /// <param name="button">バインドするButton</param>
        /// <param name="command">バインドするCommand</param>
        /// <returns>バインディングオブジェクト（Dispose可能）</returns>
        public static IDisposable BindCommand(this Button button, ICommand command)
        {
            if (button == null)
            {
                throw new ArgumentNullException(nameof(button));
            }

            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            return new ButtonCommandBinder(button, command);
        }

        #endregion

        #region Label Property Binding

        /// <summary>
        /// LabelをViewModelのプロパティにバインドします。
        /// </summary>
        /// <typeparam name="TValue">プロパティの型</typeparam>
        /// <param name="label">バインドするLabel</param>
        /// <param name="viewModel">ViewModelインスタンス</param>
        /// <param name="propertyName">監視するプロパティ名</param>
        /// <param name="getter">プロパティ値を取得する関数</param>
        /// <param name="converter">値を文字列に変換する関数（省略可能）</param>
        /// <returns>バインディングオブジェクト（Dispose可能）</returns>
        public static IDisposable BindText<TValue>(
            this Label label,
            INotifyPropertyChanged viewModel,
            string propertyName,
            Func<TValue> getter,
            Func<TValue, string>? converter = null)
        {
            if (label == null)
            {
                throw new ArgumentNullException(nameof(label));
            }

            if (viewModel == null)
            {
                throw new ArgumentNullException(nameof(viewModel));
            }

            if (propertyName == null)
            {
                throw new ArgumentNullException(nameof(propertyName));
            }

            if (getter == null)
            {
                throw new ArgumentNullException(nameof(getter));
            }

            converter ??= value => value?.ToString() ?? string.Empty;

            return new LabelPropertyBinder<TValue>(label, viewModel, propertyName, getter, converter);
        }

        #endregion

        #region VisualElement Visibility Binding

        /// <summary>
        /// VisualElementの表示/非表示をViewModelのプロパティにバインドします。
        /// </summary>
        /// <param name="element">バインドするVisualElement</param>
        /// <param name="viewModel">ViewModelインスタンス</param>
        /// <param name="propertyName">監視するプロパティ名</param>
        /// <param name="getter">プロパティ値を取得する関数</param>
        /// <returns>バインディングオブジェクト（Dispose可能）</returns>
        public static IDisposable BindVisibility(
            this VisualElement element,
            INotifyPropertyChanged viewModel,
            string propertyName,
            Func<bool> getter)
        {
            if (element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }

            if (viewModel == null)
            {
                throw new ArgumentNullException(nameof(viewModel));
            }

            if (propertyName == null)
            {
                throw new ArgumentNullException(nameof(propertyName));
            }

            if (getter == null)
            {
                throw new ArgumentNullException(nameof(getter));
            }

            return new VisualElementVisibilityBinder(element, viewModel, propertyName, getter);
        }

        #endregion

        #region ProgressBar Value Binding

        /// <summary>
        /// ProgressBarの値をViewModelのプロパティにバインドします。
        /// </summary>
        /// <param name="progressBar">バインドするProgressBar</param>
        /// <param name="viewModel">ViewModelインスタンス</param>
        /// <param name="propertyName">監視するプロパティ名</param>
        /// <param name="getter">プロパティ値を取得する関数（0.0～1.0）</param>
        /// <returns>バインディングオブジェクト（Dispose可能）</returns>
        public static IDisposable BindValue(
            this ProgressBar progressBar,
            INotifyPropertyChanged viewModel,
            string propertyName,
            Func<float> getter)
        {
            if (progressBar == null)
            {
                throw new ArgumentNullException(nameof(progressBar));
            }

            if (viewModel == null)
            {
                throw new ArgumentNullException(nameof(viewModel));
            }

            if (propertyName == null)
            {
                throw new ArgumentNullException(nameof(propertyName));
            }

            if (getter == null)
            {
                throw new ArgumentNullException(nameof(getter));
            }

            return new ProgressBarValueBinder(progressBar, viewModel, propertyName, getter);
        }

        #endregion
    }

    #region Internal Binder Classes

    /// <summary>
    /// ButtonとICommandをバインドするクラス
    /// </summary>
    internal class ButtonCommandBinder : IDisposable
    {
        private readonly Button _button;
        private readonly ICommand _command;
        private bool _isDisposed;

        public ButtonCommandBinder(Button button, ICommand command)
        {
            _button = button;
            _command = command;

            // クリックイベントを購読
            _button.clicked += OnButtonClicked;

            // コマンドの実行可能状態変更を購読
            _command.CanExecuteChanged += OnCanExecuteChanged;

            // 初期状態を設定
            UpdateButtonState();
        }

        private void OnButtonClicked()
        {
            if (_command.CanExecute(null))
            {
                _command.Execute(null);
            }
        }

        private void OnCanExecuteChanged(object? sender, EventArgs e)
        {
            UpdateButtonState();
        }

        private void UpdateButtonState()
        {
            _button.SetEnabled(_command.CanExecute(null));
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _button.clicked -= OnButtonClicked;
            _command.CanExecuteChanged -= OnCanExecuteChanged;
            _isDisposed = true;
        }
    }

    /// <summary>
    /// LabelとViewModelプロパティをバインドするクラス
    /// </summary>
    internal class LabelPropertyBinder<TValue> : IDisposable
    {
        private readonly Label _label;
        private readonly INotifyPropertyChanged _viewModel;
        private readonly string _propertyName;
        private readonly Func<TValue> _getter;
        private readonly Func<TValue, string> _converter;
        private bool _isDisposed;

        public LabelPropertyBinder(
            Label label,
            INotifyPropertyChanged viewModel,
            string propertyName,
            Func<TValue> getter,
            Func<TValue, string> converter)
        {
            _label = label;
            _viewModel = viewModel;
            _propertyName = propertyName;
            _getter = getter;
            _converter = converter;

            // プロパティ変更イベントを購読
            _viewModel.PropertyChanged += OnPropertyChanged;

            // 初期値を設定
            UpdateValue();
        }

        private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == _propertyName || string.IsNullOrEmpty(e.PropertyName))
            {
                UpdateValue();
            }
        }

        private void UpdateValue()
        {
            if (_isDisposed)
            {
                return;
            }

            try
            {
                var value = _getter();
                _label.text = _converter(value);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"LabelPropertyBinder update failed for '{_propertyName}': {ex.Message}");
            }
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _viewModel.PropertyChanged -= OnPropertyChanged;
            _isDisposed = true;
        }
    }

    /// <summary>
    /// VisualElementの表示/非表示とViewModelプロパティをバインドするクラス
    /// </summary>
    internal class VisualElementVisibilityBinder : IDisposable
    {
        private readonly VisualElement _element;
        private readonly INotifyPropertyChanged _viewModel;
        private readonly string _propertyName;
        private readonly Func<bool> _getter;
        private bool _isDisposed;

        public VisualElementVisibilityBinder(
            VisualElement element,
            INotifyPropertyChanged viewModel,
            string propertyName,
            Func<bool> getter)
        {
            _element = element;
            _viewModel = viewModel;
            _propertyName = propertyName;
            _getter = getter;

            // プロパティ変更イベントを購読
            _viewModel.PropertyChanged += OnPropertyChanged;

            // 初期値を設定
            UpdateVisibility();
        }

        private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == _propertyName || string.IsNullOrEmpty(e.PropertyName))
            {
                UpdateVisibility();
            }
        }

        private void UpdateVisibility()
        {
            if (_isDisposed)
            {
                return;
            }

            try
            {
                var isVisible = _getter();
                _element.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"VisualElementVisibilityBinder update failed for '{_propertyName}': {ex.Message}");
            }
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _viewModel.PropertyChanged -= OnPropertyChanged;
            _isDisposed = true;
        }
    }

    /// <summary>
    /// ProgressBarの値とViewModelプロパティをバインドするクラス
    /// </summary>
    internal class ProgressBarValueBinder : IDisposable
    {
        private readonly ProgressBar _progressBar;
        private readonly INotifyPropertyChanged _viewModel;
        private readonly string _propertyName;
        private readonly Func<float> _getter;
        private bool _isDisposed;

        public ProgressBarValueBinder(
            ProgressBar progressBar,
            INotifyPropertyChanged viewModel,
            string propertyName,
            Func<float> getter)
        {
            _progressBar = progressBar;
            _viewModel = viewModel;
            _propertyName = propertyName;
            _getter = getter;

            // プロパティ変更イベントを購読
            _viewModel.PropertyChanged += OnPropertyChanged;

            // 初期値を設定
            UpdateValue();
        }

        private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == _propertyName || string.IsNullOrEmpty(e.PropertyName))
            {
                UpdateValue();
            }
        }

        private void UpdateValue()
        {
            if (_isDisposed)
            {
                return;
            }

            try
            {
                var value = _getter();
                _progressBar.value = UnityEngine.Mathf.Clamp01(value) * 100f; // 0-100%
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"ProgressBarValueBinder update failed for '{_propertyName}': {ex.Message}");
            }
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _viewModel.PropertyChanged -= OnPropertyChanged;
            _isDisposed = true;
        }
    }

    #endregion
}
