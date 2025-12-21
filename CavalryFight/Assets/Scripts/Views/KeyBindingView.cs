#nullable enable

using System.Collections.Generic;
using CavalryFight.Core.MVVM;
using CavalryFight.ViewModels;
using UnityEngine;
using UnityEngine.UIElements;

namespace CavalryFight.Views
{
    /// <summary>
    /// キーバインディング設定ポップアップのView
    /// </summary>
    /// <remarks>
    /// UI Toolkitを使用してキーバインディング設定UIを表示します。
    /// KeyBindingViewModelとバインドされ、キー再バインディング機能を提供します。
    /// </remarks>
    [RequireComponent(typeof(UIDocument))]
    public class KeyBindingView : UIToolkitViewBase<KeyBindingViewModel>
    {
        #region Constants

        // Blink Animation
        /// <summary>プロンプトの点滅間隔（秒）</summary>
        private const float BlinkInterval = 0.5f;

        /// <summary>点滅時の明るい状態の不透明度</summary>
        private const float BlinkOpacityBright = 1.0f;

        /// <summary>点滅時の暗い状態の不透明度</summary>
        private const float BlinkOpacityDim = 0.85f;

        /// <summary>非表示時の不透明度</summary>
        private const float OpacityHidden = 0.0f;

        // Row Colors
        /// <summary>通常時の行の背景色（ダークブラウン）</summary>
        private static readonly Color RowBackgroundNormal = new Color(0.15f, 0.12f, 0.08f, 0.8f);

        /// <summary>リバインディング中の行の背景色（明るいオレンジ）</summary>
        private static readonly Color RowBackgroundHighlight = new Color(0.8f, 0.5f, 0.1f, 0.9f);

        /// <summary>リバインディング中の行のボーダー色（明るいイエロー）</summary>
        private static readonly Color RowBorderHighlight = new Color(1.0f, 0.7f, 0.2f);

        // Prompt Colors
        /// <summary>プロンプトの明るい背景色（明るいオレンジ）</summary>
        private static readonly Color PromptBackgroundBright = new Color(1.0f, 0.4f, 0.0f);

        /// <summary>プロンプトの明るいボーダー色（明るいイエロー）</summary>
        private static readonly Color PromptBorderBright = new Color(1.0f, 0.8f, 0.0f);

        /// <summary>プロンプトの暗い背景色（暗いオレンジ）</summary>
        private static readonly Color PromptBackgroundDim = new Color(0.8f, 0.3f, 0.0f);

        /// <summary>プロンプトの暗いボーダー色（暗いイエロー）</summary>
        private static readonly Color PromptBorderDim = new Color(0.8f, 0.6f, 0.0f);

        // Label Colors
        /// <summary>アクション名ラベルの色（ウォームホワイト）</summary>
        private static readonly Color ActionLabelColor = new Color(1.0f, 0.95f, 0.8f);

        /// <summary>キーラベルの色（明るいシアン）</summary>
        private static readonly Color KeyLabelColor = new Color(0.4f, 0.9f, 1.0f);

        /// <summary>キーコンテナの背景色（ダークブルーグレー）</summary>
        private static readonly Color KeyContainerBackground = new Color(0.2f, 0.25f, 0.35f);

        // Font Sizes
        /// <summary>アクション名のフォントサイズ</summary>
        private const int ActionLabelFontSize = 36;

        /// <summary>キーバインディングのフォントサイズ</summary>
        private const int KeyLabelFontSize = 38;

        /// <summary>リバインドボタンのフォントサイズ</summary>
        private const int RebindButtonFontSize = 32;

        // Spacing and Sizing
        /// <summary>行の上下パディング</summary>
        private const int RowPaddingVertical = 15;

        /// <summary>行の左右パディング</summary>
        private const int RowPaddingHorizontal = 20;

        /// <summary>行の下マージン</summary>
        private const int RowMarginBottom = 10;

        /// <summary>行のボーダーの太さ（ハイライト時）</summary>
        private const int RowHighlightBorderWidth = 3;

        /// <summary>行のボーダーの角丸半径</summary>
        private const int RowBorderRadius = 8;

        /// <summary>アクション名ラベルの最小幅</summary>
        private const int ActionLabelMinWidth = 350;

        /// <summary>キーコンテナの最小幅</summary>
        private const int KeyContainerMinWidth = 250;

