# AI Combat System

CavalryFight の AI 戦闘システムのドキュメントです。

## 概要

このシステムは BlazeAI アセットと統合して、騎馬弓兵 AI を実装しています。

## アーキテクチャ

```
IAICombatService (インターフェース)
    └── AICombatService (実装)
            └── AIPlayerController (個別AIの制御)
                    └── BlazeAI (サードパーティ)
```

## 主要コンポーネント

### 1. IAICombatService / AICombatService

AI システム全体を管理するサービス。

```csharp
// サービスの取得
var aiService = ServiceLocator.Instance.Get<IAICombatService>();

// ゲームモードと難易度で初期化
aiService.Initialize(GameMode.Arena, AIDifficulty.Normal);

// AIをスポーン
aiService.SpawnAIPlayer(position, rotation, teamIndex: 0, aiId: 1000);

// AIを有効化（マッチ開始時）
aiService.EnableAllAI();

// AIを無効化（マッチ終了時）
aiService.DisableAllAI();
```

### 2. AIPlayerController

個別の AI プレイヤーの行動を制御するコンポーネント。

**状態マシン:**
- `Idle` - 待機中
- `Patrol` - 巡回中
- `Chase` - 追跡中
- `Attack` - 攻撃中
- `Strafe` - 横移動中
- `Retreat` - 後退中
- `Dead` - 死亡

**BlazeAI との統合:**
- BlazeAI コンポーネントがある場合は BlazeAI に委譲
- ない場合は自前のステートマシンで動作

### 3. AIDifficultyConfig (ScriptableObject)

難易度ごとの AI パラメータを設定。

```
Assets/Resources/Settings/AIDifficultyConfig.asset
```

| パラメータ | Easy | Normal | Hard | Expert |
|-----------|------|--------|------|--------|
| ReactionTime | 1.5s | 1.0s | 0.5s | 0.2s |
| AimAccuracy | 30% | 50% | 75% | 95% |
| AttackInterval | 3-5s | 2-4s | 1-3s | 0.5-2s |
| VisionRange | 15m | 20m | 25m | 30m |
| MissChance | 40% | 25% | 10% | 2% |

### 4. AIGameModeBehavior (ScriptableObject)

ゲームモードごとの AI 戦術を設定。

```
Assets/Resources/Settings/AIGameModeBehavior.asset
```

**アリーナモード:**
- 高い攻撃性
- スコア重視
- ヘッドショット狙い

**スコアマッチ:**
- 矢の温存
- 確実なショット重視
- 慎重な攻撃

**チームファイト:**
- チーム連携
- 弱った敵を優先
- 集団行動

**デスマッチ:**
- 生存重視
- 慎重な立ち回り
- 低HP時は撤退

### 5. AISpawner

マッチ中の AI スポーンを管理。

```csharp
// AISpawnerを取得
var spawner = AISpawner.Instance;

// 初期化
spawner.Initialize(GameMode.Arena, AIDifficulty.Normal);

// AIをスポーン
spawner.SpawnAIPlayers(count: 3, teamIndex: -1);

// チーム別スポーン
var (team0, team1) = spawner.SpawnTeamAIPlayers(2, 2);

// マッチ開始時に有効化
spawner.EnableAllAI();

// マッチ終了時に無効化
spawner.DisableAllAI();
```

## セットアップ

### 1. プレハブの準備

以下のプレハブを作成して `Assets/Prefabs/AI/` に配置:

- `AIMount.prefab` - AI 用の馬
- `AIRider.prefab` - AI 用の騎手

AIRider プレハブに必要なコンポーネント:
- Animator
- NavMeshAgent
- AIPlayerController
- BlazeAI（オプション）

### 2. ScriptableObject の作成

1. `Assets/Resources/Settings/` フォルダを作成
2. 右クリック → Create → CavalryFight → AI → Service Config
   - `AIServiceConfig.asset` を作成
   - AI Rider Prefab: `Assets/Prefabs/AI/AIRider.prefab` を設定
   - AI Mount Prefab: `Assets/Prefabs/AI/AIMount.prefab` を設定
   - Difficulty Config: `AIDifficultyConfig.asset` を設定
   - Game Mode Behavior: `AIGameModeBehavior.asset` を設定
3. 右クリック → Create → CavalryFight → AI → Difficulty Config
4. 右クリック → Create → CavalryFight → AI → Game Mode Behavior

### 3. ファイル構成

```
Assets/
├── Prefabs/
│   └── AI/
│       ├── AIMount.prefab          ← AI用の馬
│       └── AIRider.prefab          ← AI用の騎手
└── Resources/
    └── Settings/
        ├── AIServiceConfig.asset   ← 必須（プレハブ参照を含む）
        ├── AIDifficultyConfig.asset
        └── AIGameModeBehavior.asset
```

### 4. シーンへの配置

マッチシーンに `AISpawner` コンポーネントを持つ GameObject を配置:

```
MatchScene
└── MatchManager
└── AISpawner (GameObject)
    └── AISpawner (Component)
        ├── Difficulty Config（オプション、上書き用）
        ├── Game Mode Behavior（オプション、上書き用）
        └── Spawn Points
```

## イベント

```csharp
// AIスポーン時
aiService.AISpawned += (aiId, gameObject) => { };

// AI死亡時
aiService.AIDied += (aiId, killerId) => { };

// AIスコア獲得時
aiService.AIScored += (aiId, score) => { };

// AI矢発射時
aiService.AIFiredArrow += (aiId) => { };
```

## BlazeAI との統合

BlazeAI コンポーネントが AIRider にアタッチされている場合:

1. Vision 設定が難易度に応じて自動調整
2. AttackStateBehaviour の設定が調整
3. 敵検出は BlazeAI に委譲
4. 攻撃判定は BlazeAI に委譲

BlazeAI がない場合:

1. 自前のステートマシンで動作
2. シンプルな視界チェック
3. NavMeshAgent で移動
4. 自前のチャージ攻撃システム

## 注意事項

- AI の ID は 1000 以上（プレイヤーと区別）
- チームインデックス -1 はチームなし
- マッチ終了時は必ず `DisableAllAI()` を呼ぶ
- シーン遷移時は `Cleanup()` を呼ぶ
