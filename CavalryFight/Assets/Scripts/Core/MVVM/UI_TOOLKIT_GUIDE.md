# UI Toolkit バインディングガイド

## 概要
CavalryFightプロジェクトでのUI Toolkit使用ガイドです。
ViewModelとUI Toolkitを簡単にバインドする方法を説明します。

---

## 基本的な使い方

### 1. Viewクラスの作成

`UIToolkitViewBase<TViewModel>` を継承してViewを作成します。

```csharp
#nullable enable

using CavalryFight.Core.MVVM;
using CavalryFight.ViewModels.UI;
using UnityEngine.UIElements;

namespace CavalryFight.Views.UI
{
    /// <summary>
    /// メインメニューのView
    /// </summary>
    public class MainMenuView : UIToolkitViewBase<MainMenuViewModel>
    {
        #region Fields

        private Button? _startButton;
        private Button? _settingsButton;
        private Button? _quitButton;
        private Label? _versionLabel;

        #endregion

        #region Protected Methods

        /// <summary>
        /// RootVisualElementが準備できた時に呼び出されます
        /// </summary>
        protected override void OnRootVisualElementReady(VisualElement root)
        {
            // UI要素を取得
            _startButton = Q<Button>("start-button");
            _settingsButton = Q<Button>("settings-button");
            _quitButton = Q<Button>("quit-button");
            _versionLabel = Q<Label>("version-label");

            // null チェック
            if (_startButton == null)
            {
                Debug.LogError("start-button not found in UXML!");
            }

            if (_settingsButton == null)
            {
                Debug.LogError("settings-button not found in UXML!");
            }

            if (_quitButton == null)
            {
                Debug.LogError("quit-button not found in UXML!");
            }
        }

        /// <summary>
        /// ViewModelバインド時に呼び出されます
        /// </summary>
        protected override void BindViewModel(MainMenuViewModel viewModel)
        {
            base.BindViewModel(viewModel);

            // Buttonをコマンドにバインド
            if (_startButton != null)
            {
                AddBinding(_startButton.BindCommand(viewModel.StartGameCommand));
            }

            if (_settingsButton != null)
            {
                AddBinding(_settingsButton.BindCommand(viewModel.OpenSettingsCommand));
            }

            if (_quitButton != null)
            {
                AddBinding(_quitButton.BindCommand(viewModel.QuitCommand));
            }

            // Labelをプロパティにバインド
            if (_versionLabel != null)
            {
                AddBinding(_versionLabel.BindText(
                    viewModel,
                    nameof(viewModel.VersionText),
                    () => viewModel.VersionText
                ));
            }
        }

        #endregion
    }
}
```

---

## バインディングメソッド

### BindCommand - Buttonをコマンドにバインド

```csharp
// ボタンをコマンドにバインド
AddBinding(_startButton.BindCommand(viewModel.StartGameCommand));

// コマンドのCanExecuteに応じて自動的にボタンが有効/無効になります
```

### BindText - Labelをプロパティにバインド

```csharp
// シンプルなバインディング
AddBinding(_nameLabel.BindText(
    viewModel,
    nameof(viewModel.PlayerName),
    () => viewModel.PlayerName
));

// カスタム変換を使用
AddBinding(_healthLabel.BindText(
    viewModel,
    nameof(viewModel.CurrentHealth),
    () => viewModel.CurrentHealth,
    health => $"HP: {health}/100"
));
```

### BindVisibility - 表示/非表示をプロパティにバインド

```csharp
// bool値で表示/非表示を制御
AddBinding(_loadingPanel.BindVisibility(
    viewModel,
    nameof(viewModel.IsLoading),
    () => viewModel.IsLoading
));
```

### BindValue - ProgressBarをプロパティにバインド

```csharp
// 0.0～1.0の値をProgressBarにバインド
AddBinding(_healthBar.BindValue(
    viewModel,
    nameof(viewModel.HealthRatio),
    () => viewModel.HealthRatio
));
```

---

## ViewModelの作成例

