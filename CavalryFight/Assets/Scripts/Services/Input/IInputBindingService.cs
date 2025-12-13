#nullable enable

using System;
using CavalryFight.Core.Services;

namespace CavalryFight.Services.Input
{
    /// <summary>
    /// 入力バインディング管理サービスのインターフェース
    /// </summary>
    /// <remarks>
    /// キーバインディングプロファイルの管理、保存/読み込み、
    /// カスタマイズ機能を提供します。
    /// </remarks>
    public interface IInputBindingService : IService
    {
        #region Events

        /// <summary>
        /// バインディングが変更された時に発生します。
        /// </summary>
        event EventHandler<BindingChangedEventArgs>? BindingChanged;

        /// <summary>
        /// プロファイルが読み込まれた時に発生します。
        /// </summary>
        event EventHandler? ProfileLoaded;

        #endregion

        #region Properties

        /// <summary>
        /// 現在のバインディングプロファイルを取得します。
        /// </summary>
        InputBindingProfile CurrentProfile { get; }

        #endregion

        #region Methods

        /// <summary>
        /// 指定されたアクションのバインディングを取得します。
        /// </summary>
        /// <param name="action">取得するアクション</param>
        /// <returns>バインディング。存在しない場合はnull</returns>
        InputBinding? GetBinding(InputAction action);

        /// <summary>
        /// 指定されたアクションのバインディングを設定します。
        /// </summary>
        /// <param name="binding">設定するバインディング</param>
        void SetBinding(InputBinding binding);

        /// <summary>
        /// バインディングプロファイルをファイルに保存します。
        /// </summary>
        /// <returns>保存に成功した場合true</returns>
        bool SaveProfile();

        /// <summary>
        /// バインディングプロファイルをファイルから読み込みます。
        /// </summary>
        /// <returns>読み込みに成功した場合true</returns>
        bool LoadProfile();

        /// <summary>
        /// デフォルトのバインディングにリセットします。
        /// </summary>
        void ResetToDefault();

        /// <summary>
        /// 現在のプロファイルの妥当性を検証します。
        /// </summary>
        /// <returns>妥当な場合true</returns>
        bool ValidateProfile();

        #endregion
    }

    /// <summary>
    /// バインディング変更イベントの引数
    /// </summary>
    public class BindingChangedEventArgs : EventArgs
    {
        /// <summary>
        /// 変更されたアクション
        /// </summary>
        public InputAction Action { get; }

        /// <summary>
        /// 新しいバインディング
        /// </summary>
        public InputBinding NewBinding { get; }

        /// <summary>
        /// BindingChangedEventArgsの新しいインスタンスを初期化します。
        /// </summary>
        /// <param name="action">変更されたアクション</param>
        /// <param name="newBinding">新しいバインディング</param>
        public BindingChangedEventArgs(InputAction action, InputBinding newBinding)
        {
            Action = action;
            NewBinding = newBinding;
        }
    }
}
