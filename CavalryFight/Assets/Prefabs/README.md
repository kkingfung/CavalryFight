# Prefabs

## 概要
再利用可能なゲームオブジェクトのプレハブを配置します。

## フォルダ構成（推奨）
```
Prefabs/
├── Characters/      # プレイヤー、敵キャラクター
├── Weapons/         # 弓、矢、近接武器
├── Mounts/          # 騎乗動物（馬等）
├── Environment/     # 環境オブジェクト
├── Effects/         # エフェクト、パーティクル
├── UI/              # UIプレハブ
└── Gameplay/        # ゲームプレイ関連
```

## 命名規則
- 説明的な名前を使用
- プレフィックスでカテゴリを示す
  - `Player_` - プレイヤー関連
  - `Enemy_` - 敵関連
  - `Weapon_` - 武器
  - `Mount_` - 騎乗動物
  - `VFX_` - ビジュアルエフェクト
  - `UI_` - UI要素

## 例
- `Player_Cavalry_Archer.prefab`
- `Weapon_LongBow.prefab`
- `Mount_Horse_Brown.prefab`
- `VFX_ArrowHit.prefab`
- `UI_HealthBar.prefab`

## 注意事項
- プレハブは再利用可能にする
- プレハブバリアントを活用
- 不要な依存関係を避ける
- パフォーマンスを考慮した設計
