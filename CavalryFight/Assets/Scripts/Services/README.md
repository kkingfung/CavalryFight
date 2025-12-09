# Services

## 概要
共通サービスとユーティリティクラス。複数の機能で共有されるロジックを実装します。

## 責務
- 共通機能の提供
- 外部システムとの連携
- ユーティリティメソッド
- 依存性注入（DI）対象

## サービスタイプ
- **データサービス**: セーブ/ロード、データベース操作
- **ネットワークサービス**: マルチプレイ通信
- **オーディオサービス**: サウンドエフェクト、BGM管理
- **入力サービス**: 入力システムの抽象化
- **ロギングサービス**: デバッグとログ記録

## 命名規則
- インターフェース: `I{機能名}Service` (例: `IAudioService`)
- 実装クラス: `{機能名}Service` (例: `AudioService`)
- Namespace: `CavalryFight.Services.{カテゴリ名}`

## 例
```csharp
#nullable enable

namespace CavalryFight.Services.Audio
{
    /// <summary>
    /// オーディオサービスのインターフェース
    /// </summary>
    public interface IAudioService
    {
        /// <summary>
        /// BGMを再生
        /// </summary>
        void PlayBGM(string clipName);

        /// <summary>
        /// SEを再生
        /// </summary>
        void PlaySE(string clipName, float volume = 1.0f);

        /// <summary>
        /// BGM音量を設定
        /// </summary>
        void SetBGMVolume(float volume);
    }

    /// <summary>
    /// オーディオサービスの実装
    /// </summary>
    public class AudioService : IAudioService
    {
        #region Fields
        private readonly AudioSource _bgmSource;
        private readonly AudioSource _seSource;
        #endregion

        #region Constructor
        public AudioService(AudioSource bgmSource, AudioSource seSource)
        {
            _bgmSource = bgmSource;
            _seSource = seSource;
        }
        #endregion

        #region Public Methods
        public void PlayBGM(string clipName)
        {
            // 実装
        }

        public void PlaySE(string clipName, float volume = 1.0f)
        {
            // 実装
        }

        public void SetBGMVolume(float volume)
        {
            _bgmSource.volume = volume;
        }
        #endregion
    }
}
```

## 注意事項
- インターフェースを定義してテスタビリティを向上
- シングルトンの使用は最小限に
- 依存性注入（DI）の使用を推奨
