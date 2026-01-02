# Services

## 概要
CavalryFightプロジェクトのコアサービスです。
すべてのサービスは`IService`インターフェースを実装し、`ServiceLocator`を通じて管理されます。

## サービス一覧

| サービス名 | 説明 | ディレクトリ |
|-----------|------|------------|
| SceneManagementService | シーン遷移とロード管理 | `SceneManagement/` |
| AudioService | BGM・SE再生管理 | `Audio/` |
| InputService | プレイヤー入力管理 | `Input/` |
| InputBindingService | キーバインディング管理 | `Input/` |
| GameSettingsService | ゲーム設定管理 | `GameSettings/` |
| ReplayRecorder | リプレイ録画管理 | `Replay/` |
| ReplayPlayer | リプレイ再生管理 | `Replay/` |
| CustomizationService | キャラクター・騎乗動物カスタマイズ | `Customization/` |
| LobbyService | マルチプレイヤーロビー管理 | `Lobby/` |
| MatchService | マッチプレイ管理 | `Match/` |

## 命名規則
- インターフェース: `I{機能名}Service`
- 実装クラス: `{機能名}Service`
- Namespace: `CavalryFight.Services.{カテゴリ名}`

## 使用例
`Examples/` フォルダを参照してください。

