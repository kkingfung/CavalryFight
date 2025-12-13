# Match Service

マッチ中のゲームプレイ、スコア管理、矢の発射を管理するサービスです。

## 概要

Match Serviceは、スコアベースの騎馬弓術ゲームプレイを実装しています。クライアント側の予測とサーバー側の検証を組み合わせて、レスポンシブで公平なゲームプレイを実現します。

### 主な機能

- **矢の発射**: クライアント側での即座の発射とサーバー側での検証
- **命中判定**: サーバー権威のレイキャスト命中判定
- **スコア管理**: 部位別のスコアリングシステム
- **リアルタイム同期**: すべてのクライアントへの命中結果の即座の通知
- **統計追跡**: 命中率、残り矢数、発射回数の追跡

## アーキテクチャ

### コンポーネント構成

```
Match Service
├── Data/
│   ├── HitLocation.cs           # 命中部位の列挙型
│   ├── ScoringConfig.cs         # スコアリング設定
│   ├── ArrowShotData.cs         # 矢の発射データ
│   ├── HitResult.cs             # 命中結果
│   └── PlayerScore.cs           # プレイヤースコア
├── Components/
│   ├── HitboxComponent.cs       # 命中部位識別コンポーネント
│   └── PlayerNetworkIdentity.cs # プレイヤー識別コンポーネント
├── IMatchService.cs             # サービスインターフェース
├── MatchService.cs              # サービス実装
└── NetworkMatchManager.cs       # ネットワークRPCマネージャー
```

### データフロー

#### 矢の発射フロー
```
1. クライアント: MatchService.FireArrow() を呼び出し
2. クライアント: ArrowShotData を作成
3. クライアント → サーバー: FireArrowServerRpc(shotData)
4. サーバー: 発射者を検証
5. サーバー: 残り矢数を確認
6. サーバー: 矢を減らす
7. サーバー: レイキャスト命中判定を実行
8. サーバー: スコアを計算
9. サーバー: PlayerScore を更新
10. サーバー → 全クライアント: NotifyHitResultClientRpc(hitResult)
11. クライアント: HitRegistered イベントを発火
12. クライアント: エフェクト・UIを更新
```

## 使用方法

### セットアップ

1. **NetworkMatchManager をシーンに配置**
   ```
   - シーンに空のGameObjectを作成
   - NetworkMatchManager コンポーネントを追加
   - NetworkObject コンポーネントを追加（自動で追加されるはず）
   ```

2. **プレイヤープレハブにコンポーネントを追加**
   ```
   プレイヤールート:
   - NetworkObject
   - PlayerNetworkIdentity ← 追加

   各ヒットボックス（子オブジェクト）:
   - Collider
   - HitboxComponent ← 追加（HitLocationを設定）
   ```

3. **MatchService を初期化**
   ```csharp
   private IMatchService _matchService;

   void Start()
   {
       _matchService = new MatchService();
       _matchService.Initialize();

       // イベントを購読
       _matchService.HitRegistered += OnHitRegistered;
       _matchService.PlayerScoreChanged += OnPlayerScoreChanged;
   }

   void Update()
   {
       _matchService?.Update();
   }
   ```

### クライアント側の使用

#### 矢を発射する
```csharp
// プレイヤーが弓を引いて発射
Vector3 origin = bowTransform.position;
Vector3 direction = bowTransform.forward;
float velocity = 50f; // m/s

_matchService.FireArrow(origin, direction, velocity);
```

#### 命中イベントを処理する
```csharp
private void OnHitRegistered(HitResult hitResult)
{
    if (!hitResult.IsValidHit) return;

    // 命中エフェクトを表示
    SpawnHitEffect(hitResult.HitPosition, hitResult.HitNormal);

    // ローカルプレイヤーが射手の場合
    if (hitResult.ShooterClientId == NetworkManager.Singleton.LocalClientId)
    {
        ShowHitMarker(hitResult.HitLocation, hitResult.ScoreAwarded);
    }

    // ローカルプレイヤーが被弾者の場合
    if (hitResult.TargetClientId == NetworkManager.Singleton.LocalClientId)
    {
        PlayHitSound();
        ShowDamageIndicator(hitResult.HitLocation);
    }
}
```

