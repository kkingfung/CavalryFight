# サービス (Services)

## 概要
このディレクトリには、CavalryFightプロジェクトのコアサービスが含まれています。
すべてのサービスは`IService`インターフェースを実装し、`ServiceLocator`を通じて管理されます。

## サービス一覧

| サービス名 | 説明 | ディレクトリ |
|-----------|------|------------|
| **SceneManagementService** | シーン遷移とロード管理 | `SceneManagement/` |
| **AudioService** | BGM・SE再生管理 | `Audio/` |
| **InputService** | プレイヤー入力管理 | `Input/` |
| **InputBindingService** | キーバインディング管理 | `Input/` |
| **BlazeAIService** | AI敵管理（Blaze AIラッパー） | `AI/` |
| **GameSettingsService** | ゲーム設定管理（保存/読込/適用） | `GameSettings/` |
| **ReplayRecorder** | リプレイ録画管理 | `Replay/` |
| **ReplayPlayer** | リプレイ再生管理 | `Replay/` |
| **CustomizationService** | キャラクター・騎乗動物カスタマイズ管理 | `Customization/` |
| **LobbyService** | マルチプレイヤーロビー管理（Unity Netcode + Relay） | `Lobby/` |

---

## サービスの登録

すべてのサービスは、ゲーム開始時にServiceLocatorに登録する必要があります。

### Bootstrap セットアップ

ゲームブートストラップは `Core/Bootstrap/GameBootstrap.cs` で実装されています。

**セットアップ手順:**
1. Startupシーンに空のGameObjectを作成（名前: "GameBootstrap"）
2. `GameBootstrap`コンポーネントをアタッチ
3. `ServiceUpdater`が自動的に追加されます
4. ゲームを実行すると、すべてのサービスが自動的に初期化されます

**詳細は `Core/Bootstrap/README.md` を参照してください。**

### 📌 重要な注意点

1. **Startup Scene**: GameBootstrapは、最初に読み込まれるシーン（Startup）に配置してください

2. **DontDestroyOnLoad**: GameBootstrapとServiceLocatorは自動的にDontDestroyOnLoadになります

3. **ServiceUpdater**: GameBootstrapが自動的にServiceUpdaterを必要とします（RequireComponent）。手動でアタッチ不要です

4. **ネットワークマネージャー**: NetworkLobbyManagerとNetworkMatchManagerはブートストラップ時には生成されません。各サービスが必要なタイミングで生成します

5. **依存関係の順序**: GameBootstrap内で自動的に正しい順序で登録されます

---

## サンプルコード

完全な使用例は以下を参照してください：

- **SceneManagement**: `Examples/SceneTransition/SceneTransitionExampleViewModel.cs`
- **Audio**: `Examples/AudioUsage/AudioUsageExampleViewModel.cs`
- **Input**: `Examples/InputUsage/InputUsageExampleViewModel.cs`
- **GameSettings**: `Examples/SettingsUsage/SettingsUsageExampleViewModel.cs`
- **Replay**: `Examples/ReplayUsage/ReplayUsageExampleViewModel.cs`
- **Customization**: `Examples/CustomizationUsage/CustomizationUsageExampleViewModel.cs`
- **Lobby**: `Examples/LobbyUsage/LobbyUsageExampleViewModel.cs`

---

## 命名規則

- **インターフェース**: `I{機能名}Service` (例: `IAudioService`)
- **実装クラス**: `{機能名}Service` (例: `AudioService`)
- **Namespace**: `CavalryFight.Services.{カテゴリ名}`

---

## 更新履歴

| バージョン | 日付 | 変更内容 |
|-----------|------|---------|
| 0.9.0 | 2025-12-13 | Lobby サービス追加（マルチプレイヤーロビーシステム、Unity Netcode + Relay統合、ホスト/ゲスト対応、CPU追加機能） |
| 0.8.0 | 2025-12-13 | Customization サービス追加（キャラクター・騎乗動物カスタマイズシステム、P09 & Malbers統合） |
| 0.7.1 | 2025-12-13 | Replay サービスをReplayRecorderとReplayPlayerに分離（録画と再生を独立したサービスに） |
| 0.7.0 | 2025-12-12 | Replay サービス追加（リプレイ録画・再生システム） |
| 0.6.0 | 2025-12-12 | GameSettings サービス追加（設定管理システム） |
| 0.5.0 | 2025-12-11 | BlazeAI サービス追加（AI敵管理） |
| 0.4.0 | 2025-12-11 | InputBinding サービス追加（キーバインディングシステム） |
| 0.3.0 | 2025-12-11 | Input サービス追加 |
| 0.2.0 | 2025-12-11 | Audio サービス追加 |
| 0.1.0 | 2025-12-10 | SceneManagement サービス追加 |
