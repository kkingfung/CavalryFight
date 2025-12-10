#nullable enable

using System;
using System.ComponentModel;

namespace CavalryFight.Core.MVVM
{
    /// <summary>
    /// ViewModelのプロパティ変更を監視し、Viewの更新を行うバインディングクラス
    /// </summary>
    /// <typeparam name="TValue">バインドするプロパティの型</typeparam>
    public class PropertyBinding<TValue> : IDisposable
    {
        #region Fields

        private readonly INotifyPropertyChanged _source;
        private readonly string _propertyName;
        private readonly Func<TValue> _getter;
        private readonly Action<TValue> _setter;
        private bool _isDisposed;

        #endregion

        #region Constructor

        /// <summary>
        /// PropertyBindingの新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="source">監視対象のオブジェクト（通常はViewModel）</param>
        /// <param name="propertyName">監視するプロパティ名</param>
        /// <param name="getter">プロパティ値を取得する関数</param>
        /// <param name="setter">プロパティ値が変更されたときに呼び出される関数</param>
        public PropertyBinding(
            INotifyPropertyChanged source,
            string propertyName,
            Func<TValue> getter,
            Action<TValue> setter)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _propertyName = propertyName ?? throw new ArgumentNullException(nameof(propertyName));
            _getter = getter ?? throw new ArgumentNullException(nameof(getter));
            _setter = setter ?? throw new ArgumentNullException(nameof(setter));

            // プロパティ変更イベントを購読
            _source.PropertyChanged += OnPropertyChanged;

            // 初期値を設定
            UpdateValue();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// プロパティ変更イベントハンドラ
        /// </summary>
        private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == _propertyName || string.IsNullOrEmpty(e.PropertyName))
            {
                UpdateValue();
            }
        }

        /// <summary>
        /// バインドされた値を更新します。
        /// </summary>
        private void UpdateValue()
        {
            if (_isDisposed)
            {
                return;
            }

            try
            {
                var value = _getter();
                _setter(value);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"PropertyBinding update failed for '{_propertyName}': {ex.Message}");
            }
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// バインディングを解除し、リソースを解放します。
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _source.PropertyChanged -= OnPropertyChanged;
            _isDisposed = true;
        }

        #endregion
    }

    /// <summary>
    /// PropertyBindingを簡単に作成するためのヘルパークラス
    /// </summary>
    public static class PropertyBindingExtensions
    {
        /// <summary>
        /// ViewModelのプロパティをViewの更新処理にバインドします。
        /// </summary>
        /// <typeparam name="TViewModel">ViewModelの型</typeparam>
        /// <typeparam name="TValue">プロパティの型</typeparam>
        /// <param name="viewModel">バインド元のViewModel</param>
        /// <param name="propertyName">監視するプロパティ名</param>
        /// <param name="getter">プロパティ値を取得する関数</param>
        /// <param name="setter">プロパティ値が変更されたときに呼び出される関数</param>
        /// <returns>作成されたPropertyBindingインスタンス</returns>
        /// <example>
        /// <code>
        /// // ViewModelのHealthプロパティをUIのテキストにバインド
        /// var binding = viewModel.Bind(
        ///     nameof(PlayerViewModel.Health),
        ///     () => viewModel.Health,
        ///     value => healthText.text = value.ToString()
        /// );
        /// </code>
        /// </example>
        public static PropertyBinding<TValue> Bind<TViewModel, TValue>(
            this TViewModel viewModel,
            string propertyName,
            Func<TValue> getter,
            Action<TValue> setter)
            where TViewModel : INotifyPropertyChanged
        {
            return new PropertyBinding<TValue>(viewModel, propertyName, getter, setter);
        }
    }
}
