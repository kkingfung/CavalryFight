# シーン管理 - Advanced Scene Manager統合

## 概要
CavalryFightプロジェクトでは、**Advanced Scene Manager (ASM)** v3.1.4を使用してシーン管理を行います。
MVVMアーキテクチャと統合された`SceneManagementService`を提供し、ViewModelから簡単にシーン遷移を実行できます。

---

## Advanced Scene Managerとは

Advanced Scene Managerは、Unityの標準シーン管理を拡張した強力なシーン管理システムです。

### 主要機能

#### 1. Scene（シーン）
- Unityのシーンをラップしたオブジェクト
- ドラッグ&ドロップで操作可能
- 個別にロード/アンロード可能

#### 2. SceneCollection（シーンコレクション）
- 複数のシーンをグループ化
- 一括でロード/アンロード
- プロファイルで管理

#### 3. SceneOperation（シーン操作）
- Fluent APIによる柔軟な操作
- ローディング画面の自動制御
- 非同期ロード対応
- コールバックシステム

#### 4. Loading Screen（ローディング画面）
- シーン遷移時の演出
- 進捗表示
- カスタマイズ可能

#### 5. Profile（プロファイル）
- シーンコレクションのセット
- 開発/本番環境の切り替え
- チーム開発での共有

---

## セットアップ

### 1. Scene Managerウィンドウを開く

```
File > Scene Manager...
```

### 2. プロファイルの作成

1. Scene Managerウィンドウ左下のプロファイルセレクターをクリック
2. 「Create」をクリックして新しいプロファイルを作成
3. プロファイル名を入力（例: "CavalryFight_Main"）

### 3. シーンコレクションの作成

CavalryFightで推奨するシーンコレクション構成：

```
CavalryFightプロファイル
├── Startup (persistent)      # 起動時に常駐するシーン
│   └── GameInitializer      # サービス初期化、設定読み込み
├── Main Menu                 # メインメニュー
│   ├── MainMenuUI
│   └── MainMenuBackground
├── Game Mode Selection       # ゲームモード選択
│   └── GameModeSelectionUI
├── Character Customization   # キャラクターカスタマイゼーション
│   ├── CustomizationUI
│   └── CustomizationPreview
├── Arena                     # アリーナモード
│   ├── ArenaMap
│   ├── ArenaUI
│   └── ArenaGameplay
├── Score Match              # スコアマッチモード
│   ├── ScoreMatchMap
│   ├── ScoreMatchUI
│   └── ScoreMatchGameplay
├── Training                 # トレーニングモード
│   ├── TrainingMap
│   ├── TrainingUI
│   └── TrainingGameplay
└── Match Result            # 試合結果
    └── ResultUI
```

### 4. シーンの作成とインポート

1. Unityで通常通りシーンを作成
2. Scene Managerウィンドウに通知が表示される
3. 通知をクリックしてインポート
4. シーンをコレクションにドラッグ&ドロップ

---

## MVVMとの統合

### SceneManagementServiceの使用

#### サービスの登録

```csharp
using CavalryFight.Core.Services;
using CavalryFight.Services.SceneManagement;
using UnityEngine;

public class GameInitializer : MonoBehaviour
{
    private void Awake()
    {
        // シーン管理サービスを登録
        var sceneService = new SceneManagementService();
        ServiceLocator.Instance.Register<ISceneManagementService>(sceneService);

        // 他のサービスも登録...

        // すべてのサービスを初期化
        ServiceLocator.Instance.Initialize();
    }
}
```

#### ViewModelから使用

