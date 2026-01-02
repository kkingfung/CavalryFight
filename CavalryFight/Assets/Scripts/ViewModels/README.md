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

## 注意事項
- ViewModelは**MonoBehaviourを継承しない**
- Nullable参照型を適切に使用（`string?` vs `string`）
- プロパティ変更通知を実装
