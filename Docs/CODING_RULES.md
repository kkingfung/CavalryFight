# コーディング規則

## 概要
本ドキュメントは、CavalryFightプロジェクトにおけるコーディング規則を定義します。
AAAゲーム開発の品質基準に従い、保守性・拡張性・可読性の高いコードを目指します。

---

## 1. アーキテクチャパターン

### 1.1 MVVMパターンの採用
プロジェクト全体でMVVM（Model-View-ViewModel）パターンを採用します。

#### 責務の分離
- **Model**: ゲームロジック、データ構造、ビジネスルール
- **View**: UI表示、ユーザー入力の受け取り（MonoBehaviourコンポーネント）
- **ViewModel**: ViewとModelの仲介、プレゼンテーションロジック

#### ディレクトリ構造
```
Assets/
├── Scripts/
│   ├── Models/          # ゲームデータ、ロジック
│   ├── Views/           # MonoBehaviour、UI制御
│   ├── ViewModels/      # プレゼンテーションロジック
│   ├── Commands/        # UIコマンド実装
│   └── Services/        # 共通サービス（DI対象）
```

---

## 2. コーディングスタイル

### 2.1 Nullable参照型の使用
ViewModelでは必ずNullable参照型を有効にし、null安全性を確保します。

#### 設定
```csharp
#nullable enable
```

#### 例
```csharp
#nullable enable

namespace CavalryFight.ViewModels
{
    public class PlayerViewModel
    {
        // null許容プロパティは明示的に?を付ける
        public string? PlayerName { get; set; }

        // null非許容プロパティは初期化を保証
        public int PlayerHealth { get; set; } = 100;

        // null許容参照型
        private IPlayerService? _playerService;
    }
}
```

### 2.2 Regionsによる整理
スクリプトの可読性を高めるため、必ずRegionsを使用してコードブロックを整理します。

#### 推奨Region構成
```csharp
#region Fields
// プライベートフィールド、SerializeField
#endregion

#region Properties
// パブリック・プライベートプロパティ
#endregion

#region Unity Lifecycle
// Awake, Start, Update, OnDestroy等
#endregion

#region Public Methods
// 外部から呼び出されるメソッド
#endregion

#region Private Methods
// 内部実装メソッド
#endregion

#region Event Handlers
// イベント購読メソッド
#endregion
```

### 2.3 Namespaceの使用
全てのスクリプトには必ず適切なNamespaceを設定します。

#### 命名規則
```csharp
// 基本形式
namespace CavalryFight.{Layer}.{Feature}

// 例
namespace CavalryFight.Models.Player
namespace CavalryFight.ViewModels.Battle
namespace CavalryFight.Views.UI
namespace CavalryFight.Services.Audio
```

---

## 3. 命名規則

### 3.1 クラス・構造体
- **PascalCase**を使用
- 単数形を使用
- 説明的な名前を付ける

```csharp
public class PlayerController { }
public struct DamageData { }
```

### 3.2 インターフェース
- 接頭辞に`I`を付ける
- **PascalCase**を使用

```csharp
public interface IPlayerService { }
public interface IDamageable { }
```

### 3.3 メソッド
- **PascalCase**を使用
- 動詞で始める

```csharp
public void Attack() { }
public int CalculateDamage() { }
private void InitializeComponents() { }
```

### 3.4 フィールド・変数
- **プライベートフィールド**: `_camelCase`（アンダースコア接頭辞）
- **パブリックプロパティ**: `PascalCase`
- **ローカル変数**: `camelCase`

```csharp
private int _health;
private Transform _targetTransform;

public int MaxHealth { get; set; }
public string PlayerName { get; set; }

void Method()
{
    int localValue = 0;
    string userName = "Player";
}
```

### 3.5 定数・列挙型
- **定数**: `UPPER_SNAKE_CASE`
- **列挙型**: `PascalCase`
- **列挙値**: `PascalCase`

```csharp
private const int MAX_PLAYER_COUNT = 4;
private const float ATTACK_COOLDOWN = 1.5f;

public enum GameState
{
    MainMenu,
    Playing,
    Paused,
    GameOver
}
```

---

## 4. コメント規則

### 4.1 XMLドキュメントコメント（必須）
以下の要素には**必ず**XMLドキュメントコメントを記述します。

#### 必須対象
- ✅ **クラス** - すべてのクラス定義
- ✅ **構造体** - すべての構造体定義
- ✅ **インターフェース** - すべてのインターフェース定義
- ✅ **列挙型** - すべての列挙型定義
- ✅ **パブリックメソッド** - すべてのpublicメソッド
- ✅ **パブリックプロパティ** - すべてのpublicプロパティ
- ✅ **プロテクトメンバー** - すべてのprotectedメンバー
- ✅ **インターナルメンバー** - すべてのinternalメンバー

