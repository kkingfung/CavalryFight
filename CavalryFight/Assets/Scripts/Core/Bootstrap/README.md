# Bootstrap System

ゲーム起動時のサービス初期化を管理するブートストラップシステムです。

## 概要

Bootstrapシステムは、ゲーム起動時にすべてのサービスを初期化し、ServiceLocatorに登録します。
シーン遷移時も破棄されず、ゲーム全体で利用可能なシングルトンとして機能します。

## コンポーネント

### GameBootstrap

すべてのサービスの登録と初期化を管理します。

**責務:**
- サービスのServiceLocatorへの登録
- サービスの初期化（Initialize()呼び出し）
- DontDestroyOnLoadによる永続化

**使用方法:**
1. 空のGameObjectを作成（名前: "GameBootstrap"）
2. `GameBootstrap`コンポーネントをアタッチ
3. シーンに配置（通常は最初のシーン "Startup" または "MainMenu"）

**重要:**
- GameBootstrapは自動的に`ServiceUpdater`を必要とします（RequireComponent）
- 1つのシーンに1つのみ配置してください

### ServiceUpdater

Update処理が必要なサービスを毎フレーム更新します。

**更新対象サービス:**
- `IMatchService` - マッチ状態の追跡
- `IBlazeAIService` - AI状態変更の検出
- `IReplayRecorder` - リプレイ録画の更新
- `IReplayPlayer` - リプレイ再生の更新

**使用方法:**
- GameBootstrapが自動的に必要とするため、手動でアタッチ不要
- GameBootstrapと同じGameObjectに自動追加されます

## サービス登録順序

依存関係に基づいて、以下の順序で登録されます：

```
1. Core Services (依存なし)
   - InputBindingService
   - InputService

2. Infrastructure Services
   - AudioService
   - GameSettingsService
   - SceneManagementService

3. Gameplay Services
   - BlazeAIService
   - CustomizationService
   - ReplayRecorder
   - ReplayPlayer

4. Network Services
   - LobbyService
   - MatchService
```

**注意:** NetworkLobbyManagerとNetworkMatchManagerはブートストラップ時には生成されません。
それぞれのサービスが必要なタイミング（ロビー作成時、マッチ開始時）で生成されます。

## セットアップ手順

### 1. Startupシーンの作成

1. 新しいシーンを作成: `Startup.unity`
2. Build Settingsで最初のシーンとして設定
3. GameBootstrap GameObjectを配置

### 2. GameBootstrap GameObject

```
Startup シーン
└── GameBootstrap (GameObject)
    ├── GameBootstrap (Component)
    └── ServiceUpdater (Component - 自動追加)
```

### 3. 実行

ゲームを再生すると、以下の順序で実行されます：

```
1. GameBootstrap.Awake()
   ├── DontDestroyOnLoad(gameObject)
   ├── RegisterServices()
   └── InitializeServices()

2. ServiceUpdater.Start()
   └── ServiceLocatorから各サービスを取得

3. ServiceUpdater.Update() (毎フレーム)
   ├── MatchService.Update()
   ├── BlazeAIService.Update()
   ├── ReplayRecorder.UpdateRecording()
   └── ReplayPlayer.UpdatePlayback()
```

## ネットワークマネージャーのライフサイクル

### NetworkLobbyManager

```
MainMenu
  ↓ ユーザーが"Create Lobby"または"Join Lobby"をクリック
LobbyService.CreateRoom() または JoinRoom()
  ↓
NetworkLobbyManagerを生成
  ↓
ロビー内で待機
  ↓ ホストが"Start Match"をクリック
ロビー終了、マッチへ遷移
```

### NetworkMatchManager

```
ロビーでマッチ開始
  ↓
MatchService.StartMatch() または GameFlowが呼び出し
  ↓
NetworkMatchManagerを生成
  ↓
マッチ中
  ↓ マッチ終了
NetworkMatchManagerを破棄
  ↓
結果画面またはロビーへ戻る
```

## 新しいサービスの追加方法

1. **サービスの作成**
   ```csharp
   public interface INewService : IService { }
   public class NewService : INewService
   {
       public void Initialize() { }
       public void Dispose() { }
   }
   ```

2. **GameBootstrapに登録**
   ```csharp
   private void RegisterServices()
   {
       // ... 既存のサービス ...

       ServiceLocator.Instance.Register<INewService>(new NewService());
   }

   private void InitializeServices()
   {
       // ... 既存のサービス ...

       var newService = ServiceLocator.Instance.Get<INewService>();
       newService?.Initialize();
   }
   ```

3. **Update処理が必要な場合**
   ```csharp
   // IServiceにUpdate()を追加
   public interface INewService : IService
   {
       void Update();
   }

   // ServiceUpdater.csに追加
   private INewService? _newService;

   void Start()
   {
       _newService = ServiceLocator.Instance.Get<INewService>();
   }

   void Update()
   {
       _newService?.Update();
   }
   ```

## トラブルシューティング

### サービスが見つからない

**症状:** `ServiceLocator.Instance.Get<IService>()` が null を返す

**原因:**
- GameBootstrapが実行されていない
- サービスの登録順序が間違っている

**解決策:**
1. Startupシーンが最初に読み込まれることを確認
2. GameBootstrapがシーンに存在することを確認
3. コンソールで "[GameBootstrap] All services registered." が表示されることを確認

### Updateが呼ばれない

**症状:** MatchServiceやAIServiceのUpdate処理が実行されない

**原因:**
- ServiceUpdaterがGameBootstrapと同じGameObjectにない
- ServiceUpdaterが無効化されている

**解決策:**
1. GameBootstrapに`[RequireComponent(typeof(ServiceUpdater))]`があることを確認
2. GameBootstrap GameObjectのInspectorでServiceUpdaterが有効であることを確認
3. コンソールで "[ServiceUpdater] Started." が表示されることを確認

### ネットワークマネージャーがない

**症状:** NetworkLobbyManagerやNetworkMatchManagerが見つからない

**原因:**
- これは正常な動作です
- ネットワークマネージャーはブートストラップ時には生成されません

**解決策:**
- LobbyService.CreateRoom/JoinRoom を呼ぶと NetworkLobbyManager が生成されます
- MatchService.StartMatch を呼ぶと NetworkMatchManager が生成されます

## ベストプラクティス

1. **サービスの依存関係を明確にする**
   - 依存するサービスより後に登録する
   - Initialize()内で他のサービスを取得する

2. **DontDestroyOnLoadを適切に使う**
   - GameBootstrapのみがDontDestroyOnLoadを設定
   - 他のサービスは不要

3. **ネットワークマネージャーの生成タイミング**
   - 必要なときだけ生成
   - 不要になったら破棄
   - メモリとネットワークリソースを節約

4. **Update処理の最小化**
   - 本当に必要なサービスのみUpdate()を実装
   - ポーリングではなくイベント駆動を優先

## 参照

- `Core/Services/ServiceLocator.cs` - サービスロケーター
- `Core/Services/IService.cs` - サービス基底インターフェース
- `Services/README.md` - 各サービスの詳細
