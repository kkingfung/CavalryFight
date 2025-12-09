# Animations

## 概要
アニメーションクリップとアニメーターコントローラーを配置します。

## フォルダ構成（推奨）
```
Animations/
├── Characters/      # キャラクターアニメーション
│   ├── Clips/      # アニメーションクリップ
│   └── Controllers/ # Animatorコントローラー
├── Mounts/          # 騎乗動物アニメーション
├── Weapons/         # 武器アニメーション
└── UI/              # UIアニメーション
```

## 命名規則
- クリップ: `ANIM_{キャラクター}_{アクション}` (例: `ANIM_Player_Idle`, `ANIM_Player_Shoot`)
- コントローラー: `AC_{キャラクター}` (例: `AC_Player`, `AC_Horse`)

## 基本アニメーション（プレイヤー）
- Idle（待機）
- Walk/Run（移動）
- Ride_Idle（騎乗待機）
- Ride_Walk/Run（騎乗移動）
- Aim（照準）
- Shoot（射撃）
- Reload（矢を取る）
- Hit（被ダメージ）
- Death（死亡）

## 注意事項
- アニメーションイベントの活用
- ブレンドツリーで滑らかな遷移
- ルートモーションの適切な使用
- アニメーションの最適化（キーフレーム削減）
