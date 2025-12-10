#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CavalryFight.Core.MVVM
{
    /// <summary>
    /// ViewModelの基底クラス
    /// INotifyPropertyChangedを実装し、プロパティ変更通知機能を提供します。
    /// </summary>
    /// <remarks>
    /// すべてのViewModelはこのクラスを継承してください。
    /// プロパティ変更時は<see cref="SetProperty{T}"/>メソッドを使用することで、
    /// 自動的に変更通知が発行されます。
    /// </remarks>
    public abstract class ViewModelBase : INotifyPropertyChanged, IDisposable
    {
        #region Events

        /// <summary>
        /// プロパティ値が変更されたときに発生します。
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        #endregion

        #region Fields

        private bool _isDisposed;

        #endregion

        #region Properties

        /// <summary>
        /// ViewModelが破棄済みかどうかを取得します。
        /// </summary>
        protected bool IsDisposed => _isDisposed;

        #endregion

        #region Protected Methods

        /// <summary>
        /// プロパティ変更通知を発行します。
        /// </summary>
        /// <param name="propertyName">変更されたプロパティの名前（自動設定）</param>
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// プロパティの値を設定し、変更があった場合は通知を発行します。
        /// </summary>
        /// <typeparam name="T">プロパティの型</typeparam>
        /// <param name="field">プロパティのバッキングフィールド</param>
        /// <param name="value">設定する新しい値</param>
        /// <param name="propertyName">プロパティの名前（自動設定）</param>
        /// <returns>値が変更された場合はtrue、それ以外はfalse</returns>
        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return false;
            }

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        /// <summary>
        /// プロパティの値を設定し、変更があった場合は通知を発行します。
        /// 値変更後にコールバックを実行します。
        /// </summary>
        /// <typeparam name="T">プロパティの型</typeparam>
        /// <param name="field">プロパティのバッキングフィールド</param>
        /// <param name="value">設定する新しい値</param>
        /// <param name="onChanged">値変更後に実行するコールバック</param>
        /// <param name="propertyName">プロパティの名前（自動設定）</param>
        /// <returns>値が変更された場合はtrue、それ以外はfalse</returns>
        protected bool SetProperty<T>(ref T field, T value, Action onChanged, [CallerMemberName] string? propertyName = null)
        {
            if (SetProperty(ref field, value, propertyName))
            {
                onChanged?.Invoke();
                return true;
            }
            return false;
        }

        /// <summary>
        /// 複数のプロパティ変更通知を発行します。
        /// </summary>
        /// <param name="propertyNames">変更されたプロパティ名のリスト</param>
        protected void OnPropertiesChanged(params string[] propertyNames)
        {
            foreach (var propertyName in propertyNames)
            {
                OnPropertyChanged(propertyName);
            }
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// ViewModelが破棄されるときに呼び出されます。
        /// リソースのクリーンアップが必要な場合はオーバーライドしてください。
        /// </summary>
        protected virtual void OnDispose()
        {
            // 派生クラスでオーバーライドしてリソースを解放
        }

        /// <summary>
        /// ViewModelを破棄し、リソースを解放します。
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            OnDispose();
            _isDisposed = true;

            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