```csharp
#nullable enable

using CavalryFight.Core.MVVM;
using CavalryFight.Core.Commands;
using CavalryFight.Core.Services;
using CavalryFight.Services.SceneManagement;
using AdvancedSceneManager.Models;
using UnityEngine;

namespace CavalryFight.ViewModels.UI
{
    /// <summary>
    /// メインメニューのViewModel
    /// </summary>
    public class MainMenuViewModel : ViewModelBase
    {
        #region Fields

        private readonly ISceneManagementService _sceneService;
        private bool _isLoading;

        #endregion

        #region Properties

        /// <summary>
        /// ロード中かどうかを取得します。
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            private set => SetProperty(ref _isLoading, value);
        }

        /// <summary>
        /// ロード進捗を取得します（0.0～1.0）
        /// </summary>
        public float LoadProgress => _sceneService.LoadProgress;

        #endregion

        #region Commands

        public ICommand StartArenaCommand { get; }
        public ICommand StartScoreMatchCommand { get; }
        public ICommand OpenCustomizationCommand { get; }
        public ICommand OpenTrainingCommand { get; }
        public ICommand QuitCommand { get; }

        #endregion

        #region Constructor

        public MainMenuViewModel()
        {
            // サービスを取得
            _sceneService = ServiceLocator.Instance.Get<ISceneManagementService>();

            // イベントを購読
            _sceneService.SceneLoadStarted += OnSceneLoadStarted;
            _sceneService.SceneLoadCompleted += OnSceneLoadCompleted;
            _sceneService.SceneLoadFailed += OnSceneLoadFailed;

            // コマンドの初期化
            StartArenaCommand = new RelayCommand(
                execute: OnStartArena,
                canExecute: () => !IsLoading
            );

            StartScoreMatchCommand = new RelayCommand(
                execute: OnStartScoreMatch,
                canExecute: () => !IsLoading
            );

            OpenCustomizationCommand = new RelayCommand(
                execute: OnOpenCustomization,
                canExecute: () => !IsLoading
            );

            OpenTrainingCommand = new RelayCommand(
                execute: OnOpenTraining,
                canExecute: () => !IsLoading
            );

            QuitCommand = new RelayCommand(OnQuit);
        }

        #endregion

        #region Private Methods

        private void OnStartArena()
        {
            // ScriptableObjectとして定義されたシーンコレクション
            var arenaCollection = Resources.Load<SceneCollection>("SceneCollections/Arena");
            if (arenaCollection != null)
            {
                _sceneService.OpenCollection(arenaCollection, openAll: true);
            }
        }

        private void OnStartScoreMatch()
        {
            var scoreMatchCollection = Resources.Load<SceneCollection>("SceneCollections/ScoreMatch");
            if (scoreMatchCollection != null)
            {
                _sceneService.OpenCollection(scoreMatchCollection, openAll: true);
            }
        }

        private void OnOpenCustomization()
        {
            var customizationCollection = Resources.Load<SceneCollection>("SceneCollections/CharacterCustomization");
            if (customizationCollection != null)
            {
                _sceneService.OpenCollection(customizationCollection);
            }
        }

        private void OnOpenTraining()
        {
            var trainingCollection = Resources.Load<SceneCollection>("SceneCollections/Training");
            if (trainingCollection != null)
            {
                _sceneService.OpenCollection(trainingCollection);
            }
        }

        private void OnQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void OnSceneLoadStarted(object? sender, SceneLoadEventArgs e)
        {
            IsLoading = true;
            Debug.Log($"[MainMenuViewModel] Scene load started: {e.SceneName}");

            // コマンドの実行可能状態を更新
            RaiseCommandsCanExecuteChanged();
        }

        private void OnSceneLoadCompleted(object? sender, SceneLoadEventArgs e)
        {
            IsLoading = false;
            Debug.Log($"[MainMenuViewModel] Scene load completed: {e.SceneName} ({e.Duration:F2}s)");

            // コマンドの実行可能状態を更新
            RaiseCommandsCanExecuteChanged();
        }

        private void OnSceneLoadFailed(object? sender, SceneLoadErrorEventArgs e)
        {
            IsLoading = false;
            Debug.LogError($"[MainMenuViewModel] Scene load failed: {e.SceneName} - {e.ErrorMessage}");

            // エラー処理...

            RaiseCommandsCanExecuteChanged();
        }

        private void RaiseCommandsCanExecuteChanged()
        {
            (StartArenaCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (StartScoreMatchCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (OpenCustomizationCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (OpenTrainingCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        #endregion

        #region Protected Methods

        protected override void OnDispose()
        {
            // イベントの購読解除
            _sceneService.SceneLoadStarted -= OnSceneLoadStarted;
            _sceneService.SceneLoadCompleted -= OnSceneLoadCompleted;
            _sceneService.SceneLoadFailed -= OnSceneLoadFailed;

            base.OnDispose();
        }

        #endregion
    }
}
```

---

## 使用パターン

### 1. 基本的なシーン遷移

```csharp
// シーンを開く
var scene = Resources.Load<Scene>("Scenes/MainMenu");
_sceneService.OpenScene(scene);

// コレクションを開く
var collection = Resources.Load<SceneCollection>("SceneCollections/Arena");
_sceneService.OpenCollection(collection);
```

