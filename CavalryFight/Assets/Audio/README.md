# Audio

## 概要
サウンドエフェクト（SE）とBGMを配置します。

## フォルダ構成（推奨）
```
Audio/
├── BGM/             # バックグラウンドミュージック
├── SE/              # サウンドエフェクト
│   ├── Combat/     # 戦闘音
│   ├── UI/         # UI効果音
│   ├── Ambient/    # 環境音
│   └── Character/  # キャラクター音声
└── Mixers/          # Audio Mixer設定
```

## 命名規則
- BGM: `BGM_{シーン}_{番号}` (例: `BGM_MainMenu_01`, `BGM_Battle_01`)
- SE: `SE_{カテゴリ}_{アクション}` (例: `SE_Combat_ArrowRelease`, `SE_UI_Click`)

## オーディオフォーマット
- **BGM**: `.ogg` または `.mp3` (ストリーミング設定)
- **SE**: `.wav` (短い音、メモリロード)

## 設定ガイドライン
### BGM
- Load Type: Streaming
- Compression Format: Vorbis
- Quality: 70-100%

### SE
- Load Type: Decompress On Load（短い音）または Compressed In Memory（長い音）
- Compression Format: PCM（高品質）または ADPCM（バランス）

## 注意事項
- Audio Mixerでボリューム管理
- 3Dサウンドの適切な設定（距離減衰）
- 重要な効果音は優先度を高く
- メモリ使用量を考慮