        /// <summary>キーコンテナのパディング（上下）</summary>
        private const int KeyContainerPaddingVertical = 8;

        /// <summary>キーコンテナのパディング（左右）</summary>
        private const int KeyContainerPaddingHorizontal = 20;

        /// <summary>キーコンテナの角丸半径</summary>
        private const int KeyContainerBorderRadius = 6;

        /// <summary>キーコンテナの左マージン</summary>
        private const int KeyContainerMarginLeft = 20;

        /// <summary>キーコンテナの右マージン</summary>
        private const int KeyContainerMarginRight = 20;

        /// <summary>リバインドボタンの幅</summary>
        private const int RebindButtonWidth = 220;

        /// <summary>リバインドボタンの高さ</summary>
        private const int RebindButtonHeight = 70;

        #endregion

        #region Fields

        private VisualElement? _root;
        private VisualElement? _overlay;
        private Label? _titleLabel;
        private Label? _rebindingPromptLabel;
        private VisualElement? _bindingsContainer;
        private Button? _resetButton;
        private Button? _closeButton;

        private readonly Dictionary<KeyBindingEntry, BindingRow> _bindingRows = new();
        private UnityEngine.Coroutine? _blinkCoroutine;

        #endregion

        #region Unity Lifecycle

        /// <summary>
        /// 初期化処理
        /// </summary>
        protected override void Awake()
        {
            base.Awake();

            // ViewModelを作成してバインド
            ViewModel = new KeyBindingViewModel();
        }