### 2. 非同期でシーン遷移

```csharp
public async Task LoadGameAsync()
{
    var gameCollection = Resources.Load<SceneCollection>("SceneCollections/Game");

    // 非同期でロード（完了まで待機）
    await _sceneService.OpenCollectionAsync(gameCollection);

    // ロード完了後の処理
    Debug.Log("Game loaded!");
}
```

### 3. ローディング画面なしでシーン遷移

```csharp
// ローディング画面を表示しない
_sceneService.OpenScene(scene, useLoadingScreen: false);
```

### 4. プリロード

```csharp
// 次のシーンを事前にロード
var nextScene = Resources.Load<Scene>("Scenes/NextLevel");
_sceneService.PreloadScene(nextScene);

// ...プレイヤーが準備できたら...

// プリロードしたシーンを開く（即座に切り替わる）
_sceneService.OpenScene(nextScene);
```

### 5. すべてのシーンを閉じる

```csharp
// タイトルに戻る場合など
_sceneService.CloseAll();
```

---

## Advanced Scene Managerの高度な機能

### Fluent API

ASMのSceneOperationは、メソッドチェーンで柔軟な操作が可能です。

```csharp
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Models.Enums;
using AdvancedSceneManager.Utility;

// 高度なシーンロード設定
scene.Open()
    .With(LoadPriority.High)              // ロード優先度を高く設定
    .With(loadingScreen)                  // カスタムローディング画面
    .UnloadUnusedAssets()                 // 未使用アセットをアンロード
    .RegisterCallback<SceneOpenPhaseEvent>(OnSceneOpened, When.After);
```

### コールバックシステム

```csharp
using AdvancedSceneManager.Callbacks.Events;

// シーンオープン時のコールバック
scene.Open().RegisterCallback<SceneOpenPhaseEvent>(e =>
{
    Debug.Log("Scene opened!");

    // コルーチンで待機
    e.WaitFor(InitializeSceneAsync());
}, When.After);

private IEnumerator InitializeSceneAsync()
{
    // シーンの初期化処理
    yield return new WaitForSeconds(1f);
    Debug.Log("Scene initialized!");
}
```

### ユーザーデータの受け渡し

```csharp
// コレクションにデータを設定
var matchSettings = ScriptableObject.CreateInstance<MatchSettings>();
matchSettings.maxPlayers = 8;
matchSettings.timeLimit = 300;

collection.userData = matchSettings;
collection.Open();

// ロード先のシーンで取得
var settings = collection.userData as MatchSettings;
```

---

## CavalryFightでの実装例

### シーン構成

#### 1. Startup（常駐）
```
Startup/
├── GameInitializer          # サービス初期化
├── AudioManager            # オーディオ管理
└── InputManager            # 入力管理
```

#### 2. Main Menu
```
MainMenu/
├── MainMenuUI              # メインメニューUI
├── MainMenuBackground      # 背景
└── MainMenuCamera          # カメラ
```

#### 3. Game Modes
```
Arena/                      # アリーナモード
├── ArenaMap_01
├── ArenaUI
└── ArenaGameplay

ScoreMatch/                 # スコアマッチモード
├── ScoreMatchMap_01
├── ScoreMatchUI
└── ScoreMatchGameplay

Training/                   # トレーニングモード
├── TrainingMap
├── TrainingUI
└── TrainingGameplay
```

### ゲームフローの実装

