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

---

## サービスの登録

すべてのサービスは、ゲーム開始時にServiceLocatorに登録する必要があります。

### Bootstrap スクリプト例

```csharp
using CavalryFight.Core.Services;
using CavalryFight.Services.SceneManagement;
using CavalryFight.Services.Audio;
using CavalryFight.Services.Input;
using CavalryFight.Services.AI;
using CavalryFight.Services.GameSettings;
using CavalryFight.Services.Replay;
using UnityEngine;

[RequireComponent(typeof(ReplayServiceUpdater))]
public class GameBootstrap : MonoBehaviour
{
    private void Awake()
    {
        // サービスを登録（依存関係の順序に注意）
        ServiceLocator.Instance.Register<IInputBindingService>(new InputBindingService());
        ServiceLocator.Instance.Register<IInputService>(new InputService());
        ServiceLocator.Instance.Register<IAudioService>(new AudioService());
        ServiceLocator.Instance.Register<IGameSettingsService>(new GameSettingsService());
        ServiceLocator.Instance.Register<IBlazeAIService>(new BlazeAIService());
        ServiceLocator.Instance.Register<IReplayRecorder>(new ReplayRecorder());
        ServiceLocator.Instance.Register<IReplayPlayer>(new ReplayPlayer());
        ServiceLocator.Instance.Register<ISceneManagementService>(new SceneManagementService());

        Debug.Log("[GameBootstrap] All services registered.");
    }
}
```

### 📌 重要な注意点

1. **Persistent Scene**: Bootstrapスクリプトは、永続シーン（Startup）に配置してください

2. **DontDestroyOnLoad**: ServiceLocatorは自動的にDontDestroyOnLoadになります

3. **ReplayServiceUpdater**: ReplayRecorderまたはReplayPlayerを使用する場合、Bootstrap GameObjectに`ReplayServiceUpdater`コンポーネントを追加してください（録画・再生のUpdate処理に必要）

4. **依存関係の順序**:
   - InputBindingServiceはInputServiceより先に登録する必要があります
   - GameSettingsServiceはAudioServiceとInputServiceより後に登録する必要があります（設定適用のため）

---

## サンプルコード

完全な使用例は以下を参照してください：

- **SceneManagement**: `Examples/SceneTransition/SceneTransitionExampleViewModel.cs`
- **Audio**: `Examples/AudioUsage/AudioUsageExampleViewModel.cs`
- **Input**: `Examples/InputUsage/InputUsageExampleViewModel.cs`
- **GameSettings**: `Examples/SettingsUsage/SettingsUsageExampleViewModel.cs`
- **Replay**: `Examples/ReplayUsage/ReplayUsageExampleViewModel.cs`

---

## 命名規則

- **インターフェース**: `I{機能名}Service` (例: `IAudioService`)
- **実装クラス**: `{機能名}Service` (例: `AudioService`)
- **Namespace**: `CavalryFight.Services.{カテゴリ名}`

---

## 更新履歴

| バージョン | 日付 | 変更内容 |
|-----------|------|---------|
| 0.7.1 | 2025-12-13 | Replay サービスをReplayRecorderとReplayPlayerに分離（録画と再生を独立したサービスに） |
| 0.7.0 | 2025-12-12 | Replay サービス追加（リプレイ録画・再生システム） |
| 0.6.0 | 2025-12-12 | GameSettings サービス追加（設定管理システム） |
| 0.5.0 | 2025-12-11 | BlazeAI サービス追加（AI敵管理） |
| 0.4.0 | 2025-12-11 | InputBinding サービス追加（キーバインディングシステム） |
| 0.3.0 | 2025-12-11 | Input サービス追加 |
| 0.2.0 | 2025-12-11 | Audio サービス追加 |
| 0.1.0 | 2025-12-10 | SceneManagement サービス追加 |
