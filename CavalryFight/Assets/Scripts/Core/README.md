# Core - MVVMフレームワーク

## 概要
CavalryFightプロジェクトで使用するMVVM（Model-View-ViewModel）パターンの基盤システムです。

## フォルダ構成

```
Core/
├── MVVM/                    # MVVM基本クラス
│   ├── ViewModelBase.cs    # ViewModelの基底クラス
│   ├── ViewBase.cs         # Viewの基底クラス
│   └── PropertyBinding.cs  # プロパティバインディングシステム
├── Commands/                # コマンドシステム
│   ├── ICommand.cs         # コマンドインターフェース
│   └── RelayCommand.cs     # 汎用コマンド実装
└── Services/                # サービスシステム
    ├── IService.cs         # サービスインターフェース
    └── ServiceLocator.cs   # サービスロケーター
```

---

## 使用方法

### 1. Modelの作成

Modelはデータとビジネスロジックのみを持ちます。UIに依存しません。

```csharp
namespace CavalryFight.Models.Player
{
    public class PlayerHealthModel
    {
        private int _currentHealth;
        private int _maxHealth;

        public int CurrentHealth
        {
            get => _currentHealth;
            set => _currentHealth = System.Math.Clamp(value, 0, _maxHealth);
        }

        public int MaxHealth
        {
            get => _maxHealth;
            set => _maxHealth = System.Math.Max(1, value);
        }

        public float HealthRatio => _maxHealth > 0 ? (float)_currentHealth / _maxHealth : 0f;

        public int TakeDamage(int damage)
        {
            int previousHealth = _currentHealth;
            _currentHealth = System.Math.Max(0, _currentHealth - damage);
            return previousHealth - _currentHealth;
        }
    }
}
```

### 2. ViewModelの作成

ViewModelはModelとViewの橋渡しをします。`ViewModelBase`を継承します。

```csharp
#nullable enable

using CavalryFight.Core.MVVM;
using CavalryFight.Core.Commands;

namespace CavalryFight.ViewModels.Player
{
    public class PlayerHealthViewModel : ViewModelBase
    {
        #region Fields

        private readonly PlayerHealthModel _model;
        private string _statusMessage;

        #endregion

        #region Properties

        public int CurrentHealth => _model.CurrentHealth;
        public int MaxHealth => _model.MaxHealth;
        public float HealthRatio => _model.HealthRatio;

        public string StatusMessage
        {
            get => _statusMessage;
            private set => SetProperty(ref _statusMessage, value);
        }

        #endregion

        #region Commands

        public ICommand TakeDamageCommand { get; }

        #endregion

        #region Constructor

        public PlayerHealthViewModel(PlayerHealthModel model)
        {
            _model = model ?? throw new System.ArgumentNullException(nameof(model));
            _statusMessage = "Ready";

            // コマンドの初期化
            TakeDamageCommand = new RelayCommand<int?>(
                execute: damage => OnTakeDamage(damage ?? 10),
                canExecute: _ => CurrentHealth > 0
            );
        }

        #endregion

        #region Private Methods

        private void OnTakeDamage(int damage)
        {
            _model.TakeDamage(damage);
            StatusMessage = $"Took {damage} damage!";

            // プロパティ変更を通知
            OnPropertiesChanged(
                nameof(CurrentHealth),
                nameof(HealthRatio)
            );

            // コマンドの実行可能状態を更新
            (TakeDamageCommand as RelayCommand<int?>)?.RaiseCanExecuteChanged();
        }

        #endregion
    }
}
```

### 3. Viewの作成

ViewはUIの表示と更新を担当します。`ViewBase<TViewModel>`を継承します。

```csharp
#nullable enable

using CavalryFight.Core.MVVM;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CavalryFight.Views.Player
{
    public class PlayerHealthView : ViewBase<PlayerHealthViewModel>
    {
        #region Inspector Fields

        [SerializeField] private Slider? healthSlider;
        [SerializeField] private TextMeshProUGUI? healthText;
        [SerializeField] private Button? takeDamageButton;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            // ViewModelの作成
            var model = new PlayerHealthModel(100);
            ViewModel = new PlayerHealthViewModel(model);
        }

        #endregion

        #region Protected Methods

        protected override void BindViewModel(PlayerHealthViewModel viewModel)
        {
            base.BindViewModel(viewModel);

            // プロパティバインディング
            if (healthSlider != null)
            {
                AddBinding(viewModel.Bind(
                    nameof(viewModel.HealthRatio),
                    () => viewModel.HealthRatio,
                    value => healthSlider.value = value
                ));
            }

            if (healthText != null)
            {
                AddBinding(viewModel.Bind(
                    nameof(viewModel.CurrentHealth),
                    () => viewModel.CurrentHealth,
                    _ => healthText.text = $"{viewModel.CurrentHealth}/{viewModel.MaxHealth}"
                ));
            }

            // コマンドバインディング
            if (takeDamageButton != null)
            {
                takeDamageButton.onClick.AddListener(() =>
                {
                    if (viewModel.TakeDamageCommand.CanExecute(10))
                    {
                        viewModel.TakeDamageCommand.Execute(10);
                    }
                });

                AddBinding(viewModel.Bind(
                    nameof(viewModel.CurrentHealth),
                    () => viewModel.CurrentHealth,
                    _ => takeDamageButton.interactable = viewModel.CurrentHealth > 0
                ));
            }
        }

        protected override void OnViewModelUnbound()
        {
            base.OnViewModelUnbound();
            takeDamageButton?.onClick.RemoveAllListeners();
        }

        #endregion
    }
}
```

