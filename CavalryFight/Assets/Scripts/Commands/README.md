# Commands

## 概要
UIコマンドパターンの実装。ユーザーアクションを処理するコマンドクラスを配置します。

## 責務
- UI操作のコマンド化
- アクションの実行とUndo/Redo
- 操作履歴の管理

## 命名規則
- クラス名: `{アクション名}Command` (例: `AttackCommand`, `MoveCommand`)
- Namespace: `CavalryFight.Commands.{カテゴリ名}`

## 注意事項
- コマンドパターンを使用してアクションを分離
- `CanExecute()`で実行可能性をチェック