        /// <summary>
        /// 開始処理
        /// </summary>
        protected virtual void Start()
        {
            // 保存済みのバインディングを読み込み
            ViewModel?.LoadBindings();
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// RootVisualElementが準備できた時に呼び出されます
        /// </summary>
        /// <param name="root">ルートVisualElement</param>
        protected override void OnRootVisualElementReady(VisualElement root)
        {
            base.OnRootVisualElementReady(root);

            // UI要素を取得
            _root = Q<VisualElement>("root");
            _overlay = Q<VisualElement>("Overlay");
            _titleLabel = Q<Label>("TitleLabel");
            _rebindingPromptLabel = Q<Label>("RebindingPromptLabel");
            _bindingsContainer = Q<VisualElement>("BindingsContainer");
            _resetButton = Q<Button>("ResetButton");
            _closeButton = Q<Button>("CloseButton");

            // UI要素の検証
            ValidateUIElements();

            // 初期状態: 非表示
            Hide();

            // イベントハンドラを登録
            RegisterEventHandlers();

            // バインディングリストを生成
            GenerateBindingList();
        }

        /// <summary>
        /// ViewModelとのバインディングを設定します
        /// </summary>
        /// <param name="viewModel">バインドするViewModel</param>
        protected override void BindViewModel(KeyBindingViewModel viewModel)
        {
            base.BindViewModel(viewModel);

            // ViewModelイベントを購読
            viewModel.PropertyChanged += OnViewModelPropertyChanged;
            viewModel.CloseRequested += OnCloseRequested;
            viewModel.BindingUpdated += OnBindingUpdated;
        }

        /// <summary>
        /// ViewModelとのバインディングを解除します
        /// </summary>
        protected override void UnbindViewModel()
        {
            // Stop blink coroutine if running
            if (_blinkCoroutine != null)
            {
                StopCoroutine(_blinkCoroutine);
                _blinkCoroutine = null;
            }

            // イベント購読解除
            if (ViewModel != null)
            {
                ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
                ViewModel.CloseRequested -= OnCloseRequested;
                ViewModel.BindingUpdated -= OnBindingUpdated;
            }

            // イベントハンドラを解除
            UnregisterEventHandlers();

            base.UnbindViewModel();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// UI要素が正しく取得できているか検証します
        /// </summary>
        private void ValidateUIElements()
        {
            if (_root == null)
            {
                Debug.LogWarning("[KeyBindingView] root not found in UXML.", this);
            }

            if (_overlay == null)
            {
                Debug.LogWarning("[KeyBindingView] Overlay not found in UXML.", this);
            }

            if (_titleLabel == null)
            {
                Debug.LogWarning("[KeyBindingView] TitleLabel not found in UXML.", this);
            }

            if (_rebindingPromptLabel == null)
            {
                Debug.LogWarning("[KeyBindingView] RebindingPromptLabel not found in UXML.", this);
            }

            if (_bindingsContainer == null)
            {
                Debug.LogWarning("[KeyBindingView] BindingsContainer not found in UXML.", this);
            }

            if (_resetButton == null)
            {
                Debug.LogWarning("[KeyBindingView] ResetButton not found in UXML.", this);
            }

            if (_closeButton == null)
            {
                Debug.LogWarning("[KeyBindingView] CloseButton not found in UXML.", this);
            }
        }

        /// <summary>
        /// イベントハンドラを登録します
        /// </summary>
        private void RegisterEventHandlers()
        {
            if (_resetButton != null)
            {
                _resetButton.clicked += OnResetButtonClicked;
            }

            if (_closeButton != null)
            {
                _closeButton.clicked += OnCloseButtonClicked;
            }
        }

        /// <summary>
        /// イベントハンドラを解除します
        /// </summary>
        private void UnregisterEventHandlers()
        {
            if (_resetButton != null)
            {
                _resetButton.clicked -= OnResetButtonClicked;
            }

            if (_closeButton != null)
            {
                _closeButton.clicked -= OnCloseButtonClicked;
            }
        }

        /// <summary>
        /// バインディングリストを生成します
        /// </summary>
        private void GenerateBindingList()
        {
            if (_bindingsContainer == null || ViewModel == null)
            {
                return;
            }

            // 既存の行をクリア
            _bindingsContainer.Clear();
            _bindingRows.Clear();

            // 各バインディングエントリに対して行を生成
            foreach (var entry in ViewModel.Bindings)
            {
                var row = CreateBindingRow(entry);
                _bindingsContainer.Add(row.Container);
                _bindingRows[entry] = row;
            }
        }

        /// <summary>
        /// バインディング行を作成します
        /// </summary>
        private BindingRow CreateBindingRow(KeyBindingEntry entry)
        {
            // 行コンテナ
            var rowContainer = new VisualElement();
            rowContainer.AddToClassList("binding-row");
            rowContainer.style.flexDirection = FlexDirection.Row;
            rowContainer.style.alignItems = Align.Center;
            rowContainer.style.justifyContent = Justify.SpaceBetween;
            rowContainer.style.paddingTop = RowPaddingVertical;
            rowContainer.style.paddingBottom = RowPaddingVertical;
            rowContainer.style.paddingLeft = RowPaddingHorizontal;
            rowContainer.style.paddingRight = RowPaddingHorizontal;
            rowContainer.style.marginBottom = RowMarginBottom;
            rowContainer.style.backgroundColor = RowBackgroundNormal;
            rowContainer.style.borderTopLeftRadius = RowBorderRadius;
            rowContainer.style.borderTopRightRadius = RowBorderRadius;
            rowContainer.style.borderBottomLeftRadius = RowBorderRadius;
            rowContainer.style.borderBottomRightRadius = RowBorderRadius;

            // アクション名ラベル
            var actionLabel = new Label(entry.ActionName);
            actionLabel.AddToClassList("binding-action-name");
            actionLabel.style.unityFontDefinition = new StyleFontDefinition(Resources.Load<Font>("FlatSkin/Font/Roboto-Medium"));
            actionLabel.style.fontSize = ActionLabelFontSize;
            actionLabel.style.color = ActionLabelColor;
            actionLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            actionLabel.style.minWidth = ActionLabelMinWidth;
            actionLabel.style.flexGrow = 1;

            // 現在のキーラベルコンテナ（背景付き）
            var keyContainer = new VisualElement();
            keyContainer.style.backgroundColor = KeyContainerBackground;
            keyContainer.style.paddingTop = KeyContainerPaddingVertical;
            keyContainer.style.paddingBottom = KeyContainerPaddingVertical;
            keyContainer.style.paddingLeft = KeyContainerPaddingHorizontal;
            keyContainer.style.paddingRight = KeyContainerPaddingHorizontal;
            keyContainer.style.borderTopLeftRadius = KeyContainerBorderRadius;
            keyContainer.style.borderTopRightRadius = KeyContainerBorderRadius;
            keyContainer.style.borderBottomLeftRadius = KeyContainerBorderRadius;
            keyContainer.style.borderBottomRightRadius = KeyContainerBorderRadius;
            keyContainer.style.minWidth = KeyContainerMinWidth;
            keyContainer.style.marginLeft = KeyContainerMarginLeft;
            keyContainer.style.marginRight = KeyContainerMarginRight;

            var keyLabel = new Label(entry.CurrentBinding);
            keyLabel.AddToClassList("binding-current-key");
            keyLabel.style.unityFontDefinition = new StyleFontDefinition(Resources.Load<Font>("FlatSkin/Font/Roboto-Medium"));
            keyLabel.style.fontSize = KeyLabelFontSize;
            keyLabel.style.color = KeyLabelColor;
            keyLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            keyLabel.style.unityTextAlign = TextAnchor.MiddleCenter;

            keyContainer.Add(keyLabel);

            // リバインドボタン
            var rebindButton = new Button(() => OnRebindButtonClicked(entry));
            rebindButton.text = "Rebind";
            rebindButton.AddToClassList("binding-rebind-button");
            rebindButton.style.unityFontDefinition = new StyleFontDefinition(Resources.Load<Font>("FlatSkin/Font/Roboto-Medium"));
            rebindButton.style.fontSize = RebindButtonFontSize;
            rebindButton.style.width = RebindButtonWidth;
            rebindButton.style.height = RebindButtonHeight;
            rebindButton.style.unityFontStyleAndWeight = FontStyle.Bold;

            // 要素を行に追加
            rowContainer.Add(actionLabel);
            rowContainer.Add(keyContainer);
            rowContainer.Add(rebindButton);

            return new BindingRow
            {
                Container = rowContainer,
                ActionLabel = actionLabel,
                KeyLabel = keyLabel,
                RebindButton = rebindButton
            };
        }

        /// <summary>
        /// ポップアップを表示します
        /// </summary>
        public void Show()
        {
            if (_root != null)
            {
                _root.style.display = DisplayStyle.Flex;
                _root.pickingMode = PickingMode.Position;
            }

            if (_overlay != null)
            {
                _overlay.style.display = DisplayStyle.Flex;
                _overlay.pickingMode = PickingMode.Position;
            }
        }

        /// <summary>
        /// ポップアップを非表示にします
        /// </summary>
        public void Hide()
        {
            if (_root != null)
            {
                _root.style.display = DisplayStyle.None;
                _root.pickingMode = PickingMode.Ignore;
            }

            if (_overlay != null)
            {
                _overlay.style.display = DisplayStyle.None;
                _overlay.pickingMode = PickingMode.Ignore;
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// ViewModelのプロパティ変更イベントを処理します
        /// </summary>
        private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (ViewModel == null)
            {
                return;
            }

            switch (e.PropertyName)
            {
                case nameof(KeyBindingViewModel.IsRebinding):
                    UpdateRebindingPrompt();
                    break;

                case nameof(KeyBindingViewModel.RebindingPrompt):
                    UpdateRebindingPrompt();
                    break;

                case nameof(KeyBindingViewModel.CurrentRebindingEntry):
                    UpdateRebindingHighlight();
                    break;
            }
        }

        /// <summary>
        /// リバインディングプロンプトを更新します
        /// </summary>
        private void UpdateRebindingPrompt()
        {
            if (_rebindingPromptLabel == null || ViewModel == null)
            {
                return;
            }

            if (ViewModel.IsRebinding)
            {
                _rebindingPromptLabel.text = ViewModel.RebindingPrompt;
                _rebindingPromptLabel.style.opacity = BlinkOpacityBright;

                // Start blink effect
                if (_blinkCoroutine != null)
                {
                    StopCoroutine(_blinkCoroutine);
                }
                _blinkCoroutine = StartCoroutine(BlinkPrompt());
            }
            else
            {
                // Stop blink effect
                if (_blinkCoroutine != null)
                {
                    StopCoroutine(_blinkCoroutine);
                    _blinkCoroutine = null;
                }

                _rebindingPromptLabel.style.opacity = OpacityHidden;
            }
        }

        /// <summary>
        /// プロンプトを点滅させるコルーチン
        /// </summary>
        private System.Collections.IEnumerator BlinkPrompt()
        {
            if (_rebindingPromptLabel == null)
            {
                yield break;
            }

            float elapsedTime = 0f;
            bool isBright = true;

            while (true)
            {
                elapsedTime += Time.deltaTime;

                if (elapsedTime >= BlinkInterval)
                {
                    isBright = !isBright;

                    if (isBright)
                    {
                        _rebindingPromptLabel.style.opacity = BlinkOpacityBright;
                        _rebindingPromptLabel.style.backgroundColor = PromptBackgroundBright;
                        _rebindingPromptLabel.style.borderTopColor = PromptBorderBright;
                        _rebindingPromptLabel.style.borderBottomColor = PromptBorderBright;
                        _rebindingPromptLabel.style.borderLeftColor = PromptBorderBright;
                        _rebindingPromptLabel.style.borderRightColor = PromptBorderBright;
                    }
                    else
                    {
                        _rebindingPromptLabel.style.opacity = BlinkOpacityDim;
                        _rebindingPromptLabel.style.backgroundColor = PromptBackgroundDim;
                        _rebindingPromptLabel.style.borderTopColor = PromptBorderDim;
                        _rebindingPromptLabel.style.borderBottomColor = PromptBorderDim;
                        _rebindingPromptLabel.style.borderLeftColor = PromptBorderDim;
                        _rebindingPromptLabel.style.borderRightColor = PromptBorderDim;
                    }

                    elapsedTime = 0f;
                }

                yield return null;
            }
        }

        /// <summary>
        /// リバインディング中の行のハイライトを更新します
        /// </summary>
        private void UpdateRebindingHighlight()
        {
            if (ViewModel == null)
            {
                return;
            }

            // すべての行のハイライトをクリア
            foreach (var kvp in _bindingRows)
            {
                var row = kvp.Value;
                row.Container.style.backgroundColor = RowBackgroundNormal;
                row.Container.style.borderTopWidth = 0;
                row.Container.style.borderBottomWidth = 0;
                row.Container.style.borderLeftWidth = 0;
                row.Container.style.borderRightWidth = 0;
            }

            // 現在リバインディング中の行をハイライト
            if (ViewModel.CurrentRebindingEntry != null && _bindingRows.TryGetValue(ViewModel.CurrentRebindingEntry, out var rebindingRow))
            {
                rebindingRow.Container.style.backgroundColor = RowBackgroundHighlight;
                rebindingRow.Container.style.borderTopWidth = RowHighlightBorderWidth;
                rebindingRow.Container.style.borderBottomWidth = RowHighlightBorderWidth;
                rebindingRow.Container.style.borderLeftWidth = RowHighlightBorderWidth;
                rebindingRow.Container.style.borderRightWidth = RowHighlightBorderWidth;
                rebindingRow.Container.style.borderTopColor = RowBorderHighlight;
                rebindingRow.Container.style.borderBottomColor = RowBorderHighlight;
                rebindingRow.Container.style.borderLeftColor = RowBorderHighlight;
                rebindingRow.Container.style.borderRightColor = RowBorderHighlight;
            }
        }

        /// <summary>
        /// バインディング更新イベントを処理します
        /// </summary>
        private void OnBindingUpdated(object? sender, KeyBindingEntry e)
        {
            if (_bindingRows.TryGetValue(e, out var row))
            {
                row.KeyLabel.text = e.CurrentBinding;
            }
        }

        /// <summary>
        /// 閉じる要求イベントを処理します
        /// </summary>
        private void OnCloseRequested(object? sender, System.EventArgs e)
        {
            Hide();
        }

        /// <summary>
        /// リバインドボタンがクリックされた時の処理
        /// </summary>
        private void OnRebindButtonClicked(KeyBindingEntry entry)
        {
            ViewModel?.StartRebindCommand.Execute(entry);
        }

        /// <summary>
        /// リセットボタンがクリックされた時の処理
        /// </summary>
        private void OnResetButtonClicked()
        {
            ViewModel?.ResetToDefaultCommand.Execute(null);
        }

        /// <summary>
        /// 閉じるボタンがクリックされた時の処理
        /// </summary>
        private void OnCloseButtonClicked()
        {
            ViewModel?.CloseCommand.Execute(null);
        }

        #endregion

        #region Nested Classes

        /// <summary>
        /// バインディング行のUI要素を保持するクラス
        /// </summary>
        private class BindingRow
        {
            public VisualElement Container { get; set; } = null!;
            public Label ActionLabel { get; set; } = null!;
            public Label KeyLabel { get; set; } = null!;
            public Button RebindButton { get; set; } = null!;
        }

        #endregion
    }
}