#### スコアを表示する
```csharp
private void UpdateScoreUI()
{
    ulong localClientId = NetworkManager.Singleton.LocalClientId;
    PlayerScore? score = _matchService.GetPlayerScore(localClientId);

    if (score.HasValue)
    {
        scoreText.text = $"Score: {score.Value.Score}";
        arrowsText.text = $"Arrows: {score.Value.RemainingArrows}";
        accuracyText.text = $"Accuracy: {score.Value.GetAccuracy() * 100f:F1}%";
    }
}
```

#### スコアボードを表示する
```csharp
private void ShowScoreboard()
{
    var allScores = _matchService.GetAllPlayerScores();

    // スコア順にソート
    var sortedScores = allScores.OrderByDescending(s => s.Score);

    foreach (var score in sortedScores)
    {
        // スコアボードUIに追加
        AddScoreboardEntry(
            score.PlayerName.ToString(),
            score.Score,
            score.HitCount,
            score.ShotCount,
            score.GetAccuracy()
        );
    }
}
```

### サーバー側の使用

#### マッチを開始する
```csharp
// ロビーからマッチに遷移する際に呼び出す
public void StartMatchFromLobby()
{
    if (!NetworkManager.Singleton.IsServer) return;

    // ロビーからプレイヤー情報を取得
    var playerSlots = _lobbyService.PlayerSlots;

    // ゲームモードに応じた矢の数を決定
    int arrowsPerPlayer = GetArrowCountForGameMode(currentGameMode);

    // マッチ開始
    _matchService.StartMatch(playerSlots, arrowsPerPlayer);
}
```

#### 勝者を判定してマッチを終了する
```csharp
private void CheckWinCondition()
{
    if (!NetworkManager.Singleton.IsServer) return;

    var allScores = _matchService.GetAllPlayerScores();

    // スコア目標に到達したプレイヤーを探す
    foreach (var score in allScores)
    {
        if (score.Score >= targetScore)
        {
            _matchService.EndMatch(score.ClientId);
            break;
        }
    }

    // または全員の矢が尽きた場合、最高得点者を勝者に
    if (allScores.All(s => s.RemainingArrows == 0))
    {
        var winner = allScores.OrderByDescending(s => s.Score).First();
        _matchService.EndMatch(winner.ClientId);
    }
}
```

#### カスタムスコアリングを設定する
```csharp
public void ApplyHardModeScoring()
{
    if (!NetworkManager.Singleton.IsServer) return;

    var hardModeScoring = new ScoringConfig
    {
        HeartScore = 500,   // 心臓のみ超高得点
        HeadScore = 100,
        TorsoScore = 20,
        ArmScore = 5,
        LegScore = 5,
        MountScore = 1
    };

    _matchService.UpdateScoringConfig(hardModeScoring);
}
```

## データ構造

### HitLocation (列挙型)
命中部位を表します。

```csharp
public enum HitLocation
{
    Heart = 0,   // 心臓（最高得点）
    Head = 1,    // 頭部
    Torso = 2,   // 胴体
    Arm = 3,     // 腕
    Leg = 4,     // 脚
    Mount = 5,   // 騎乗動物
    Miss = 6     // ミス
}
```

### ScoringConfig (構造体)
各部位のスコアを定義します。

```csharp
var config = ScoringConfig.CreateDefault();
// デフォルト値:
// Heart = 100, Head = 50, Torso = 30, Arm = 10, Leg = 10, Mount = 5
```

### PlayerScore (構造体)
プレイヤーのスコアと統計情報を保持します。