### 4. Serviceの作成

Serviceは複数のViewModelから共有されるビジネスロジックを提供します。

```csharp
#nullable enable

using CavalryFight.Core.Services;

namespace CavalryFight.Services.Player
{
    public class PlayerHealthService : IService
    {
        #region Fields

        private PlayerHealthModel? _playerHealth;

        #endregion

        #region IService Implementation

        public void Initialize()
        {
            _playerHealth = new PlayerHealthModel(100);
        }

        public void Dispose()
        {
            _playerHealth = null;
        }

        #endregion

        #region Public Methods

        public void ResetHealth()
        {
            _playerHealth?.RestoreToFull();
        }

        #endregion
    }
}
```

### 5. ServiceLocatorの使用

アプリケーション起動時にサービスを登録します。

```csharp
using CavalryFight.Core.Services;
using UnityEngine;

public class GameInitializer : MonoBehaviour
{
    private void Awake()
    {
        // サービスを登録
        ServiceLocator.Instance.Register(new PlayerHealthService());
        ServiceLocator.Instance.Register(new AudioService());
        ServiceLocator.Instance.Register(new SaveDataService());

        // すべてのサービスを初期化
        ServiceLocator.Instance.Initialize();
    }

    private void OnApplicationQuit()
    {
        // アプリケーション終了時にすべてのサービスを破棄
        ServiceLocator.Instance.Shutdown();
    }
}
```

ViewModelからサービスを取得：

```csharp
public class PlayerViewModel : ViewModelBase
{
    private readonly PlayerHealthService _healthService;

    public PlayerViewModel()
    {
        // ServiceLocatorからサービスを取得
        _healthService = ServiceLocator.Instance.Get<PlayerHealthService>();
    }
}
```

---

## 主要クラスの説明

### ViewModelBase

すべてのViewModelが継承する基底クラスです。

#### 主要機能
- `INotifyPropertyChanged`の実装
- `SetProperty<T>()`: プロパティ変更と通知
- `OnPropertyChanged()`: プロパティ変更通知の発行
- `OnPropertiesChanged()`: 複数プロパティの一括通知
- `IDisposable`の実装

#### 使用例
```csharp
private string _name;
public string Name
{
    get => _name;
    set => SetProperty(ref _name, value);
}

// コールバック付き
private int _score;
public int Score
{
    get => _score;
    set => SetProperty(ref _score, value, () => Debug.Log("Score changed!"));
}
```

### ViewBase<TViewModel>

すべてのViewが継承する基底クラスです。

#### 主要機能
- ViewModelとの自動バインディング
- ライフサイクル管理
- バインディングの自動破棄

#### オーバーライド可能なメソッド
- `OnInitialize()`: 初期化処理
- `BindViewModel()`: ViewModelのバインディング設定
- `OnViewModelBound()`: バインド後の処理
- `UnbindViewModel()`: バインディング解除
- `OnViewModelUnbound()`: バインド解除後の処理

### PropertyBinding<TValue>

ViewModelのプロパティ変更を監視し、自動的にViewを更新します。

#### 使用例
```csharp
// 拡張メソッドを使用
AddBinding(viewModel.Bind(
    nameof(viewModel.Health),
    () => viewModel.Health,
    value => healthText.text = value.ToString()
));

// 直接作成
var binding = new PropertyBinding<int>(
    viewModel,
    nameof(viewModel.Health),
    () => viewModel.Health,
    value => healthText.text = value.ToString()
);
AddBinding(binding);
```

### ICommand / RelayCommand

コマンドパターンの実装です。

#### 使用例
```csharp
// パラメータなし
public ICommand SaveCommand { get; }

SaveCommand = new RelayCommand(
    execute: OnSave,
    canExecute: () => HasChanges
);

// パラメータあり
public ICommand SelectItemCommand { get; }

SelectItemCommand = new RelayCommand<int>(
    execute: itemId => OnSelectItem(itemId),
    canExecute: itemId => itemId >= 0
);

// 実行可能状態の更新
(SaveCommand as RelayCommand)?.RaiseCanExecuteChanged();
```

