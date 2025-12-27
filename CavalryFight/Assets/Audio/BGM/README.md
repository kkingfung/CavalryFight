# BGM (Background Music)

## 📍 ファイル配置場所

このフォルダに BGM ファイルを配置してください。

## 🎵 ファイル命名規則（AAA Game Standards）

### 命名原則:
- ❌ **スペースを使用しない**（ファイルシステムの互換性）
- ✅ **アンダースコアで単語を区切る**
- ✅ **明確なプレフィックス**: `BGM_`（Background Music）
- ✅ **コンテキスト識別子**: シーン名やゲームモード
- ✅ **曲名を保持**: 将来的なバリエーション追加に対応

### 現在のプロジェクトBGM:

```
BGM_Menu_SilentBladesOfIchi.mp3          - メニュー系BGM（MainMenu, Settings, Customization共通）
BGM_Replay_WindOverWaitingGrounds.mp3    - リプレイ/履歴画面用BGM
BGM_Lobby_WindOverWaitingGrounds.mp3     - ロビー用BGM
BGM_Training_SilentBladesOfIchi.mp3      - トレーニング用BGM
BGM_Match_1v1_EdgeOfCrimsonDojo.mp3      - 1v1戦闘用BGM
BGM_Match_Team_EdgeOfCrimsonDojo.mp3     - チーム戦闘用BGM
```

### 将来追加する場合の例:

```
BGM_Menu_SamuraiChant.mp3                - メニュー用BGMバリエーション
BGM_Match_1v1_MidnightDuel.mp3           - 1v1戦闘用BGMバリエーション
BGM_Results_Victory.mp3                  - 勝利リザルトBGM
BGM_Results_Defeat.mp3                   - 敗北リザルトBGM
```

## ⚙️ Unity での設定

MP3 ファイルをインポートしたら、Inspector で以下を設定:

### 推奨設定:
1. **Load Type**: `Streaming`
   - 理由: BGMは長いファイルなので、メモリに全てロードせずストリーミング再生
2. **Compression Format**: `Vorbis`
   - 理由: 高品質で容量削減
3. **Quality**: `70-100%`
   - メインメニュー・結果画面: 100%（高品質）
   - 戦闘中: 70-80%（容量節約）

### 設定手順:
1. MP3 ファイルを選択
2. Inspector の **Audio Importer** セクションを確認
3. **Load Type** → `Streaming` に変更
4. **Compression Format** → `Vorbis` に変更
5. **Quality** スライダーを調整
6. **Apply** をクリック

## 🎮 使用方法

### 各シーンへのBGM割り当て:

| シーン | View コンポーネント | 推奨BGMファイル |
|-------|-------------------|---------------|
| MainMenu.unity | MainMenuView | `BGM_Menu_SilentBladesOfIchi.mp3` |
| Settings.unity | SettingsView | `BGM_Menu_SilentBladesOfIchi.mp3` |
| Customization.unity | CustomizationView | `BGM_Menu_SilentBladesOfIchi.mp3` |
| History.unity | HistoryView | `BGM_Replay_WindOverWaitingGrounds.mp3` |
| Lobby.unity | LobbyView | `BGM_Lobby_WindOverWaitingGrounds.mp3` |
| Training.unity | TrainingView | `BGM_Training_SilentBladesOfIchi.mp3` |
| Match.unity (1v1) | MatchView | `BGM_Match_1v1_EdgeOfCrimsonDojo.mp3` |
| Match.unity (Team) | MatchView | `BGM_Match_Team_EdgeOfCrimsonDojo.mp3` |

### Unity Editor での設定手順:

1. Unity Editor でシーン（例: `MainMenu.unity`）を開く
2. Hierarchy で UI GameObject（例: `MainMenuUI`）を選択
3. Inspector で View コンポーネント（例: `Main Menu View (Script)`）を確認
4. **Audio** セクションの **Bgm Clip** フィールドに BGM ファイルをドラッグ&ドロップ
5. シーンを保存（Ctrl+S）

### コードから再生する場合（自動実装済み）:

すべての View は自動的に BGM を再生します:

```csharp
// OnEnable() でBGMを再生（2秒フェードイン）
protected override void OnEnable()
{
    base.OnEnable();

    if (_bgmClip != null)
    {
        var audioService = ServiceLocator.Instance.Get<IAudioService>();
        if (audioService != null)
        {
            audioService.PlayBgm(_bgmClip, loop: true, fadeInDuration: 2f);
        }
    }
}

// OnDisable() でBGMを停止（1秒フェードアウト）
protected override void OnDisable()
{
    var audioService = ServiceLocator.Instance.Get<IAudioService>();
    if (audioService != null)
    {
        audioService.StopBgm(fadeOutDuration: 1f);
    }

    base.OnDisable();
}
```

## 💡 ベストプラクティス

### シーン遷移時のBGM:
- **同じBGMを継続**: `PlayBgm()` を呼ばないか、既に再生中かチェック
- **新しいBGMに切り替え**: `StopBgm(fadeOutDuration: 1f)` → `PlayBgm(newClip, fadeInDuration: 1f)`
- **クロスフェード**: 古いBGMをフェードアウトしながら新しいBGMをフェードイン

### ボリューム管理:
```csharp
// ユーザー設定からボリュームを設定
audioService.BgmVolume = 0.7f; // 70%

// ミュート切り替え
audioService.IsBgmMuted = !audioService.IsBgmMuted;
```

## 📝 注意事項

- ファイルサイズに注意（目安: 3-5分で 5-10MB 程度）
- ループポイントを考慮して作曲/編集
- シーン遷移時のBGM重複再生に注意
- メモリ使用量を監視（Profiler で確認）

---

作成日: 2025-12-27