```csharp
#nullable enable

using CavalryFight.Core.MVVM;
using CavalryFight.Core.Commands;

namespace CavalryFight.ViewModels.UI
{
    public class MainMenuViewModel : ViewModelBase
    {
        #region Fields

        private string _versionText;
        private bool _isLoading;

        #endregion

        #region Properties

        public string VersionText
        {
            get => _versionText;
            set => SetProperty(ref _versionText, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        #endregion

        #region Commands

        public ICommand StartGameCommand { get; }
        public ICommand OpenSettingsCommand { get; }
        public ICommand QuitCommand { get; }

        #endregion

        #region Constructor

        public MainMenuViewModel()
        {
            _versionText = "v1.0.0";
            _isLoading = false;

            StartGameCommand = new RelayCommand(
                execute: OnStartGame,
                canExecute: () => !IsLoading
            );

            OpenSettingsCommand = new RelayCommand(OnOpenSettings);
            QuitCommand = new RelayCommand(OnQuit);
        }

        #endregion

        #region Private Methods

        private void OnStartGame()
        {
            // ゲーム開始処理
            Debug.Log("Start Game!");
        }

        private void OnOpenSettings()
        {
            // 設定画面を開く
            Debug.Log("Open Settings!");
        }

        private void OnQuit()
        {
            // ゲーム終了
            Application.Quit();
        }

        #endregion
    }
}
```

---

## UXMLファイルの例

```xml
<ui:UXML xmlns:ui="UnityEngine.UIElements">
    <ui:VisualElement name="main-menu-container" class="main-menu">
        <ui:Label name="title-label" text="CAVALRY FIGHT" class="title" />
        <ui:Label name="version-label" text="v1.0.0" class="version" />

        <ui:VisualElement name="button-container" class="button-group">
            <ui:Button name="start-button" text="Start Game" class="menu-button" />
            <ui:Button name="settings-button" text="Settings" class="menu-button" />
            <ui:Button name="quit-button" text="Quit" class="menu-button" />
        </ui:VisualElement>

        <ui:VisualElement name="loading-panel" class="loading-panel">
            <ui:Label text="Loading..." />
            <ui:ProgressBar name="loading-progress" />
        </ui:VisualElement>
    </ui:VisualElement>
</ui:UXML>
```

---

## GameObjectのセットアップ

1. 空のGameObjectを作成（名前: "MainMenuView"）
2. `UIDocument` コンポーネントをアタッチ
3. UIDocumentに作成したUXMLファイルを設定
4. `MainMenuView` スクリプトをアタッチ
5. Awake時にViewModelが自動作成されるか、手動で設定

```csharp
// Awakeで自動作成する場合
protected override void OnInitialize()
{
    ViewModel = new MainMenuViewModel();
}
```

---

## ベストプラクティス

### 1. UI要素の取得はOnRootVisualElementReadyで行う
```csharp
protected override void OnRootVisualElementReady(VisualElement root)
{
    _button = Q<Button>("my-button");

    if (_button == null)
    {
        Debug.LogError("my-button not found!");
    }
}
```

### 2. バインディングは必ずAddBindingで登録
```csharp
// ✅ 正しい - Dispose時に自動解除される
AddBinding(_button.BindCommand(viewModel.Command));

// ❌ 間違い - メモリリーク発生
_button.BindCommand(viewModel.Command);
```

### 3. ViewModelのプロパティ変更はSetPropertyを使用
```csharp
// ✅ 正しい - 自動的に通知される
public string Name
{
    get => _name;
    set => SetProperty(ref _name, value);
}

// ❌ 間違い - UIが更新されない
public string Name
{
    get => _name;
    set => _name = value;
}
```

### 4. コマンドのCanExecuteを活用
```csharp
// ボタンの有効/無効を自動制御
StartCommand = new RelayCommand(
    execute: OnStart,
    canExecute: () => !IsLoading && IsValid
);

// 状態変更時にCanExecuteChangedを発行
private void OnLoadingChanged()
{
    (StartCommand as RelayCommand)?.RaiseCanExecuteChanged();
}
```

---

## トラブルシューティング

### UI要素が見つからない
- UXMLでnameが正しく設定されているか確認
- Q<T>()の要素名が一致しているか確認
- OnRootVisualElementReadyが呼ばれているか確認

### バインディングが動作しない
- AddBinding()で登録しているか確認
- ViewModelのプロパティでSetProperty()を使用しているか確認
- PropertyChangedイベントが発火しているか確認

### ボタンが反応しない
- UIDocumentがアタッチされているか確認
- BindCommand()が正しく呼ばれているか確認
- コマンドのCanExecute()がtrueを返しているか確認

---

## 参照
- `Core/MVVM/UIToolkitViewBase.cs` - View基底クラス
- `Core/MVVM/UIToolkitBindingHelpers.cs` - バインディングヘルパー
- `Core/MVVM/ViewModelBase.cs` - ViewModel基底クラス
- `Core/Commands/RelayCommand.cs` - コマンド実装