```csharp
/// <summary>
/// ゲームフローを管理するViewModel
/// </summary>
public class GameFlowViewModel : ViewModelBase
{
    private readonly ISceneManagementService _sceneService;

    public async Task StartNewGame(GameMode mode)
    {
        // 1. マッチ設定を作成
        var matchSettings = CreateMatchSettings(mode);

        // 2. ゲームシーンをロード
        var gameCollection = GetCollectionForMode(mode);
        gameCollection.userData = matchSettings;

        await _sceneService.OpenCollectionAsync(gameCollection);

        // 3. ゲーム開始
        Debug.Log($"Game started: {mode}");
    }

    public async Task ReturnToMainMenu()
    {
        // 1. ゲームシーンをアンロード
        _sceneService.CloseAll();

        // 2. メインメニューをロード
        var mainMenuCollection = Resources.Load<SceneCollection>("SceneCollections/MainMenu");
        await _sceneService.OpenCollectionAsync(mainMenuCollection);
    }

    public async Task ShowMatchResult(MatchResult result)
    {
        // 1. リザルトシーンをアディティブでロード
        var resultCollection = Resources.Load<SceneCollection>("SceneCollections/MatchResult");
        resultCollection.userData = result;

        await _sceneService.OpenCollectionAsync(resultCollection);
    }

    private SceneCollection GetCollectionForMode(GameMode mode)
    {
        return mode switch
        {
            GameMode.Arena => Resources.Load<SceneCollection>("SceneCollections/Arena"),
            GameMode.ScoreMatch => Resources.Load<SceneCollection>("SceneCollections/ScoreMatch"),
            GameMode.Training => Resources.Load<SceneCollection>("SceneCollections/Training"),
            _ => throw new ArgumentException($"Unknown game mode: {mode}")
        };
    }
}
```

---

## ベストプラクティス

### 1. シーンコレクションの設計

✅ **良い例**: 機能ごとにコレクションを分ける
```
- Main Menu (UIのみ)
- Arena (Map + UI + Gameplay)
- Result (UI + Background)
```

❌ **悪い例**: 1つのコレクションに全部入れる
```
- Game (Menu + Map + UI + Result + Settings)
```

### 2. Startupシーンの活用

✅ **良い例**: 常駐するシステムをStartupに配置
```csharp
// Startupシーンで初期化
public class GameInitializer : MonoBehaviour
{
    private void Awake()
    {
        // サービス初期化
        InitializeServices();

        // DontDestroyOnLoadは不要（Startupが常駐）
    }
}
```

### 3. シーンコレクションの参照管理

✅ **良い例**: ScriptableObjectとして管理
```csharp
[CreateAssetMenu(fileName = "SceneCollections", menuName = "CavalryFight/Scene Collections")]
public class SceneCollectionReferences : ScriptableObject
{
    public SceneCollection mainMenu;
    public SceneCollection arena;
    public SceneCollection scoreMatch;
    public SceneCollection training;
}
```

### 4. ロード中のUI表示

```csharp
// ViewModelでロード状態を管理
public class LoadingViewModel : ViewModelBase
{
    private readonly ISceneManagementService _sceneService;

    public bool IsLoading => _sceneService.IsLoading;
    public float LoadProgress => _sceneService.LoadProgress;

    public LoadingViewModel()
    {
        _sceneService = ServiceLocator.Instance.Get<ISceneManagementService>();

        // 進捗が変わったら通知
        _sceneService.SceneLoadStarted += (s, e) =>
        {
            OnPropertyChanged(nameof(IsLoading));
            OnPropertyChanged(nameof(LoadProgress));
        };
    }
}
```

---

## トラブルシューティング

### Scene Managerウィンドウが開かない
**解決策**: `File > Scene Manager...` から開く

### シーンがインポートされない
**解決策**: Scene Managerウィンドウの通知からインポート、または手動で `Tools > Advanced Scene Manager > Import Scene`

### Start/Awakeが早すぎる
**問題**: ASMのロード完了前にStart/Awakeが呼ばれる

**解決策**: ASMのコールバックを使用
```csharp
using AdvancedSceneManager.Callbacks;

public class GameController : MonoBehaviour
{
    // Start/Awakeの代わりに使用
    [OnCollectionOpen]
    private void OnCollectionOpened()
    {
        // すべてのシーンがロードされた後に呼ばれる
        Initialize();
    }
}
```

### ローディング画面が表示されない
**解決策**: デフォルトのローディング画面をインポート
```
Window > Package Manager > Advanced Scene Manager > Samples > "Default ASM Scenes" > Import
```

---

## 参考リンク

- [Advanced Scene Manager - GitHub](https://github.com/Lazy-Solutions/AdvancedSceneManager/tree/main/docs)
- [Quick Start Guide](https://github.com/Lazy-Solutions/AdvancedSceneManager/blob/main/docs/guides/Quick%20start.md)
- [Callbacks Documentation](https://github.com/Lazy-Solutions/AdvancedSceneManager/blob/main/docs/guides/Callbacks.md)

---

## 更新履歴

| 日付 | 内容 |
|------|------|
| 2025-12-10 | 初版作成 - Advanced Scene Manager統合ドキュメント |
