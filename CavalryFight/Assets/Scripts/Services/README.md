# サービス (Services)

## 概要
このディレクトリには、CavalryFightプロジェクトのコアサービスが含まれています。
すべてのサービスは`IService`インターフェースを実装し、`ServiceLocator`を通じて管理されます。

## サービス一覧

| サービス名 | 説明 | ディレクトリ |
|-----------|------|------------|
| **SceneManagementService** | シーン遷移とロード管理 | `SceneManagement/` |
| **AudioService** | BGM・SE再生管理 | `Audio/` |

---

## サービスの登録

すべてのサービスは、ゲーム開始時にServiceLocatorに登録する必要があります。

### Bootstrap スクリプト例

```csharp
using CavalryFight.Core.Services;
using CavalryFight.Services.SceneManagement;
using CavalryFight.Services.Audio;
using UnityEngine;

public class GameBootstrap : MonoBehaviour
{
    private void Awake()
    {
        // サービスを登録
        ServiceLocator.Instance.Register<IAudioService>(new AudioService());
        ServiceLocator.Instance.Register<ISceneManagementService>(new SceneManagementService());

        Debug.Log("[GameBootstrap] All services registered.");
    }
}
```

### 📌 重要な注意点

1. **Persistent Scene**: Bootstrapスクリプトは、永続シーン（Startup）に配置してください

2. **DontDestroyOnLoad**: ServiceLocatorは自動的にDontDestroyOnLoadになります

---

## サンプルコード

完全な使用例は以下を参照してください：

- **SceneManagement**: `Examples/SceneTransition/SceneTransitionExampleViewModel.cs`

---

## 命名規則

- **インターフェース**: `I{機能名}Service` (例: `IAudioService`)
- **実装クラス**: `{機能名}Service` (例: `AudioService`)
- **Namespace**: `CavalryFight.Services.{カテゴリ名}`

---

## 更新履歴

| バージョン | 日付 | 変更内容 |
|-----------|------|---------|
| 0.2.0 | 2025-12-11 | Audio サービス追加 |
| 0.1.0 | 2025-12-10 | SceneManagement サービス追加 |
