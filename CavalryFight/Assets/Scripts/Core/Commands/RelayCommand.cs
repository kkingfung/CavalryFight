#nullable enable

using System;

namespace CavalryFight.Core.Commands
{
    /// <summary>
    /// デリゲートを使用したコマンドの実装
    /// </summary>
    /// <remarks>
    /// ViewModelで簡単にコマンドを定義できるようにするクラスです。
    /// Execute処理とCanExecute判定をデリゲートで渡すことで、
    /// 各コマンドごとにクラスを作成する必要がありません。
    /// </remarks>
    public class RelayCommand : ICommand
    {
        #region Fields

        private readonly Action<object?> _execute;
        private readonly Func<object?, bool>? _canExecute;

        #endregion

        #region Events

        /// <summary>
        /// コマンドの実行可能状態が変更されたときに発生します。
        /// </summary>
        public event EventHandler? CanExecuteChanged;

        #endregion

        #region Constructor

        /// <summary>
        /// RelayCommandの新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="execute">コマンド実行時に呼び出される処理</param>
        /// <param name="canExecute">コマンドが実行可能かどうかを判定する処理（省略可能）</param>
        /// <exception cref="ArgumentNullException">executeがnullの場合</exception>
        public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        /// <summary>
        /// パラメータなしのRelayCommandの新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="execute">コマンド実行時に呼び出される処理</param>
        /// <param name="canExecute">コマンドが実行可能かどうかを判定する処理（省略可能）</param>
        /// <exception cref="ArgumentNullException">executeがnullの場合</exception>
        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            if (execute == null)
            {
                throw new ArgumentNullException(nameof(execute));
            }

            _execute = _ => execute();
            _canExecute = canExecute != null ? _ => canExecute() : null;
        }

        #endregion

        #region ICommand Implementation

        /// <summary>
        /// コマンドが実行可能かどうかを判定します。
        /// </summary>
        /// <param name="parameter">コマンドパラメータ</param>
        /// <returns>実行可能な場合はtrue、それ以外はfalse</returns>
        public bool CanExecute(object? parameter)
        {
            return _canExecute?.Invoke(parameter) ?? true;
        }

        /// <summary>
        /// コマンドを実行します。
        /// </summary>
        /// <param name="parameter">コマンドパラメータ</param>
        public void Execute(object? parameter)
        {
            if (CanExecute(parameter))
            {
                _execute(parameter);
            }
        }

        /// <summary>
        /// CanExecuteChangedイベントを発行します。
        /// </summary>
        /// <remarks>
        /// コマンドの実行可能状態が変わった場合に呼び出してください。
        /// これにより、UIが自動的に更新されます。
        /// </remarks>
        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        #endregion
    }

    /// <summary>
    /// 型付きパラメータを持つデリゲートコマンド
    /// </summary>
    /// <typeparam name="T">コマンドパラメータの型</typeparam>
    public class RelayCommand<T> : ICommand
    {
        #region Fields

        private readonly Action<T?> _execute;
        private readonly Func<T?, bool>? _canExecute;

        #endregion

        #region Events

        /// <summary>
        /// コマンドの実行可能状態が変更されたときに発生します。
        /// </summary>
        public event EventHandler? CanExecuteChanged;

        #endregion

        #region Constructor

        /// <summary>
        /// RelayCommand&lt;T&gt;の新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="execute">コマンド実行時に呼び出される処理</param>
        /// <param name="canExecute">コマンドが実行可能かどうかを判定する処理（省略可能）</param>
        /// <exception cref="ArgumentNullException">executeがnullの場合</exception>
        public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        #endregion

        #region ICommand Implementation

        /// <summary>
        /// コマンドが実行可能かどうかを判定します。
        /// </summary>
        /// <param name="parameter">コマンドパラメータ</param>
        /// <returns>実行可能な場合はtrue、それ以外はfalse</returns>
        public bool CanExecute(object? parameter)
        {
            if (parameter == null && typeof(T).IsValueType && Nullable.GetUnderlyingType(typeof(T)) == null)
            {
                return false;
            }

            return _canExecute?.Invoke((T?)parameter) ?? true;
        }

        /// <summary>
        /// コマンドを実行します。
        /// </summary>
        /// <param name="parameter">コマンドパラメータ</param>
        public void Execute(object? parameter)
        {
            if (CanExecute(parameter))
            {
                _execute((T?)parameter);
            }
        }

        /// <summary>
        /// CanExecuteChangedイベントを発行します。
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        #endregion
    }
}
