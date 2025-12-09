# UI

## 概要
ユーザーインターフェース関連のアセットを配置します。

## フォルダ構成（推奨）
```
UI/
├── Prefabs/         # UIプレハブ
│   ├── HUD/        # ゲームプレイHUD
│   ├── Menus/      # メニュー画面
│   └── Popups/     # ポップアップ、ダイアログ
├── Sprites/         # UIスプライト、アイコン
├── Fonts/           # フォント
└── Styles/          # UI Toolkit スタイルシート
```

## UI要素（予定）
### ゲームプレイHUD
- Health Bar（体力ゲージ）
- Arrow Count（矢の残数）
- Score Display（スコア表示）
- Timer（タイマー）
- Mini Map（ミニマップ）
- Crosshair（照準）

### メニュー
- Main Menu（メインメニュー）
- Game Mode Selection（ゲームモード選択）
- Customization（カスタマイゼーション）
- Settings（設定）
- Training Mode UI（トレーニングモード）

### その他
- Match Results（試合結果）
- Leaderboard（リーダーボード）
- Replay Controls（リプレイ操作）

## 命名規則
- プレハブ: `UI_{画面名}_{要素名}` (例: `UI_HUD_HealthBar`, `UI_Menu_Main`)
- スプライト: `SPR_UI_{名前}` (例: `SPR_UI_IconArrow`, `SPR_UI_ButtonBG`)

## 注意事項
- Canvas Scalerの適切な設定
- アンカーとピボットの正しい使用
- レスポンシブデザイン（複数解像度対応）
- アクセシビリティの考慮
- パフォーマンス（ドローコール削減）
