# Match Service

## 概要
マッチ中のゲームプレイ、スコア管理、矢の発射を管理するサービスです。

## 主な機能
- 矢の発射とサーバー検証
- 命中判定（サーバー権威）
- 部位別スコアリングシステム
- リアルタイム同期

## フォルダ構成
```
Match/
├── Data/
│   ├── HitLocation.cs         # 命中部位の列挙型
│   ├── ScoringConfig.cs       # スコアリング設定
│   ├── ArrowShotData.cs       # 矢の発射データ
│   ├── HitResult.cs           # 命中結果
│   └── PlayerScore.cs         # プレイヤースコア
├── Components/
│   ├── HitboxComponent.cs     # 命中部位識別
│   └── PlayerNetworkIdentity.cs
├── IMatchService.cs           # インターフェース
├── MatchService.cs            # 実装
└── NetworkMatchManager.cs     # ネットワークRPC
```

## 使用例
`Examples/MatchUsage/` フォルダを参照してください。