#### 推奨対象
- ⚠️ **複雑なプライベートメソッド** - ロジックが複雑な場合は記述推奨

#### XMLコメント基本構成
```csharp
/// <summary>
/// クラス/メソッドの概要説明（日本語で記述）
/// </summary>
/// <param name="paramName">パラメータの説明</param>
/// <returns>戻り値の説明</returns>
/// <exception cref="ExceptionType">例外が発生する条件</exception>
/// <remarks>補足情報や使用上の注意</remarks>
```

#### クラスのドキュメント例
```csharp
/// <summary>
/// プレイヤーキャラクターを制御するコントローラークラス
/// </summary>
/// <remarks>
/// MVVMパターンのViewに相当し、入力処理とアニメーション制御を担当します
/// </remarks>
public class PlayerController : MonoBehaviour
{
    // 実装
}
```

#### 構造体のドキュメント例
```csharp
/// <summary>
/// ダメージ情報を保持する構造体
/// </summary>
public struct DamageData
{
    /// <summary>
    /// ダメージ量
    /// </summary>
    public int Amount { get; set; }

    /// <summary>
    /// ダメージタイプ
    /// </summary>
    public DamageType Type { get; set; }
}
```

#### インターフェースのドキュメント例
```csharp
/// <summary>
/// ダメージを受けることができるオブジェクトのインターフェース
/// </summary>
public interface IDamageable
{
    /// <summary>
    /// ダメージを受ける
    /// </summary>
    /// <param name="damage">与えるダメージ量</param>
    /// <returns>ダメージ適用後の残りHP</returns>
    int TakeDamage(int damage);
}
```

#### 列挙型のドキュメント例
```csharp
/// <summary>
/// ゲームの状態を表す列挙型
/// </summary>
public enum GameState
{
    /// <summary>
    /// メインメニュー画面
    /// </summary>
    MainMenu,

    /// <summary>
    /// ゲームプレイ中
    /// </summary>
    Playing,

    /// <summary>
    /// 一時停止中
    /// </summary>
    Paused,

    /// <summary>
    /// ゲームオーバー
    /// </summary>
    GameOver
}
```

#### メソッドのドキュメント例
```csharp
/// <summary>
/// プレイヤーにダメージを与えます
/// </summary>
/// <param name="damage">与えるダメージ量</param>
/// <returns>ダメージ適用後の残りHP</returns>
/// <exception cref="ArgumentOutOfRangeException">damageが負の値の場合</exception>
public int TakeDamage(int damage)
{
    if (damage < 0)
        throw new ArgumentOutOfRangeException(nameof(damage));

    // 実装
}
```

#### プロパティのドキュメント例
```csharp
/// <summary>
/// プレイヤーの現在HP
/// </summary>
/// <remarks>
/// 0未満にはならず、MaxHealthを超えることもありません
/// </remarks>
public int Health { get; private set; }

/// <summary>
/// プレイヤーが生存しているかどうか
/// </summary>
public bool IsAlive => Health > 0;
```

### 4.2 実装コメント
- 複雑なロジックには日本語でコメントを記述
- なぜそうするのか（Why）を説明
- 何をするか（What）は自明な場合は不要

```csharp
// プレイヤーが無敵状態の場合はダメージを無効化
if (_isInvincible)
{
    return 0;
}

// NOTE: パフォーマンス最適化のため、事前にキャッシュ
_cachedTransform = transform;
```

---

## 5. パフォーマンス規則

### 5.1 Update最適化
- 不要なUpdate呼び出しを避ける
- 重い処理はコルーチンやイベント駆動に移行
- GetComponentの繰り返し呼び出しを避ける

```csharp
#region Fields
private Transform _cachedTransform;
#endregion

#region Unity Lifecycle
private void Awake()
{
    // Awakeでキャッシュ
    _cachedTransform = transform;
}

private void Update()
{
    // キャッシュを使用
    _cachedTransform.position = Vector3.zero;
}
#endregion
```

### 5.2 メモリ管理
- オブジェクトプーリングを活用
- StringBuilderを使用（文字列連結が多い場合）
- 不要なアロケーションを避ける

```csharp
// 悪い例：毎フレームアロケーション発生
void Update()
{
    Vector3 position = new Vector3(0, 0, 0);
}

// 良い例：フィールドで再利用
private Vector3 _tempPosition;

void Update()
{
    _tempPosition.Set(0, 0, 0);
}
```

---

## 6. MVVMサンプルコード

### 6.1 Model例
```csharp
#nullable enable

namespace CavalryFight.Models.Player
{
    /// <summary>
    /// プレイヤーデータモデル
    /// </summary>
    public class PlayerModel
    {
        #region Properties
        public int Health { get; set; }
        public int MaxHealth { get; private set; }
        public string PlayerName { get; set; }
        #endregion

        #region Constructor
        public PlayerModel(string playerName, int maxHealth)
        {
            PlayerName = playerName;
            MaxHealth = maxHealth;
            Health = maxHealth;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// ダメージを受ける
        /// </summary>
        public void TakeDamage(int damage)
        {
            Health = Math.Max(0, Health - damage);
        }
        #endregion
    }
}
```

