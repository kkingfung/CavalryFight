#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace CavalryFight.Core.MVVM
{
    /// <summary>
    /// UI Toolkit専用のViewの基底クラス
    /// </summary>
    /// <typeparam name="TViewModel">このViewに関連付けられるViewModelの型</typeparam>
    /// <remarks>
    /// UI ToolkitでUIを構築する場合はこのクラスを継承してください。
    /// ViewModelとの自動バインディング、ライフサイクル管理を提供します。
    /// </remarks>
    public abstract class UIToolkitViewBase<TViewModel> : MonoBehaviour where TViewModel : ViewModelBase
    {
        #region Fields

        private TViewModel? _viewModel;
        private readonly List<IDisposable> _bindings = new List<IDisposable>();
        private bool _isInitialized;
        private UIDocument? _uiDocument;
        private VisualElement? _rootVisualElement;

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

        /// <summary>
        /// UIDocumentを取得します。
        /// </summary>
        protected UIDocument? UIDocument => _uiDocument;

        /// <summary>
        /// ルートVisualElementを取得します。
        /// </summary>
        protected VisualElement? RootVisualElement => _rootVisualElement;

        #endregion

        #region Unity Lifecycle

        /// <summary>
        /// Unityの初期化処理
        /// </summary>
        protected virtual void Awake()
        {
            // UIDocumentコンポーネントを取得
            _uiDocument = GetComponent<UIDocument>();
            if (_uiDocument == null)
            {
                Debug.LogError($"[{GetType().Name}] UIDocument component not found! Please attach UIDocument to this GameObject.", this);
                return;
            }

            Initialize();
        }

        /// <summary>
        /// Unityの有効化処理
        /// </summary>
        protected virtual void OnEnable()
        {
            if (_uiDocument != null && _rootVisualElement == null)
            {
                _rootVisualElement = _uiDocument.rootVisualElement;

                if (_rootVisualElement != null)
                {
                    OnRootVisualElementReady(_rootVisualElement);
                }
            }
        }

        /// <summary>
        /// Unityの破棄処理
        /// </summary>
        protected virtual void OnDestroy()
        {
            UnbindViewModel();
            _viewModel?.Dispose();
            _viewModel = null;
            _rootVisualElement = null;
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Viewの初期化処理を行います。
        /// </summary>
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
        /// RootVisualElementが準備できた時に呼び出されます。
        /// </summary>
        /// <param name="root">ルートVisualElement</param>
        /// <remarks>
        /// UI要素のクエリやイベント設定を行う場合はこのメソッドをオーバーライドしてください。
        /// </remarks>
        protected virtual void OnRootVisualElementReady(VisualElement root)
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

        /// <summary>
        /// 名前でUI要素を取得します。
        /// </summary>
        /// <typeparam name="T">取得する要素の型</typeparam>
        /// <param name="name">要素の名前</param>
        /// <returns>見つかった要素、または null</returns>
        protected T? Q<T>(string name) where T : VisualElement
        {
            return _rootVisualElement?.Q<T>(name);
        }

        /// <summary>
        /// クラス名でUI要素を取得します。
        /// </summary>
        /// <typeparam name="T">取得する要素の型</typeparam>
        /// <param name="className">要素のクラス名</param>
        /// <returns>見つかった要素、または null</returns>
        protected T? QByClass<T>(string className) where T : VisualElement
        {
            return _rootVisualElement?.Q<T>(className: className);
        }

        #endregion
    }
}