```csharp
public struct PlayerScore
{
    public ulong ClientId;              // クライアントID
    public FixedString64Bytes PlayerName; // プレイヤー名
    public int Score;                   // 現在のスコア
    public int RemainingArrows;         // 残り矢数
    public int HitCount;                // 命中回数
    public int ShotCount;               // 発射回数
    public int TeamIndex;               // チームインデックス

    public float GetAccuracy();         // 命中率を取得
}
```

## イベント

### MatchStarted
マッチが開始された時に発生します。
```csharp
_matchService.MatchStarted += () =>
{
    ShowMatchUI();
    ResetPlayerState();
};
```

### MatchEnded
マッチが終了した時に発生します。
```csharp
_matchService.MatchEnded += (winnerClientId) =>
{
    bool isWinner = winnerClientId == NetworkManager.Singleton.LocalClientId;
    ShowResultScreen(isWinner);
};
```

### HitRegistered
命中があった時に発生します。
```csharp
_matchService.HitRegistered += (hitResult) =>
{
    if (hitResult.IsValidHit)
    {
        SpawnHitEffect(hitResult.HitPosition, hitResult.HitNormal);
    }
};
```

### PlayerScoreChanged
プレイヤーのスコアが変更された時に発生します。
```csharp
_matchService.PlayerScoreChanged += (clientId, newScore) =>
{
    UpdateScoreboardEntry(clientId, newScore);
};
```

### ArrowFired
矢が発射された時に発生します（クライアント側の矢ビジュアル生成用）。
```csharp
_matchService.ArrowFired += (shotData) =>
{
    SpawnArrowVisual(shotData.Origin, shotData.Direction, shotData.InitialVelocity);
};
```

## ベストプラクティス

### クライアント側
1. **即座のフィードバック**: `ArrowFired` イベントで矢のビジュアルを即座に生成
2. **サーバー結果の尊重**: `HitRegistered` で受け取った権威あるサーバー結果を信頼
3. **UI更新**: `PlayerScoreChanged` で即座にUIを更新
4. **エフェクト表示**: `HitResult.HitPosition` と `HitNormal` を使って正確な位置にエフェクトを表示

### サーバー側
1. **勝利条件の確認**: `PlayerScoreChanged` イベントで勝利条件を確認
2. **不正防止**: NetworkMatchManager が自動的に発射者を検証し、矢の残数をチェック
3. **ゲームモード対応**: `StartMatch()` で適切な矢の数を設定

### パフォーマンス
1. **レイキャスト最適化**: `_hitDetectionLayerMask` でレイキャスト対象を制限
2. **ヒットボックスの配置**: 重要な部位（心臓、頭部）に適切なコライダーを配置

## トラブルシューティング

### 矢が発射できない
- マッチが開始されているか確認: `_matchService.IsMatchStarted`
- ネットワークに接続されているか確認: `NetworkManager.Singleton.IsClient`
- 残り矢数を確認: `PlayerScore.RemainingArrows > 0`

### 命中判定が機能しない
- HitboxComponent がコライダーに追加されているか確認
- PlayerNetworkIdentity がプレイヤールートに追加されているか確認
- レイヤーマスクが正しく設定されているか確認

### スコアが更新されない
- サーバーが起動しているか確認
- NetworkMatchManager がシーンに存在するか確認
- `MatchService.Update()` が毎フレーム呼ばれているか確認

## 次のステップ

1. **矢の物理演算**: クライアント側の矢の軌道シミュレーション実装
2. **ゲームモード拡張**: モード別の勝利条件と矢数の実装
3. **チーム戦**: TeamIndex を使ったチーム別スコア集計
4. **リプレイシステム**: HitResult の記録と再生

## 参照

- `Assets/Scripts/Services/Examples/MatchUsage/MatchUsageExampleViewModel.cs` - 使用例
- `Assets/Scripts/Services/Lobby/` - ロビーシステムとの連携
