# Tests

## 概要
ユニットテストと統合テストを配置します。Unity Test Frameworkを使用します。

## フォルダ構成
- **EditMode/** - エディットモードテスト（ゲーム実行不要）
- **PlayMode/** - プレイモードテスト（ゲーム実行が必要）

## テスト実行
Unity Editor → Window → General → Test Runner

## 注意事項
- すべてのビジネスロジックにテストを記述
- モックを活用して依存関係を分離
