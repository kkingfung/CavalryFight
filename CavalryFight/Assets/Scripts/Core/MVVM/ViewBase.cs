#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine;

namespace CavalryFight.Core.MVVM
{
    /// <summary>
    /// Viewの基底クラス
    /// ViewModelとのバインディング機能を提供します。
    /// </summary>
    /// <typeparam name="TViewModel">このViewに関連付けられるViewModelの型</typeparam>
    /// <remarks>
    /// すべてのViewはこのクラスを継承してください。
    /// ViewModelとの自動バインディング、ライフサイクル管理を提供します。
    /// </remarks>
    public abstract class ViewBase<TViewModel> : MonoBehaviour where TViewModel : ViewModelBase
    {
        #region Fields

        private TViewModel? _viewModel;
        private readonly List<IDisposable> _bindings = new List<IDisposable>();
        private bool _isInitialized;

        #endregion

        #region Properties

        /// <summary>
        /// このViewに関連付けられたViewModelを取得または設定します。
        /// </summary>
        public TViewModel? ViewModel
        {
            get => _viewModel;
            set
            {
                if (_viewModel == value)
                {
                    return;
                }

                // 既存のバインディングを解除
                UnbindViewModel();

                _viewModel = value;

                // 新しいViewModelをバインド
                if (_viewModel != null)
                {
                    BindViewModel(_viewModel);
                }
            }
        }

        /// <summary>
        /// Viewが初期化済みかどうかを取得します。
        /// </summary>
        protected bool IsInitialized => _isInitialized;

        #endregion

        #region Unity Lifecycle

        /// <summary>
        /// Unityの初期化処理
        /// </summary>
        protected virtual void Awake()
        {
            Initialize();
        }

        /// <summary>
        /// Unityの破棄処理
        /// </summary>
        protected virtual void OnDestroy()
        {
            UnbindViewModel();
            _viewModel?.Dispose();
            _viewModel = null;
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Viewの初期化処理を行います。
        /// </summary>
        /// <remarks>
        /// ViewModelの作成や初期設定を行う場合はオーバーライドしてください。
        /// </remarks>
        protected virtual void Initialize()
        {
            if (_isInitialized)
            {
                return;
            }

            OnInitialize();
            _isInitialized = true;
        }

        /// <summary>
        /// 初期化処理の実装を記述します。
        /// </summary>
        protected virtual void OnInitialize()
        {
            // 派生クラスで実装
        }

        /// <summary>
        /// ViewModelとのバインディングを設定します。
        /// </summary>
        /// <param name="viewModel">バインドするViewModel</param>
        /// <remarks>
        /// このメソッドをオーバーライドして、プロパティやコマンドのバインディングを実装してください。
        /// </remarks>
        protected virtual void BindViewModel(TViewModel viewModel)
        {
            // 派生クラスで実装
            OnViewModelBound(viewModel);
        }

        /// <summary>
        /// ViewModelバインド後の処理を実装します。
        /// </summary>
        /// <param name="viewModel">バインドされたViewModel</param>
        protected virtual void OnViewModelBound(TViewModel viewModel)
        {
            // 派生クラスで実装
        }

        /// <summary>
        /// ViewModelとのバインディングを解除します。
        /// </summary>
        protected virtual void UnbindViewModel()
        {
            // すべてのバインディングを破棄
            foreach (var binding in _bindings)
            {
                binding?.Dispose();
            }
            _bindings.Clear();

            OnViewModelUnbound();
        }

        /// <summary>
        /// ViewModelバインド解除後の処理を実装します。
        /// </summary>
        protected virtual void OnViewModelUnbound()
        {
            // 派生クラスで実装
        }

        /// <summary>
        /// バインディングを登録します。
        /// </summary>
        /// <param name="binding">登録するバインディング</param>
        protected void AddBinding(IDisposable binding)
        {
            if (binding != null)
            {
                _bindings.Add(binding);
            }
        }

        #endregion
    }
}
