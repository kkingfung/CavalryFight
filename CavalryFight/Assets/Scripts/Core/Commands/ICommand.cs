#nullable enable

using System;

namespace CavalryFight.Core.Commands
{
    /// <summary>
    /// コマンドパターンのインターフェース
    /// </summary>
    /// <remarks>
    /// ViewModelでユーザーアクションを表現するために使用します。
    /// ボタンクリックなどのUI操作をViewModelのコマンドにバインドできます。
    /// </remarks>
    public interface ICommand
    {
        /// <summary>
        /// コマンドの実行可能状態が変更されたときに発生します。
        /// </summary>
        event EventHandler? CanExecuteChanged;

        /// <summary>
        /// コマンドが実行可能かどうかを判定します。
        /// </summary>
        /// <param name="parameter">コマンドパラメータ</param>
        /// <returns>実行可能な場合はtrue、それ以外はfalse</returns>
        bool CanExecute(object? parameter);

        /// <summary>
        /// コマンドを実行します。
        /// </summary>
        /// <param name="parameter">コマンドパラメータ</param>
        void Execute(object? parameter);

        /// <summary>
        /// CanExecuteChangedイベントを発行します。
        /// </summary>
        void RaiseCanExecuteChanged();
    }
}
