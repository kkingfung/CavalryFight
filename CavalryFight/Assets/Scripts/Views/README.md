# Views

## 概要
MVVMパターンのView層。UI表示とユーザー入力の受け取りを担当します。

## 責務
- MonoBehaviourコンポーネントの実装
- UIの表示と更新
- ユーザー入力の受け取り
- ViewModelとのバインディング

## 命名規則
- クラス名: `{機能名}View` (例: `PlayerView`, `CombatHUDView`)
- Namespace: `CavalryFight.Views.{機能名}`

## 注意事項
- Viewは**MonoBehaviour**を継承
- ビジネスロジックは含めない（ViewModelに委譲）
- SerializeFieldで必要なUIコンポーネントを参照
