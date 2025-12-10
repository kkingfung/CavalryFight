# Examples - MVVMフレームワーク使用例

## 概要
このフォルダには、MVVMフレームワークの実装例が含まれています。
実際のプロジェクトで使用するための参考として活用してください。

---

## PlayerHealth Example

プレイヤーの体力システムを実装した完全な例です。

### フォルダ構成
```
PlayerHealth/
├── PlayerHealthModel.cs       # データモデル
├── PlayerHealthViewModel.cs   # ビューモデル
├── PlayerHealthView.cs        # ビュー
└── PlayerHealthService.cs     # サービス
```

### 実装内容

#### 1. PlayerHealthModel
体力データとビジネスロジックを管理します。

**主要機能:**
- 現在の体力と最大体力の管理
- ダメージ処理
- 回復処理
- 体力割合の計算

#### 2. PlayerHealthViewModel
ModelとViewの橋渡しをします。

**主要機能:**
- プロパティ: `CurrentHealth`, `MaxHealth`, `HealthRatio`, `IsAlive`, `StatusMessage`
- コマンド: `TakeDamageCommand`, `HealCommand`, `RestoreToFullCommand`
- 自動的なプロパティ変更通知
- コマンドの実行可能状態管理

#### 3. PlayerHealthView
UIの表示と更新を担当します。

**主要機能:**
- Slider（体力バー）のバインディング
- TextMeshPro（体力テキスト）のバインディング
- ボタンクリックとコマンドの接続
- UIの自動更新

#### 4. PlayerHealthService
グローバルなプレイヤー体力状態を管理します。

**主要機能:**
- プレイヤー体力の作成
- 体力のリセット
- 複数のViewModelからアクセス可能

---

## Unityでのセットアップ方法

### 1. UI作成

1. Hierarchyで右クリック → UI → Canvas
2. Canvas内に以下を作成:
   - Slider (名前: HealthSlider)
   - TextMeshPro - Text (名前: HealthText)
   - TextMeshPro - Text (名前: StatusText)
   - Button (名前: TakeDamageButton)
   - Button (名前: HealButton)
   - Button (名前: RestoreButton)

### 2. Viewコンポーネントの追加

1. CanvasにPlayerHealthViewコンポーネントをアタッチ
2. Inspectorで以下を設定:
   - Health Slider: HealthSliderをドラッグ&ドロップ
   - Health Text: HealthTextをドラッグ&ドロップ
   - Status Text: StatusTextをドラッグ&ドロップ
   - Take Damage Button: TakeDamageButtonをドラッグ&ドロップ
   - Heal Button: HealButtonをドラッグ&ドロップ
   - Restore Button: RestoreButtonをドラッグ&ドロップ
3. 設定値を調整:
   - Max Health: 100
   - Damage Amount: 10
   - Heal Amount: 20

### 3. Sliderの設定

1. HealthSliderを選択
2. Inspectorで設定:
   - Min Value: 0
   - Max Value: 1
   - Whole Numbers: オフ
   - Value: 1

### 4. 実行

Play Modeで実行すると、以下の動作が確認できます:

- **Take Damageボタン**: 体力が10減少
- **Healボタン**: 体力が20回復
- **Restoreボタン**: 体力が完全回復
- 体力バーがリアルタイムで更新
- 体力テキストが自動更新
- ステータスメッセージが表示
- 体力が0になるとボタンが無効化

---

## コードの主要ポイント

### 1. プロパティバインディング

```csharp
// ViewModelのプロパティが変更されると自動的にUIが更新される
AddBinding(viewModel.Bind(
    nameof(viewModel.HealthRatio),
    () => viewModel.HealthRatio,
    value => healthSlider.value = value
));
```

### 2. コマンドバインディング

```csharp
// ボタンクリックでコマンドを実行
takeDamageButton.onClick.AddListener(() =>
{
    if (viewModel.TakeDamageCommand.CanExecute(damageAmount))
    {
        viewModel.TakeDamageCommand.Execute(damageAmount);
    }
});
```

### 3. 複数プロパティの一括通知

```csharp
// 複数のプロパティが変更されたことを一度に通知
OnPropertiesChanged(
    nameof(CurrentHealth),
    nameof(HealthRatio),
    nameof(IsAlive)
);
```

### 4. コマンドの実行可能状態管理

```csharp
// コマンド作成時にCanExecuteを指定
TakeDamageCommand = new RelayCommand<int?>(
    execute: damage => OnTakeDamage(damage ?? 10),
    canExecute: _ => IsAlive  // 生存中のみ実行可能
);

// 状態が変わったらCanExecuteChangedを発行
(TakeDamageCommand as RelayCommand<int?>)?.RaiseCanExecuteChanged();
```