### ServiceLocator

サービスを一元管理します。

#### 主要メソッド
- `Register<TService>()`: サービスを登録
- `RegisterFactory<TService>()`: 遅延初期化用のファクトリー登録
- `Get<TService>()`: サービスを取得
- `TryGet<TService>()`: サービスを取得（失敗時はnull）
- `IsRegistered<TService>()`: 登録確認
- `Unregister<TService>()`: 登録解除
- `Initialize()`: すべてのサービスを初期化
- `InitializeAsync()`: 非同期初期化
- `Shutdown()`: すべてのサービスを破棄

---

## ベストプラクティス

### 1. Nullable Reference Typesの使用

```csharp
#nullable enable

public class ExampleViewModel : ViewModelBase
{
    private string? _nullableText;  // nullを許可
    private string _nonNullText = "";  // nullを許可しない
}
```

### 2. Regionsによる整理

```csharp
public class ExampleViewModel : ViewModelBase
{
    #region Fields
    // フィールド
    #endregion

    #region Properties
    // プロパティ
    #endregion

    #region Commands
    // コマンド
    #endregion

    #region Constructor
    // コンストラクタ
    #endregion

    #region Public Methods
    // パブリックメソッド
    #endregion

    #region Private Methods
    // プライベートメソッド
    #endregion

    #region Protected Methods
    // プロテクテッドメソッド
    #endregion
}
```

### 3. 命名規則

- プライベートフィールド: `_camelCase`
- プロパティ: `PascalCase`
- コマンド: `{動詞}Command` (例: `SaveCommand`, `AttackCommand`)
- メソッド: `On{イベント名}` (例: `OnSave`, `OnAttack`)

### 4. XMLドキュメントコメント

```csharp
/// <summary>
/// プレイヤーの体力を管理するViewModel
/// </summary>
/// <remarks>
/// 体力の表示、ダメージ処理、回復処理を提供します。
/// </remarks>
public class PlayerHealthViewModel : ViewModelBase
{
    /// <summary>
    /// 現在の体力を取得します。
    /// </summary>
    public int CurrentHealth => _model.CurrentHealth;
}
```

### 5. パフォーマンス最適化

```csharp
// ✅ 良い例: 値が変更された時のみ通知
public string Name
{
    get => _name;
    set => SetProperty(ref _name, value);  // 変更時のみ通知
}

// ❌ 悪い例: 毎回通知
public string Name
{
    get => _name;
    set
    {
        _name = value;
        OnPropertyChanged(nameof(Name));  // 常に通知
    }
}
```

---

## サンプルコード

完全な実装例は `Scripts/Examples/PlayerHealth/` フォルダを参照してください。

- `PlayerHealthModel.cs`: Modelの実装例
- `PlayerHealthViewModel.cs`: ViewModelの実装例
- `PlayerHealthView.cs`: Viewの実装例
- `PlayerHealthService.cs`: Serviceの実装例

---

## 注意事項

1. **ViewからModelに直接アクセスしない**
   - 必ずViewModelを経由してください

2. **ViewModelにUnity固有のクラスを含めない**
   - ViewModelはピュアなC#クラスにしてください
   - `GameObject`, `Transform`, `MonoBehaviour`などは使用しない

3. **バインディングは必ずAddBindingで登録**
   - 自動的に破棄されるため、メモリリークを防げます

4. **コマンドの実行可能状態を適切に管理**
   - プロパティ変更時に`RaiseCanExecuteChanged()`を呼び出してください

5. **Serviceは共有状態の管理に使用**
   - View固有の状態はViewModelで管理してください

---

## トラブルシューティング

### プロパティ変更が反映されない

原因: `SetProperty`を使っていない、または`OnPropertyChanged`を呼んでいない

解決策:
```csharp
// ✅ 正しい
public int Health
{
    get => _health;
    set => SetProperty(ref _health, value);
}

// ❌ 間違い
public int Health { get; set; }
```

### コマンドが実行されない

原因: `CanExecute`がfalseを返している

解決策: `CanExecute`の条件を確認し、必要に応じて`RaiseCanExecuteChanged()`を呼び出す

### メモリリーク

原因: バインディングが破棄されていない

解決策: `AddBinding()`を使用してバインディングを登録する

---

## 参考資料

- [Docs/CODING_RULES.md](../../../Docs/CODING_RULES.md) - コーディング規則
- [Docs/THIRD_PARTY_ASSETS.md](../../../Docs/THIRD_PARTY_ASSETS.md) - サードパーティアセット