### 6.2 ViewModel例
```csharp
#nullable enable

using System;
using System.ComponentModel;

namespace CavalryFight.ViewModels.Player
{
    /// <summary>
    /// プレイヤーViewModel
    /// </summary>
    public class PlayerViewModel : INotifyPropertyChanged
    {
        #region Fields
        private readonly PlayerModel _model;
        private string? _displayName;
        #endregion

        #region Properties
        /// <summary>
        /// 表示用プレイヤー名
        /// </summary>
        public string? DisplayName
        {
            get => _displayName;
            set
            {
                if (_displayName != value)
                {
                    _displayName = value;
                    OnPropertyChanged(nameof(DisplayName));
                }
            }
        }

        /// <summary>
        /// HP割合（0.0～1.0）
        /// </summary>
        public float HealthRatio => (float)_model.Health / _model.MaxHealth;
        #endregion

        #region Events
        public event PropertyChangedEventHandler? PropertyChanged;
        #endregion

        #region Constructor
        public PlayerViewModel(PlayerModel model)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _displayName = model.PlayerName;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// ダメージを適用し、UIを更新
        /// </summary>
        public void ApplyDamage(int damage)
        {
            _model.TakeDamage(damage);
            OnPropertyChanged(nameof(HealthRatio));
        }
        #endregion

        #region Private Methods
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
```

### 6.3 View例
```csharp
#nullable enable

using UnityEngine;
using UnityEngine.UI;
using CavalryFight.ViewModels.Player;

namespace CavalryFight.Views.Player
{
    /// <summary>
    /// プレイヤーUIビュー
    /// </summary>
    public class PlayerView : MonoBehaviour
    {
        #region SerializeField
        [SerializeField] private Slider? _healthSlider;
        [SerializeField] private Text? _playerNameText;
        #endregion

        #region Fields
        private PlayerViewModel? _viewModel;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            ValidateComponents();
        }

        private void OnDestroy()
        {
            UnbindViewModel();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// ViewModelをバインド
        /// </summary>
        public void BindViewModel(PlayerViewModel viewModel)
        {
            UnbindViewModel();

            _viewModel = viewModel;
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;

            UpdateView();
        }
        #endregion

        #region Private Methods
        private void ValidateComponents()
        {
            if (_healthSlider == null)
                Debug.LogError($"{nameof(_healthSlider)} が設定されていません", this);

            if (_playerNameText == null)
                Debug.LogError($"{nameof(_playerNameText)} が設定されていません", this);
        }

        private void UnbindViewModel()
        {
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
                _viewModel = null;
            }
        }

        private void UpdateView()
        {
            if (_viewModel == null) return;

            if (_healthSlider != null)
                _healthSlider.value = _viewModel.HealthRatio;

            if (_playerNameText != null)
                _playerNameText.text = _viewModel.DisplayName ?? "";
        }
        #endregion

        #region Event Handlers
        private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            UpdateView();
        }
        #endregion
    }
}
```

---

## 7. 禁止事項

### 7.1 避けるべきコード
- ❌ マジックナンバー（定数化すべき）
- ❌ 長すぎるメソッド（20行以上は分割検討）
- ❌ 深いネスト（3階層以上は早期return検討）
- ❌ グローバル変数（Singletonも最小限に）
- ❌ 不要なコメントアウトコード

### 7.2 セキュリティ
- ❌ 機密情報のハードコード
- ❌ エラーメッセージでの内部情報露出

---

## 8. 推奨ツール・拡張

### 8.1 Unity推奨設定
- Script Changes While Playing: Recompile And Continue Playing
- Asset Serialization Mode: Force Text
- C# Project Generation: Embedded packages, Local packages, Git packages

### 8.2 IDE推奨設定
- EditorConfig使用
- コードフォーマッター有効化
- Nullable参照型チェック有効化

---

## 9. レビュー基準

プルリクエスト時には以下をチェックします：

- [ ] MVVMパターンに従っているか
- [ ] Nullable参照型が適切に使用されているか
- [ ] Regionsで整理されているか
- [ ] Namespaceが正しく設定されているか
- [ ] XMLコメントが記述されているか
- [ ] パフォーマンスへの配慮があるか
- [ ] 命名規則に従っているか
- [ ] 不要なコメントアウトコードがないか

---

## 10. バージョン管理

| バージョン | 日付 | 変更内容 |
|-----------|------|---------|
| 1.0.0 | 2025-12-09 | 初版作成 |

---

## 参考資料
- [Unity C# Coding Standards](https://docs.unity3d.com/)
- [Microsoft C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
