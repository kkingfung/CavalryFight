# ViewModels

## 概要
MVVMパターンのViewModel層。ViewとModelの仲介とプレゼンテーションロジックを実装します。

## 責務
- ViewとModelの仲介
- プレゼンテーションロジック
- データバインディング
- UI状態の管理

## 命名規則
- クラス名: `{機能名}ViewModel` (例: `PlayerViewModel`, `CombatViewModel`)
- Namespace: `CavalryFight.ViewModels.{機能名}`

## 必須事項
- **`#nullable enable`を使用**
- **`INotifyPropertyChanged`の実装**（データバインディング用）

## 例
```csharp
#nullable enable

using System;
using System.ComponentModel;
using CavalryFight.Models.Player;

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

## 注意事項
- ViewModelは**MonoBehaviourを継承しない**
- 必ず`#nullable enable`を使用
- Nullable参照型を適切に使用（`string?` vs `string`）
- プロパティ変更通知を実装
