# Textures

## 概要
ゲームで使用するテクスチャ画像を配置します。

## フォルダ構成（推奨）
```
Textures/
├── Characters/      # キャラクターテクスチャ
├── Environment/     # 環境テクスチャ
├── Weapons/         # 武器テクスチャ
├── UI/              # UIテクスチャ、アイコン
└── Effects/         # エフェクト用テクスチャ
```

## 命名規則
- `TEX_{カテゴリ}_{名前}_{タイプ}`
- 例: `TEX_Character_Player_Diffuse`, `TEX_Character_Player_Normal`

## テクスチャタイプ
- `_Diffuse` - 拡散マップ
- `_Normal` - 法線マップ
- `_Specular` - スペキュラマップ
- `_Emission` - 発光マップ
- `_AO` - アンビエントオクルージョン

## 注意事項
- 適切な圧縮形式を使用
- 解像度は2の累乗（256, 512, 1024等）
- ミップマップの設定
- ターゲットプラットフォームに応じた最適化
