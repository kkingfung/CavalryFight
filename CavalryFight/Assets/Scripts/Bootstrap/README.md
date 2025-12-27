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
- サービス依存関係の検証
- サービスの初期化（Initialize()呼び出し）
- 初期化失敗時のエラーハンドリング
- サービスの破棄（Dispose()呼び出し）
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
   - GameStateService (SceneManagementServiceに依存)

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

## サービス依存関係

一部のサービスは他のサービスに依存しています。GameBootstrapは起動時にこれらの依存関係を自動的に検証します。

### 現在の依存関係

現在、明示的なサービス間依存関係はありません。

**注意:** GameStateService は独立して動作し、状態変更のみを管理します。実際のシーン遷移は呼び出し側（ViewModel等）が `StateChanged` イベントを監視して実行します。

### 新しい依存関係の追加方法

新しいサービスが他のサービスに依存する場合、`GameBootstrap.BuildDependencyMap()`メソッドに追加してください：

```csharp
private Dictionary<Type, List<Type>> BuildDependencyMap()
{
    var dependencies = new Dictionary<Type, List<Type>>();

    dependencies[typeof(IGameStateService)] = new List<Type>
    {
        typeof(ISceneManagementService)
    };

    // 新しい依存関係を追加
    dependencies[typeof(IYourNewService)] = new List<Type>
    {
        typeof(IRequiredService1),
        typeof(IRequiredService2)
    };

    return dependencies;
}
```

依存関係が満たされていない場合、起動時にエラーログが出力されます。

## クリティカルサービス

一部のサービスはアプリケーションの動作に不可欠です。これらの「クリティカルサービス」が初期化に失敗した場合、アプリケーションは起動を中止します。

### 現在のクリティカルサービス

- `IInputService` - 入力処理（ゲームプレイの基本）
- `ISceneManagementService` - シーン管理（画面遷移に必須）
- `IGameStateService` - ゲーム状態管理（全体フローの制御）

### 非クリティカルサービス

その他のサービスは初期化に失敗しても、アプリケーションは起動を継続します。
ただし、失敗したサービスに依存する機能は正しく動作しない可能性があります。

例：
- `IAudioService` - 音声が再生されないだけでゲームは続行可能
- `IReplayRecorder` - リプレイ録画機能が使えないだけで本編はプレイ可能
- `IBlazeAIService` - AI対戦ができないだけでマルチプレイは可能

### クリティカルサービスの追加

新しいサービスをクリティカルとして指定するには、`GameBootstrap.CriticalServices`に追加してください：

```csharp
private static readonly HashSet<Type> CriticalServices = new HashSet<Type>
{
    typeof(IInputService),
    typeof(ISceneManagementService),
    typeof(IGameStateService),
    typeof(IYourNewCriticalService) // 追加
};
```

## エラーハンドリング

### 初期化失敗時の動作

1. **非クリティカルサービスの失敗**
   - エラーログを出力
   - 警告メッセージを表示
   - アプリケーションは起動を継続

2. **クリティカルサービスの失敗**
   - エラーログを出力
   - クリティカルエラーメッセージを表示
   - エディタ: プレイモードを停止
   - ビルド: アプリケーションを終了

### ServiceLocatorのエラーメッセージ

サービスが見つからない場合、ServiceLocatorは登録されているすべてのサービスの一覧を含む詳細なエラーメッセージを出力します：

```
Service of type IUnknownService is not registered.
Registered services:
  - IInputBindingService (instance: InputBindingService)
  - IInputService (instance: InputService)
  - IAudioService (instance: AudioService)
  ...
```

これにより、どのサービスが登録されているか、何が不足しているかが一目でわかります。

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
   │   ├── すべてのサービスをServiceLocatorに登録
   │   └── LogRegisteredServices()でサービス一覧を出力
   ├── ValidateServiceDependencies()
   │   ├── BuildDependencyMap()で依存関係マップを構築
   │   └── 各サービスの依存関係をチェック
   └── InitializeServices()
       ├── 各サービスのInitializeService<T>()を呼び出し
       ├── 失敗したサービスを記録
       ├── クリティカルサービスの失敗をチェック
       └── クリティカル失敗時はアプリケーションを終了

2. ServiceUpdater.Start()
   └── ServiceLocatorから各サービスを取得

3. ServiceUpdater.Update() (毎フレーム)
   ├── MatchService.Update()
   ├── BlazeAIService.Update()
   ├── ReplayRecorder.UpdateRecording()
   └── ReplayPlayer.UpdatePlayback()

4. GameBootstrap.OnDestroy() (アプリケーション終了時またはGameObject破棄時)
   └── DisposeServices()
      └── 各サービスのDispose()を初期化の逆順で呼び出し
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

### 依存関係検証エラー

**症状:** コンソールに "Dependency validation failed" エラーが表示される

**原因:**
- 必要なサービスが登録されていない
- サービスの登録順序が依存関係を満たしていない

**解決策:**
1. エラーメッセージで指摘されているサービスを確認
2. `RegisterServices()`メソッドで該当サービスが登録されていることを確認
3. 依存先サービスが依存元サービスより先に登録されていることを確認
4. 新しいサービスを追加した場合は`BuildDependencyMap()`に依存関係を追加

### サービス初期化失敗

**症状:** コンソールに "Failed to initialize [サービス名]" エラーが表示される

**原因:**
- サービスのInitialize()メソッド内で例外が発生
- 依存するリソースやサービスが利用できない
- 設定ファイルの読み込みエラー

**解決策:**
1. エラーメッセージとスタックトレースを確認
2. 該当サービスの依存関係を確認
3. 必要なリソースファイルが存在するか確認
4. サービスのInitialize()実装を確認

**非クリティカルサービスの場合:**
- 警告が表示されるがアプリケーションは起動を継続
- 該当機能が使えないだけで他の機能は動作

**クリティカルサービスの場合:**
- エラーメッセージが表示され、アプリケーションが終了
- 問題を解決してから再起動が必要

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
   - 新しいサービスが他のサービスに依存する場合、`BuildDependencyMap()`に追加する
   - 依存するサービスより後に登録する
   - Initialize()内で他のサービスを取得する
   - 循環依存を避ける

2. **DontDestroyOnLoadを適切に使う**
   - GameBootstrapのみがDontDestroyOnLoadを設定
   - 他のサービスは不要

3. **Dispose処理を適切に実装する**
   - すべてのサービスは IService.Dispose() を実装
   - 破棄は初期化の逆順で行う
   - イベントハンドラ、リソース、参照を適切にクリーンアップ
   - Dispose()内で例外をスローしない

4. **ネットワークマネージャーの生成タイミング**
   - 必要なときだけ生成
   - 不要になったら破棄
   - メモリとネットワークリソースを節約

5. **Update処理の最小化**
   - 本当に必要なサービスのみUpdate()を実装
   - ポーリングではなくイベント駆動を優先

6. **エラーハンドリングを適切に実装する**
   - サービスのInitialize()内で発生する可能性のある例外を処理
   - リソース読み込みエラーに対するフォールバックを用意
   - クリティカルなエラーのみを例外としてスロー
   - デバッグ情報を含む詳細なエラーメッセージを出力

## 参照

- `Core/Services/ServiceLocator.cs` - サービスロケーター
- `Core/Services/IService.cs` - サービス基底インターフェース
- `Services/README.md` - 各サービスの詳細