---

## 学習のポイント

### MVVMパターンの責務分離

```
Model (PlayerHealthModel)
└─ データとビジネスロジック
   └─ 体力の計算、ダメージ処理

ViewModel (PlayerHealthViewModel)
└─ ModelとViewの橋渡し
   └─ UIに表示するデータの整形
   └─ ユーザー操作のコマンド

View (PlayerHealthView)
└─ UI表示と更新
   └─ ViewModelのデータをUIに反映
   └─ ユーザー入力をViewModelに伝達

Service (PlayerHealthService)
└─ 共有ビジネスロジック
   └─ グローバルな状態管理
```

### データフロー

```
ユーザー操作（ボタンクリック）
    ↓
View（イベント検知）
    ↓
ViewModel（コマンド実行）
    ↓
Model（データ更新）
    ↓
ViewModel（プロパティ変更通知）
    ↓
View（UIバインディング）
    ↓
UI更新
```

---

## カスタマイズ例

### 異なるダメージタイプの実装

```csharp
// Model
public enum DamageType
{
    Physical,
    Magical,
    True
}

public int TakeDamage(int damage, DamageType type)
{
    // ダメージタイプに応じた処理
    int finalDamage = type switch
    {
        DamageType.Physical => damage - _defense,
        DamageType.Magical => damage - _magicResist,
        DamageType.True => damage,
        _ => damage
    };

    return TakeDamage(finalDamage);
}

// ViewModel
public ICommand TakePhysicalDamageCommand { get; }
public ICommand TakeMagicalDamageCommand { get; }

TakePhysicalDamageCommand = new RelayCommand<int?>(
    damage => _model.TakeDamage(damage ?? 10, DamageType.Physical)
);
```

### アニメーション連携

```csharp
// View
protected override void BindViewModel(PlayerHealthViewModel viewModel)
{
    base.BindViewModel(viewModel);

    AddBinding(viewModel.Bind(
        nameof(viewModel.CurrentHealth),
        () => viewModel.CurrentHealth,
        value =>
        {
            // 体力変化時にアニメーション再生
            if (value < _previousHealth)
            {
                PlayDamageAnimation();
            }
            _previousHealth = value;
        }
    ));
}

private void PlayDamageAnimation()
{
    // ダメージアニメーションの実装
    // 例: DOTweenでシェイク効果
}
```

### サウンド効果の追加

```csharp
// ViewModel
private void OnTakeDamage(int damage)
{
    int actualDamage = _model.TakeDamage(damage);

    if (actualDamage > 0)
    {
        // サウンドサービスを使用
        var audioService = ServiceLocator.Instance.Get<AudioService>();
        audioService.PlaySE("Damage");

        StatusMessage = $"Took {actualDamage} damage!";
    }

    NotifyHealthChanged();
}
```

---

## テスト

このサンプルはユニットテストの良い例にもなります。

```csharp
[Test]
public void TakeDamage_ReducesHealth()
{
    // Arrange
    var model = new PlayerHealthModel(100);
    var viewModel = new PlayerHealthViewModel(model);

    // Act
    viewModel.TakeDamageCommand.Execute(20);

    // Assert
    Assert.AreEqual(80, viewModel.CurrentHealth);
}

[Test]
public void TakeDamageCommand_DisabledWhenDead()
{
    // Arrange
    var model = new PlayerHealthModel(10);
    var viewModel = new PlayerHealthViewModel(model);

    // Act
    viewModel.TakeDamageCommand.Execute(20);

    // Assert
    Assert.IsFalse(viewModel.TakeDamageCommand.CanExecute(10));
}
```

---

## 次のステップ

この例を理解したら、以下の機能を実装してみましょう:

1. **スタミナシステム**: 体力と同様のシステムをスタミナで実装
2. **バフ/デバフシステム**: 一時的な効果の管理
3. **インベントリシステム**: アイテムの管理
4. **スキルシステム**: クールダウンと実行可能状態の管理

---

## トラブルシューティング

### UIが更新されない
- ViewModelのプロパティが`SetProperty`を使っているか確認
- バインディングが`AddBinding`で登録されているか確認
- ViewModelが正しく設定されているか確認

### ボタンが反応しない
- ボタンにEventSystemがあるか確認
- コマンドの`CanExecute`がtrueを返しているか確認
- ボタンの`onClick`イベントが正しく設定されているか確認

### コンソールにエラーが出る
- UI要素がすべてInspectorで設定されているか確認
- TextMeshProがプロジェクトにインポートされているか確認

---

## 参考資料

- [Core/README.md](../Core/README.md) - MVVMフレームワークの詳細
- [Docs/CODING_RULES.md](../../../Docs/CODING_RULES.md) - コーディング規則
