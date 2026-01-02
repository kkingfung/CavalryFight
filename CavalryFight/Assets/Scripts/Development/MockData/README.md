# MockData

## 概要
エディタで個別シーンを直接テストするためのモックサービスとデータを提供します。

## 主要ファイル
- `DevBootstrap.cs` - 自動モックサービス初期化
- `MockSceneConfig.cs` - シーン別モック設定（ScriptableObject）
- `MockServices/` - 各サービスのモック実装

## 使い方
1. Startupシーン以外から直接シーンを開くと、DevBootstrapが自動的にモックサービスを登録します
2. `Resources/MockData/MockSceneConfig`でモックデータをカスタマイズできます
