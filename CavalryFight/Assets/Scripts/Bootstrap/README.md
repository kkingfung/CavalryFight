# Bootstrap

## 概要
ゲーム起動時の初期化処理を担当します。ServiceLocatorへのサービス登録を行います。

## 主要ファイル
- `GameBootstrap.cs` - サービス初期化・登録
- `SceneCollectionConfig.cs` - シーンコレクション設定
- `ServiceUpdater.cs` - サービスの毎フレーム更新

## セットアップ
Startupシーンに`GameBootstrap`コンポーネントをアタッチしてください。

